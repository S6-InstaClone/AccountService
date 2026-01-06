using AccountService.Business;
using AccountService.Controllers;
using AccountService.Data;
using AccountService.Dtos;
using AccountService.Models;
using AccountService.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for ProfileController
/// </summary>
public class ProfileControllerTests : TestBase, IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly ProfileService _profileService;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        _mockBlobService = new Mock<IBlobService>();
        
        var loggerMock = new Mock<ILogger<ProfileService>>();
        _profileService = new ProfileService(_dbContext, loggerMock.Object);
        
        _controller = new ProfileController(_profileService, _mockBlobService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Helper Methods

    private void SetAuthHeaders(string userId, string? username = null)
    {
        _controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        if (username != null)
            _controller.ControllerContext.HttpContext.Request.Headers["X-User-Name"] = username;
    }

    private void ClearAuthHeaders()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Name");
    }

    private async Task<Profile> CreateTestProfile(string keycloakUserId, string username, string name, string? description = null)
    {
        var profile = new Profile(username, name, description)
        {
            KeycloakUserId = keycloakUserId
        };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        return profile;
    }

    #endregion

    #region GetProfiles Tests

    [Fact]
    public async Task GetProfiles_ReturnsEmptyList_WhenNoProfiles()
    {
        // Act
        var result = await _controller.GetProfiles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(okResult.Value);
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task GetProfiles_ReturnsAllProfiles()
    {
        // Arrange
        await CreateTestProfile("user-1", "user1", "User One");
        await CreateTestProfile("user-2", "user2", "User Two");
        await CreateTestProfile("user-3", "user3", "User Three");

        // Act
        var result = await _controller.GetProfiles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(okResult.Value).ToList();
        Assert.Equal(3, profiles.Count);
    }

    #endregion

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Act
        var result = await _controller.GetProfile(99999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetProfile_ReturnsProfile_WhenExists()
    {
        // Arrange
        var profile = await CreateTestProfile("user-1", "testuser", "Test User");

        // Act
        var result = await _controller.GetProfile(profile.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProfile = Assert.IsType<Profile>(okResult.Value);
        Assert.Equal("testuser", returnedProfile.Username);
        Assert.Equal("Test User", returnedProfile.Name);
    }

    [Fact]
    public async Task GetProfile_ReturnsCorrectProfileWithDescription()
    {
        // Arrange
        var profile = await CreateTestProfile("user-1", "descuser", "Desc User", "My description");

        // Act
        var result = await _controller.GetProfile(profile.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProfile = Assert.IsType<Profile>(okResult.Value);
        Assert.Equal("My description", returnedProfile.Description);
    }

    #endregion

    #region SearchProfiles Tests

    [Fact]
    public async Task SearchProfiles_ReturnsEmptyList_WhenNoMatches()
    {
        // Arrange
        await CreateTestProfile("user-1", "alice", "Alice");
        await CreateTestProfile("user-2", "bob", "Bob");

        // Act
        var result = await _controller.SearchProfiles("xyz");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(okResult.Value);
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task SearchProfiles_ReturnsMatchingProfiles()
    {
        // Arrange
        await CreateTestProfile("user-1", "alexsmith", "Alex Smith");
        await CreateTestProfile("user-2", "alexander", "Alexander");
        await CreateTestProfile("user-3", "bob", "Bob");

        // Act
        var result = await _controller.SearchProfiles("alex");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(okResult.Value).ToList();
        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public async Task SearchProfiles_IsCaseInsensitive()
    {
        // Arrange
        await CreateTestProfile("user-1", "TestUser", "Test User");

        // Act
        var result = await _controller.SearchProfiles("testuser");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(okResult.Value).ToList();
        Assert.Single(profiles);
    }

    #endregion

    #region CreateProfile Tests

    [Fact]
    public async Task CreateProfile_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var dto = new CreateProfileDto { Username = "newuser", Name = "New User" };

        // Act
        var result = await _controller.CreateProfile(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateProfile_CreatesProfile_WhenValid()
    {
        // Arrange
        SetAuthHeaders("new-keycloak-id", "newuser");
        var dto = new CreateProfileDto { Username = "newuser", Name = "New User", Description = "Hello!" };

        // Act
        var result = await _controller.CreateProfile(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var profile = Assert.IsType<Profile>(createdResult.Value);
        Assert.Equal("newuser", profile.Username);
        Assert.Equal("New User", profile.Name);
        Assert.Equal("Hello!", profile.Description);
        Assert.Equal("new-keycloak-id", profile.KeycloakUserId);
    }

    [Fact]
    public async Task CreateProfile_ReturnsBadRequest_WhenProfileAlreadyExists()
    {
        // Arrange
        await CreateTestProfile("existing-user", "existinguser", "Existing User");
        SetAuthHeaders("existing-user", "existinguser");
        var dto = new CreateProfileDto { Username = "newname", Name = "New Name" };

        // Act
        var result = await _controller.CreateProfile(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateProfile_SetsDefaultProfilePicture()
    {
        // Arrange
        SetAuthHeaders("user-new", "newuser");
        var dto = new CreateProfileDto { Username = "newuser", Name = "New User" };

        // Act
        var result = await _controller.CreateProfile(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var profile = Assert.IsType<Profile>(createdResult.Value);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var dto = new UpdateProfileDto { Name = "Updated" };

        // Act
        var result = await _controller.UpdateProfile(1, dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Arrange
        SetAuthHeaders("user-123");

        // Act
        var result = await _controller.UpdateProfile(99999, new UpdateProfileDto { Name = "Test" });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsForbidden_WhenNotOwner()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("different-user");
        var dto = new UpdateProfileDto { Name = "Hacked" };

        // Act
        var result = await _controller.UpdateProfile(profile.Id, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesName_WhenOwner()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Original Name");
        SetAuthHeaders("owner-user");
        var dto = new UpdateProfileDto { Name = "Updated Name" };

        // Act
        var result = await _controller.UpdateProfile(profile.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("Updated Name", updatedProfile!.Name);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesDescription()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Name", "Old description");
        SetAuthHeaders("owner-user");
        var dto = new UpdateProfileDto { Description = "New description" };

        // Act
        var result = await _controller.UpdateProfile(profile.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("New description", updatedProfile!.Description);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesMultipleFields()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Original", "Original desc");
        SetAuthHeaders("owner-user");
        var dto = new UpdateProfileDto { Name = "New Name", Description = "New desc" };

        // Act
        var result = await _controller.UpdateProfile(profile.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("New Name", updatedProfile!.Name);
        Assert.Equal("New desc", updatedProfile.Description);
    }

    #endregion

    #region DeleteProfile Tests

    [Fact]
    public async Task DeleteProfile_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();

        // Act
        var result = await _controller.DeleteProfile(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Arrange
        SetAuthHeaders("user-123");

        // Act
        var result = await _controller.DeleteProfile(99999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteProfile_ReturnsForbidden_WhenNotOwner()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("different-user");

        // Act
        var result = await _controller.DeleteProfile(profile.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteProfile_DeletesProfile_WhenOwner()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("owner-user");

        // Act
        var result = await _controller.DeleteProfile(profile.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var deletedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Null(deletedProfile);
    }

    [Fact]
    public async Task DeleteProfile_DeletesProfilePictureFromBlob_WhenNotDefault()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        profile.ProfilePictureLink = "https://blob.storage/user/pic.jpg";
        await _dbContext.SaveChangesAsync();
        SetAuthHeaders("owner-user");

        // Act
        var result = await _controller.DeleteProfile(profile.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockBlobService.Verify(
            x => x.DeleteProfilePictureAsync(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProfile_DoesNotDeleteBlob_WhenDefaultPicture()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        // Profile has default_pr_pic by default
        SetAuthHeaders("owner-user");

        // Act
        var result = await _controller.DeleteProfile(profile.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockBlobService.Verify(
            x => x.DeleteProfilePictureAsync(It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region UploadProfilePicture Tests

    [Fact]
    public async Task UploadProfilePicture_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var dto = new UploadProfilePictureDto();

        // Act
        var result = await _controller.UploadProfilePicture(1, dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Arrange
        SetAuthHeaders("user-123");
        var dto = new UploadProfilePictureDto();

        // Act
        var result = await _controller.UploadProfilePicture(99999, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsForbidden_WhenNotOwner()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("different-user");
        var dto = new UploadProfilePictureDto();

        // Act
        var result = await _controller.UploadProfilePicture(profile.Id, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsBadRequest_WhenNoFile()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("owner-user");
        var dto = new UploadProfilePictureDto { File = null };

        // Act
        var result = await _controller.UploadProfilePicture(profile.Id, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadProfilePicture_UploadsAndUpdatesProfile()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        SetAuthHeaders("owner-user");
        
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("newpic.jpg");
        mockFile.Setup(f => f.Length).Returns(1024);
        
        var dto = new UploadProfilePictureDto { File = mockFile.Object };
        
        _mockBlobService
            .Setup(x => x.UploadProfilePictureAsync(It.IsAny<string>(), It.IsAny<IFormFile>()))
            .ReturnsAsync("https://blob.storage/new-pic-url.jpg");

        // Act
        var result = await _controller.UploadProfilePicture(profile.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("https://blob.storage/new-pic-url.jpg", updatedProfile!.ProfilePictureLink);
    }

    [Fact]
    public async Task UploadProfilePicture_DeletesOldPicture_WhenNotDefault()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        profile.ProfilePictureLink = "https://blob.storage/old-pic.jpg";
        await _dbContext.SaveChangesAsync();
        SetAuthHeaders("owner-user");
        
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("newpic.jpg");
        mockFile.Setup(f => f.Length).Returns(1024);
        
        var dto = new UploadProfilePictureDto { File = mockFile.Object };
        
        _mockBlobService
            .Setup(x => x.UploadProfilePictureAsync(It.IsAny<string>(), It.IsAny<IFormFile>()))
            .ReturnsAsync("https://blob.storage/new-pic.jpg");

        // Act
        await _controller.UploadProfilePicture(profile.Id, dto);

        // Assert
        _mockBlobService.Verify(
            x => x.DeleteProfilePictureAsync("https://blob.storage/old-pic.jpg"),
            Times.Once);
    }

    [Fact]
    public async Task UploadProfilePicture_DoesNotDeleteOld_WhenDefault()
    {
        // Arrange
        var profile = await CreateTestProfile("owner-user", "owner", "Owner");
        // Has default_pr_pic
        SetAuthHeaders("owner-user");
        
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("newpic.jpg");
        mockFile.Setup(f => f.Length).Returns(1024);
        
        var dto = new UploadProfilePictureDto { File = mockFile.Object };
        
        _mockBlobService
            .Setup(x => x.UploadProfilePictureAsync(It.IsAny<string>(), It.IsAny<IFormFile>()))
            .ReturnsAsync("https://blob.storage/new-pic.jpg");

        // Act
        await _controller.UploadProfilePicture(profile.Id, dto);

        // Assert
        _mockBlobService.Verify(
            x => x.DeleteProfilePictureAsync(It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region GetMyProfile Tests

    [Fact]
    public async Task GetMyProfile_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsNotFound_WhenNoProfile()
    {
        // Arrange
        SetAuthHeaders("user-without-profile");

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsProfile_WhenExists()
    {
        // Arrange
        await CreateTestProfile("my-user-id", "myuser", "My User");
        SetAuthHeaders("my-user-id");

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<Profile>(okResult.Value);
        Assert.Equal("myuser", profile.Username);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
