using AccountService.Business;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Tests;

/// <summary>
/// Tests for KeycloakService
/// </summary>
public class KeycloakServiceTests : TestBase
{
    private readonly Mock<ILogger<KeycloakService>> _mockLogger;

    static KeycloakServiceTests()
    {
        SetRequiredEnvVars();
    }

    public KeycloakServiceTests()
    {
        SetRequiredEnvVars();
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
        var service = new KeycloakService(new HttpClient(), _mockLogger.Object);
        Assert.NotNull(service);
    }

    #endregion

    #region DeleteUserAsync - Token Acquisition Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenRequestFails()
    {
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent("Invalid credentials")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseIsEmpty()
    {
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseMissingAccessToken()
    {
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"token_type\":\"bearer\"}")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenIsNull()
    {
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"access_token\":null}")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTokenResponseIsInvalidJson()
    {
        var httpClient = CreateMockHttpClient(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("not valid json")
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Delete Operation Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenDeleteSucceeds()
    {
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
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-to-delete");
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenUserNotFound()
    {
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
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("already-deleted-user");
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteFails()
    {
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
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenDeleteReturnsForbidden()
    {
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
            return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };
        });

        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Exception Handling Tests

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenHttpRequestThrows()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenTaskCanceled()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new KeycloakService(httpClient, _mockLogger.Object);
        var result = await service.DeleteUserAsync("user-123");
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync - Request Validation Tests

    [Fact]
    public async Task DeleteUserAsync_SendsCorrectTokenRequest()
    {
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
        await service.DeleteUserAsync("user-123");

        Assert.NotNull(capturedTokenRequest);
        Assert.Equal(HttpMethod.Post, capturedTokenRequest.Method);
        Assert.Contains("token", capturedTokenRequest.RequestUri?.ToString() ?? "");
    }

    [Fact]
    public async Task DeleteUserAsync_SendsDeleteRequestWithCorrectUserId()
    {
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
        await service.DeleteUserAsync("specific-user-id-123");

        Assert.NotNull(capturedDeleteRequest);
        Assert.Equal(HttpMethod.Delete, capturedDeleteRequest.Method);
        Assert.Contains("specific-user-id-123", capturedDeleteRequest.RequestUri?.ToString() ?? "");
    }

    [Fact]
    public async Task DeleteUserAsync_SetsAuthorizationHeader()
    {
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
        await service.DeleteUserAsync("user-123");

        Assert.NotNull(capturedDeleteRequest);
        Assert.NotNull(capturedDeleteRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedDeleteRequest.Headers.Authorization.Scheme);
        Assert.Equal("my-secret-token", capturedDeleteRequest.Headers.Authorization.Parameter);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DeleteUserAsync_HandlesUuidUserId()
    {
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
        var result = await service.DeleteUserAsync(userId);
        Assert.True(result);
    }

    #endregion
}
