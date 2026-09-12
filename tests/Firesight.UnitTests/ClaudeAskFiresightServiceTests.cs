using System.Text.Json;
using Anthropic.Models.Messages;
using Firesight.Application.Common;
using Firesight.Application.Locations;
using Firesight.Application.Wildfires;
using Firesight.Infrastructure.Claude;
using Microsoft.Extensions.Options;
using Moq;

namespace Firesight.UnitTests;

public sealed class ClaudeAskFiresightServiceTests
{
    [Fact]
    public async Task AskAsync_ReturnsDeterministicCountFromToolData()
    {
        var client = CreateClient(
            CreateFunctionCallResponse(
                "call-1",
                "get_active_wildfires",
                """{"status":null,"responseMode":"count"}"""));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    CreateWildfire("2026_ON_TEST_001"),
                    CreateWildfire("2026_ON_TEST_002")
                ]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            Mock.Of<ILocationGeocoder>());

        var result = await service.AskAsync(
            "How many wildfire records are currently available?");

        Assert.Equal(
            "Firesight shows 2 wildfires in the current dataset.",
            result.Answer);
        Assert.Equal(
            ["get_active_wildfires"],
            result.ToolsUsed);
        Assert.Null(result.MapContext);

        wildfireService.Verify(
            service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(
            messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AskAsync_ReturnsDeterministicStatusCountForLocationSearch()
    {
        var client = CreateClient(
            CreateFunctionCallResponse(
                "call-1",
                "geocode_location",
                """{"query":"Ottawa, ON"}"""),
            CreateFunctionCallResponse(
                "call-2",
                "find_wildfires_near_location",
                """
                {
                  "latitude": 45.4215,
                  "longitude": -75.6972,
                  "radiusKm": 200,
                  "status": "OC",
                  "responseMode": "exists"
                }
                """));

        var geocoder = new Mock<ILocationGeocoder>();
        geocoder
            .Setup(service => service.FindAsync(
                "Ottawa, ON",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GeocodedLocationDto(
                    "Ottawa, Ontario, Canada",
                    45.4215,
                    -75.6972));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.FindWildfiresNearAsync(
                45.4215,
                -75.6972,
                200,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new NearbyWildfireDto(
                        CreateWildfire("2026_QC_TEST_001", "OC"),
                        55),
                    new NearbyWildfireDto(
                        CreateWildfire("2026_ON_TEST_002", "UC"),
                        90)
                ]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            geocoder.Object);

        var result = await service.AskAsync(
            "Are there any out-of-control fires near Ottawa?");

        Assert.Equal(
            "Yes. Firesight shows 1 out-of-control wildfire within 200 km of Ottawa.",
            result.Answer);
        Assert.NotNull(result.MapContext);
        Assert.Equal(200, result.MapContext.RadiusKm);

        client.Verify(
            messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AskAsync_ReturnsMapContextForLocationSearch()
    {
        var client = CreateClient(
            CreateFunctionCallResponse(
                "call-1",
                "geocode_location",
                """{"query":"Kamloops, BC"}"""),
            CreateFunctionCallResponse(
                "call-2",
                "find_wildfires_near_location",
                """
                {
                  "latitude": 50.6758,
                  "longitude": -120.3394,
                  "radiusKm": 200,
                  "status": null,
                  "responseMode": "exists"
                }
                """));

        var geocoder = new Mock<ILocationGeocoder>();
        geocoder
            .Setup(service => service.FindAsync(
                "Kamloops, BC",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GeocodedLocationDto(
                    "Kamloops, Thompson-Nicola Regional District, British Columbia, Canada",
                    50.6758,
                    -120.3394));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.FindWildfiresNearAsync(
                50.6758,
                -120.3394,
                200,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            geocoder.Object);

        var result = await service.AskAsync(
            "Are there fires near Kamloops?");

        Assert.Equal(
            "No. Firesight shows no wildfires within 200 km of Kamloops.",
            result.Answer);
        Assert.NotNull(result.MapContext);
        Assert.Equal(50.6758, result.MapContext.Latitude);
        Assert.Equal(-120.3394, result.MapContext.Longitude);
        Assert.Equal(200, result.MapContext.RadiusKm);
        Assert.Contains("Kamloops", result.MapContext.Label);
    }

    [Fact]
    public async Task AskAsync_DeduplicatesToolsUsed()
    {
        var firstResponse = new Message
        {
            Content =
            [
                new ToolUseBlock
                {
                    ID = "call-1",
                    Name = "get_active_wildfires",
                    Input = ParseInput("""{"status":null,"responseMode":"records"}""")
                },
                new ToolUseBlock
                {
                    ID = "call-2",
                    Name = "get_active_wildfires",
                    Input = ParseInput("""{"status":null,"responseMode":"records"}""")
                }
            ]
        };

        var client = CreateClient(
            firstResponse,
            CreateAnswerResponse("Done."));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            Mock.Of<ILocationGeocoder>());

        var result = await service.AskAsync("Check the current fires.");

        Assert.Equal(
            ["get_active_wildfires"],
            result.ToolsUsed);
    }

    [Fact]
    public async Task AskAsync_ContinuesToolLoopWhenResponseIncludesThinkingBlock()
    {
        var firstResponse = new Message
        {
            Content =
            [
                new ThinkingBlock
                {
                    Thinking = "I should check the current wildfire records.",
                    Signature = "sig-123"
                },
                new ToolUseBlock
                {
                    ID = "call-1",
                    Name = "get_active_wildfires",
                    Input = ParseInput("""{"status":null,"responseMode":"records"}""")
                }
            ]
        };

        var client = CreateClient(
            firstResponse,
            CreateAnswerResponse("Here are the current wildfires."));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            Mock.Of<ILocationGeocoder>());

        var result = await service.AskAsync("List the current wildfires.");

        Assert.Equal("Here are the current wildfires.", result.Answer);
        client.Verify(
            messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AskAsync_ReturnsApplicationValidationErrorToModel()
    {
        var requests = new List<MessageCreateParams>();
        var responses = new Queue<Message>(
        [
            CreateFunctionCallResponse(
                "call-1",
                "find_wildfires_near_location",
                """
                {
                  "latitude": 45.4215,
                  "longitude": -75.6972,
                  "radiusKm": -1,
                  "status": null,
                  "responseMode": "records"
                }
                """),
            CreateAnswerResponse("The requested radius was invalid.")
        ]);

        var client = new Mock<IClaudeMessagesClient>();
        client
            .Setup(messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (MessageCreateParams parameters, CancellationToken _) =>
                {
                    requests.Add(parameters);
                    return Task.FromResult(responses.Dequeue());
                });

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.FindWildfiresNearAsync(
                45.4215,
                -75.6972,
                -1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new ApplicationValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["radiusKm"] =
                            ["Radius must be a finite value greater than 0."]
                    }));

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            Mock.Of<ILocationGeocoder>());

        var result = await service.AskAsync(
            "Find fires near Ottawa using a negative radius.");

        Assert.Equal(
            "The requested radius was invalid.",
            result.Answer);
        Assert.Null(result.MapContext);
        Assert.Equal(2, requests.Count);
    }

    [Fact]
    public async Task AskAsync_ThrowsWhenModelReturnsNoAnswer()
    {
        var client = CreateClient(new Message { Content = [] });

        var service = CreateService(
            client.Object,
            Mock.Of<IWildfireService>(),
            Mock.Of<ILocationGeocoder>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AskAsync("Summarize the wildfire situation."));

        Assert.Equal(
            "Ask Firesight returned no answer.",
            exception.Message);
    }

    [Fact]
    public async Task AskAsync_StopsAfterFourToolCallRounds()
    {
        var client = new Mock<IClaudeMessagesClient>();
        client
            .Setup(messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                CreateFunctionCallResponse(
                    "call-1",
                    "get_active_wildfires",
                    """{"status":null,"responseMode":"records"}"""));

        var wildfireService = new Mock<IWildfireService>();
        wildfireService
            .Setup(service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(
            client.Object,
            wildfireService.Object,
            Mock.Of<ILocationGeocoder>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AskAsync("Keep checking current fires."));

        Assert.Equal(
            "Ask Firesight exceeded the maximum of 4 tool-call rounds.",
            exception.Message);

        client.Verify(
            messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    private static ClaudeAskFiresightService CreateService(
        IClaudeMessagesClient client,
        IWildfireService wildfireService,
        ILocationGeocoder locationGeocoder) =>
        new(
            client,
            Options.Create(
                new ClaudeOptions
                {
                    Model = "test-model"
                }),
            wildfireService,
            locationGeocoder);

    private static Mock<IClaudeMessagesClient> CreateClient(
        params Message[] responses)
    {
        var client = new Mock<IClaudeMessagesClient>();
        var sequence = client.SetupSequence(
            messagesClient => messagesClient.CreateAsync(
                It.IsAny<MessageCreateParams>(),
                It.IsAny<CancellationToken>()));

        foreach (var response in responses)
        {
            sequence.ReturnsAsync(response);
        }

        return client;
    }

    private static Message CreateFunctionCallResponse(
        string callId,
        string toolName,
        string argumentsJson) =>
        new()
        {
            Content =
            [
                new ToolUseBlock
                {
                    ID = callId,
                    Name = toolName,
                    Input = ParseInput(argumentsJson)
                }
            ]
        };

    private static Message CreateAnswerResponse(string answer) =>
        new()
        {
            Content = [new TextBlock { Text = answer }]
        };

    private static Dictionary<string, JsonElement> ParseInput(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static WildfireDto CreateWildfire(
        string externalId,
        string status = "OC") =>
        new(
            Guid.NewGuid(),
            externalId,
            "ON",
            null,
            45.4215,
            -75.6972,
            null,
            100,
            status,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false);
}
