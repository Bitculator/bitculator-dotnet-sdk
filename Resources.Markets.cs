using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>Markets, tickers, and trading pairs.</summary>
public sealed class Markets : ResourceBase
{
    internal Markets(HttpTransport http) : base(http) { }

    /// <summary>Markets (tickers) for a coin (paginated). Params: page, per_page, exchange, pair,
    /// instrument, search, min_volume, max_volume, min_change, max_change, sort.</summary>
    public Task<Page> CoinMarketsAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/coins/{Enc(slug)}/markets", p, ct);

    /// <summary>Trading symbols for a coin.</summary>
    public Task<JsonNode?> CoinSymbolsAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/symbols", null, ct);

    /// <summary>List tickers across the market (paginated).</summary>
    public Task<Page> TickersAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/tickers", p, ct);

    /// <summary>List trading pairs (paginated). Params: page, per_page, search, coin, min_volume, max_volume, sort.</summary>
    public Task<Page> PairsAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/pairs", p, ct);

    /// <summary>Get a single trading pair by id.</summary>
    public Task<JsonNode?> PairAsync(object id, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/pairs/{Enc(id)}", null, ct);
}

/// <summary>Exchanges: listings, trust score, history, markets, and listed assets.</summary>
public sealed class Exchanges : ResourceBase
{
    internal Exchanges(HttpTransport http) : base(http) { }

    /// <summary>List exchanges (paginated). Params: page, per_page, type, search, min_pairs, max_pairs,
    /// min_assets, max_assets, min_volume, max_volume, ids, slugs, sort.</summary>
    public Task<Page> ListAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/exchanges", p, ct);

    /// <summary>Get a single exchange by slug.</summary>
    public Task<JsonNode?> GetAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/exchanges/{Enc(slug)}", null, ct);

    /// <summary>Get an exchange's trust score.</summary>
    public Task<JsonNode?> TrustScoreAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/exchanges/{Enc(slug)}/trust-score", null, ct);

    /// <summary>Volume/history for an exchange. Params: interval, start, end, limit.</summary>
    public Task<JsonNode?> HistoryAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/exchanges/{Enc(slug)}/history", p, ct);

    /// <summary>Sparkline for an exchange. Params: period.</summary>
    public Task<JsonNode?> SparklineAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/exchanges/{Enc(slug)}/sparkline", p, ct);

    /// <summary>Markets (tickers) on an exchange (paginated).</summary>
    public Task<Page> TickersAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/exchanges/{Enc(slug)}/tickers", p, ct);

    /// <summary>Coins listed on an exchange (paginated).</summary>
    public Task<Page> AssetsAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/exchanges/{Enc(slug)}/assets", p, ct);
}

/// <summary>Wallets: listings, release timeline, comparison, and supported coins.</summary>
public sealed class Wallets : ResourceBase
{
    internal Wallets(HttpTransport http) : base(http) { }

    /// <summary>List wallets (paginated). Params: page, per_page, search, min_score, max_score, tags, ids, slugs, sort.</summary>
    public Task<Page> ListAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/wallets", p, ct);

    /// <summary>Wallet release timeline (paginated).</summary>
    public Task<Page> TimelineAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/wallets/timeline", p, ct);

    /// <summary>Compare a set of wallets side by side. Params: slugs.</summary>
    public Task<JsonNode?> CompareAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/wallets/compare", p, ct);

    /// <summary>Get a single wallet by slug.</summary>
    public Task<JsonNode?> GetAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/wallets/{Enc(slug)}", null, ct);

    /// <summary>Coins supported by a wallet (paginated).</summary>
    public Task<Page> AssetsAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/wallets/{Enc(slug)}/assets", p, ct);
}
