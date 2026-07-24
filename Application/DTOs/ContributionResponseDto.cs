using System;

namespace Application.DTOs;

public class ContributionResponseDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
    public int WorkflowState { get; set; }
    public string? SourceDocumentUrl { get; set; }
    public string? NoSqlDocumentId { get; set; }
    public int? Version { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
