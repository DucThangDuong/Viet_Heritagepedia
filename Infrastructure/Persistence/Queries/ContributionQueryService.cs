using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using Dapper;
using Domain.Entities;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Persistence.Queries;

public class ContributionQueryService : IContributionQueryService
{
    private readonly VietHeritagePediaContext _sqlContext;
    private readonly IMongoCollection<HeritageDetailDocument> _collection;

    public ContributionQueryService(
        VietHeritagePediaContext sqlContext,
        Infrastructure.Persistence.MongoDb.MongoDbContext mongoContext)
    {
        _sqlContext = sqlContext;
        _collection = mongoContext.GetCollection<HeritageDetailDocument>();
    }

    public async Task<IEnumerable<MyContributionListDto>> GetMyContributionsAsync(Guid authorId, int pageIndex = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        const string sql = @"
            SELECT c.Id, c.LocationId, l.Name as LocationName, c.Title, c.Summary, c.WorkflowState, c.CreatedAt, c.UpdatedAt
            FROM Contributions c
            LEFT JOIN Locations l ON c.LocationId = l.Id
            WHERE c.AuthorId = @AuthorId
            ORDER BY c.UpdatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var offset = (pageIndex - 1) * pageSize;
        return await connection.QueryAsync<MyContributionListDto>(sql, new { AuthorId = authorId, Offset = offset, PageSize = pageSize });
    }

    public async Task<MyContributionDetailDto?> GetMyContributionDetailAsync(Guid authorId, Guid contributionId, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        const string sql = @"
            SELECT c.Id, c.LocationId, l.Name as LocationName, c.Title, c.Summary, c.WorkflowState, c.CreatedAt, c.UpdatedAt, c.NoSqlDocumentId
            FROM Contributions c
            LEFT JOIN Locations l ON c.LocationId = l.Id
            WHERE c.Id = @ContributionId AND c.AuthorId = @AuthorId";

        var row = await connection.QueryFirstOrDefaultAsync(sql, new { ContributionId = contributionId, AuthorId = authorId });

        if (row == null) return null;

        var dto = new MyContributionDetailDto
        {
            Id = row.Id,
            LocationId = row.LocationId,
            LocationName = row.LocationName ?? string.Empty,
            Title = row.Title ?? string.Empty,
            Summary = row.Summary ?? string.Empty,
            WorkflowState = row.WorkflowState,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };

        string noSqlDocumentId = row.NoSqlDocumentId ?? string.Empty;
        if (!string.IsNullOrEmpty(noSqlDocumentId))
        {
            var filter = Builders<HeritageDetailDocument>.Filter.Eq("_id", ObjectId.Parse(noSqlDocumentId));
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            if (doc != null)
            {
                dto.ContentHtml = doc.ContentHtml ?? string.Empty;
                if (doc.Blocks != null)
                {
                    dto.Blocks = doc.Blocks.Select(b => MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(b)).ToList();
                }
            }
        }

        return dto;
    }
    // Admin 
    public async Task<IEnumerable<MyContributionListDto>> GetAllContributionsAsync(int pageIndex = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        const string sql = @"
            SELECT c.Id, c.LocationId, l.Name as LocationName, c.Title, c.Summary, c.WorkflowState, c.CreatedAt, c.UpdatedAt
            FROM Contributions c
            LEFT JOIN Locations l ON c.LocationId = l.Id
            ORDER BY c.UpdatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var offset = (pageIndex - 1) * pageSize;
        return await connection.QueryAsync<MyContributionListDto>(sql, new { Offset = offset, PageSize = pageSize });
    }

    public async Task<MyContributionDetailDto?> GetContributionDetailAsync(Guid contributionId, CancellationToken ct = default)
    {
        var connection = _sqlContext.Database.GetDbConnection();

        const string sql = @"
            SELECT c.Id, c.LocationId, l.Name as LocationName, c.Title, c.Summary, c.WorkflowState, c.CreatedAt, c.UpdatedAt, c.NoSqlDocumentId
            FROM Contributions c
            LEFT JOIN Locations l ON c.LocationId = l.Id
            WHERE c.Id = @ContributionId";

        var row = await connection.QueryFirstOrDefaultAsync(sql, new { ContributionId = contributionId });

        if (row == null) return null;

        var dto = new MyContributionDetailDto
        {
            Id = row.Id,
            LocationId = row.LocationId,
            LocationName = row.LocationName ?? string.Empty,
            Title = row.Title ?? string.Empty,
            Summary = row.Summary ?? string.Empty,
            WorkflowState = row.WorkflowState,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };

        string noSqlDocumentId = row.NoSqlDocumentId ?? string.Empty;
        if (!string.IsNullOrEmpty(noSqlDocumentId))
        {
            var filter = Builders<HeritageDetailDocument>.Filter.Eq("_id", ObjectId.Parse(noSqlDocumentId));
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            if (doc != null)
            {
                dto.ContentHtml = doc.ContentHtml ?? string.Empty;
                if (doc.Blocks != null)
                {
                    dto.Blocks = doc.Blocks.Select(b => MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(b)).ToList();
                }
            }
        }

        return dto;
    }
}
