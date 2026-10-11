using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>Global market aggregates.</summary>
public sealed class GlobalMarket : ResourceBase
{
    internal GlobalMarket(HttpTransport http) : base(http) { }

    /// <summary>Global market snapshot (total marketcap, volume, dominance, ...).</summary>
    public Task<JsonNode?> SnapshotAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/global", null, ct);

    /// <summary>Market heatmap data.</summary>
    public Task<JsonNode?> HeatmapAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/global/heatmap", null, ct);

    /// <summary>Global marketcap or volume history. metric is e.g. "marketcap" or "volume". Params: period.</summary>
    public Task<JsonNode?> HistoryAsync(string metric, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/global/history/{Enc(metric)}", p, ct);
}

/// <summary>Community sentiment and market-wide sentiment indices.</summary>
public sealed class Sentiment : ResourceBase
{
    internal Sentiment(HttpTransport http) : base(http) { }

    /// <summary>Community vote tallies for a coin.</summary>
    public Task<JsonNode?> VotesAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/votes", null, ct);

    /// <summary>Cast a sentiment vote for a coin ("bullish" or "bearish").</summary>
    public Task<JsonNode?> VoteAsync(string slug, string vote, CancellationToken ct = default) =>
        Http.PostAsync($"/api/v1/coins/{Enc(slug)}/votes", new { vote }, ct);

    /// <summary>Fear &amp; Greed index. Params: coin. Each reading carries <c>index</c> (0-100, as shown on Bitculator; use it for display) and the raw <c>score</c> (-100..+100).</summary>
    public Task<JsonNode?> FearGreedAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/sentiment/fear-greed", p, ct);

    /// <summary>Bull / Bear index. Params: coin.</summary>
    public Task<JsonNode?> BullBearAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/sentiment/bull-bear", p, ct);

    /// <summary>Altseason index. Params: days.</summary>
    public Task<JsonNode?> AltseasonAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/sentiment/altseason", p, ct);

    /// <summary>Aggregate market indicator tally.</summary>
    public Task<JsonNode?> IndicatorsAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/sentiment/indicators", null, ct);
}

/// <summary>Liquidation feed and aggregates.</summary>
public sealed class Liquidations : ResourceBase
{
    internal Liquidations(HttpTransport http) : base(http) { }

    /// <summary>Live liquidation feed (paginated). Params: page, per_page, exchange, instrument,
    /// position, order, symbol, min_usd.</summary>
    public Task<Page> FeedAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/liquidations", p, ct);

    /// <summary>Hourly liquidation totals. Params: hours.</summary>
    public Task<JsonNode?> HourlyAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/liquidations/hourly", p, ct);

    /// <summary>Daily liquidation totals. Params: days.</summary>
    public Task<JsonNode?> DailyAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/liquidations/daily", p, ct);

    /// <summary>Today's liquidation summary.</summary>
    public Task<JsonNode?> SummaryAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/liquidations/summary", null, ct);

    /// <summary>Long/short liquidation netflow. Params: days.</summary>
    public Task<JsonNode?> NetflowAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/liquidations/netflow", p, ct);

    /// <summary>Most-liquidated coins. Params: hours, limit.</summary>
    public Task<JsonNode?> CoinsAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/liquidations/coins", p, ct);
}
