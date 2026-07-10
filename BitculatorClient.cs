using System;

namespace Bitculator;

/// <summary>
/// The Bitculator Data API client.
///
/// <code>
/// var client = new BitculatorClient("your-api-key");
/// JsonNode? bitcoin = await client.Coins.GetAsync("bitcoin");
/// </code>
///
/// Each API group is a property: <c>client.Coins</c>, <c>client.Prices</c>,
/// <c>client.Exchanges</c>, and so on.
/// </summary>
public sealed class BitculatorClient : IDisposable
{
    /// <summary>SDK version, derived from the assembly version (csproj &lt;Version&gt; is the single source).</summary>
    public static readonly string Version =
        typeof(BitculatorClient).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    private readonly HttpTransport _http;

    public Coins Coins { get; }
    public Prices Prices { get; }
    public Markets Markets { get; }
    public Exchanges Exchanges { get; }
    public Wallets Wallets { get; }
    public GlobalMarket GlobalMarket { get; }
    public Sentiment Sentiment { get; }
    public Indicators Indicators { get; }
    public Liquidations Liquidations { get; }
    public Conversion Conversion { get; }
    public Calculators Calculators { get; }
    public Editorial Editorial { get; }
    public Alarms Alarms { get; }
    public Webhooks Webhooks { get; }
    public Meta Meta { get; }

    public BitculatorClient(string apiKey, BitculatorClientOptions? options = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new BitculatorException("A Data API key is required: new BitculatorClient(apiKey)");
        }
        options ??= new BitculatorClientOptions();
        _http = new HttpTransport(
            apiKey,
            options.BaseUrl.TrimEnd('/'),
            options.Timeout,
            options.MaxRetries,
            options.UserAgent ?? $"bitculator-dotnet/{Version}",
            options.Handler);

        Coins = new Coins(_http);
        Prices = new Prices(_http);
        Markets = new Markets(_http);
        Exchanges = new Exchanges(_http);
        Wallets = new Wallets(_http);
        GlobalMarket = new GlobalMarket(_http);
        Sentiment = new Sentiment(_http);
        Indicators = new Indicators(_http);
        Liquidations = new Liquidations(_http);
        Conversion = new Conversion(_http);
        Calculators = new Calculators(_http);
        Editorial = new Editorial(_http);
        Alarms = new Alarms(_http);
        Webhooks = new Webhooks(_http);
        Meta = new Meta(_http);
    }

    /// <summary>Quota (X-Quota-*) from the most recent response, or null before the first call.</summary>
    public Quota? Quota => _http.Quota;

    /// <summary>Disposes the underlying transport (and its HttpClient / sockets).</summary>
    public void Dispose() => _http.Dispose();
}

/// <summary>Configuration for <see cref="BitculatorClient"/>.</summary>
public sealed class BitculatorClientOptions
{
    public string BaseUrl { get; set; } = "https://bitculator.com";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Automatic retries for 429/5xx/network errors (default 2; 0 to disable).</summary>
    public int MaxRetries { get; set; } = 2;

    public string? UserAgent { get; set; }

    /// <summary>
    /// Optional <see cref="System.Net.Http.HttpMessageHandler"/> for proxies, testing,
    /// or tracing. The handler is not disposed with the client; its owner disposes it.
    /// </summary>
    public System.Net.Http.HttpMessageHandler? Handler { get; set; }
}
