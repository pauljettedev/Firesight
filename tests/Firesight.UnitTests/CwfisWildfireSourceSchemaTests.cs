using System.Net;
using System.Text;
using Firesight.Infrastructure.Cwfis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Firesight.UnitTests;

public sealed class CwfisWildfireSourceSchemaTests
{
    [Fact]
    public async Task MapsCurrentCwfis2ActiveFireFields()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.23660887",
            "geometry": { "type": "Point", "coordinates": [-79.12345, 46.54321] },
            "properties": {
              "id": 23660887,
              "agency_code": "ON",
              "national_fire_id": "2026_ON_NIP_FIRE_109",
              "agency_fire_id": "NIP_FIRE_109",
              "fire_size": 123.45,
              "stage_of_control_status": "UC",
              "status_date": "2026-09-05T18:30:00Z",
              "latitude": 46.54321,
              "longitude": -79.12345,
              "record_start": "2026-09-05T00:00:00Z",
              "record_end": "2026-12-31T23:59:59.999Z"
            }
          }]
        }
        """;

        using var client = new HttpClient(new StaticJsonHandler(json));
        var result = await CreateSource(client).GetActiveWildfiresAsync();

        Assert.Equal(1, result.Received);
        Assert.Equal(1, result.Accepted);
        Assert.Equal(0, result.Rejected);

        var fire = Assert.Single(result.Records);
        Assert.Equal("2026_ON_NIP_FIRE_109", fire.ExternalId);
        Assert.Equal("ON", fire.Agency);
        Assert.Null(fire.Name);
        Assert.Null(fire.StartDate);
        Assert.Equal(123.45, fire.AreaHectares);
        Assert.Equal("UC", fire.Status);
        Assert.Equal(
            new DateTime(2026, 9, 5, 18, 30, 0, DateTimeKind.Utc),
            fire.StatusDateUtc);
    }

    [Fact]
    public async Task RejectsFeatureWithoutNationalFireId()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.23660887",
            "geometry": { "type": "Point", "coordinates": [-79.12345, 46.54321] },
            "properties": {
              "id": 23660887,
              "agency_code": "ON",
              "agency_fire_id": "NIP_FIRE_109",
              "stage_of_control_status": "OC"
            }
          }]
        }
        """;

        using var client = new HttpClient(new StaticJsonHandler(json));
        var logger = new TestLogger<CwfisWildfireSource>();
        var result = await CreateSource(client, logger).GetActiveWildfiresAsync();

        Assert.Equal(1, result.Received);
        Assert.Equal(0, result.Accepted);
        Assert.Equal(1, result.Rejected);
        Assert.Empty(result.Records);
        Assert.Contains(
            logger.Messages,
            message =>
                message.Contains("cwfif_national_activefires.23660887") &&
                message.Contains("missing national_fire_id"));
    }

    [Fact]
    public async Task DoesNotUseWfsNumericOrAgencyIdAsFallback()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.999",
            "geometry": { "type": "Point", "coordinates": [-79, 46] },
            "properties": {
              "id": 999,
              "agency_code": "ON",
              "agency_fire_id": "ON999",
              "stage_of_control_status": "OC"
            }
          }]
        }
        """;

        using var client = new HttpClient(new StaticJsonHandler(json));
        var result = await CreateSource(client).GetActiveWildfiresAsync();

        Assert.Empty(result.Records);
        Assert.Equal(1, result.Rejected);
    }


    [Fact]
    public async Task ActiveFireRequest_DoesNotUseStartIndexPaging()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 0,
          "features": []
        }
        """;

        var handler = new StaticJsonHandler(json);
        using var client = new HttpClient(handler);

        await CreateSource(client).GetActiveWildfiresAsync();

        Assert.NotNull(handler.RequestUri);
        Assert.DoesNotContain("startIndex", handler.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count=100", handler.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    private static CwfisWildfireSource CreateSource(
        HttpClient client,
        ILogger<CwfisWildfireSource>? logger = null) =>
        new(
            client,
            Options.Create(new CwfisOptions
            {
                BaseUrl = "https://example.invalid/ows",
                ActiveFiresLayer = "public:cwfif_national_activefires",
                PageSize = 100
            }),
            logger ?? new TestLogger<CwfisWildfireSource>());

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class StaticJsonHandler(string json) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
