using Firesight.Api.Endpoints;
using Firesight.Application;
using Firesight.Infrastructure;
using Firesight.Infrastructure.Persistence;
using Firesight.Mcp.Tools;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<WildfireTools>();

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

app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapWildfireEndpoints();
app.MapMcp("/mcp");

app.Run();
