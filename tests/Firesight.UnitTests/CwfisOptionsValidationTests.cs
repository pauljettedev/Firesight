using Firesight.Infrastructure;
using Firesight.Infrastructure.Cwfis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Firesight.UnitTests;

public sealed class CwfisOptionsValidationTests
{
    [Fact]
    public void ValidConfiguration_PassesValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Cwfis:BaseUrl"] = "https://example.test/geoserver/ows",
            ["Cwfis:ActiveFiresLayer"] = "public:cwfif_national_activefires",
            ["Cwfis:PageSize"] = "1000"
        });

        var options = provider.GetRequiredService<IOptions<CwfisOptions>>().Value;

        Assert.Equal("https://example.test/geoserver/ows", options.BaseUrl);
        Assert.Equal("public:cwfif_national_activefires", options.ActiveFiresLayer);
        Assert.Equal(1000, options.PageSize);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.test/ows")]
    public void InvalidBaseUrl_FailsValidation(string baseUrl)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Cwfis:BaseUrl"] = baseUrl
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CwfisOptions>>().Value);
    }

    [Fact]
    public void BlankActiveFiresLayer_FailsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Cwfis:ActiveFiresLayer"] = " "
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CwfisOptions>>().Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void InvalidPageSize_FailsValidation(int pageSize)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Cwfis:PageSize"] = pageSize.ToString()
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CwfisOptions>>().Value);
    }

    private static ServiceProvider BuildProvider(
        Dictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:FiresightDatabase"] =
                "Host=localhost;Database=firesight;Username=test;Password=test",
            ["Cwfis:BaseUrl"] = "https://example.test/geoserver/ows",
            ["Cwfis:ActiveFiresLayer"] = "public:cwfif_national_activefires",
            ["Cwfis:PageSize"] = "1000"
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
