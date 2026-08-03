using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Contribution
{
    public Guid Id { get; private set; }

    public Guid LocationId { get; private set; }

    public Guid AuthorId { get; private set; }

    public int ContributionType { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Summary { get; private set; }

    public int LikesCount { get; private set; }

    public int WorkflowState { get; private set; }

    public string? SourceDocumentUrl { get; private set; }

    public string? NoSqlDocumentId { get; private set; }

    public int? Version { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public virtual User Author { get; private set; } = null!;

    private readonly List<ContributionLike> _contributionLikes = new();
    public virtual IReadOnlyCollection<ContributionLike> ContributionLikes => _contributionLikes.AsReadOnly();

    public virtual Location Location { get; private set; } = null!;
}
