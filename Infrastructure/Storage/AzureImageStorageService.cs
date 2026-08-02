using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Application.Interfaces.Storage;

namespace Infrastructure.Storage;

public class AzureImageStorageService : IImageStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    private static readonly byte[] JpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] WebpMagicBytes = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // "RIFF" (Needs more complex check but this is basic)

    public AzureImageStorageService(string connectionString, string containerName = "images")
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = containerName;
    }

    public async Task<bool> ValidateImageAsync(Stream imageStream, string fileExtension, CancellationToken ct = default)
    {
        if (imageStream == null || imageStream.Length < 8)
            return false;

        var initialPosition = imageStream.CanSeek ? imageStream.Position : 0;
        byte[] header = new byte[8];

        try
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            int bytesRead = await imageStream.ReadAsync(header, 0, 8, ct);
            if (bytesRead < 3)
                return false;

            var ext = fileExtension.ToLowerInvariant().TrimStart('.');

            return ext switch
            {
                "jpg" or "jpeg" => header.Take(3).SequenceEqual(JpegMagicBytes),
                "png" => header.SequenceEqual(PngMagicBytes),
                "webp" => header.Take(4).SequenceEqual(WebpMagicBytes),
                _ => false 
            };
        }
        finally
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = initialPosition;
            }
        }
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string targetFileName, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(targetFileName);

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(imageStream, uploadOptions, ct);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteImageAsync(string fileUrlOrName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        string blobName = fileUrlOrName;
        if (Uri.TryCreate(fileUrlOrName, UriKind.Absolute, out Uri? uri))
        {
            blobName = Path.GetFileName(uri.LocalPath);
        }

        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
        
        return response.Value;
    }
}
