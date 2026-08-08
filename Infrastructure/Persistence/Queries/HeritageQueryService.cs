using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using Dapper;
using Domain.Entities;
using Infrastructure.Persistence.MongoDb;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Infrastructure.Persistence.Queries;

public class HeritageQueryService : IHeritageQueryService
{
    private readonly IMongoCollection<HeritageDetailDocument> _collection;
    private readonly VietHeritagePediaContext _sqlContext;

    public HeritageQueryService(MongoDbContext mongoContext, VietHeritagePediaContext sqlContext)
    {
        _collection = mongoContext.GetCollection<HeritageDetailDocument>("contributions_details");
        _sqlContext = sqlContext;
    }

    private class LocationRow
    {
        public Guid Id { get; set; }
        public string IdStr { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string VietnameseName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsPlainRegion { get; set; }
        public string Image { get; set; } = string.Empty;
        public bool Featured { get; set; }
        public int? UnescoYear { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    private class ContributionRow
    {
        public Guid Id { get; set; }
        public int ContributionType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int LikesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string NoSqlDocumentId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
    }

    public async Task<HeritageDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        // 1. Fetch SQL Location
        const string locationSql = @"
            SELECT Id, Slug as IdStr, Name as Title, VietnameseName, Category, Region, Province, Address as Location, IsPlainRegion, CoverImageUrl as Image, IsFeatured as Featured, UnescoYear
            FROM Locations 
            WHERE Slug = @Slug AND IsActive = 1";

        var locationRow = await connection.QueryFirstOrDefaultAsync<LocationRow>(locationSql, new { Slug = slug });
        if (locationRow == null) return null;

        Guid locationId = locationRow.Id;

        // 2. Fetch SQL Contributions (Type 1 & Type 2)
        const string contributionsSql = @"
            SELECT c.Id, c.ContributionType, c.Title, c.Summary, c.LikesCount, c.CreatedAt, c.NoSqlDocumentId, u.FullName as AuthorName, u.AvatarUrl as AuthorAvatar
            FROM Contributions c
            LEFT JOIN Users u ON c.AuthorId = u.Id
            WHERE c.LocationId = @LocationId AND c.WorkflowState = 3";

        var contributions = (await connection.QueryAsync<ContributionRow>(contributionsSql, new { LocationId = locationId })).ToList();

        // 3. Process Type 1 (Main Content)
        var mainContribution = contributions.FirstOrDefault(c => c.ContributionType == 1);
        HeritageDetailDocument? mainMongoDoc = null;
        if (mainContribution != null && !string.IsNullOrEmpty(mainContribution.NoSqlDocumentId))
        {
            var filter = Builders<HeritageDetailDocument>.Filter.Eq("_id", ObjectId.Parse(mainContribution.NoSqlDocumentId));
            mainMongoDoc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        }

        // 4. Process Type 2 (Community Articles)
        var communityContributions = contributions.Where(c => c.ContributionType == 2).ToList();

        // 5. Build DTO
        var dto = new HeritageDetailDto
        {
            Id = locationRow.IdStr ?? slug,
            LocationId = locationId,
            Title = locationRow.Title ?? string.Empty,
            VietnameseTitle = locationRow.VietnameseName,
            Category = locationRow.Category,
            CategoryName = GetCategoryName(locationRow.Category),
            Location = locationRow.Location ?? string.Empty,
            Region = locationRow.Region,
            UnescoYear = locationRow.UnescoYear,
            Image = locationRow.Image ?? string.Empty,
            Featured = locationRow.Featured,
            Coordinates = locationRow.Lat != null && locationRow.Lng != null ? new GeoCoordinatesDto 
            { 
                Lat = locationRow.Lat.Value, 
                Lng = locationRow.Lng.Value,
                GeoCode = $"{locationRow.Lat.Value}° N, {locationRow.Lng.Value}° E",
                Zoom = 15
            } : null,
            
            ShortDescription = mainContribution?.Summary ?? string.Empty
        };

        if (mainMongoDoc != null)
        {
            dto.LeadQuote = mainMongoDoc.LeadQuote ?? string.Empty;
            dto.FullDescription = mainMongoDoc.FullDescription ?? string.Empty;
            dto.Quote = mainMongoDoc.Quote ?? string.Empty;
            dto.GalleryImages = mainMongoDoc.GalleryImages ?? new List<string>();
            dto.Highlights = mainMongoDoc.Highlights ?? new List<string>();
            dto.HistoricalEra = mainMongoDoc.HistoricalEra ?? string.Empty;
            dto.Tags = mainMongoDoc.Tags ?? new List<string>();
            
            if (mainMongoDoc.Timeline != null)
            {
                dto.Timeline = mainMongoDoc.Timeline.Select(t => new TimelineNodeDto 
                { 
                    Year = t.Year, 
                    Title = t.Title, 
                    Description = t.Description 
                }).ToList();
            }

            if (mainMongoDoc.PracticalInfo != null)
            {
                dto.PracticalInfo = new PracticalInfoDto
                {
                    OpeningHours = mainMongoDoc.PracticalInfo.OpeningHours,
                    AdmissionFee = mainMongoDoc.PracticalInfo.AdmissionFee,
                    DressCode = mainMongoDoc.PracticalInfo.DressCode,
                    GuidedTours = mainMongoDoc.PracticalInfo.GuidedTours
                };
            }
        }

        foreach (var cc in communityContributions)
        {
            var articleDto = new CommunityArticleDto
            {
                Id = cc.Id.ToString(),
                Title = cc.Title ?? string.Empty,
                Summary = cc.Summary ?? string.Empty,
                LikesCount = cc.LikesCount,
                CreatedAt = cc.CreatedAt.ToString("MMM dd, yyyy"),
                AuthorName = string.IsNullOrEmpty(cc.AuthorName) ? "Ẩn danh" : cc.AuthorName,
                AuthorAvatar = cc.AuthorAvatar
            };

            dto.CommunityArticles.Add(articleDto);
        }

        return dto;
    }

    public async Task<UserArticleDto?> GetUserArticleByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        const string sql = @"
            SELECT c.Id, l.Slug as HeritageId, c.Title, u.FullName as AuthorName, u.Role as AuthorRole, u.AvatarUrl as AuthorAvatar, c.CreatedAt, c.Summary, c.LikesCount, c.NoSqlDocumentId 
            FROM Contributions c
            JOIN Locations l ON c.LocationId = l.Id
            LEFT JOIN Users u ON c.AuthorId = u.Id
            WHERE c.Id = @Id AND c.ContributionType = 2 AND c.WorkflowState = 3";

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row == null) return null;

