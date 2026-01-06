using AccountService.Business;
using AccountService.Controllers;
using AccountService.Data;
using AccountService.Models;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for AccountController endpoints
/// </summary>
public class AccountControllerTests : IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<KeycloakService> _mockKeycloakService;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        
        // Create mock for KeycloakService
        var mockHttpClient = new Mock<HttpClient>();
        var mockKeycloakLogger = new Mock<ILogger<KeycloakService>>();
        _mockKeycloakService = new Mock<KeycloakService>(mockHttpClient.Object, mockKeycloakLogger.Object);
        
        _mockLogger = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(
            _dbContext,
            _mockPublishEndpoint.Object,
            _mockKeycloakService.Object,
            _mockLogger.Object);

        // Setup default HTTP context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Helper Methods

    private void SetAuthHeaders(string userId, string? email = null, string? username = null)
    {
        _controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        if (email != null)
            _controller.ControllerContext.HttpContext.Request.Headers["X-User-Email"] = email;
        if (username != null)
            _controller.ControllerContext.HttpContext.Request.Headers["X-User-Name"] = username;
    }

    private void ClearAuthHeaders()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Email");
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Name");
    }

    private async Task<Profile> CreateTestProfile(string keycloakUserId, string username, string name)
    {
        var profile = new Profile(username, name)
        {
            KeycloakUserId = keycloakUserId
        };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        return profile;
    }

    #endregion

    #region GetMyAccount Tests

    [Fact]
    public async Task GetMyAccount_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsBasicInfo_WhenNoProfileExists()
    {
        // Arrange
        var userId = "user-123";
        SetAuthHeaders(userId, "test@example.com", "testuser");

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Check that hasProfile is false
        var value = okResult.Value;
        var hasProfileProperty = value.GetType().GetProperty("hasProfile");
        Assert.NotNull(hasProfileProperty);
        Assert.False((bool)hasProfileProperty.GetValue(value)!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsProfileInfo_WhenProfileExists()
    {
        // Arrange
        var userId = "user-456";
        await CreateTestProfile(userId, "existinguser", "Existing User");
        SetAuthHeaders(userId, "existing@example.com", "existinguser");

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var value = okResult.Value;
        var hasProfileProperty = value.GetType().GetProperty("hasProfile");
        Assert.NotNull(hasProfileProperty);
        Assert.True((bool)hasProfileProperty.GetValue(value)!);
    }

    [Fact]
    public async Task GetMyAccount_ReturnsCorrectUserId()
    {
        // Arrange
        var userId = "specific-user-id";
        SetAuthHeaders(userId);

        // Act
        var result = await _controller.GetMyAccount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var userIdProperty = value!.GetType().GetProperty("userId");
        Assert.Equal(userId, userIdProperty!.GetValue(value));
    }

    #endregion

    #region DeleteMyAccount Tests

    [Fact]
    public async Task DeleteMyAccount_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();

        // Act
        var result = await _controller.DeleteMyAccount();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var userId = "user-to-delete";
        SetAuthHeaders(userId, "delete@example.com", "deleteuser");
        
        _mockKeycloakService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteMyAccount();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_PublishesAccountDeletedEvent()
    {
        // Arrange
        var userId = "user-to-delete";
        SetAuthHeaders(userId, "delete@example.com", "deleteuser");
        
        _mockKeycloakService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountService.Messages.AccountDeletedEvent>(e => e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_ContinuesEvenIfKeycloakFails()
    {
        // Arrange
        var userId = "user-keycloak-fail";
        SetAuthHeaders(userId);
        
        _mockKeycloakService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(false); // Keycloak deletion fails

        // Act
        var result = await _controller.DeleteMyAccount();

        // Assert - should still return OK
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsServerError_OnException()
    {
        // Arrange
        var userId = "user-exception";
        SetAuthHeaders(userId);
        
        _mockKeycloakService
            .Setup(x => x.DeleteUserAsync(userId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.DeleteMyAccount();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteMyAccount_SetsCorrectReason()
    {
        // Arrange
        var userId = "user-reason-check";
        SetAuthHeaders(userId);
        
        _mockKeycloakService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteMyAccount();

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<AccountService.Messages.AccountDeletedEvent>(e => e.Reason == "GDPR_USER_REQUEST"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
