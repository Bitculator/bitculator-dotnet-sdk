using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator.Tests;

/// <summary>
/// A scripted <see cref="HttpMessageHandler"/> injected through
/// <c>BitculatorClientOptions.Handler</c> — the SDK's transport seam. Shifts one
/// queued step per request and records every request. No network, no mocks.
/// </summary>
public sealed class StubHandler : HttpMessageHandler
{
    public sealed record Recorded(string Method, Uri Uri, Dictionary<string, string> Headers, string? Body)
    {
        public string? Header(string name) =>
            Headers.TryGetValue(name.ToLowerInvariant(), out var value) ? value : null;

        public bool HasQueryPair(string pair) =>
            Uri.Query.TrimStart('?').Split('&').Contains(pair);
    }

    private delegate Task<HttpResponseMessage> Step(CancellationToken ct);

    private readonly Queue<Step> _steps = new();

    public List<Recorded> Calls { get; } = new();

    public int Hits => Calls.Count;

    public StubHandler Json(int status, string body, params (string Name, string Value)[] headers)
    {
        _steps.Enqueue(_ => Task.FromResult(Build(status, body, headers)));
        return this;
    }

    public StubHandler Error(int status, params (string Name, string Value)[] headers) =>
        Json(status, $"{{\"error\":{{\"code\":\"err_{status}\",\"message\":\"stub error {status}\",\"details\":null}}}}", headers);

    public StubHandler Ok() => Json(200, "{\"data\":\"ok\"}");

    public StubHandler NetworkError()
    {
        _steps.Enqueue(_ => throw new HttpRequestException("connection refused"));
        return this;
    }

    /// <summary>A response that arrives after <paramref name="ms"/> — trips the client timeout.</summary>
    public StubHandler Delayed(int ms)
    {
        _steps.Enqueue(async ct =>
        {
            await Task.Delay(ms, ct);
            return Build(200, "{\"data\":\"ok\"}", Array.Empty<(string, string)>());
        });
        return this;
    }

    private static HttpResponseMessage Build(int status, string body, (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage((HttpStatusCode)status);
        if (body.Length > 0)
        {
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }
        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in request.Headers)
        {
            headers[header.Key.ToLowerInvariant()] = string.Join(",", header.Value);
        }
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key.ToLowerInvariant()] = string.Join(",", header.Value);
            }
        }
        string? body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
        Calls.Add(new Recorded(request.Method.Method, request.RequestUri!, headers, body));

        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("StubHandler: no more queued responses");
        }
        return await _steps.Dequeue()(ct);
    }
}

public static class TestClient
{
    public static BitculatorClient Create(StubHandler handler, int maxRetries = 2, TimeSpan? timeout = null) =>
        new("test-key", new BitculatorClientOptions
        {
            BaseUrl = "https://api.test",
            Handler = handler,
            MaxRetries = maxRetries,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
            UserAgent = "bitculator-dotnet/test",
        });
}
