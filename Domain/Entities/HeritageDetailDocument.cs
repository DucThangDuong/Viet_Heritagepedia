using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;

[BsonIgnoreExtraElements]
public class HeritageDetailDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = string.Empty;

    [BsonElement("locationId")]
    public string LocationId { get; private set; } = string.Empty;

    // Fields for ContributionType = 1 (Main Detail)
    [BsonElement("leadQuote")]
    public string? LeadQuote { get; private set; }

    [BsonElement("fullDescription")]
    public string? FullDescription { get; private set; }

    [BsonElement("quote")]
    public string? Quote { get; private set; }

    [BsonElement("galleryImages")]
    public List<string>? GalleryImages { get; private set; }

    [BsonElement("highlights")]
    public List<string>? Highlights { get; private set; }

    [BsonElement("historicalEra")]
    public string? HistoricalEra { get; private set; }

    [BsonElement("tags")]
    public List<string>? Tags { get; private set; }

    [BsonElement("timeline")]
    public List<TimelineNode>? Timeline { get; private set; }

    [BsonElement("practicalInfo")]
    public PracticalInfo? PracticalInfo { get; private set; }

    [BsonElement("contentHtml")]
    public string? ContentHtml { get; private set; }

    [BsonElement("blocks")]
    public List<BsonDocument>? Blocks { get; private set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public HeritageDetailDocument() { } // Default constructor for BSON serialization

    public static HeritageDetailDocument CreateCommunityArticle(string locationId, string contentHtml)
    {
        return new HeritageDetailDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            LocationId = locationId,
            ContentHtml = contentHtml,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateCommunityArticle(string contentHtml)
    {
        ContentHtml = contentHtml;
        UpdatedAt = DateTime.UtcNow;
    }

    public static HeritageDetailDocument CreateMainArticle(string locationId, string fullDescription)
    {
        return new HeritageDetailDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            LocationId = locationId,
            FullDescription = fullDescription,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateMainArticle(string fullDescription)
    {
        FullDescription = fullDescription;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class TimelineNode
{
    [BsonElement("year")]
    public string Year { get; private set; } = string.Empty;
    
    [BsonElement("title")]
    public string Title { get; private set; } = string.Empty;
    
    [BsonElement("description")]
    public string Description { get; private set; } = string.Empty;
}

public class PracticalInfo
{
    [BsonElement("openingHours")]
    public string OpeningHours { get; private set; } = string.Empty;
    
    [BsonElement("admissionFee")]
    public string AdmissionFee { get; private set; } = string.Empty;
    
    [BsonElement("dressCode")]
    public string DressCode { get; private set; } = string.Empty;
    
    [BsonElement("guidedTours")]
    public string GuidedTours { get; private set; } = string.Empty;
}
