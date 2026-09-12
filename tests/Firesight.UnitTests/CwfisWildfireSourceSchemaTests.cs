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
          "numberReturned": 1,
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

        using var client = new HttpClient(new SequenceJsonHandler(json));
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
    public async Task TreatsNegativeFireSizeAsNotReported()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 1,
          "numberReturned": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.1",
            "geometry": { "type": "Point", "coordinates": [-79.12345, 46.54321] },
            "properties": {
              "agency_code": "ON",
              "national_fire_id": "2026_ON_NIP_FIRE_110",
              "fire_size": -1,
              "stage_of_control_status": "OC"
            }
          }]
        }
        """;

        using var client = new HttpClient(new SequenceJsonHandler(json));
        var result = await CreateSource(client).GetActiveWildfiresAsync();

        var fire = Assert.Single(result.Records);
        Assert.Null(fire.AreaHectares);
    }

    [Fact]
    public async Task RejectsFeatureWithoutNationalFireId()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 1,
          "numberReturned": 1,
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

        using var client = new HttpClient(new SequenceJsonHandler(json));
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
          "numberReturned": 1,
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

        using var client = new HttpClient(new SequenceJsonHandler(json));
        var result = await CreateSource(client).GetActiveWildfiresAsync();

        Assert.Empty(result.Records);
        Assert.Equal(1, result.Rejected);
    }

    [Fact]
    public async Task ActiveFireRequest_UsesCurrentSnapshotFilterAndStableSort()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "numberMatched": 0,
          "numberReturned": 0,
          "features": []
        }
        """;

        var handler = new SequenceJsonHandler(json);
        using var client = new HttpClient(handler);

        await CreateSource(client).GetActiveWildfiresAsync();

        var requestUri = Assert.Single(handler.RequestUris);
        var query = Uri.UnescapeDataString(requestUri.Query);

        Assert.Contains("count=100", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("startIndex=0", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sortBy=national_fire_id", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("record_start <= '", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("record_end > '", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TIME=", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActiveFireRequest_PagesThroughCurrentSnapshotUsingSameFilter()
    {
        const string firstPage = """
        {
          "type": "FeatureCollection",
          "numberMatched": 2,
          "numberReturned": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.1",
            "geometry": { "type": "Point", "coordinates": [-79, 46] },
            "properties": {
              "agency_code": "ON",
              "national_fire_id": "2026_ON_TEST_001",
              "stage_of_control_status": "OC"
            }
          }]
        }
        """;

        const string secondPage = """
        {
          "type": "FeatureCollection",
          "numberMatched": 2,
          "numberReturned": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.2",
            "geometry": { "type": "Point", "coordinates": [-80, 47] },
            "properties": {
              "agency_code": "ON",
              "national_fire_id": "2026_ON_TEST_002",
              "stage_of_control_status": "UC"
            }
          }]
        }
        """;

        var handler = new SequenceJsonHandler(firstPage, secondPage);
        using var client = new HttpClient(handler);

        var result = await CreateSource(client, pageSize: 1).GetActiveWildfiresAsync();

        Assert.Equal(2, result.Received);
        Assert.Equal(2, result.Accepted);
        Assert.Equal(0, result.Rejected);
        Assert.Equal(2, result.Records.Count);
        Assert.Equal(2, handler.RequestUris.Count);

        var firstQuery = Uri.UnescapeDataString(handler.RequestUris[0].Query);
        var secondQuery = Uri.UnescapeDataString(handler.RequestUris[1].Query);

        Assert.Contains("startIndex=0", firstQuery, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("startIndex=1", secondQuery, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            GetQueryValue(firstQuery, "cql_filter"),
            GetQueryValue(secondQuery, "cql_filter"));
    }

    [Fact]
    public async Task ActiveFireRequest_ThrowsWhenPagingEndsBeforeNumberMatched()
    {
        const string firstPage = """
        {
          "type": "FeatureCollection",
          "numberMatched": 2,
          "numberReturned": 1,
          "features": [{
            "type": "Feature",
            "id": "cwfif_national_activefires.1",
            "geometry": { "type": "Point", "coordinates": [-79, 46] },
            "properties": {
              "agency_code": "ON",
              "national_fire_id": "2026_ON_TEST_001",
              "stage_of_control_status": "OC"
            }
          }]
        }
        """;

        const string emptySecondPage = """
        {
          "type": "FeatureCollection",
          "numberMatched": 2,
          "numberReturned": 0,
          "features": []
        }
        """;

        var handler = new SequenceJsonHandler(firstPage, emptySecondPage);
        using var client = new HttpClient(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSource(client, pageSize: 1).GetActiveWildfiresAsync());

        Assert.Contains("1 of 2 matched records", exception.Message);
        Assert.Equal(2, handler.RequestUris.Count);
    }

    private static string GetQueryValue(string query, string name)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Single(parts => string.Equals(parts[0], name, StringComparison.OrdinalIgnoreCase))[1];
    }

    private static CwfisWildfireSource CreateSource(
        HttpClient client,
        ILogger<CwfisWildfireSource>? logger = null,
        int pageSize = 100) =>
        new(
            client,
            Options.Create(new CwfisOptions
            {
                BaseUrl = "https://example.invalid/ows",
                ActiveFiresLayer = "public:cwfif_national_activefires",
                PageSize = pageSize
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

    private sealed class SequenceJsonHandler(params string[] responses) : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new(responses);

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No test response was configured for this request.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            });
        }
    }
}
