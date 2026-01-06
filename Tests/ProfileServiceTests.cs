using AccountService.Business;
using AccountService.Data;
using AccountService.Dtos;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace Tests;

/// <summary>
/// Tests for ProfileService - matches actual implementation (no logger parameter)
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
        // ProfileService only takes AccountRepository - no logger
        _profileService = new ProfileService(_dbContext);
    }

    #region CreateProfile Tests

    [Fact]
    public async Task CreateProfile_CreatesProfile_WithValidDto()
    {
        // Arrange - use constructor with parameters
        var dto = new CreateProfileDto("newuser", "New User", "A description");

        // Act
        var profileId = await _profileService.CreateProfile(dto);

        // Assert
        Assert.True(profileId > 0);
        var profile = await _dbContext.Profile.FindAsync(profileId);
        Assert.NotNull(profile);
        Assert.Equal("newuser", profile.Username);
        Assert.Equal("New User", profile.Name);
        Assert.Equal("A description", profile.Description);
    }

    [Fact]
    public async Task CreateProfile_SetsDefaultProfilePicture()
    {
        var dto = new CreateProfileDto("testuser", "Test User", null);
        var profileId = await _profileService.CreateProfile(dto);
        var profile = await _dbContext.Profile.FindAsync(profileId);
        Assert.Equal("default_pr_pic", profile!.ProfilePictureLink);
    }

    [Fact]
    public async Task CreateProfile_WithNullDescription_Succeeds()
    {
        var dto = new CreateProfileDto("user", "User Name", null);
        var profileId = await _profileService.CreateProfile(dto);
        var profile = await _dbContext.Profile.FindAsync(profileId);
        Assert.Null(profile!.Description);
    }

    [Fact]
    public async Task CreateProfile_ReturnsCorrectId()
    {
        var dto = new CreateProfileDto("idtest", "ID Test", null);
        var profileId = await _profileService.CreateProfile(dto);
        var profile = await _dbContext.Profile.FirstOrDefaultAsync(p => p.Username == "idtest");
        Assert.Equal(profile!.Id, profileId);
    }

    #endregion

    #region UpdateProfileName Tests

    [Fact]
    public async Task UpdateProfileName_UpdatesName_WhenProfileExists()
    {
        var profile = new Profile("user", "Original Name");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileNameDto { Id = profile.Id, Name = "Updated Name" };
        var result = await _profileService.UpdateProfileName(dto);

        Assert.True(result);
        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("Updated Name", updated!.Name);
    }

    [Fact]
    public async Task UpdateProfileName_ThrowsException_WhenProfileNotFound()
    {
        var dto = new UpdateProfileNameDto { Id = 99999, Name = "New Name" };
        await Assert.ThrowsAsync<Exception>(() => _profileService.UpdateProfileName(dto));
    }

    [Fact]
    public async Task UpdateProfileName_PreservesOtherFields()
    {
        var profile = new Profile("user", "Original Name", "Original Desc");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileNameDto { Id = profile.Id, Name = "New Name" };
        await _profileService.UpdateProfileName(dto);

        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("user", updated!.Username);
        Assert.Equal("Original Desc", updated.Description);
    }

    #endregion

    #region UpdateProfileDescription Tests

    [Fact]
    public async Task UpdateProfileDescription_UpdatesDescription_WhenProfileExists()
    {
        var profile = new Profile("user", "User");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileDescDto { Id = profile.Id, Description = "New description" };
        var result = await _profileService.UpdateProfileDescription(dto);

        Assert.True(result);
        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("New description", updated!.Description);
    }

    [Fact]
    public async Task UpdateProfileDescription_ThrowsException_WhenProfileNotFound()
    {
        var dto = new UpdateProfileDescDto { Id = 99999, Description = "New desc" };
        await Assert.ThrowsAsync<Exception>(() => _profileService.UpdateProfileDescription(dto));
    }

    [Fact]
    public async Task UpdateProfileDescription_PreservesOtherFields()
    {
        var profile = new Profile("user", "User Name");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateProfileDescDto { Id = profile.Id, Description = "New Desc" };
        await _profileService.UpdateProfileDescription(dto);

        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("user", updated!.Username);
        Assert.Equal("User Name", updated.Name);
    }

    #endregion

    #region UpdateProfilePicture Tests

    [Fact]
    public async Task UpdateProfilePicture_UpdatesLink()
    {
        var profile = new Profile("user", "User");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        await _profileService.UpdateProfilePicture(profile.Id, "https://newpic.url/pic.jpg");

        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("https://newpic.url/pic.jpg", updated!.ProfilePictureLink);
    }

    [Fact]
    public async Task UpdateProfilePicture_OverwritesExistingLink()
    {
        var profile = new Profile("user", "User");
        profile.ProfilePictureLink = "https://old.url/old.jpg";
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();

        await _profileService.UpdateProfilePicture(profile.Id, "https://new.url/new.jpg");

        var updated = await _dbContext.Profile.FindAsync(profile.Id);
        Assert.Equal("https://new.url/new.jpg", updated!.ProfilePictureLink);
    }

    #endregion

    #region SearchForAProfile Tests

    [Fact]
    public void SearchForAProfile_ReturnsMatchingProfiles()
    {
        _dbContext.Profile.AddRange(
            new Profile("alexsmith", "Alex Smith"),
            new Profile("alexander", "Alexander"),
            new Profile("bob", "Bob")
        );
        _dbContext.SaveChanges();

        var results = _profileService.SearchForAProfile("alex");

        Assert.Equal(2, results.Count());
    }

    [Fact]
    public void SearchForAProfile_ReturnsEmptyList_WhenNoMatches()
    {
        _dbContext.Profile.AddRange(
            new Profile("alice", "Alice"),
            new Profile("bob", "Bob")
        );
        _dbContext.SaveChanges();

        var results = _profileService.SearchForAProfile("xyz");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchForAProfile_ThrowsException_WhenUsernameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _profileService.SearchForAProfile(null!));
    }

    [Fact]
    public void SearchForAProfile_ThrowsException_WhenUsernameIsEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => _profileService.SearchForAProfile(""));
    }

    [Fact]
    public void SearchForAProfile_FindsPartialMatches()
    {
        _dbContext.Profile.AddRange(
            new Profile("johndoe", "John Doe"),
            new Profile("john123", "John 123"),
            new Profile("jane", "Jane")
        );
        _dbContext.SaveChanges();

        var results = _profileService.SearchForAProfile("john");

        Assert.Equal(2, results.Count());
    }

    #endregion

    #region DeleteProfile Tests

    [Fact]
    public async Task DeleteProfile_DeletesProfile_WhenExists()
    {
        var profile = new Profile("deleteuser", "Delete User");
        _dbContext.Profile.Add(profile);
        await _dbContext.SaveChangesAsync();
        var profileId = profile.Id;

        var result = await _profileService.DeleteProfile(profileId);

        Assert.Equal(profileId, result);
        var deleted = await _dbContext.Profile.FindAsync(profileId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteProfile_ThrowsException_WhenProfileNotFound()
    {
        await Assert.ThrowsAsync<Exception>(() => _profileService.DeleteProfile(99999));
    }

    [Fact]
    public async Task DeleteProfile_OnlyDeletesSpecifiedProfile()
    {
        var profile1 = new Profile("user1", "User 1");
        var profile2 = new Profile("user2", "User 2");
        _dbContext.Profile.AddRange(profile1, profile2);
        await _dbContext.SaveChangesAsync();

        await _profileService.DeleteProfile(profile1.Id);

        var remaining = await _dbContext.Profile.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("user2", remaining[0].Username);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
