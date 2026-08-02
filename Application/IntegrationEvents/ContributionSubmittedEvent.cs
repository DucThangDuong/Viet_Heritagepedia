using System;

namespace Application.Contracts;
public class ContributionSubmittedEvent
{
    public Guid ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? NoSqlDocumentId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
