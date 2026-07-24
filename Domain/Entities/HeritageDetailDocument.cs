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
    public string Id { get; set; } = string.Empty;

    [BsonElement("locationId")]
    public string LocationId { get; set; } = string.Empty;

    // Fields for ContributionType = 1 (Main Detail)
    [BsonElement("leadQuote")]
    public string? LeadQuote { get; set; }

    [BsonElement("fullDescription")]
    public string? FullDescription { get; set; }

    [BsonElement("quote")]
    public string? Quote { get; set; }

    [BsonElement("galleryImages")]
    public List<string>? GalleryImages { get; set; }

    [BsonElement("highlights")]
    public List<string>? Highlights { get; set; }

    [BsonElement("historicalEra")]
    public string? HistoricalEra { get; set; }

    [BsonElement("tags")]
    public List<string>? Tags { get; set; }

    [BsonElement("timeline")]
    public List<TimelineNode>? Timeline { get; set; }

    [BsonElement("practicalInfo")]
    public PracticalInfo? PracticalInfo { get; set; }

    // Fields for ContributionType = 2 (Community Article)
    [BsonElement("contentHtml")]
    public string? ContentHtml { get; set; }

    [BsonElement("blocks")]
    public List<BsonDocument>? Blocks { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TimelineNode
{
    [BsonElement("year")]
    public string Year { get; set; } = string.Empty;
    
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;
    
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
}

public class PracticalInfo
{
    [BsonElement("openingHours")]
    public string OpeningHours { get; set; } = string.Empty;
    
    [BsonElement("admissionFee")]
    public string AdmissionFee { get; set; } = string.Empty;
    
    [BsonElement("dressCode")]
    public string DressCode { get; set; } = string.Empty;
    
    [BsonElement("guidedTours")]
    public string GuidedTours { get; set; } = string.Empty;
}
