using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>
/// Technical indicators for a coin. Period indicators (Rsi, StochRsi, Sma, Cci,
/// Mfi, WilliamsR, PriceVolatility, VolumeVolatility) accept: period, start, end,
/// limit. Series indicators (Macd, Obv, Adx, Vwap, Cmf) accept: start, end, limit.
/// </summary>
public sealed class Indicators : ResourceBase
{
    internal Indicators(HttpTransport http) : base(http) { }

    private Task<JsonNode?> Indicator(string slug, string name, Params? p, CancellationToken ct) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/indicators/{name}", p, ct);

    /// <summary>All indicators for a coin at a glance.</summary>
    public Task<JsonNode?> SnapshotAsync(string slug, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/indicators", null, ct);

    public Task<JsonNode?> RsiAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "rsi", p, ct);
    public Task<JsonNode?> StochRsiAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "stoch-rsi", p, ct);
    public Task<JsonNode?> SmaAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "sma", p, ct);
    public Task<JsonNode?> CciAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "cci", p, ct);
    public Task<JsonNode?> MfiAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "mfi", p, ct);
    public Task<JsonNode?> WilliamsRAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "williams-r", p, ct);
    public Task<JsonNode?> PriceVolatilityAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "price-volatility", p, ct);
    public Task<JsonNode?> VolumeVolatilityAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "volume-volatility", p, ct);
    public Task<JsonNode?> MacdAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "macd", p, ct);
    public Task<JsonNode?> ObvAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "obv", p, ct);
    public Task<JsonNode?> AdxAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "adx", p, ct);
    public Task<JsonNode?> VwapAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "vwap", p, ct);
    public Task<JsonNode?> CmfAsync(string slug, Params? p = null, CancellationToken ct = default) => Indicator(slug, "cmf", p, ct);
}

/// <summary>Financial calculators.</summary>
public sealed class Calculators : ResourceBase
{
    internal Calculators(HttpTransport http) : base(http) { }

    /// <summary>Dollar-cost-averaging calculator. Params: slug, amount, interval, start, end, series.</summary>
    public Task<JsonNode?> DcaAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/calculators/dca", p, ct);

    /// <summary>Profit / loss calculator. Params: slug, amount, buy_date, sell_date, buy_fee, sell_fee.</summary>
    public Task<JsonNode?> ProfitLossAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/calculators/profit-loss", p, ct);

    /// <summary>Compound interest calculator. Params: principal, rate, duration, duration_unit,
    /// compound_frequency, contribution, contribution_frequency. <c>rate</c> is the annual nominal
    /// rate in %; each compounding period earns <c>rate</c> divided by the periods per year.</summary>
    public Task<JsonNode?> CompoundInterestAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/calculators/compound-interest", p, ct);

    /// <summary>Loan-vs-sell calculator. Params: slug, crypto_amount, needed_cash, term_months,
    /// interest_rate, ltv, expected_growth, tax_rate.</summary>
    public Task<JsonNode?> LoanAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/calculators/loan", p, ct);

    /// <summary>Staking rewards calculator. Params: amount, period, period_unit, apy, compound_frequency, commission.</summary>
    public Task<JsonNode?> StakingAsync(Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/calculators/staking", p, ct);
}
