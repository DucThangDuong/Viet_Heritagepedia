using System;

namespace Application.DTOs;

public class LocationResponseDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? VietnameseName { get; set; }
    public int Category { get; set; }
    public string Region { get; set; } = "Trung Bộ";
    public string Province { get; set; } = "Thừa Thiên Huế";
    public string? Address { get; set; }
    public bool IsPlainRegion { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int? UnescoYear { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateLocationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? VietnameseName { get; set; }
    public int Category { get; set; }
    public string Region { get; set; } = "Trung Bộ";
    public string Province { get; set; } = "Thừa Thiên Huế";
    public string? Address { get; set; }
    public bool IsPlainRegion { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int? UnescoYear { get; set; }
}
