using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Location
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? VietnameseName { get; set; }

    public int Category { get; set; }

    public string Region { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsPlainRegion { get; set; }

    public int? UnescoYear { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
