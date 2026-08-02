using System;

namespace Application.Contracts;

public class ProcessLocationDocumentCommand
{
    public Guid JobId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
