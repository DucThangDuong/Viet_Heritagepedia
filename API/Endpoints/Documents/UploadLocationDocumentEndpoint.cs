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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;

namespace API.Endpoints.Documents;

public class UploadLocationDocumentRequest
{
    public Guid LocationId { get; set; }
    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid AuthorId { get; set; }
    public IFormFile File { get; set; } = null!;
}

public class UploadLocationDocumentResponse
{
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string WebSocketUrl { get; set; } = "/hubs/document-processing";
    public string Message { get; set; } = string.Empty;
}

public class UploadLocationDocumentEndpoint : Endpoint<UploadLocationDocumentRequest, UploadLocationDocumentResponse>
{
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IFileStorageService _fileStorageService;

    public UploadLocationDocumentEndpoint(ISendEndpointProvider sendEndpointProvider, IFileStorageService fileStorageService)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _fileStorageService = fileStorageService;
    }

    public override void Configure()
    {
        Post("/api/locations/{LocationId}/documents");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Upload PDF/DOCX document for a Location & dispatch conversion command";
            s.Description = "Requires authenticated user JWT token. Validates file header signature, saves file, and emits ProcessLocationDocumentCommand to RabbitMQ.";
        });
        
        Options(x => 
        {
            x.RequireRateLimiting("UploadLimit");
            x.AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 5_242_880;
                return await next(context);
            });
        });
    }

    public override async Task HandleAsync(UploadLocationDocumentRequest req, CancellationToken ct)
    {

        // kiểm tra file người dùng gửi lên
        if (req.File == null || req.File.Length == 0)
        {
            var fail = Result<UploadLocationDocumentResponse>.Failure("ERR_FILE_REQUIRED", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }
        if (req.File.Length > 5_242_880)
        {
            var fail = Result<UploadLocationDocumentResponse>.Failure("ERR_FILE_TOO_LARGE", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
        var fileExt = Path.GetExtension(req.File.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExt))
        {
            var fail = Result<UploadLocationDocumentResponse>.Failure("ERR_INVALID_FILE_TYPE", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }
        using var stream = req.File.OpenReadStream();
        var isValidSignature = await _fileStorageService.ValidateMagicBytesAsync(stream, fileExt, ct);

        if (!isValidSignature)
        {
            var signatureFail = Result<UploadLocationDocumentResponse>.Failure("ERR_INVALID_FILE_SIGNATURE", 400);
            await this.SendApiResponseAsync(signatureFail, ct);
            return;
        }

        var jobId = Guid.NewGuid();
        var savedFileName = $"{jobId}{fileExt}";
        var fullPath = await _fileStorageService.SaveFileAsync(stream, savedFileName, ct);
        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:pdf_conversion_queue"));
        await sendEndpoint.Send(new ProcessLocationDocumentCommand
        {
            JobId = jobId,
            LocationId = req.LocationId,
            AuthorId = req.AuthorId,
            FileName = req.File.FileName,
            FilePath = fullPath,
            FileType = fileExt.TrimStart('.'),
            UploadedAt = DateTime.UtcNow
        }, ct);

        var response = new UploadLocationDocumentResponse
        {
            JobId = jobId,
            FileName = req.File.FileName,
            WebSocketUrl = "/hubs/document-processing",
            Message = "Document uploaded successfully for location, magic bytes verified, and conversion job published."
        };

        var success = Result<UploadLocationDocumentResponse>.Success(response, 202);
        await this.SendApiResponseAsync(success, ct);
    }
}
