# Bitculator Data API — .NET SDK

[bitculator.com](https://bitculator.com) · [API documentation](https://bitculator.com/en/documentation/api/v1) · [Get an API key](https://bitculator.com/user/developer/api)
[NuGet — `Bitculator`](https://www.nuget.org/packages/Bitculator) · [GitHub](https://github.com/Bitculator/bitculator-dotnet-sdk)

The official .NET SDK for the [Bitculator Data API](https://bitculator.com).
Async, `HttpClient` + in-box `System.Text.Json` (zero NuGet dependencies), .NET 8+.

> Keep your API key server-side. It is a Bearer token with the `data-api` ability
> and is not meant for client-side embedding.

## Install

```bash
dotnet add package Bitculator
```

## Quick start

```csharp
using Bitculator;
using System.Text.Json.Nodes;

var client = new BitculatorClient(Environment.GetEnvironmentVariable("BITCULATOR_API_KEY")!);

// Single resource — the { "data": ... } envelope is unwrapped; rows are JsonNodes.
JsonNode? bitcoin = await client.Coins.GetAsync("bitcoin");
Console.WriteLine(bitcoin?["price"]?.GetValue<string>()); // "63520.780763913"

// A paginated list.
var page = await client.Coins.ListAsync(Params.Of().Set("per_page", 50).Set("sort", "-marketcap"));
Console.WriteLine($"{page.Meta.Total} {page.Data.Count}");
```

## Prices are decimal strings

Prices, rates, and supplies come back as **strings** (e.g. `"63520.780763913"`)
to preserve full precision. Read them with `.GetValue<string>()` (and parse with
`decimal` if you need math) — never `.GetValue<double>()`, which loses precision.

## Typed models

Rows are `JsonNode`s. Deserialize into your own type when you want:

```csharp
record Coin(string Slug, string Symbol, string Price);
var node = await client.Coins.GetAsync("bitcoin");
Coin? btc = node.Deserialize<Coin>();
```

## Pagination

Paginated methods return a `Page`. Read one page, or async-enumerate them all:

```csharp
// One page at a time.
Page? page = await client.Exchanges.ListAsync(Params.Of().Set("per_page", 100));
while (page != null)
{
    foreach (var exchange in page.Data) Console.WriteLine(exchange?["name"]);
    page = await page.NextPageAsync();
}

// Or auto-paginate across every page.
var first = await client.Coins.ListAsync(Params.Of().Set("sort", "-marketcap"));
await foreach (var coin in first.AutoPagingAsync())
{
    Console.WriteLine(coin?["symbol"]);
}
```

## Errors

HTTP failures throw `ApiException` (extends `BitculatorException`) carrying the
`{ error: { code, message, details } }` envelope; branch on the `IsXxx` predicates.

```csharp
try
{
    await client.Coins.ListAsync(Params.Of().Set("per_page", 9999)); // over the plan cap -> 422
}
catch (ApiException e) when (e.IsValidation) { Console.WriteLine($"{e.Code} {e.Details}"); }
catch (ApiException e) when (e.IsRateLimited) { Console.WriteLine($"retry after {e.RetryAfter}"); }
catch (ApiException e) { Console.WriteLine($"{e.Status} {e.Message}"); }
```

`IsAuth` (401) · `IsPermission` (403) · `IsNotFound` (404) · `IsValidation` (422) ·
`IsRateLimited` (429) · `IsServer` (5xx). Timeouts throw `BitculatorTimeoutException`;
network failures throw `BitculatorConnectionException`.

## Quota

Every response carries `X-Quota-*` headers, surfaced on the client after each call:

```csharp
await client.Coins.ListAsync();
Quota? q = client.Quota; // { Limit, Remaining, Reset, Raw }
```

## Configuration

```csharp
var client = new BitculatorClient("your-api-key", new BitculatorClientOptions
{
    BaseUrl = "https://bitculator.com",     // default
    Timeout = TimeSpan.FromSeconds(30),     // default
    MaxRetries = 2,                         // auto-retry 429/5xx/network; 0 to disable
});
```

Every method takes an optional `CancellationToken`.

## Resources

`Coins` · `Prices` · `Markets` · `Exchanges` · `Wallets` · `GlobalMarket` ·
`Sentiment` · `Indicators` · `Liquidations` · `Conversion` · `Calculators` ·
`Editorial` · `Alarms` · `Webhooks` · `Meta`

Full endpoint reference: <https://bitculator.com/en/documentation/api/v1>.

## License

MIT
