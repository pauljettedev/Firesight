using System.ClientModel;
using System.ClientModel.Primitives;
using Firesight.Application.Common;
using Firesight.Application.Locations;
using Firesight.Application.Wildfires;
using Firesight.Infrastructure.OpenAI;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Responses;

namespace Firesight.UnitTests;

#pragma warning disable OPENAI001

public sealed class OpenAiAskFiresightServiceTests
{
    [Fact]
    public async Task AskAsync_ExecutesToolAndReturnsFinalAnswer()
    {
        var client = CreateClient(
            CreateFunctionCallResponse(
                "call-1",
                "get_active_wildfires",
                "{}"),
            CreateAnswerResponse("There are two current wildfire records."));

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
            "There are two current wildfire records.",
            result.Answer);
        Assert.Equal(
            ["get_active_wildfires"],
            result.ToolsUsed);
        Assert.Null(result.MapContext);

        wildfireService.Verify(
            service => service.GetActiveWildfiresAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
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
                  "radiusKm": 200
                }
                """),
            CreateAnswerResponse("There are fires near Kamloops."));

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

        Assert.NotNull(result.MapContext);
        Assert.Equal(50.6758, result.MapContext.Latitude);
        Assert.Equal(-120.3394, result.MapContext.Longitude);
        Assert.Equal(200, result.MapContext.RadiusKm);
        Assert.Contains("Kamloops", result.MapContext.Label);
    }

    [Fact]
    public async Task AskAsync_DeduplicatesToolsUsed()
    {
        var firstResponse = new ResponseResult();
        firstResponse.OutputItems.Add(
            ResponseItem.CreateFunctionCallItem(
                "call-1",
                "get_active_wildfires",
                BinaryData.FromString("{}")));
        firstResponse.OutputItems.Add(
            ResponseItem.CreateFunctionCallItem(
                "call-2",
                "get_active_wildfires",
                BinaryData.FromString("{}")));

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
    public async Task AskAsync_ReturnsApplicationValidationErrorToModel()
    {
        var requests = new List<CreateResponseOptions>();
        var responses = new Queue<ClientResult<ResponseResult>>(
        [
            ToClientResult(
                CreateFunctionCallResponse(
                    "call-1",
                    "find_wildfires_near_location",
                    """
                    {
                      "latitude": 45.4215,
                      "longitude": -75.6972,
                      "radiusKm": -1
                    }
                    """)),
            ToClientResult(
                CreateAnswerResponse("The requested radius was invalid."))
        ]);

        var client = new Mock<ResponsesClient>();
        client
            .Setup(responseClient => responseClient.CreateResponseAsync(
                It.IsAny<CreateResponseOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (CreateResponseOptions options, CancellationToken _) =>
                {
                    requests.Add(options);
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

        var toolOutput = requests[1]
            .InputItems
            .OfType<FunctionCallOutputResponseItem>()
            .Single();

        Assert.Contains(
            "Firesight rejected the tool arguments.",
            toolOutput.FunctionOutput,
            StringComparison.Ordinal);
        Assert.Contains(
            "radiusKm",
            toolOutput.FunctionOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AskAsync_ThrowsWhenModelReturnsNoAnswer()
    {
        var client = CreateClient(new ResponseResult());

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
        var client = new Mock<ResponsesClient>();
        client
            .Setup(responseClient => responseClient.CreateResponseAsync(
                It.IsAny<CreateResponseOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ToClientResult(
                    CreateFunctionCallResponse(
                        "call-1",
                        "get_active_wildfires",
                        "{}")));

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
            responseClient => responseClient.CreateResponseAsync(
                It.IsAny<CreateResponseOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    private static OpenAiAskFiresightService CreateService(
        ResponsesClient client,
        IWildfireService wildfireService,
        ILocationGeocoder locationGeocoder) =>
        new(
            client,
            Options.Create(
                new OpenAiOptions
                {
                    Model = "test-model"
                }),
            wildfireService,
            locationGeocoder);

    private static Mock<ResponsesClient> CreateClient(
        params ResponseResult[] responses)
    {
        var client = new Mock<ResponsesClient>();
        var sequence = client.SetupSequence(
            responseClient => responseClient.CreateResponseAsync(
                It.IsAny<CreateResponseOptions>(),
                It.IsAny<CancellationToken>()));

        foreach (var response in responses)
        {
            sequence.ReturnsAsync(ToClientResult(response));
        }

        return client;
    }

    private static ResponseResult CreateFunctionCallResponse(
        string callId,
        string functionName,
        string arguments)
    {
        var response = new ResponseResult();
        response.OutputItems.Add(
            ResponseItem.CreateFunctionCallItem(
                callId,
                functionName,
                BinaryData.FromString(arguments)));

        return response;
    }

    private static ResponseResult CreateAnswerResponse(string answer)
    {
        var response = new ResponseResult();
        response.OutputItems.Add(
            ResponseItem.CreateAssistantMessageItem(answer));

        return response;
    }

    private static ClientResult<ResponseResult> ToClientResult(
        ResponseResult response) =>
        ClientResult.FromValue(
            response,
            Mock.Of<PipelineResponse>());

    private static WildfireDto CreateWildfire(string externalId) =>
        new(
            Guid.NewGuid(),
            externalId,
            "ON",
            null,
            45.4215,
            -75.6972,
            null,
            100,
            "OUT",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false);
}

#pragma warning restore OPENAI001
