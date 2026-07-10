using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Bitculator.Tests;

public class TransportTests
{
    [Fact]
    public async Task SendsBearerAuthAcceptAndUserAgent()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = TestClient.Create(handler);
        await client.Coins.GetAsync("bitcoin");
        var request = handler.Calls[0];
        Assert.Equal("Bearer test-key", request.Header("authorization"));
        Assert.Equal("application/json", request.Header("accept"));
        Assert.Equal("bitculator-dotnet/test", request.Header("user-agent"));
        Assert.Null(request.Header("content-type"));
    }

    [Fact]
    public async Task PostSendsAJsonBodyWithContentType()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = TestClient.Create(handler);
        await client.Editorial.SubmitArticleFeedbackAsync("bitcoin-halving", true);
        var request = handler.Calls[0];
        Assert.Equal("POST", request.Method);
        Assert.StartsWith("application/json", request.Header("content-type"));
        Assert.Contains("true", request.Body);
        Assert.Equal("/api/v1/articles/bitcoin-halving/feedback", request.Uri.AbsolutePath);
    }

    [Fact]
    public async Task UnwrapsTheDataEnvelope()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{\"name\":\"Bitcoin\",\"price\":\"67412.83451201\"}}");
        using var client = TestClient.Create(handler);
        JsonNode? coin = await client.Coins.GetAsync("bitcoin");
        Assert.Equal("Bitcoin", (string?)coin?["name"]);
        Assert.Equal("67412.83451201", (string?)coin?["price"]); // decimal STRING, not a float
    }

    [Fact]
    public async Task PassesThroughBodiesWithoutADataKey()
    {
        var handler = new StubHandler().Json(200, "{\"openapi\":\"3.0.0\"}");
        using var client = TestClient.Create(handler);
        JsonNode? spec = await client.Meta.OpenApiAsync();
        Assert.Equal("3.0.0", (string?)spec?["openapi"]);
    }

    [Fact]
    public async Task Empty2xxBodyIsNull()
    {
        var handler = new StubHandler().Json(204, "");
        using var client = TestClient.Create(handler);
        JsonNode? gone = await client.Webhooks.DeleteAsync(7);
        Assert.Null(gone);
        Assert.Equal("DELETE", handler.Calls[0].Method);
        Assert.Equal("/api/v1/webhooks/7", handler.Calls[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task Malformed2xxBodyThrowsADecodeError()
    {
        var handler = new StubHandler().Json(200, "<html>gateway page</html>");
        using var client = TestClient.Create(handler);
        var e = await Assert.ThrowsAsync<BitculatorException>(() => client.Coins.GetAsync("bitcoin"));
        Assert.Contains("Failed to decode response", e.Message);
    }

    [Fact]
    public async Task ParsesQuotaHeaders()
    {
        var handler = new StubHandler().Json(200, "{\"data\":[]}",
            ("X-Quota-Limit", "10000"), ("X-Quota-Remaining", "9871"), ("X-Quota-Reset", "1720569600"));
        using var client = TestClient.Create(handler);
        Assert.Null(client.Quota);
        await client.Coins.GetAsync("bitcoin");
        var quota = client.Quota!;
        Assert.Equal(10000, quota.Limit);
        Assert.Equal(9871, quota.Remaining);
        Assert.Equal(1720569600, quota.Reset);
        Assert.Equal("10000", quota.Raw["x-quota-limit"]);
    }

    [Fact]
    public async Task MissingQuotaHeadersParseToNull()
    {
        var handler = new StubHandler().Json(200, "{\"data\":[]}");
        using var client = TestClient.Create(handler);
        await client.Coins.GetAsync("bitcoin");
        var quota = client.Quota!;
        Assert.Null(quota.Limit);
        Assert.Null(quota.Remaining);
        Assert.Null(quota.Reset);
        Assert.Empty(quota.Raw);
    }

    [Fact]
    public async Task ParamsSerializeCsvBoolsAndSkipNulls()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = TestClient.Create(handler);
        await client.Coins.GetAsync("bitcoin", Params.Of()
            .Set("symbols", new[] { "btc", "eth", "sol" })
            .Set("include_inactive", true)
            .Set("locale", null)
            .Set("empty", Array.Empty<string>())
            .Set("page", 1));
        var request = handler.Calls[0];
        Assert.True(request.HasQueryPair("symbols=btc%2Ceth%2Csol"), request.Uri.Query);
        Assert.True(request.HasQueryPair("include_inactive=true"), request.Uri.Query);
        Assert.True(request.HasQueryPair("page=1"), request.Uri.Query);
        Assert.DoesNotContain("locale", request.Uri.Query);
        Assert.DoesNotContain("empty", request.Uri.Query);
    }

    [Fact]
    public async Task FloatsGoOnTheWireAsPlainDecimals()
    {
        var handler = new StubHandler().Json(200, "{\"data\":{}}");
        using var client = TestClient.Create(handler);
        await client.Coins.GetAsync("bitcoin", Params.Of()
            .Set("min_price", 1e-7)
            .Set("max_price", -2.5e-8)
            .Set("min_marketcap", 1e21));
        var request = handler.Calls[0];
        Assert.True(request.HasQueryPair("min_price=0.0000001"), request.Uri.Query);
        Assert.True(request.HasQueryPair("max_price=-0.000000025"), request.Uri.Query);
        Assert.True(request.HasQueryPair("min_marketcap=1000000000000000000000"), request.Uri.Query);
    }
}
