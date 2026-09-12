using Anthropic;
using Firesight.Application.AskFiresight;
using Firesight.Application.Locations;
using Firesight.Application.Wildfires;
using Firesight.Infrastructure.Claude;
using Firesight.Infrastructure.Cwfis;
using Firesight.Infrastructure.HostedServices;
using Firesight.Infrastructure.Nominatim;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Firesight.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FiresightDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'FiresightDatabase' was not found.");

        services.AddDbContext<FiresightDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

        services.AddOptions<CwfisOptions>()
            .Bind(configuration.GetSection(CwfisOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "Cwfis:BaseUrl must be a valid absolute HTTP or HTTPS URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ActiveFiresLayer),
                "Cwfis:ActiveFiresLayer is required.")
            .Validate(
                options => options.PageSize is >= 1 and <= 10000,
                "Cwfis:PageSize must be between 1 and 10000.")
            .ValidateOnStart();

        services.AddOptions<NominatimOptions>()
            .Bind(configuration.GetSection(NominatimOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "Nominatim:BaseUrl must be a valid absolute HTTP or HTTPS URL.")
            .ValidateOnStart();

        services.AddOptions<ClaudeOptions>()
            .Bind(configuration.GetSection(ClaudeOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Claude:Model is required.")
            .ValidateOnStart();

        services.AddOptions<WildfireRetentionOptions>()
            .Bind(configuration.GetSection(WildfireRetentionOptions.SectionName))
            .Validate(
                options => options.ExtinguishedDays >= 0,
                "WildfireRetention:ExtinguishedDays must be zero or greater.")
            .ValidateOnStart();

        services.AddOptions<WildfireFreshnessOptions>()
            .Bind(configuration.GetSection(WildfireFreshnessOptions.SectionName))
            .Validate(
                options => options.StaleAfterHours is >= 1 and <= 720,
                "WildfireFreshness:StaleAfterHours must be between 1 and 720 hours.")
            .ValidateOnStart();

        services.AddHttpClient<IWildfireSource, CwfisWildfireSource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Firesight/0.1");
        });

        services.AddHttpClient<ILocationGeocoder, NominatimLocationGeocoder>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<NominatimOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Firesight/0.1");
                client.DefaultRequestHeaders.Referrer =
                    new Uri("https://github.com/pauljettedev/Firesight");
            });

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<ClaudeOptions>>()
                .Value;

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Claude:ApiKey is required to use Ask Firesight.");
            }

            return new AnthropicClient { ApiKey = options.ApiKey };
        });

        services.AddSingleton<IClaudeMessagesClient, ClaudeMessagesClient>();
        services.AddScoped<IAskFiresightService, ClaudeAskFiresightService>();
        services.AddScoped<IWildfireRepository, WildfireRepository>();
        services.AddScoped<IWildfireSyncStateRepository, WildfireSyncStateRepository>();
        services.AddScoped<DatabaseInitializer>();
        services.AddHostedService<CwfisSyncBackgroundService>();

        return services;
    }
}
