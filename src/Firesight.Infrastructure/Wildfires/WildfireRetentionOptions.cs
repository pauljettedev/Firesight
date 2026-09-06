namespace Firesight.Infrastructure.Wildfires;

public sealed class WildfireRetentionOptions
{
    public const string SectionName = "WildfireRetention";

    public int ExtinguishedDays { get; init; } = 7;
}
