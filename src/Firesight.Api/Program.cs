using Firesight.Api.Endpoints;
using Firesight.Api.Errors;
using Firesight.Application;
using Firesight.Infrastructure;
using Firesight.Infrastructure.Persistence;
using Firesight.Mcp.Models;
using Firesight.Mcp.Tools;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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
    .WithTools([
        McpServerTool.Create(
            typeof(WildfireTools).GetMethod(nameof(WildfireTools.GetActiveWildfiresAsync))!,
            options: new McpServerToolCreateOptions
            {
                Name = "get_active_wildfires",
                Description = "Returns the current wildfire records available in Firesight.",
                ReadOnly = true,
                Idempotent = true,
                OpenWorld = false,
                UseStructuredContent = true,
                OutputSchema = WildfireResultSchema.ActiveWildfires
            }),
        McpServerTool.Create(
            typeof(WildfireTools).GetMethod(nameof(WildfireTools.GetWildfireByExternalIdAsync))!,
            options: new McpServerToolCreateOptions
            {
                Name = "get_wildfire_by_external_id",
                Description = "Returns a wildfire by its CWFIS national fire identifier, or null when no current record is available.",
                ReadOnly = true,
                Idempotent = true,
                OpenWorld = false,
                UseStructuredContent = true,
                OutputSchema = WildfireResultSchema.WildfireById
            })
    ]);

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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapWildfireEndpoints();
app.MapMcp("/mcp");

app.Run();
