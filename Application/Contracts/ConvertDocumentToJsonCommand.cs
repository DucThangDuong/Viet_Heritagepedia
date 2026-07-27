using System;

namespace Application.Contracts;

public class ConvertDocumentToJsonCommand
{
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
