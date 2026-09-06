namespace Firesight.Infrastructure.Wildfires;

public sealed class WildfireFreshnessOptions
{
    public const string SectionName = "WildfireFreshness";

    public int StaleAfterHours { get; init; } = 48;
}
