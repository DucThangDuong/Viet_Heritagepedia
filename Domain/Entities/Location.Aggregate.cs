using System;
using System.Collections.Generic;

namespace Domain.Entities;
public partial class Location
{
    public static Location Create(
        string name, 
        string? slug, 
        string? vietnameseName, 
        int category, 
        string region, 
        string province, 
        string? address, 
        bool isPlainRegion, 
        string? coverImageUrl, 
        bool isFeatured, 
        int? unescoYear)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug ?? name.ToLowerInvariant().Replace(" ", "-"),
            VietnameseName = vietnameseName,
            Category = category,
            Region = string.IsNullOrWhiteSpace(region) ? "Trung Bộ" : region,
            Province = string.IsNullOrWhiteSpace(province) ? "Thừa Thiên Huế" : province,
            Address = address,
            IsPlainRegion = isPlainRegion,
            CoverImageUrl = coverImageUrl,
            IsFeatured = isFeatured,
            UnescoYear = unescoYear,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return location;
    }
    public void UpdateDetails(string name, string? vietnameseName, string? address)
    {
        Name = name;
        VietnameseName = vietnameseName;
        Address = address;
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
    }
    public void UpdateFull(
        string name, 
        string? slug, 
        string? vietnameseName, 
        int category, 
        string region, 
        string province, 
        string? address, 
        bool isPlainRegion, 
        string? coverImageUrl, 
        bool isFeatured, 
        int? unescoYear)
    {
        Name = name;
        if (!string.IsNullOrWhiteSpace(slug)) Slug = slug;
        VietnameseName = vietnameseName;
        Category = category;
        if (!string.IsNullOrWhiteSpace(region)) Region = region;
        if (!string.IsNullOrWhiteSpace(province)) Province = province;
        Address = address;
        IsPlainRegion = isPlainRegion;
        CoverImageUrl = coverImageUrl;
        IsFeatured = isFeatured;
        UnescoYear = unescoYear;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
