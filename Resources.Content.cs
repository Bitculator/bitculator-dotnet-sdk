using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>Editorial content: articles, videos, and insights.</summary>
public sealed class Editorial : ResourceBase
{
    internal Editorial(HttpTransport http) : base(http) { }

    /// <summary>Videos for a coin (paginated). Params: page, per_page, type, search.</summary>
    public Task<Page> CoinVideosAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/coins/{Enc(slug)}/videos", p, ct);

    /// <summary>Insight timeline for a coin. Params: locale, offset, limit.</summary>
    public Task<JsonNode?> CoinInsightsAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/coins/{Enc(slug)}/insights", p, ct);

    /// <summary>List articles (paginated). Params: page, per_page, locale, tag, coin, exchange, wallet, search.</summary>
    public Task<Page> ArticlesAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/articles", p, ct);

    /// <summary>Get a single article by slug. Params: locale.</summary>
    public Task<JsonNode?> ArticleAsync(string slug, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/articles/{Enc(slug)}", p, ct);

    /// <summary>Submit helpful/unhelpful feedback on an article.</summary>
    public Task<JsonNode?> SubmitArticleFeedbackAsync(string slug, bool helpful, CancellationToken ct = default) =>
        Http.PostAsync($"/api/v1/articles/{Enc(slug)}/feedback", new { helpful }, ct);

    /// <summary>Get a single video by id.</summary>
    public Task<JsonNode?> VideoAsync(object id, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/videos/{Enc(id)}", null, ct);

    /// <summary>List insights (paginated). Params: page, per_page, locale, type, coin, search, sort.</summary>
    public Task<Page> InsightsAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/insights", p, ct);

    /// <summary>Get a single insight by id. Params: locale.</summary>
    public Task<JsonNode?> InsightAsync(object id, Params? p = null, CancellationToken ct = default) =>
        Http.GetAsync($"/api/v1/insights/{Enc(id)}", p, ct);
}

/// <summary>Price alarms owned by the API key.</summary>
public sealed class Alarms : ResourceBase
{
    internal Alarms(HttpTransport http) : base(http) { }

    /// <summary>List your alarms (paginated). Params: page, per_page, status, direction, notification.</summary>
    public Task<Page> ListAsync(Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync("/api/v1/alarms", p, ct);

    /// <summary>Create an alarm.</summary>
    public Task<JsonNode?> CreateAsync(string name, string coin, string metric, string direction, double target, string notification, CancellationToken ct = default) =>
        Http.PostAsync("/api/v1/alarms", new { name, coin, metric, direction, target, notification }, ct);

    /// <summary>Delete an alarm by id.</summary>
    public Task<JsonNode?> DeleteAsync(object id, CancellationToken ct = default) =>
        Http.DeleteAsync($"/api/v1/alarms/{Enc(id)}", ct);
}

/// <summary>Webhook endpoints and their delivery log.</summary>
public sealed class Webhooks : ResourceBase
{
    internal Webhooks(HttpTransport http) : base(http) { }

    /// <summary>List your webhook endpoints.</summary>
    public Task<JsonNode?> ListAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/webhooks", null, ct);

    /// <summary>Register a webhook endpoint for a set of events.</summary>
    public Task<JsonNode?> CreateAsync(string url, IEnumerable<string> events, CancellationToken ct = default) =>
        Http.PostAsync("/api/v1/webhooks", new { url, events }, ct);

    /// <summary>Delete a webhook endpoint by id.</summary>
    public Task<JsonNode?> DeleteAsync(object id, CancellationToken ct = default) =>
        Http.DeleteAsync($"/api/v1/webhooks/{Enc(id)}", ct);

    /// <summary>Send a test event to a webhook endpoint.</summary>
    public Task<JsonNode?> TestAsync(object id, CancellationToken ct = default) =>
        Http.PostAsync($"/api/v1/webhooks/{Enc(id)}/test", null, ct);

    /// <summary>Delivery log for a webhook endpoint (paginated). Params: page, per_page.</summary>
    public Task<Page> DeliveriesAsync(object id, Params? p = null, CancellationToken ct = default) =>
        Http.PageAsync($"/api/v1/webhooks/{Enc(id)}/deliveries", p, ct);
}

/// <summary>Service metadata: spec, health, and key usage.</summary>
public sealed class Meta : ResourceBase
{
    internal Meta(HttpTransport http) : base(http) { }

    /// <summary>The raw OpenAPI specification for the API.</summary>
    public Task<JsonNode?> OpenApiAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/openapi.json", null, ct);

    /// <summary>Health check.</summary>
    public Task<JsonNode?> PingAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/ping", null, ct);

    /// <summary>Current key usage and quota.</summary>
    public Task<JsonNode?> UsageAsync(CancellationToken ct = default) =>
        Http.GetAsync("/api/v1/usage", null, ct);
}
