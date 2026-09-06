using NetTopologySuite.Geometries;

namespace Firesight.Domain.Wildfires;

public class Wildfire
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Agency { get; set; } = string.Empty;

    public string? Name { get; set; }

    public Point Location { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public double? AreaHectares { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? StatusDateUtc { get; set; }

    public DateTime LastSeenInFeedUtc { get; set; }

    public DateTime? FirstObservedExtinguishedUtc { get; set; }
}
