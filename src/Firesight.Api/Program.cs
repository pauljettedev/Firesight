using System.Threading.RateLimiting;
using Firesight.Api.Endpoints;
using Firesight.Api.Errors;
using Firesight.Application;
using Firesight.Infrastructure;
using Firesight.Infrastructure.Persistence;
using Firesight.Mcp;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // When this app runs behind a proxy or load balancer, every request looks
    // like it comes from the proxy's own IP address, not the real visitor's.
    // That would break the rate limits below, since every visitor would end
    // up sharing one limit instead of getting their own.
    // We clear KnownIPNetworks and KnownProxies because we don't know the
    // exact proxy address ahead of time on most hosting platforms.
    // That's fine here, because this limit only exists to control cost on a
    // small demo. It isn't guarding anything sensitive.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // AI calls cost real money per request, unlike the rest of the API.
    // We limit each visitor's IP address so one person can't run up our
    // Claude API bill on this public demo.
    options.AddPolicy("AskFiresight", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Geocoding sends requests to Nominatim's free public service, which has
    // its own usage rules. A visitor hammering this endpoint doesn't cost us
    // money directly. But it can get our server rate-limited or blocked by
    // Nominatim, which would break the search for every user of this app,
    // not just the one making too many requests.
    options.AddPolicy("Geocode", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithFiresightTools();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    // In production this container serves the built React app itself (see the
    // root Dockerfile), so the frontend and API share one origin. In
    // development the Vite dev server serves the frontend instead, and
    // wwwroot doesn't exist.
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapAskFiresightEndpoints();
app.MapHealthEndpoints();
app.MapLocationEndpoints();
app.MapWildfireEndpoints();
app.MapMcp("/mcp");

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
