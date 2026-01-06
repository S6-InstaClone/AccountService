using AccountService.Dtos;
using AccountService.Messages;
using AccountService.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Tests;

/// <summary>
/// Tests for DTOs and Messages
/// </summary>
public class DtoAndMessageTests : TestBase
{
    #region CreateProfileDto Tests

    [Fact]
    public void CreateProfileDto_DefaultValues()
    {
        // Act
        var dto = new CreateProfileDto();

        // Assert
        Assert.Equal(string.Empty, dto.Username);
        Assert.Equal(string.Empty, dto.Name);
        Assert.Null(dto.Description);
    }

    [Fact]
    public void CreateProfileDto_CanSetUsername()
    {
        // Act
        var dto = new CreateProfileDto { Username = "testuser" };

        // Assert
        Assert.Equal("testuser", dto.Username);
    }

    [Fact]
    public void CreateProfileDto_CanSetName()
    {
        // Act
        var dto = new CreateProfileDto { Name = "Test User" };

        // Assert
        Assert.Equal("Test User", dto.Name);
    }

    [Fact]
    public void CreateProfileDto_CanSetDescription()
    {
        // Act
        var dto = new CreateProfileDto { Description = "A description" };

        // Assert
        Assert.Equal("A description", dto.Description);
    }

    [Fact]
    public void CreateProfileDto_CanSetAllProperties()
    {
        // Act
        var dto = new CreateProfileDto
        {
            Username = "user1",
            Name = "User One",
            Description = "Description here"
        };

        // Assert
        Assert.Equal("user1", dto.Username);
        Assert.Equal("User One", dto.Name);
        Assert.Equal("Description here", dto.Description);
    }

    #endregion

    #region UpdateProfileDto Tests

    [Fact]
    public void UpdateProfileDto_DefaultValues()
    {
        // Act
        var dto = new UpdateProfileDto();

        // Assert
        Assert.Null(dto.Name);
        Assert.Null(dto.Description);
    }

    [Fact]
    public void UpdateProfileDto_CanSetName()
    {
        // Act
        var dto = new UpdateProfileDto { Name = "New Name" };

        // Assert
        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public void UpdateProfileDto_CanSetDescription()
    {
        // Act
        var dto = new UpdateProfileDto { Description = "New Desc" };

        // Assert
        Assert.Equal("New Desc", dto.Description);
    }

    #endregion

    #region UploadProfilePictureDto Tests

    [Fact]
    public void UploadProfilePictureDto_DefaultValues()
    {
        // Act
        var dto = new UploadProfilePictureDto();

        // Assert
        Assert.Null(dto.File);
    }

    [Fact]
    public void UploadProfilePictureDto_CanSetFile()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        // Act
        var dto = new UploadProfilePictureDto { File = mockFile.Object };

        // Assert
        Assert.NotNull(dto.File);
        Assert.Equal("test.jpg", dto.File.FileName);
    }

    [Fact]
    public void UploadProfilePictureDto_FileCanBeNull()
    {
        // Act
        var dto = new UploadProfilePictureDto { File = null };

        // Assert
        Assert.Null(dto.File);
    }

    #endregion

    #region AccountDeletedEvent Tests

    [Fact]
    public void AccountDeletedEvent_DefaultValues()
    {
        // Act
        var evt = new AccountDeletedEvent();

        // Assert
        Assert.Equal(string.Empty, evt.UserId);
        Assert.Null(evt.Username);
        Assert.Null(evt.Email);
        Assert.Equal(default(DateTime), evt.DeletedAt);
        Assert.Equal(string.Empty, evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUserId()
    {
        // Act
        var evt = new AccountDeletedEvent { UserId = "user-123" };

        // Assert
        Assert.Equal("user-123", evt.UserId);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUsername()
    {
        // Act
        var evt = new AccountDeletedEvent { Username = "testuser" };

        // Assert
        Assert.Equal("testuser", evt.Username);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetEmail()
    {
        // Act
        var evt = new AccountDeletedEvent { Email = "test@example.com" };

        // Assert
        Assert.Equal("test@example.com", evt.Email);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetDeletedAt()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var evt = new AccountDeletedEvent { DeletedAt = timestamp };

        // Assert
        Assert.Equal(timestamp, evt.DeletedAt);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetReason()
    {
        // Act
        var evt = new AccountDeletedEvent { Reason = "GDPR_USER_REQUEST" };

        // Assert
        Assert.Equal("GDPR_USER_REQUEST", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var evt = new AccountDeletedEvent
        {
            UserId = "user-456",
            Username = "johndoe",
            Email = "john@example.com",
            DeletedAt = timestamp,
            Reason = "ADMIN_ACTION"
        };

        // Assert
        Assert.Equal("user-456", evt.UserId);
        Assert.Equal("johndoe", evt.Username);
        Assert.Equal("john@example.com", evt.Email);
        Assert.Equal(timestamp, evt.DeletedAt);
        Assert.Equal("ADMIN_ACTION", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_IsRecord_HasValueEquality()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt1 = new AccountDeletedEvent
        {
            UserId = "user-1",
            Username = "test",
            Email = "test@test.com",
            DeletedAt = timestamp,
            Reason = "TEST"
        };

        var evt2 = new AccountDeletedEvent
        {
            UserId = "user-1",
            Username = "test",
            Email = "test@test.com",
            DeletedAt = timestamp,
            Reason = "TEST"
        };

        // Assert
        Assert.Equal(evt1, evt2);
    }

    [Fact]
    public void AccountDeletedEvent_DifferentValues_NotEqual()
    {
        // Arrange
        var evt1 = new AccountDeletedEvent { UserId = "user-1" };
        var evt2 = new AccountDeletedEvent { UserId = "user-2" };

        // Assert
        Assert.NotEqual(evt1, evt2);
    }

    #endregion

    #region Profile Model Tests

    [Fact]
    public void Profile_DefaultConstructor()
    {
        // Act
        var profile = new Profile();

        // Assert
        Assert.Equal(0, profile.Id);
        Assert.Null(profile.Username);
        Assert.Null(profile.Name);
        Assert.Null(profile.Description);
        Assert.Null(profile.ProfilePictureLink);
        Assert.Null(profile.KeycloakUserId);
    }

    [Fact]
    public void Profile_TwoParamConstructor()
    {
        // Act
        var profile = new Profile("testuser", "Test User");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_ThreeParamConstructor()
    {
        // Act
        var profile = new Profile("testuser", "Test User", "A description");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("A description", profile.Description);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_CanSetId()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Id = 42;

        // Assert
        Assert.Equal(42, profile.Id);
    }

    [Fact]
    public void Profile_CanSetKeycloakUserId()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.KeycloakUserId = "keycloak-123";

        // Assert
        Assert.Equal("keycloak-123", profile.KeycloakUserId);
    }

    [Fact]
    public void Profile_CanSetProfilePictureLink()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.ProfilePictureLink = "https://example.com/pic.jpg";

        // Assert
        Assert.Equal("https://example.com/pic.jpg", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_DescriptionCanBeNull()
    {
        // Arrange
        var profile = new Profile("user", "User", "Desc");

        // Act
        profile.Description = null;

        // Assert
        Assert.Null(profile.Description);
    }

    #endregion
}
