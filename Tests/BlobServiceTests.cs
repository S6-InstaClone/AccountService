using AccountService.Persistence;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for BlobService
/// </summary>
public class BlobServiceTests : TestBase
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Mock<ILogger<BlobService>> _mockLogger;

    public BlobServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _mockLogger = new Mock<ILogger<BlobService>>();

        // Setup chain: BlobServiceClient -> BlobContainerClient -> BlobClient
        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);
    }

    [Fact]
    public void BlobService_CanBeInstantiated()
    {
        // Act
        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task UploadProfilePictureAsync_ReturnsUrl_WhenUploadSucceeds()
    {
        // Arrange
        var expectedUrl = new Uri("https://storage.blob.core.windows.net/container/user123/profile.jpg");
        
        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(expectedUrl);

        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<AccessTier?>(),
                It.IsAny<StorageTransferOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);
        
        // Create mock file
        var content = "fake image content"u8.ToArray();
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.FileName).Returns("profile.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(content.Length);

        // Act
        var result = await service.UploadProfilePictureAsync("user123", mockFile.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("user123", result);
    }

    [Fact]
    public async Task UploadProfilePictureAsync_GeneratesUniqueBlobName()
    {
        // Arrange
        string? capturedBlobName = null;
        
        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Callback<string>(name => capturedBlobName = name)
            .Returns(_mockBlobClient.Object);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/blob"));

        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<AccessTier?>(),
                It.IsAny<StorageTransferOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);
        
        var content = "test"u8.ToArray();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        await service.UploadProfilePictureAsync("user456", mockFile.Object);

        // Assert
        Assert.NotNull(capturedBlobName);
        Assert.Contains("user456", capturedBlobName);
    }

    [Fact]
    public async Task UploadProfilePictureAsync_SetsCorrectContentType()
    {
        // Arrange
        BlobHttpHeaders? capturedHeaders = null;

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/blob"));

        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<AccessTier?>(),
                It.IsAny<StorageTransferOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, BlobHttpHeaders, IDictionary<string, string>, BlobRequestConditions, IProgress<long>, AccessTier?, StorageTransferOptions, CancellationToken>(
                (s, h, m, c, p, a, t, ct) => capturedHeaders = h)
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
        mockFile.Setup(f => f.FileName).Returns("photo.png");
        mockFile.Setup(f => f.ContentType).Returns("image/png");

        // Act
        await service.UploadProfilePictureAsync("user789", mockFile.Object);

        // Assert
        Assert.NotNull(capturedHeaders);
        Assert.Equal("image/png", capturedHeaders.ContentType);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_CallsDeleteOnBlob()
    {
        // Arrange
        _mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        // Act
        await service.DeleteProfilePictureAsync("user123/profile.jpg");

        // Assert
        _mockBlobClient.Verify(
            x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_HandlesNonExistentBlob()
    {
        // Arrange
        _mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        // Act & Assert - should not throw
        await service.DeleteProfilePictureAsync("nonexistent/blob.jpg");
    }

    [Fact]
    public async Task UploadProfilePictureAsync_HandlesEmptyFile()
    {
        // Arrange
        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/blob"));

        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<AccessTier?>(),
                It.IsAny<StorageTransferOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        mockFile.Setup(f => f.FileName).Returns("empty.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(0);

        // Act
        var result = await service.UploadProfilePictureAsync("user-empty", mockFile.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UploadProfilePictureAsync_PreservesFileExtension()
    {
        // Arrange
        string? capturedBlobName = null;

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Callback<string>(name => capturedBlobName = name)
            .Returns(_mockBlobClient.Object);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://test.blob.core.windows.net/container/blob"));

        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<AccessTier?>(),
                It.IsAny<StorageTransferOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var service = new BlobService(_mockBlobServiceClient.Object, _mockLogger.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1]));
        mockFile.Setup(f => f.FileName).Returns("myimage.webp");
        mockFile.Setup(f => f.ContentType).Returns("image/webp");

        // Act
        await service.UploadProfilePictureAsync("userX", mockFile.Object);

        // Assert
        Assert.NotNull(capturedBlobName);
        Assert.EndsWith(".webp", capturedBlobName);
    }
}
