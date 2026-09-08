using System.Text.Json;
using Firesight.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Firesight.IntegrationTests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ClientAbortedCancellation_Returns499WithoutProblemDetailsBody()
    {
        using var requestCancellation = new CancellationTokenSource();
        requestCancellation.Cancel();

        await using var services = CreateServices();
        var handler = CreateHandler(services);
        await using var responseBody = new MemoryStream();

        var context = new DefaultHttpContext
        {
            RequestAborted = requestCancellation.Token,
            RequestServices = services
        };
        context.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(requestCancellation.Token),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public async Task NonClientCancellation_Returns500ProblemDetails()
    {
        await using var services = CreateServices();
        var handler = CreateHandler(services);
        await using var responseBody = new MemoryStream();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException("Internal operation was cancelled."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        responseBody.Position = 0;
        using var json = await JsonDocument.ParseAsync(responseBody);
        var root = json.RootElement;

        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal(
            "An unexpected error occurred.",
            root.GetProperty("title").GetString());
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        return services.BuildServiceProvider();
    }

    private static ApiExceptionHandler CreateHandler(IServiceProvider services) =>
        new(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<ApiExceptionHandler>.Instance);
}
