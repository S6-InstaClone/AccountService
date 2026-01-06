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
/// Tests for AccountController (the actual controller in ProfileController.cs)
/// </summary>
public class AccountControllerTests : TestBase, IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<AccountController>> _mockLogger;

    static AccountControllerTests()
    {
        SetRequiredEnvVars();
    }

    public AccountControllerTests()
    {
        SetRequiredEnvVars();
        
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<AccountController>>();
    }

    private KeycloakService CreateKeycloakService(bool deleteSucceeds = true)
    {
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

    #region GetMyAccount Tests

    [Fact]
    public async Task GetMyAccount_ReturnsUnauthorized_WhenNoUserId()
    {
        var controller = CreateController();
        var result = await controller.GetMyAccount();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsUnauthorized_WhenEmptyUserId()
    {
        var controller = CreateController();
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "";
        var result = await controller.GetMyAccount();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsOk_WithBasicInfo_WhenNoProfile()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "test@example.com", "testuser");
        var result = await controller.GetMyAccount();
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsHasProfileFalse_WhenNoProfile()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-no-profile", "test@test.com");
        var result = await controller.GetMyAccount();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var hasProfile = value!.GetType().GetProperty("hasProfile")?.GetValue(value);
        Assert.False((bool)hasProfile!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsHasProfileTrue_WhenProfileExists()
    {
        await CreateTestProfile("user-with-profile", "profileuser", "Profile User");
        var controller = CreateController();
        SetAuthHeaders(controller, "user-with-profile", "test@test.com");
        var result = await controller.GetMyAccount();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var hasProfile = value!.GetType().GetProperty("hasProfile")?.GetValue(value);
        Assert.True((bool)hasProfile!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsCorrectUserId()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "specific-user-id-123");
        var result = await controller.GetMyAccount();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var userId = value!.GetType().GetProperty("userId")?.GetValue(value);
        Assert.Equal("specific-user-id-123", userId);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsCorrectEmail()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "specific@email.com");
        var result = await controller.GetMyAccount();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var email = value!.GetType().GetProperty("email")?.GetValue(value);
        Assert.Equal("specific@email.com", email);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsProfileInfo_WhenProfileExists()
    {
        var profile = await CreateTestProfile("profile-user", "profilename", "Profile Name");
        profile.Description = "My bio";
        await _dbContext.SaveChangesAsync();
        
        var controller = CreateController();
        SetAuthHeaders(controller, "profile-user");
        var result = await controller.GetMyAccount();
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
        var controller = CreateController();
        var result = await controller.DeleteMyAccount();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsUnauthorized_WhenEmptyUserId()
    {
        var controller = CreateController();
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "";
        var result = await controller.DeleteMyAccount();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_PublishesAccountDeletedEvent()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-to-delete", "user@example.com");
        await controller.DeleteMyAccount();
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.UserId == "user-to-delete"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsCorrectEmail()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123", "specific@email.com");
        await controller.DeleteMyAccount();
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.Email == "specific@email.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsGDPRReason()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");
        await controller.DeleteMyAccount();
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountDeletedEvent>(e => e.Reason == "GDPR_USER_REQUEST"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_EventContainsTimestamp()
    {
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");
        var beforeDelete = DateTime.UtcNow;
        await controller.DeleteMyAccount();
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
        var controller = CreateController();
        SetAuthHeaders(controller, "user-123");
        var result = await controller.DeleteMyAccount();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ContinuesEvenIfKeycloakFails()
    {
        var controller = CreateController(keycloakDeleteSucceeds: false);
        SetAuthHeaders(controller, "user-123");
        var result = await controller.DeleteMyAccount();
        Assert.IsType<OkObjectResult>(result);
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<AccountDeletedEvent>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
