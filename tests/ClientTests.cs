using System;
using System.Threading.Tasks;
using Xunit;

namespace Bitculator.Tests;

public class ClientTests
{
    [Fact]
    public void RequiresAnApiKey()
    {
        Assert.Throws<BitculatorException>(() => new BitculatorClient(""));
    }

    [Fact]
    public void ExposesAll15ResourceGroups()
    {
        using var client = new BitculatorClient("k");
        var groups = new object[]
        {
            client.Coins, client.Prices, client.Markets, client.Exchanges, client.Wallets,
            client.GlobalMarket, client.Sentiment, client.Indicators, client.Liquidations,
            client.Conversion, client.Calculators, client.Editorial, client.Alarms,
            client.Webhooks, client.Meta,
        };
        Assert.Equal(15, groups.Length);
        Assert.All(groups, Assert.NotNull);
    }

    [Fact]
    public async Task PercentEncodesDynamicPathSegments()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = TestClient.Create(handler);
        await client.Coins.GetAsync("weird/slug name");
        Assert.Equal("/api/v1/coins/weird%2Fslug%20name", handler.Calls[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task BaseUrlTrimsTrailingSlashes()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = new BitculatorClient("k", new BitculatorClientOptions
        {
            BaseUrl = "https://api.test///",
            Handler = handler,
        });
        await client.Coins.GetAsync("bitcoin");
        Assert.Equal("/api/v1/coins/bitcoin", handler.Calls[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task DefaultUserAgentCarriesTheVersion()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = new BitculatorClient("k", new BitculatorClientOptions
        {
            BaseUrl = "https://api.test",
            Handler = handler,
        });
        await client.Coins.GetAsync("bitcoin");
        var userAgent = handler.Calls[0].Header("user-agent");
        Assert.NotNull(userAgent);
        Assert.StartsWith("bitculator-dotnet/", userAgent);
        Assert.Contains(BitculatorClient.Version, userAgent);
    }
}
