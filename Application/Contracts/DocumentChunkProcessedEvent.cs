using System;

namespace Application.Contracts;

public class DocumentChunkProcessedEvent
{
    public Guid JobId { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public string DataJson { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string? ErrorMessage { get; set; }
}
