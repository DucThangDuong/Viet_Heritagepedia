using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Endpoints.Documents;
using Application.Common;
using Application.Contracts;
using Application.Interfaces.Storage;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Viet_Heritagepedia.Tests.Endpoints;

public class UploadLocationDocumentEndpointTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly UploadLocationDocumentEndpoint _endpoint;

    public UploadLocationDocumentEndpointTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _endpoint = new UploadLocationDocumentEndpoint(_publishEndpointMock.Object, _fileStorageServiceMock.Object);
    }

    private static IFormFile CreateMockFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        return fileMock.Object;
    }

    [Fact]
    public async Task UploadLocationDocument_ShouldReturn400_WhenFileIsNull()
    {
        // Arrange
        var request = new UploadLocationDocumentRequest 
        { 
            LocationId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            File = null! 
        };

        // Act
        await Record.ExceptionAsync(() => _endpoint.HandleAsync(request, CancellationToken.None));

        // Assert
        _fileStorageServiceMock.Verify(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<ProcessLocationDocumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLocationDocument_ShouldReturn400_WhenFileExtensionIsInvalid()
    {
        // Arrange
        var mockFile = CreateMockFormFile("malicious.exe", Encoding.UTF8.GetBytes("MZ_test_content"));
        var request = new UploadLocationDocumentRequest 
        { 
            LocationId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            File = mockFile 
        };

        // Act
        await Record.ExceptionAsync(() => _endpoint.HandleAsync(request, CancellationToken.None));

        // Assert
        _fileStorageServiceMock.Verify(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<ProcessLocationDocumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLocationDocument_ShouldReturn400_WhenMagicBytesValidationFails()
    {
        // Arrange
        var mockFile = CreateMockFormFile("spoofed.pdf", Encoding.UTF8.GetBytes("NOT_A_PDF_HEADER"));
        var request = new UploadLocationDocumentRequest 
        { 
            LocationId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            File = mockFile 
        };

        _fileStorageServiceMock
            .Setup(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await Record.ExceptionAsync(() => _endpoint.HandleAsync(request, CancellationToken.None));

        // Assert
        _fileStorageServiceMock.Verify(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), ".pdf", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageServiceMock.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(x => x.Publish(It.IsAny<ProcessLocationDocumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLocationDocument_ShouldReturn202_WhenFileIsValidAndMagicBytesMatch()
    {
        // Arrange
        byte[] pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7 valid pdf header");
        var mockFile = CreateMockFormFile("valid_heritage_doc.pdf", pdfBytes);
        var locationId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var request = new UploadLocationDocumentRequest 
        { 
            LocationId = locationId,
            AuthorId = authorId,
            File = mockFile 
        };

        _fileStorageServiceMock
            .Setup(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/shared_uploads/saved_file.pdf");

        // Act
        await Record.ExceptionAsync(() => _endpoint.HandleAsync(request, CancellationToken.None));

        // Assert
        _fileStorageServiceMock.Verify(x => x.ValidateMagicBytesAsync(It.IsAny<Stream>(), ".pdf", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageServiceMock.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpointMock.Verify(x => x.Publish(It.Is<ProcessLocationDocumentCommand>(c => 
            c.FileName == "valid_heritage_doc.pdf" && 
            c.FileType == "pdf" &&
            c.LocationId == locationId &&
            c.AuthorId == authorId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
