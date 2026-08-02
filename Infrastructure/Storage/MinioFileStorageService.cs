using Amazon.S3;
using Amazon.S3.Model;
using Application.Interfaces.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Storage;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    private static readonly byte[] PdfMagicBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
    private static readonly byte[] DocxMagicBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK.. (Zip container)
    private static readonly byte[] DocMagicBytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0 };  // Compound File Binary

    public MinioFileStorageService(IAmazonS3 s3Client, string bucketName = "documents")
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task<bool> ValidateMagicBytesAsync(Stream fileStream, string fileExtension, CancellationToken ct = default)
    {
        if (fileStream == null || fileStream.Length < 4)
            return false;

        var initialPosition = fileStream.CanSeek ? fileStream.Position : 0;
        byte[] header = new byte[4];

        try
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            int bytesRead = await fileStream.ReadAsync(header, 0, 4, ct);
            if (bytesRead < 4)
                return false;

            var ext = fileExtension.ToLowerInvariant().TrimStart('.');

            return ext switch
            {
                "pdf" => header.SequenceEqual(PdfMagicBytes),
                "docx" => header.SequenceEqual(DocxMagicBytes),
                "doc" => header.SequenceEqual(DocMagicBytes) || header.SequenceEqual(DocxMagicBytes),
                _ => false
            };
        }
        finally
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = initialPosition;
            }
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string targetFileName, CancellationToken ct = default)
    {
        try
        {
            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            if (!bucketExists)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest, ct);
            }
        }
        catch
        {
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = targetFileName,
            InputStream = fileStream,
            AutoCloseStream = false 
        };
        await _s3Client.PutObjectAsync(putRequest, ct);
        return targetFileName; 
    }
}
