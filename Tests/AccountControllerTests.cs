using AccountService.Business;
using AccountService.Controllers;
using AccountService.Data;
using AccountService.Messages;
using AccountService.Models;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Tests;

/// <summary>
/// Tests for AccountController (handles account operations including GDPR deletion)
/// Uses a real KeycloakService with mocked HttpClient to avoid env var issues.
/// </summary>
public class AccountControllerTests : TestBase, IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<AccountController>> _mockLogger;

    // Static constructor ensures env vars are set FIRST
    static AccountControllerTests()
    {
        SetRequiredEnvVars();
    }

    public AccountControllerTests()
    {
        // Also set in instance constructor for safety
        SetRequiredEnvVars();
        
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<AccountController>>();
    }

    /// <summary>
    /// Creates a KeycloakService with a mocked HttpClient that returns success.
    /// </summary>
    private KeycloakService CreateKeycloakService(bool deleteSucceeds = true)
    {
        // Ensure env vars are set
        SetRequiredEnvVars();
        
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"test-token\"}")
        };
        
        var deleteResponse = new HttpResponseMessage(
            deleteSucceeds ? HttpStatusCode.NoContent : HttpStatusCode.InternalServerError);

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? tokenResponse : deleteResponse;
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var logger = new Mock<ILogger<KeycloakService>>();
        
        return new KeycloakService(httpClient, logger.Object);
    }

    private AccountController CreateController(bool keycloakDeleteSucceeds = true)
    {
        var keycloakService = CreateKeycloakService(keycloakDeleteSucceeds);
        
        var controller = new AccountController(
            _dbContext,
            _mockPublishEndpoint.Object,
            keycloakService,
            _mockLogger.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        
        return controller;
    }

    #region Helper Methods

    private void SetAuthHeaders(AccountController controller, string userId, string? email = null, string? username = null)
    {
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        if (email != null)
            controller.ControllerContext.HttpContext.Request.Headers["X-User-Email"] = email;
        if (username != null)
            controller.ControllerContext.HttpContext.Request.Headers["X-User-Name"] = username;
    }

    private async Task<Profile> CreateTestProfile(string keycloakUserId, string username, string name)
    {
        var profile = new Profile(username, name) { KeycloakUserId = keycloakUserId };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        return profile;
    }

    #endregion

    #region GetMyAccount Tests

    [Fact]
    public async Task GetMyAccount_ReturnsUnauthorized_WhenNoUserId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsUnauthorized_WhenEmptyUserId()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "";

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsOk_WithBasicInfo_WhenNoProfile()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "test@example.com", "testuser");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsHasProfileFalse_WhenNoProfile()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-no-profile", "test@test.com");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var hasProfile = value!.GetType().GetProperty("hasProfile")?.GetValue(value);
        Assert.False((bool)hasProfile!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsHasProfileTrue_WhenProfileExists()
    {
        // Arrange
        await CreateTestProfile("user-with-profile", "profileuser", "Profile User");
        var controller = CreateController();
        SetAuthHeaders(controller, "user-with-profile", "test@test.com");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var hasProfile = value!.GetType().GetProperty("hasProfile")?.GetValue(value);
        Assert.True((bool)hasProfile!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsCorrectUserId()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "specific-user-id-123");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var userId = value!.GetType().GetProperty("userId")?.GetValue(value);
        Assert.Equal("specific-user-id-123", userId);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsCorrectEmail()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "specific@email.com");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var email = value!.GetType().GetProperty("email")?.GetValue(value);
        Assert.Equal("specific@email.com", email);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsProfileInfo_WhenProfileExists()
    {
        // Arrange
        var profile = await CreateTestProfile("profile-user", "profilename", "Profile Name");
        profile.Description = "My bio";
        profile.ProfilePictureLink = "https://pic.url/img.jpg";
        await _dbContext.SaveChangesAsync();
        
        var controller = CreateController();
        SetAuthHeaders(controller, "profile-user");

        // Act
        var result = await controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var name = value!.GetType().GetProperty("name")?.GetValue(value);
        Assert.Equal("Profile Name", name);
    }

    #endregion

    #region DeleteMyAccount Tests

    [Fact]
    public async Task DeleteMyAccount_ReturnsUnauthorized_WhenNoUserId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.DeleteMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsUnauthorized_WhenEmptyUserId()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "";

        // Act
        var result = await controller.DeleteMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_PublishesAccountDeletedEvent()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-to-delete", "user@example.com");

        // Act
        await controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.UserId == "user-to-delete"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsCorrectEmail()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "specific@email.com");

        // Act
        await controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.Email == "specific@email.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsCorrectUsername()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "user@test.com", "specificusername");

        // Act
        await controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.Username == "specificusername"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsGDPRReason()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");

        // Act
        await controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.Reason == "GDPR_USER_REQUEST"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsTimestamp()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");
        var beforeDelete = DateTime.UtcNow;

        // Act
        await controller.DeleteMyAccount();

        // Assert
        var afterDelete = DateTime.UtcNow;
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => 
                    e.DeletedAt >= beforeDelete && e.DeletedAt <= afterDelete),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");

        // Act
        var result = await controller.DeleteMyAccount();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ContinuesEvenIfKeycloakFails()
    {
        // Arrange - Create controller with KeycloakService that fails
        var controller = CreateController(keycloakDeleteSucceeds: false);
        SetAuthHeaders(controller, "user-123");

        // Act
        var result = await controller.DeleteMyAccount();

        // Assert - Still publishes event and returns OK
        Assert.IsType<OkObjectResult>(result);
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<AccountDeletedEvent>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteMyAccount_DeletesProfileFromDatabase()
    {
        // Arrange
        await CreateTestProfile("user-to-delete", "deleteuser", "Delete User");
        var controller = CreateController();
        SetAuthHeaders(controller, "user-to-delete");

        // Act
        await controller.DeleteMyAccount();

        // Assert
        var deletedProfile = await _dbContext.Profile
            .FirstOrDefaultAsync(p => p.KeycloakUserId == "user-to-delete");
        Assert.Null(deletedProfile);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
