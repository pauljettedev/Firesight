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
    // A hosted deployment (container platform, reverse proxy, load balancer)
    // sits in front of this app, so without this, every request's
    // RemoteIpAddress would be the proxy's IP, not the visitor's — collapsing
    // the per-client rate limit below into one shared budget for everyone.
    // KnownIPNetworks/KnownProxies are cleared because the exact proxy address
    // isn't fixed ahead of time on most hosting platforms; this is a standard
    // trade-off for a cost-protection limiter on a low-stakes public demo,
    // not something guarding anything security-sensitive.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // AI calls cost real money per request, unlike the rest of the API — cap
    // per-IP usage so a single client can't run up the Claude API bill on this
    // public demo.
    options.AddPolicy("AskFiresight", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
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
}

app.MapAskFiresightEndpoints();
app.MapHealthEndpoints();
app.MapLocationEndpoints();
app.MapWildfireEndpoints();
app.MapMcp("/mcp");

app.Run();
