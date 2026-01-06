using AccountService.Dtos;
using AccountService.Messages;
using AccountService.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Tests;

/// <summary>
/// Tests for Models and DTOs
/// </summary>
public class ModelAndDtoTests : TestBase
{
    #region AccountData Model Tests

    [Fact]
    public void AccountData_DefaultConstructor_InitializesProperties()
    {
        var account = new AccountData();
        Assert.Equal(0, account.Id);
        Assert.Null(account.Username);
        Assert.Null(account.Password);
        Assert.Null(account.Email);
    }

    [Fact]
    public void AccountData_CanSetId()
    {
        var account = new AccountData { Id = 123 };
        Assert.Equal(123, account.Id);
    }

    [Fact]
    public void AccountData_CanSetUsername()
    {
        var account = new AccountData { Username = "testuser" };
        Assert.Equal("testuser", account.Username);
    }

    [Fact]
    public void AccountData_CanSetPassword()
    {
        var account = new AccountData { Password = "securepassword123" };
        Assert.Equal("securepassword123", account.Password);
    }

    [Fact]
    public void AccountData_CanSetEmail()
    {
        var account = new AccountData { Email = "test@example.com" };
        Assert.Equal("test@example.com", account.Email);
    }

    [Fact]
    public void AccountData_CanSetAllProperties()
    {
        var account = new AccountData
        {
            Id = 42,
            Username = "johndoe",
            Password = "password123",
            Email = "john@example.com"
        };

        Assert.Equal(42, account.Id);
        Assert.Equal("johndoe", account.Username);
        Assert.Equal("password123", account.Password);
        Assert.Equal("john@example.com", account.Email);
    }

    #endregion

    #region Profile Model Tests

