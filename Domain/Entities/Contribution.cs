using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Contribution
{
    public Guid Id { get; set; }

    public Guid LocationId { get; set; }

    public Guid AuthorId { get; set; }

    public int ContributionType { get; set; }

    public string Title { get; set; } = null!;

    public string? Summary { get; set; }

    public int LikesCount { get; set; }

    public int WorkflowState { get; set; }

    public string? SourceDocumentUrl { get; set; }

    public string? NoSqlDocumentId { get; set; }

    public int? Version { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual ICollection<ContributionLike> ContributionLikes { get; set; } = new List<ContributionLike>();

    public virtual Location Location { get; set; } = null!;
}
