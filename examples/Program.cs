// Illustrative example (not part of the library build). Copy into your own project.
// Run with: BITCULATOR_API_KEY=... dotnet run
using System;
using System.Text.Json.Nodes;
using Bitculator;

var client = new BitculatorClient(
    Environment.GetEnvironmentVariable("BITCULATOR_API_KEY") ?? "",
    new BitculatorClientOptions
    {
        BaseUrl = Environment.GetEnvironmentVariable("BITCULATOR_BASE_URL") ?? "https://bitculator.com",
    });

try
{
    // Single resource — the { "data": ... } envelope is unwrapped; rows are JsonNodes.
    JsonNode? bitcoin = await client.Coins.GetAsync("bitcoin");
    Console.WriteLine($"BTC price: {bitcoin?["price"]?.GetValue<string>()}"); // decimal string, never rounded

    // Top 5 coins by marketcap.
    var page = await client.Coins.ListAsync(Params.Of().Set("per_page", 5).Set("sort", "-marketcap"));
    foreach (var coin in page.Data)
    {
        Console.WriteLine($"#{coin?["rank"]} {coin?["symbol"]} {coin?["price"]}");
    }

    // Auto-paginate the first ~40 gainers.
    int seen = 0;
    await foreach (var coin in (await client.Coins.GainersAsync(Params.Of().Set("per_page", 20))).AutoPagingAsync())
    {
        Console.WriteLine($"gainer: {coin?["symbol"]}");
        if (++seen >= 40)
        {
            break;
        }
    }

    Console.WriteLine($"quota: {client.Quota}");
}
catch (ApiException e)
{
    Console.Error.WriteLine($"API {e.Status}: {e.Code} {e.Message}");
}
