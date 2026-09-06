using Firesight.Application.Wildfires;
using Microsoft.Extensions.DependencyInjection;

namespace Firesight.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWildfireService, WildfireService>();
        return services;
    }
}
