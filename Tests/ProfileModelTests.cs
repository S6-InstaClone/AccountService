using AccountService.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests;

/// <summary>
/// Tests for the Profile model
/// </summary>
public class ProfileModelTests
{
    [Fact]
    public void Profile_DefaultConstructor_SetsDefaultValues()
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
    public void Profile_TwoParamConstructor_SetsUsernameAndName()
    {
        // Act
        var profile = new Profile("testuser", "Test User");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
        Assert.Null(profile.Description);
    }

    [Fact]
    public void Profile_ThreeParamConstructor_SetsAllFields()
    {
        // Act
        var profile = new Profile("testuser", "Test User", "A test description");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("A test description", profile.Description);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_Id_HasKeyAttribute()
    {
        // Arrange
        var property = typeof(Profile).GetProperty(nameof(Profile.Id));
        var attribute = property?.GetCustomAttributes(typeof(KeyAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Profile_Id_HasDatabaseGeneratedAttribute()
    {
        // Arrange
        var property = typeof(Profile).GetProperty(nameof(Profile.Id));
        var attribute = property?.GetCustomAttributes(typeof(DatabaseGeneratedAttribute), false)
            .FirstOrDefault() as DatabaseGeneratedAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(DatabaseGeneratedOption.Identity, attribute.DatabaseGeneratedOption);
    }

    [Fact]
    public void Profile_CanSetKeycloakUserId()
    {
        // Arrange
        var profile = new Profile();
        var keycloakId = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        profile.KeycloakUserId = keycloakId;

        // Assert
        Assert.Equal(keycloakId, profile.KeycloakUserId);
    }

    [Fact]
    public void Profile_CanSetProfilePictureLink()
    {
        // Arrange
        var profile = new Profile("user", "User");
        var newLink = "http://blob.storage/profile-pics/user.jpg";

        // Act
        profile.ProfilePictureLink = newLink;

        // Assert
        Assert.Equal(newLink, profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_CanModifyAllProperties()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Id = 1;
        profile.Username = "modified_user";
        profile.Name = "Modified User";
        profile.Description = "Modified description";
        profile.ProfilePictureLink = "http://new-link.com/pic.jpg";
        profile.KeycloakUserId = "keycloak-123";

        // Assert
        Assert.Equal(1, profile.Id);
        Assert.Equal("modified_user", profile.Username);
        Assert.Equal("Modified User", profile.Name);
        Assert.Equal("Modified description", profile.Description);
        Assert.Equal("http://new-link.com/pic.jpg", profile.ProfilePictureLink);
        Assert.Equal("keycloak-123", profile.KeycloakUserId);
    }
}
