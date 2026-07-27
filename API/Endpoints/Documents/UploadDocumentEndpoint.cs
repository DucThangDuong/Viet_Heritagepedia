using Application.Common;
using Application.Contracts;
using Application.Interfaces.Storage;
using API.Extensions;
using FastEndpoints;
using MassTransit;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Endpoints.Documents;

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;
}

public class UploadDocumentResponse
{
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string WebSocketUrl { get; set; } = "/hubs/document-processing";
    public string Message { get; set; } = string.Empty;
}

public class UploadDocumentEndpoint : Endpoint<UploadDocumentRequest, UploadDocumentResponse>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IFileStorageService _fileStorageService;

    public UploadDocumentEndpoint(IPublishEndpoint publishEndpoint, IFileStorageService fileStorageService)
    {
        _publishEndpoint = publishEndpoint;
        _fileStorageService = fileStorageService;
    }

    public override void Configure()
    {
        Post("/api/documents/upload");
        AllowAnonymous();
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Upload PDF/DOCX document with Magic Bytes validation & dispatch conversion command via MassTransit";
            s.Description = "Validates file header signature (magic bytes), saves uploaded document via IFileStorageService, emits ConvertDocumentToJsonCommand to RabbitMQ, and returns JobId for WebSocket tracking.";
        });
    }

    public override async Task HandleAsync(UploadDocumentRequest req, CancellationToken ct)
    {
        if (req.File == null || req.File.Length == 0)
        {
            var fail = Result<UploadDocumentResponse>.Failure("ERR_FILE_REQUIRED", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
        var fileExt = Path.GetExtension(req.File.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExt))
        {
            var fail = Result<UploadDocumentResponse>.Failure("ERR_INVALID_FILE_TYPE", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        // Validate Magic Bytes (File Header Signature) to prevent extension spoofing
        using var stream = req.File.OpenReadStream();
        var isValidSignature = await _fileStorageService.ValidateMagicBytesAsync(stream, fileExt, ct);

        if (!isValidSignature)
        {
            var signatureFail = Result<UploadDocumentResponse>.Failure("ERR_INVALID_FILE_SIGNATURE", 400);
            await this.SendApiResponseAsync(signatureFail, ct);
            return;
        }

        var jobId = Guid.NewGuid();
        var savedFileName = $"{jobId}{fileExt}";

        // Save file to storage via clean architecture IFileStorageService abstraction
        var fullPath = await _fileStorageService.SaveFileAsync(stream, savedFileName, ct);

        // Publish conversion command to MassTransit / RabbitMQ
        await _publishEndpoint.Publish(new ConvertDocumentToJsonCommand
        {
            JobId = jobId,
            FileName = req.File.FileName,
            FilePath = fullPath,
            FileType = fileExt.TrimStart('.'),
            UploadedAt = DateTime.UtcNow
        }, ct);

        var response = new UploadDocumentResponse
        {
            JobId = jobId,
            FileName = req.File.FileName,
            WebSocketUrl = "/hubs/document-processing",
            Message = "Document uploaded successfully, magic bytes verified, and conversion job published."
        };

        var success = Result<UploadDocumentResponse>.Success(response, 202);
        await this.SendApiResponseAsync(success, ct);
    }
}
