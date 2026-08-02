using System;

namespace Application.Contracts;

public class LocationDocumentProcessedEvent
{
    public Guid JobId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
    public string MongoDbId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