        var dto = new UserArticleDto
        {
            Id = row.Id.ToString(),
            HeritageId = row.HeritageId ?? string.Empty,
            Title = row.Title ?? string.Empty,
            AuthorName = string.IsNullOrEmpty(row.AuthorName) ? "Ẩn danh" : row.AuthorName,
            AuthorRole = string.IsNullOrEmpty(row.AuthorRole) ? "Thành viên" : row.AuthorRole,
            AuthorAvatar = row.AuthorAvatar,
            CreatedAt = ((DateTime)row.CreatedAt).ToString("MMM dd, yyyy"),
            Summary = row.Summary ?? string.Empty,
            LikesCount = row.LikesCount,
            Images = new List<string>()
        };

        string noSqlDocumentId = row.NoSqlDocumentId ?? string.Empty;
        if (!string.IsNullOrEmpty(noSqlDocumentId))
        {
            var filter = Builders<HeritageDetailDocument>.Filter.Eq("_id", ObjectId.Parse(noSqlDocumentId));
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            if (doc != null)
            {
                dto.ContentHtml = doc.ContentHtml;
                if (doc.GalleryImages != null)
                {
                    dto.Images = doc.GalleryImages;
                }
                if (doc.Blocks != null)
                {
                    dto.Blocks = doc.Blocks.Select(b => MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(b)).ToList();
                }
            }
        }

        return dto;
    }

    private string GetCategoryName(string categoryCode)
    {
        return categoryCode switch
        {
            "tangible" => "Di sản Văn hóa Thế giới",
            "intangible" => "Di sản Phi vật thể Thế giới",
            "natural" => "Di sản Thiên nhiên Thế giới",
            "mixed" => "Di sản Hỗn hợp Thế giới",
            _ => "Di sản Văn hóa"
        };
    }
}
