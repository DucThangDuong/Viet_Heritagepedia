using System;

namespace Application.DTOs;

public class NearbyLocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Category { get; set; }
    public int PendingContributionsCount { get; set; }
}

public class GetNearbyLocationsRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInKm { get; set; } = 5.0;
}
