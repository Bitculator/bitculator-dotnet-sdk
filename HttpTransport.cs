using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>
/// The DRY transport core. Every request in the SDK flows through SendAsync():
/// URL + query building, auth headers, JSON (de)serialization, the response
/// envelope, quota capture, retries with backoff, and error mapping. Uses the
/// BCL HttpClient and System.Text.Json, so the SDK has no NuGet dependencies.
/// </summary>
internal sealed class HttpTransport : IDisposable
{
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _userAgent;
    private readonly int _maxRetries;
    private readonly HttpClient _http;

    private volatile Quota? _quota;

    public HttpTransport(string apiKey, string baseUrl, TimeSpan timeout, int maxRetries, string userAgent, HttpMessageHandler? handler = null)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _userAgent = userAgent;
        _maxRetries = maxRetries;
        // A caller-supplied handler (proxy/testing/tracing) is owned by the caller,
        // so the HttpClient wrapper never disposes it or changes its decompression
        // settings; Dispose() below still disposes the HttpClient itself either way.
        _http = handler != null
            ? new HttpClient(handler, disposeHandler: false) { Timeout = timeout }
            : new HttpClient(GzipHandler()) { Timeout = timeout };
    }

    // The SDK's own handler asks for gzip and decompresses transparently. Gzip only:
    // a corrupt Brotli body throws a bare InvalidOperationException that cannot be
    // told apart from a misconfigured request, so it could not be mapped to a typed
    // error the way a corrupt gzip body is in SendAsync. Browser-wasm rejects the
    // setting (the browser decompresses by itself), hence the Supports check.
    private static HttpClientHandler GzipHandler()
    {
        var own = new HttpClientHandler();
        if (own.SupportsAutomaticDecompression)
        {
            own.AutomaticDecompression = DecompressionMethods.GZip;
        }
        return own;
    }

    public Quota? Quota => _quota;

    public void Dispose() => _http.Dispose();

    public async Task<JsonNode?> GetAsync(string path, Params? p, CancellationToken ct) =>
        Unwrap(await SendAsync(HttpMethod.Get, path, p, null, ct).ConfigureAwait(false));

    public async Task<JsonNode?> PostAsync(string path, object? body, CancellationToken ct) =>
        Unwrap(await SendAsync(HttpMethod.Post, path, null, body, ct).ConfigureAwait(false));

    public async Task<JsonNode?> DeleteAsync(string path, CancellationToken ct) =>
        Unwrap(await SendAsync(HttpMethod.Delete, path, null, null, ct).ConfigureAwait(false));

    public async Task<Page> PageAsync(string path, Params? p, CancellationToken ct) =>
        new Page(this, path, p, await SendAsync(HttpMethod.Get, path, p, null, ct).ConfigureAwait(false));

    private async Task<JsonNode?> SendAsync(HttpMethod method, string path, Params? p, object? body, CancellationToken ct)
    {
        var url = _baseUrl + path + (p?.ToQueryString() ?? "");
        // GET and DELETE are idempotent and safe to auto-retry; POST is not — a
        // retried POST can duplicate a server-side effect the first attempt already
        // applied (alarm, webhook, vote), so POST is never retried on 5xx or on a
        // network/timeout error (where the request may have reached the server).
        bool isIdempotent = method == HttpMethod.Get || method == HttpMethod.Delete;
        int attempt = 0;
        while (true)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            if (body != null)
            {
                string json;
                try
                {
                    json = JsonSerializer.Serialize(body);
                }
                catch (Exception e)
                {
                    throw new BitculatorException($"Failed to encode request body: {e.Message}", e);
                }
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                if (isIdempotent && attempt < _maxRetries)
                {
                    await Task.Delay(Backoff(attempt++), ct).ConfigureAwait(false);
                    continue;
                }
                throw new BitculatorTimeoutException($"Request to {path} timed out");
            }
            catch (HttpRequestException e)
            {
                if (isIdempotent && attempt < _maxRetries)
                {
                    await Task.Delay(Backoff(attempt++), ct).ConfigureAwait(false);
                    continue;
                }
                throw new BitculatorConnectionException($"Network error requesting {path}: {e.Message}");
            }
            catch (InvalidDataException e)
            {
                // Thrown while the body is buffered and gunzipped: a corrupt compressed
                // body is a decode failure, never a success and never an untyped crash.
                throw new BitculatorException($"Failed to decode response from {path}: {e.Message}", e);
            }

            using (response)
            {
                _quota = ParseQuota(response);
                int status = (int)response.StatusCode;
                var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (status is >= 200 and < 300)
                {
                    // A malformed 2xx body must never be returned as success.
                    return ParseStrict(text);
                }
                var node = Parse(text);
                if ((status == 429 || (status >= 500 && isIdempotent)) && attempt < _maxRetries)
                {
                    await Task.Delay(RetryDelay(response, attempt++), ct).ConfigureAwait(false);
                    continue;
                }
                throw ApiException.From(status, node, response);
            }
        }
    }

    // ── helpers (all pure; the single place these concerns are implemented) ──

    private static JsonNode? Unwrap(JsonNode? node) =>
        node is JsonObject obj && obj.TryGetPropertyValue("data", out var data) ? data : node;

    // Lenient parse for non-2xx error bodies: a mangled error page still maps
    // to an ApiException with the status code.
    private static JsonNode? Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Strict parse for 2xx bodies: invalid JSON on a success response is a
    // hard error, never silently returned as a null success.
    private static JsonNode? ParseStrict(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException e)
        {
            throw new BitculatorException($"Failed to decode response: {e.Message}", e);
        }
    }

    private static Quota ParseQuota(HttpResponseMessage resp)
    {
        var raw = new Dictionary<string, string>();
        foreach (var header in resp.Headers)
        {
            var key = header.Key.ToLowerInvariant();
            if (key.StartsWith("x-quota-", StringComparison.Ordinal))
            {
                var value = header.Value.FirstOrDefault();
                if (value != null)
                {
                    raw[key] = value;
                }
            }
        }
        return new Quota(Num(raw, "x-quota-limit"), Num(raw, "x-quota-remaining"), Num(raw, "x-quota-reset"), raw);
    }

    private static int? Num(Dictionary<string, string> raw, string key) =>
        raw.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : null;

    private static TimeSpan RetryDelay(HttpResponseMessage resp, int attempt)
    {
        if (resp.Headers.TryGetValues("retry-after", out var values)
            && int.TryParse(values.FirstOrDefault(), out var seconds) && seconds > 0)
        {
            // Cap the honored Retry-After at 60s — a misbehaving proxy must not
            // block a synchronous call for an hour.
            return TimeSpan.FromSeconds(Math.Min(seconds, 60));
        }
        return Backoff(attempt);
    }

    private static TimeSpan Backoff(int attempt)
    {
        long ms = Math.Min(8000L, 250L * (1L << attempt));
        return TimeSpan.FromMilliseconds(ms + Random.Shared.Next(100));
    }
}
