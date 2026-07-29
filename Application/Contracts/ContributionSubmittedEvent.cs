using System;

namespace Application.Contracts;

/// <summary>
/// Event published to RabbitMQ when a contribution transitions from Draft → Pending Review.
/// Consumed by downstream services for notifications, audit logging, search indexing, etc.
/// </summary>
public class ContributionSubmittedEvent
{
    public Guid ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? NoSqlDocumentId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