    [Fact]
    public void Profile_DefaultConstructor()
    {
        var profile = new Profile();
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
        var profile = new Profile("testuser", "Test User");
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_ThreeParamConstructor()
    {
        var profile = new Profile("testuser", "Test User", "A description");
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("A description", profile.Description);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_CanSetId()
    {
        var profile = new Profile { Id = 42 };
        Assert.Equal(42, profile.Id);
    }

    [Fact]
    public void Profile_CanSetKeycloakUserId()
    {
        var profile = new Profile { KeycloakUserId = "keycloak-123" };
        Assert.Equal("keycloak-123", profile.KeycloakUserId);
    }

    [Fact]
    public void Profile_CanSetProfilePictureLink()
    {
        var profile = new Profile { ProfilePictureLink = "https://example.com/pic.jpg" };
        Assert.Equal("https://example.com/pic.jpg", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_DescriptionCanBeNull()
    {
        var profile = new Profile("user", "User", "Desc") { Description = null };
        Assert.Null(profile.Description);
    }

    #endregion

    #region CreateProfileDto Tests

    [Fact]
    public void CreateProfileDto_Constructor_SetsAllProperties()
    {
        var dto = new CreateProfileDto("testuser", "Test User", "A description");
        Assert.Equal("testuser", dto.Username);
        Assert.Equal("Test User", dto.Name);
        Assert.Equal("A description", dto.Description);
    }

    [Fact]
    public void CreateProfileDto_Constructor_WithNullDescription()
    {
        var dto = new CreateProfileDto("user", "User", null);
        Assert.Equal("user", dto.Username);
        Assert.Equal("User", dto.Name);
        Assert.Null(dto.Description);
    }

    [Fact]
    public void CreateProfileDto_CanModifyUsername()
    {
        var dto = new CreateProfileDto("old", "Name", null) { Username = "newuser" };
        Assert.Equal("newuser", dto.Username);
    }

    [Fact]
    public void CreateProfileDto_CanModifyName()
    {
        var dto = new CreateProfileDto("user", "Old Name", null) { Name = "New Name" };
        Assert.Equal("New Name", dto.Name);
    }

    #endregion

    #region UpdateProfileNameDto Tests

    [Fact]
    public void UpdateProfileNameDto_DefaultValues()
    {
        var dto = new UpdateProfileNameDto();
        Assert.Equal(0, dto.Id);
        Assert.Null(dto.Name);
    }

    [Fact]
    public void UpdateProfileNameDto_CanSetId()
    {
        var dto = new UpdateProfileNameDto { Id = 42 };
        Assert.Equal(42, dto.Id);
    }

    [Fact]
    public void UpdateProfileNameDto_CanSetName()
    {
        var dto = new UpdateProfileNameDto { Name = "New Name" };
        Assert.Equal("New Name", dto.Name);
    }

    #endregion

    #region UpdateProfileDescDto Tests

    [Fact]
    public void UpdateProfileDescDto_DefaultValues()
    {
        var dto = new UpdateProfileDescDto();
        Assert.Equal(0, dto.Id);
        Assert.Null(dto.Description);
    }

    [Fact]
    public void UpdateProfileDescDto_CanSetId()
    {
        var dto = new UpdateProfileDescDto { Id = 42 };
        Assert.Equal(42, dto.Id);
    }

    [Fact]
    public void UpdateProfileDescDto_CanSetDescription()
    {
        var dto = new UpdateProfileDescDto { Description = "New Desc" };
        Assert.Equal("New Desc", dto.Description);
    }

    #endregion

    #region UploadProfilePictureDto Tests

    [Fact]
    public void UploadProfilePictureDto_DefaultValues()
    {
        var dto = new UploadProfilePictureDto();
        Assert.Equal(0, dto.Id);
        Assert.Null(dto.File);
    }

    [Fact]
    public void UploadProfilePictureDto_CanSetId()
    {
        var dto = new UploadProfilePictureDto { Id = 42 };
        Assert.Equal(42, dto.Id);
    }

    [Fact]
    public void UploadProfilePictureDto_CanSetFile()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        var dto = new UploadProfilePictureDto { File = mockFile.Object };
        Assert.NotNull(dto.File);
        Assert.Equal("test.jpg", dto.File.FileName);
    }

    #endregion

    #region SearchProfileDto Tests

    [Fact]
    public void SearchProfileDto_DefaultValues()
    {
        var dto = new SearchProfileDto();
        Assert.Equal(0, dto.Id);
        Assert.Null(dto.Username);
    }

    [Fact]
    public void SearchProfileDto_CanSetProperties()
    {
        var dto = new SearchProfileDto { Id = 1, Username = "testuser" };
        Assert.Equal(1, dto.Id);
        Assert.Equal("testuser", dto.Username);
    }

    #endregion

    #region AccountDeletedEvent Tests

    [Fact]
    public void AccountDeletedEvent_DefaultValues()
    {
        var evt = new AccountDeletedEvent();
        Assert.Equal(string.Empty, evt.UserId);
        Assert.Null(evt.Username);
        Assert.Null(evt.Email);
        Assert.Equal(default(DateTime), evt.DeletedAt);
        Assert.Equal("GDPR_USER_REQUEST", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUserId()
    {
        var evt = new AccountDeletedEvent { UserId = "user-123" };
        Assert.Equal("user-123", evt.UserId);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUsername()
    {
        var evt = new AccountDeletedEvent { Username = "testuser" };
        Assert.Equal("testuser", evt.Username);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetEmail()
    {
        var evt = new AccountDeletedEvent { Email = "test@example.com" };
        Assert.Equal("test@example.com", evt.Email);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetDeletedAt()
    {
        var timestamp = DateTime.UtcNow;
        var evt = new AccountDeletedEvent { DeletedAt = timestamp };
        Assert.Equal(timestamp, evt.DeletedAt);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetReason()
    {
        var evt = new AccountDeletedEvent { Reason = "ADMIN_ACTION" };
        Assert.Equal("ADMIN_ACTION", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetAllProperties()
    {
        var timestamp = DateTime.UtcNow;
        var evt = new AccountDeletedEvent
        {
            UserId = "user-456",
            Username = "johndoe",
            Email = "john@example.com",
            DeletedAt = timestamp,
            Reason = "ADMIN_ACTION"
        };

        Assert.Equal("user-456", evt.UserId);
        Assert.Equal("johndoe", evt.Username);
        Assert.Equal("john@example.com", evt.Email);
        Assert.Equal(timestamp, evt.DeletedAt);
        Assert.Equal("ADMIN_ACTION", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_IsRecord_HasValueEquality()
    {
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

        Assert.Equal(evt1, evt2);
    }

    [Fact]
    public void AccountDeletedEvent_DifferentValues_NotEqual()
    {
        var evt1 = new AccountDeletedEvent { UserId = "user-1" };
        var evt2 = new AccountDeletedEvent { UserId = "user-2" };
        Assert.NotEqual(evt1, evt2);
    }

    #endregion
}
