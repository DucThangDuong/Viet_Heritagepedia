using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using Dapper;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class LocationQueryService : ILocationQueryService
{
    private readonly VietHeritagePediaContext _context;

    public LocationQueryService(VietHeritagePediaContext context)
    {
        _context = context;
    }

    public async Task<LocationResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Slug, Name, VietnameseName, Category, Region, Province, Address, IsPlainRegion, CoverImageUrl, IsFeatured, UnescoYear, IsActive, CreatedAt 
            FROM Locations 
            WHERE Id = @Id";

        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        return await connection.QueryFirstOrDefaultAsync<LocationResponseDto>(command);
    }

    public async Task<List<LocationResponseDto>> GetAllLocationsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Slug, Name, VietnameseName, Category, Region, Province, Address, IsPlainRegion, CoverImageUrl, IsFeatured, UnescoYear, IsActive, CreatedAt 
            FROM Locations 
            ORDER BY CreatedAt DESC";

        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var command = new CommandDefinition(sql, cancellationToken: ct);
        var result = await connection.QueryAsync<LocationResponseDto>(command);
        return result.ToList();
    }

    public async Task<List<NearbyLocationDto>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInKm, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                l.Id, 
                l.Name, 
                l.Category,
                COUNT(c.Id) AS PendingContributionsCount
            FROM Locations l
            LEFT JOIN Contributions c ON l.Id = c.LocationId AND c.WorkflowState = 1
            WHERE l.IsActive = 1
            GROUP BY l.Id, l.Name, l.Category";

        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var command = new CommandDefinition(sql, cancellationToken: ct);
        var result = await connection.QueryAsync<NearbyLocationDto>(command);
        return result.ToList();
    }
}
