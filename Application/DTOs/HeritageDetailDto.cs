using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class HeritageDetailDto
{
    // Location Details (SQL)
    public string Id { get; set; } = string.Empty; // Slug
    public Guid LocationId { get; set; } // Actual SQL ID
    public string Title { get; set; } = string.Empty; // Name
    public string VietnameseTitle { get; set; } = string.Empty; // VietnameseName
    public string Category { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // Address
    public string Region { get; set; } = string.Empty;
    public int? UnescoYear { get; set; }
    public string Image { get; set; } = string.Empty; // CoverImageUrl
    public bool Featured { get; set; }

    public GeoCoordinatesDto? Coordinates { get; set; }

    // Heritage Content (MongoDB Type 1)
    public string ShortDescription { get; set; } = string.Empty; 
    public string LeadQuote { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string Quote { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public string HistoricalEra { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();

    public List<TimelineNodeDto> Timeline { get; set; } = new();
    public PracticalInfoDto? PracticalInfo { get; set; }

    // Community Articles (SQL + MongoDB Type 2)
    public List<CommunityArticleDto> CommunityArticles { get; set; } = new();
}

public class GeoCoordinatesDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string GeoCode { get; set; } = string.Empty;
    public int Zoom { get; set; } = 15;
}

public class TimelineNodeDto
{
    public string Year { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PracticalInfoDto
{
    public string OpeningHours { get; set; } = string.Empty;
    public string AdmissionFee { get; set; } = string.Empty;
    public string DressCode { get; set; } = string.Empty;
    public string GuidedTours { get; set; } = string.Empty;
}

public class CommunityArticleDto
{
    // SQL Contribution Data
    public string Id { get; set; } = string.Empty; // SQL Contribution ID (Guid string)
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty; 
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
}

public class CreateHeritageDetailDto
{
    public string Title { get; set; } = string.Empty;
    public string HistoricalContext { get; set; } = string.Empty;
    public string ArchitectureDetails { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
}
