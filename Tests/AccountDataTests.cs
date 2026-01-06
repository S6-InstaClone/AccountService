using AccountService.Models;

namespace Tests;

/// <summary>
/// Tests for AccountData model
/// </summary>
public class AccountDataTests : TestBase
{
    [Fact]
    public void AccountData_DefaultConstructor_InitializesProperties()
    {
        // Act
        var account = new AccountData();

        // Assert
        Assert.Equal(0, account.Id);
        Assert.Null(account.Username);
        Assert.Null(account.Password);
        Assert.Null(account.Email);
    }

    [Fact]
    public void AccountData_CanSetId()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Id = 123;

        // Assert
        Assert.Equal(123, account.Id);
    }

    [Fact]
    public void AccountData_CanSetUsername()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Username = "testuser";

        // Assert
        Assert.Equal("testuser", account.Username);
    }

    [Fact]
    public void AccountData_CanSetPassword()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Password = "securepassword123";

        // Assert
        Assert.Equal("securepassword123", account.Password);
    }

    [Fact]
    public void AccountData_CanSetEmail()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Email = "test@example.com";

        // Assert
        Assert.Equal("test@example.com", account.Email);
    }

    [Fact]
    public void AccountData_CanSetAllProperties()
    {
        // Act
        var account = new AccountData
        {
            Id = 42,
            Username = "johndoe",
            Password = "password123",
            Email = "john@example.com"
        };

        // Assert
        Assert.Equal(42, account.Id);
        Assert.Equal("johndoe", account.Username);
        Assert.Equal("password123", account.Password);
        Assert.Equal("john@example.com", account.Email);
    }

    [Fact]
    public void AccountData_PropertiesCanBeNull()
    {
        // Arrange
        var account = new AccountData
        {
            Username = "test",
            Email = "test@test.com"
        };

        // Act
        account.Username = null;
        account.Email = null;

        // Assert
        Assert.Null(account.Username);
        Assert.Null(account.Email);
    }

    [Fact]
    public void AccountData_IdCanBeNegative()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Id = -1;

        // Assert
        Assert.Equal(-1, account.Id);
    }
}
