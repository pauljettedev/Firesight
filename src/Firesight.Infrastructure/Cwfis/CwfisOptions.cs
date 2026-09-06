namespace Firesight.Infrastructure.Cwfis;

public sealed class CwfisOptions
{
    public const string SectionName = "Cwfis";

    public string BaseUrl { get; set; } =
        "https://geoserver.cwfif.nrcan.gc.ca/geoserver/ows";

    public string ActiveFiresLayer { get; set; } =
        "public:cwfif_national_activefires";

    public int PageSize { get; set; } = 1000;
}
