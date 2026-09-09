namespace Firesight.Infrastructure.Nominatim;

public sealed class NominatimOptions
{
    public const string SectionName = "Nominatim";

    public string BaseUrl { get; init; } = string.Empty;
}
