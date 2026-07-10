using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>Coins &amp; tokens: listings, movers, detail, and history.</summary>
public sealed class Coins : ResourceBase
{
    internal Coins(HttpTransport http) : base(http) { }

    /// <summary>List coins (paginated). Params: page, per_page, type, status, search, min_price,
    /// max_price, min_marketcap, max_marketcap, min_volume, max_volume, ids, slugs, symbols, sort, interval.</summary>
    public Task<Page> ListAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/coins", p, ct);

    public Task<Page> RecentlyAddedAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/coins/recently-added", p, ct);

    public Task<Page> GainersAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/coins/gainers", p, ct);

    public Task<Page> LosersAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/coins/losers", p, ct);

    /// <summary>Trending coins. Params: limit.</summary>
    public Task<JsonNode?> TrendingAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/coins/trending", p, ct);

    /// <summary>Get a single coin by slug. Params: locale.</summary>
    public Task<JsonNode?> GetAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}", p, ct);

    /// <summary>OHLC candle history. Params: interval, start, end, limit.</summary>
    public Task<JsonNode?> HistoryAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/history", p, ct);

    /// <summary>Marketcap history. Params: interval, start, end, limit.</summary>
    public Task<JsonNode?> MarketcapHistoryAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/marketcap-history", p, ct);

    /// <summary>Price sparkline. Params: period.</summary>
    public Task<JsonNode?> SparklineAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/sparkline", p, ct);
}

/// <summary>Lightweight price lookups.</summary>
public sealed class Prices : ResourceBase
{
    internal Prices(HttpTransport http) : base(http) { }

    /// <summary>Get prices for many coins at once. Params: ids, slugs, symbols, convert.</summary>
    public Task<JsonNode?> ListAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/prices", p, ct);

    /// <summary>Get the price of a single coin. Params: convert.</summary>
    public Task<JsonNode?> GetAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/prices/{Enc(slug)}", p, ct);

    /// <summary>Historical price of a coin on a date. Params: slug, date.</summary>
    public Task<JsonNode?> HistoricalAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/historical-price", p, ct);
}

/// <summary>Asset conversion and reference rates.</summary>
public sealed class Conversion : ResourceBase
{
    internal Conversion(HttpTransport http) : base(http) { }

    /// <summary>Convert an amount between two assets. Params: from, to, amount.</summary>
    public Task<JsonNode?> ConvertAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/convert", p, ct);

    /// <summary>List supported fiat currencies.</summary>
    public Task<JsonNode?> FiatsAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/fiats", null, ct);

    /// <summary>List conversion rates.</summary>
    public Task<JsonNode?> RatesAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/rates", null, ct);
}
