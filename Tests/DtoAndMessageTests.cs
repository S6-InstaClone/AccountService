using AccountService.Dtos;
using AccountService.Messages;
using System.ComponentModel.DataAnnotations;

namespace Tests;

/// <summary>
/// Tests for DTOs and Messages
/// </summary>
public class DtoAndMessageTests
{
    #region CreateProfileDto Tests

    [Fact]
    public void CreateProfileDto_CanSetAllProperties()
    {
        // Act
        var dto = new CreateProfileDto("username", "name", "description");

        // Assert
        Assert.Equal("username", dto.Username);
        Assert.Equal("name", dto.Name);
        Assert.Equal("description", dto.Description);
    }

    [Fact]
    public void CreateProfileDto_Username_HasRequiredAttribute()
    {
        // Arrange
        var property = typeof(CreateProfileDto).GetProperty(nameof(CreateProfileDto.Username));
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void CreateProfileDto_Name_HasRequiredAttribute()
    {
        // Arrange
        var property = typeof(CreateProfileDto).GetProperty(nameof(CreateProfileDto.Name));
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void CreateProfileDto_Description_IsOptional()
    {
        // Arrange
        var property = typeof(CreateProfileDto).GetProperty(nameof(CreateProfileDto.Description));
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert - no Required attribute
        Assert.Null(attribute);
    }

    #endregion

    #region UpdateProfileNameDto Tests

    [Fact]
    public void UpdateProfileNameDto_CanSetProperties()
    {
        // Act
        var dto = new UpdateProfileNameDto { Id = 1, Name = "New Name" };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("New Name", dto.Name);
    }

    #endregion

    #region UpdateProfileDescDto Tests

    [Fact]
    public void UpdateProfileDescDto_CanSetProperties()
    {
        // Act
        var dto = new UpdateProfileDescDto { Id = 1, Description = "New Description" };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("New Description", dto.Description);
    }

    #endregion

    #region SearchProfileDto Tests

    [Fact]
    public void SearchProfileDto_CanSetProperties()
    {
        // Act
        var dto = new SearchProfileDto { Id = 1, Username = "searchuser" };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("searchuser", dto.Username);
    }

    #endregion

    #region SearchProfilesResultsDto Tests

    [Fact]
    public void SearchProfilesResultsDto_CanSetResults()
    {
        // Arrange
        var results = new List<SearchProfileDto>
        {
            new SearchProfileDto { Id = 1, Username = "user1" },
            new SearchProfileDto { Id = 2, Username = "user2" }
        };

        // Act
        var dto = new SearchProfilesResultsDto { Results = results };

        // Assert
        Assert.Equal(2, dto.Results.Count);
    }

    #endregion

    #region UploadProfilePictureDto Tests

    [Fact]
    public void UploadProfilePictureDto_CanSetId()
    {
        // Act
        var dto = new UploadProfilePictureDto { Id = 123 };

        // Assert
        Assert.Equal(123, dto.Id);
    }

    #endregion

    #region AccountDeletedEvent Tests

    [Fact]
    public void AccountDeletedEvent_HasDefaultValues()
    {
        // Act
        var evt = new AccountDeletedEvent();

        // Assert
        Assert.Equal(string.Empty, evt.UserId);
        Assert.Null(evt.Username);
        Assert.Null(evt.Email);
        Assert.Equal("GDPR_USER_REQUEST", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetAllProperties()
    {
        // Arrange
        var deletedAt = DateTime.UtcNow;

        // Act
        var evt = new AccountDeletedEvent
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@example.com",
            DeletedAt = deletedAt,
            Reason = "ADMIN_ACTION"
        };

        // Assert
        Assert.Equal("user-123", evt.UserId);
        Assert.Equal("testuser", evt.Username);
        Assert.Equal("test@example.com", evt.Email);
        Assert.Equal(deletedAt, evt.DeletedAt);
        Assert.Equal("ADMIN_ACTION", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_IsRecord()
    {
        // Assert - Records are reference types with value equality
        var evt1 = new AccountDeletedEvent { UserId = "user-1", Reason = "TEST" };
        var evt2 = new AccountDeletedEvent { UserId = "user-1", Reason = "TEST" };

        Assert.Equal(evt1, evt2);
    }

    #endregion
}
