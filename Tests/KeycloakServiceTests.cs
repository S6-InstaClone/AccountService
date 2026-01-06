using AccountService.Business;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Tests;

/// <summary>
/// Tests for KeycloakService - covers all branches and conditions
/// </summary>
public class KeycloakServiceTests : TestBase
{
    private readonly Mock<ILogger<KeycloakService>> _mockLogger;

    // Static constructor ensures env vars are set BEFORE any KeycloakService is created
    static KeycloakServiceTests()
    {
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
    }

    public KeycloakServiceTests()
    {
        _mockLogger = new Mock<ILogger<KeycloakService>>();
    }

    private HttpClient CreateMockHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => handler(req));

        return new HttpClient(mockHandler.Object);
    }

    #region Constructor Tests

    [Fact]
    public void KeycloakService_CanBeInstantiated_WhenEnvVarsSet()
    {
        // Arrange & Act
        var service = new KeycloakService(new HttpClient(), _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region DeleteUserAsync - Token Acquisition Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenRequestFails()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent("Invalid credentials")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseIsEmpty()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseMissingAccessToken()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"token_type\":\"bearer\"}")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenIsNullString()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"access_token\":null}")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenIsEmptyString()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"access_token\":\"\"}")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseIsInvalidJson()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("not valid json")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Delete Operation Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenDeleteSucceeds()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1) // Token request
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\"}")
                };
            }
            // Delete request
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-to-delete");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenUserNotFound()
    {
        // Arrange - 404 means user already deleted, should return true
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\"}")
                };
            }
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("already-deleted-user");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteFails()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\"}")
                };
            }
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteReturnsForbidden()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\"}")
                };
            }
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden
            };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteReturnsUnauthorized()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\"}")
                };
            }
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Exception Handling Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenHttpRequestThrows()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTaskCanceled()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteThrowsException()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"access_token\":\"valid-token\"}")
                    };
                }
                throw new HttpRequestException("Delete failed");
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-123");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Request Validation Tests

    [Fact]
    public async Task DeleteUserAsync_SendsCorrectTokenRequest()
    {
        // Arrange
        HttpRequestMessage? capturedTokenRequest = null;
        var requestCount = 0;
        
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    capturedTokenRequest = req;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"access_token\":\"test-token\"}")
                    };
                }
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("user-123");

        // Assert
        Assert.NotNull(capturedTokenRequest);
        Assert.Equal(HttpMethod.Post, capturedTokenRequest.Method);
        Assert.Contains("token", capturedTokenRequest.RequestUri?.ToString() ?? "");
    }

    [Fact]
    public async Task DeleteUserAsync_SendsDeleteRequestWithCorrectUserId()
    {
        // Arrange
        HttpRequestMessage? capturedDeleteRequest = null;
        var requestCount = 0;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"access_token\":\"test-token\"}")
                    };
                }
                capturedDeleteRequest = req;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("specific-user-id-123");

        // Assert
        Assert.NotNull(capturedDeleteRequest);
        Assert.Equal(HttpMethod.Delete, capturedDeleteRequest.Method);
        Assert.Contains("specific-user-id-123", capturedDeleteRequest.RequestUri?.ToString() ?? "");
    }

    [Fact]
    public async Task DeleteUserAsync_SetsAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedDeleteRequest = null;
        var requestCount = 0;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"access_token\":\"my-secret-token\"}")
                    };
                }
                capturedDeleteRequest = req;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("user-123");

        // Assert
        Assert.NotNull(capturedDeleteRequest);
        Assert.NotNull(capturedDeleteRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedDeleteRequest.Headers.Authorization.Scheme);
        Assert.Equal("my-secret-token", capturedDeleteRequest.Headers.Authorization.Parameter);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task DeleteUserAsync_LogsAttempt()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("user-123");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user-123")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteUserAsync_LogsSuccess()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"token\"}")
                };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("user-123");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteUserAsync_LogsError_WhenFails()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        await service.DeleteUserAsync("user-123");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsIn(LogLevel.Warning, LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DeleteUserAsync_HandlesEmptyUserId()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"token\"}")
                };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_HandlesSpecialCharactersInUserId()
    {
        // Arrange
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"token\"}")
                };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync("user-with-special-chars-!@#$%");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_HandlesUuidUserId()
    {
        // Arrange
        var userId = "550e8400-e29b-41d4-a716-446655440000";
        var requestCount = 0;
        var httpClient = CreateMockHttpClient(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"token\"}")
                };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);

        // Act
        var result = await service.DeleteUserAsync(userId);

        // Assert
        Assert.True(result);
    }

    #endregion
}
