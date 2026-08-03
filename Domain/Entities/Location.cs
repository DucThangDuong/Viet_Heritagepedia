using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Location
{
    public Guid Id { get; private set; }

    public string Slug { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? VietnameseName { get; private set; }

    public int Category { get; private set; }

    public string Region { get; private set; } = null!;

    public string Province { get; private set; } = null!;

    public string? Address { get; private set; }

    public bool IsPlainRegion { get; private set; }

    public int? UnescoYear { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public bool IsFeatured { get; private set; }

    public bool? IsActive { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    private readonly List<Contribution> _contributions = new();
    public virtual IReadOnlyCollection<Contribution> Contributions => _contributions.AsReadOnly();
}
