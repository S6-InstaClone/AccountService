using AccountService.Business;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for ProfileService
/// </summary>
public class ProfileServiceTests : TestBase, IDisposable
{
    private readonly AccountRepository _dbContext;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AccountRepository(options);
        var loggerMock = new Mock<ILogger<ProfileService>>();
        _profileService = new ProfileService(_dbContext, loggerMock.Object);
    }

    #region GetAllProfilesAsync Tests

    [Fact]
    public async Task GetAllProfilesAsync_ReturnsEmptyList_WhenNoProfiles()
    {
        // Act
        var profiles = await _profileService.GetAllProfilesAsync();

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task GetAllProfilesAsync_ReturnsAllProfiles()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("user1", "User One") { KeycloakUserId = "kc-1" },
            new Profile("user2", "User Two") { KeycloakUserId = "kc-2" },
            new Profile("user3", "User Three") { KeycloakUserId = "kc-3" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var profiles = await _profileService.GetAllProfilesAsync();

        // Assert
        Assert.Equal(3, profiles.Count());
    }

    #endregion

    #region GetProfileByIdAsync Tests

    [Fact]
    public async Task GetProfileByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var profile = await _profileService.GetProfileByIdAsync(99999);

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task GetProfileByIdAsync_ReturnsProfile_WhenExists()
    {
        // Arrange
        var newProfile = new Profile("testuser", "Test User") { KeycloakUserId = "kc-123" };
        _dbContext.Profile.Add(newProfile);
        await _dbContext.SaveChangesAsync();

        // Act
        var profile = await _profileService.GetProfileByIdAsync(newProfile.Id);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("testuser", profile.Username);
    }

    #endregion

    #region GetProfileByKeycloakIdAsync Tests

    [Fact]
    public async Task GetProfileByKeycloakIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var profile = await _profileService.GetProfileByKeycloakIdAsync("nonexistent-id");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task GetProfileByKeycloakIdAsync_ReturnsProfile_WhenExists()
    {
        // Arrange
        var newProfile = new Profile("testuser", "Test User") { KeycloakUserId = "specific-kc-id" };
        _dbContext.Profile.Add(newProfile);
        await _dbContext.SaveChangesAsync();

        // Act
        var profile = await _profileService.GetProfileByKeycloakIdAsync("specific-kc-id");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("testuser", profile.Username);
    }

    #endregion

    #region SearchProfilesAsync Tests

    [Fact]
    public async Task SearchProfilesAsync_ReturnsEmptyList_WhenNoMatches()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("alice", "Alice") { KeycloakUserId = "kc-1" },
            new Profile("bob", "Bob") { KeycloakUserId = "kc-2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var profiles = await _profileService.SearchProfilesAsync("xyz");

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task SearchProfilesAsync_FindsMatchingUsername()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("alexsmith", "Alex Smith") { KeycloakUserId = "kc-1" },
            new Profile("bob", "Bob") { KeycloakUserId = "kc-2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var profiles = await _profileService.SearchProfilesAsync("alex");

        // Assert
        Assert.Single(profiles);
        Assert.Equal("alexsmith", profiles.First().Username);
    }

    [Fact]
    public async Task SearchProfilesAsync_FindsMatchingName()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("user1", "Alexander Great") { KeycloakUserId = "kc-1" },
            new Profile("user2", "Bob") { KeycloakUserId = "kc-2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var profiles = await _profileService.SearchProfilesAsync("Alexander");

        // Assert
        Assert.Single(profiles);
    }

    [Fact]
    public async Task SearchProfilesAsync_FindsMultipleMatches()
    {
        // Arrange
        _dbContext.Profile.AddRange(
            new Profile("alexsmith", "Alex Smith") { KeycloakUserId = "kc-1" },
            new Profile("alexander", "Alexander") { KeycloakUserId = "kc-2" },
            new Profile("bob", "Bob") { KeycloakUserId = "kc-3" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var profiles = await _profileService.SearchProfilesAsync("alex");

        // Assert
        Assert.Equal(2, profiles.Count());
    }

    #endregion

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_CreatesProfile()
    {
        // Arrange
        var profile = new Profile("newuser", "New User") { KeycloakUserId = "kc-new" };

        // Act
        var created = await _profileService.CreateProfileAsync(profile);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("newuser", created.Username);
    }

    [Fact]
    public async Task CreateProfileAsync_SavesToDatabase()
    {
        // Arrange
        var profile = new Profile("saveduser", "Saved User") { KeycloakUserId = "kc-saved" };

        // Act
        await _profileService.CreateProfileAsync(profile);

        // Assert
        var fromDb = await _dbContext.Profile.FirstOrDefaultAsync(p => p.Username == "saveduser");
        Assert.NotNull(fromDb);
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_UpdatesExistingProfile()
    {
        // Arrange
        var profile = new Profile("updateuser", "Original Name") { KeycloakUserId = "kc-update" };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        profile.Name = "Updated Name";
        await _profileService.UpdateProfileAsync(profile);

        // Assert
        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("Updated Name", updated!.Name);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesDescription()
    {
        // Arrange
        var profile = new Profile("user", "User") { KeycloakUserId = "kc-1" };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        profile.Description = "New description";
        await _profileService.UpdateProfileAsync(profile);

        // Assert
        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("New description", updated!.Description);
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_DeletesProfile()
    {
        // Arrange
        var profile = new Profile("deleteuser", "Delete User") { KeycloakUserId = "kc-delete" };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        var profileId = profile.Id;

        // Act
        await _profileService.DeleteProfileAsync(profile);

        // Assert
        var deleted = await _dbContext.Profile.FindAsync(profileId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteProfileAsync_OnlyDeletesSpecifiedProfile()
    {
        // Arrange
        var profile1 = new Profile("user1", "User 1") { KeycloakUserId = "kc-1" };
        var profile2 = new Profile("user2", "User 2") { KeycloakUserId = "kc-2" };
        _dbContext.Profile.AddRange(profile1, profile2);
        await _dbContext.SaveChangesAsync();

        // Act
        await _profileService.DeleteProfileAsync(profile1);

        // Assert
        var remaining = await _dbContext.Profile.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("user2", remaining[0].Username);
    }

    #endregion

    #region ProfileExistsAsync Tests

    [Fact]
    public async Task ProfileExistsAsync_ReturnsFalse_WhenNotExists()
    {
        // Act
        var exists = await _profileService.ProfileExistsAsync("nonexistent-kc-id");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ProfileExistsAsync_ReturnsTrue_WhenExists()
    {
        // Arrange
        var profile = new Profile("existinguser", "Existing") { KeycloakUserId = "existing-kc-id" };
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var exists = await _profileService.ProfileExistsAsync("existing-kc-id");

        // Assert
        Assert.True(exists);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
