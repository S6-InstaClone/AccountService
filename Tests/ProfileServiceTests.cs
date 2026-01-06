using AccountService.Business;
using AccountService.Data;
using AccountService.Dtos;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace Tests;

/// <summary>
/// Tests for ProfileService CRUD operations
/// </summary>
public class ProfileServiceTests : IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        _profileService = new ProfileService(_dbContext);
    }

    #region CreateProfile Tests

    [Fact]
    public async Task CreateProfile_ReturnsProfileId_WhenSuccessful()
    {
        // Arrange
        var dto = new CreateProfileDto("testuser", "Test User", "A test description");

        // Act
        var result = await _profileService.CreateProfile(dto);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public async Task CreateProfile_SavesProfileToDatabase()
    {
        // Arrange
        var dto = new CreateProfileDto("newuser", "New User", "Description");

        // Act
        var profileId = await _profileService.CreateProfile(dto);

        // Assert
        var savedProfile = await _dbContext.Profile.FindAsync(profileId);
        Assert.NotNull(savedProfile);
        Assert.Equal("newuser", savedProfile.Username);
        Assert.Equal("New User", savedProfile.Name);
        Assert.Equal("Description", savedProfile.Description);
    }

    [Fact]
    public async Task CreateProfile_SetsDefaultProfilePicture()
    {
        // Arrange
        var dto = new CreateProfileDto("user", "User", null);

        // Act
        var profileId = await _profileService.CreateProfile(dto);

        // Assert
        var savedProfile = await _dbContext.Profile.FindAsync(profileId);
        Assert.NotNull(savedProfile);
        Assert.Equal("default_pr_pic", savedProfile.ProfilePictureLink);
    }

    [Fact]
    public async Task CreateProfile_AllowsNullDescription()
    {
        // Arrange
        var dto = new CreateProfileDto("user", "User", null);

        // Act
        var profileId = await _profileService.CreateProfile(dto);

        // Assert
        var savedProfile = await _dbContext.Profile.FindAsync(profileId);
        Assert.NotNull(savedProfile);
        Assert.Null(savedProfile.Description);
    }

    #endregion

    #region UpdateProfileName Tests

    [Fact]
    public async Task UpdateProfileName_ReturnsTrue_WhenProfileExists()
    {
        // Arrange
        var profile = new Profile("user", "Old Name");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileNameDto { Id = profile.Id, Name = "New Name" };

        // Act
        var result = await _profileService.UpdateProfileName(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateProfileName_UpdatesNameInDatabase()
    {
        // Arrange
        var profile = new Profile("user", "Old Name");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileNameDto { Id = profile.Id, Name = "Updated Name" };

        // Act
        await _profileService.UpdateProfileName(dto);

        // Assert
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("Updated Name", updatedProfile!.Name);
    }

    [Fact]
    public async Task UpdateProfileName_ThrowsException_WhenProfileNotFound()
    {
        // Arrange
        var dto = new UpdateProfileNameDto { Id = 999, Name = "New Name" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _profileService.UpdateProfileName(dto));
    }

    #endregion

    #region UpdateProfileDescription Tests

    [Fact]
    public async Task UpdateProfileDescription_ReturnsTrue_WhenProfileExists()
    {
        // Arrange
        var profile = new Profile("user", "Name", "Old description");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileDescDto { Id = profile.Id, Description = "New description" };

        // Act
        var result = await _profileService.UpdateProfileDescription(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateProfileDescription_UpdatesDescriptionInDatabase()
    {
        // Arrange
        var profile = new Profile("user", "Name", "Old description");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileDescDto { Id = profile.Id, Description = "Updated description" };

        // Act
        await _profileService.UpdateProfileDescription(dto);

        // Assert
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("Updated description", updatedProfile!.Description);
    }

    [Fact]
    public async Task UpdateProfileDescription_ThrowsException_WhenProfileNotFound()
    {
        // Arrange
        var dto = new UpdateProfileDescDto { Id = 999, Description = "New description" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _profileService.UpdateProfileDescription(dto));
    }

    #endregion

    #region UpdateProfilePicture Tests

    [Fact]
    public async Task UpdateProfilePicture_UpdatesUrlInDatabase()
    {
        // Arrange
        var profile = new Profile("user", "Name");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var newUrl = "http://blob.storage/new-picture.jpg";

        // Act
        await _profileService.UpdateProfilePicture(profile.Id, newUrl);

        // Assert
        var updatedProfile = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal(newUrl, updatedProfile!.ProfilePictureLink);
    }

    #endregion

    #region SearchForAProfile Tests

    [Fact]
    public void SearchForAProfile_ReturnsMatchingProfiles()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("aleksandar", "Aleksandar"),
            new Profile("alex123", "Alex"),
            new Profile("john", "John")
        );
        _dbContext.SaveChanges();

        // Act
        var results = _profileService.SearchForAProfile("alex");

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, p => Assert.Contains("alex", p.Username!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchForAProfile_ReturnsEmptyList_WhenNoMatches()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("user1", "User One"),
            new Profile("user2", "User Two")
        );
        _dbContext.SaveChanges();

        // Act
        var results = _profileService.SearchForAProfile("xyz");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void SearchForAProfile_ThrowsArgumentNullException_WhenUsernameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _profileService.SearchForAProfile(""));
    }

    [Fact]
    public void SearchForAProfile_ThrowsArgumentNullException_WhenUsernameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _profileService.SearchForAProfile(null!));
    }

    [Fact]
    public void SearchForAProfile_IsCaseInsensitive()
    {
        // Arrange
        _dbContext.Profile.Add(new Profile("TestUser", "Test"));
        _dbContext.SaveChanges();

        // Act
        var results = _profileService.SearchForAProfile("testuser");

        // Assert
        Assert.Single(results);
    }

    #endregion

    #region DeleteProfile Tests

    [Fact]
    public async Task DeleteProfile_ReturnsProfileId_WhenSuccessful()
    {
        // Arrange
        var profile = new Profile("todelete", "To Delete");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _profileService.DeleteProfile(profile.Id);

        // Assert
        Assert.Equal(profile.Id, result);
    }

    [Fact]
    public async Task DeleteProfile_RemovesProfileFromDatabase()
    {
        // Arrange
        var profile = new Profile("todelete", "To Delete");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        var profileId = profile.Id;

        // Act
        await _profileService.DeleteProfile(profileId);

        // Assert
        var deletedProfile = await _dbContext.Profile.FindAsync(profileId);
        Assert.Null(deletedProfile);
    }

    [Fact]
    public async Task DeleteProfile_ThrowsException_WhenProfileNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _profileService.DeleteProfile(999));
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
