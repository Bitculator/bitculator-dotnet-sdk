using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bitculator.Tests;

public class RetryTests
{
    [Fact]
    public async Task GetRetriesA5xxAndSucceeds()
    {
        var handler = new StubHandler().Error(500).Ok();
        using var client = TestClient.Create(handler);
        var value = await client.Coins.GetAsync("bitcoin");
        Assert.Equal("ok", (string?)value);
        Assert.Equal(2, handler.Hits);
    }

    [Fact]
    public async Task GetRetriesANetworkErrorAndSucceeds()
    {
        var handler = new StubHandler().NetworkError().Ok();
        using var client = TestClient.Create(handler);
        Assert.Equal("ok", (string?)await client.Coins.GetAsync("bitcoin"));
        Assert.Equal(2, handler.Hits);
    }

    [Fact]
    public async Task DeleteRetriesA5xxAndSucceeds()
    {
        var handler = new StubHandler().Error(500).Ok();
        using var client = TestClient.Create(handler);
        await client.Webhooks.DeleteAsync(7);
        Assert.Equal(2, handler.Hits);
    }

    [Fact]
    public async Task PostNeverRetriesA5xx()
    {
        var handler = new StubHandler().Error(500).Ok();
        using var client = TestClient.Create(handler);
        var e = await Assert.ThrowsAsync<ApiException>(() => client.Editorial.SubmitArticleFeedbackAsync("x", true));
        Assert.True(e.IsServer);
        Assert.Equal(1, handler.Hits); // a POST must never be re-sent on a 5xx
    }

    [Fact]
    public async Task PostNeverRetriesANetworkError()
    {
        var handler = new StubHandler().NetworkError().Ok();
        using var client = TestClient.Create(handler);
        await Assert.ThrowsAsync<BitculatorConnectionException>(() => client.Editorial.SubmitArticleFeedbackAsync("x", true));
        Assert.Equal(1, handler.Hits);
    }

    [Fact]
    public async Task PostDoesRetryA429()
    {
        var handler = new StubHandler().Error(429).Ok();
        using var client = TestClient.Create(handler);
        await client.Editorial.SubmitArticleFeedbackAsync("x", true);
        Assert.Equal(2, handler.Hits);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetries()
    {
        var handler = new StubHandler().Error(500).Error(500).Error(500).Ok();
        using var client = TestClient.Create(handler, maxRetries: 2);
        var e = await Assert.ThrowsAsync<ApiException>(() => client.Coins.GetAsync("bitcoin"));
        Assert.True(e.IsServer);
        Assert.Equal(3, handler.Hits); // initial attempt + 2 retries; the queued 200 is never reached
    }

    [Fact]
    public async Task HonorsRetryAfterSecondsOnA429()
    {
        var handler = new StubHandler().Error(429, ("Retry-After", "1")).Ok();
        using var client = TestClient.Create(handler);
        var watch = Stopwatch.StartNew();
        await client.Coins.GetAsync("bitcoin");
        Assert.True(watch.ElapsedMilliseconds >= 900, $"Retry-After not honored; elapsed {watch.ElapsedMilliseconds}ms");
        Assert.Equal(2, handler.Hits);
    }

    [Fact]
    public async Task TimedOutGetRetriesThenThrows()
    {
        var handler = new StubHandler().Delayed(500).Delayed(500);
        using var client = TestClient.Create(handler, maxRetries: 1, timeout: TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<BitculatorTimeoutException>(() => client.Coins.GetAsync("bitcoin"));
        Assert.Equal(2, handler.Hits); // the timeout is a transport failure, so the idempotent GET retried once
    }

    [Fact]
    public async Task TimedOutPostFailsWithoutRetrying()
    {
        var handler = new StubHandler().Delayed(500).Ok();
        using var client = TestClient.Create(handler, timeout: TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<BitculatorTimeoutException>(() => client.Editorial.SubmitArticleFeedbackAsync("x", true));
        Assert.Equal(1, handler.Hits); // a POST must never be re-sent on a timeout
    }

    [Fact]
    public async Task CallerCancellationIsNotTreatedAsATimeout()
    {
        var handler = new StubHandler().Delayed(500);
        using var client = TestClient.Create(handler);
        using var cts = new CancellationTokenSource(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Coins.GetAsync("bitcoin", null, cts.Token));
        Assert.Equal(1, handler.Hits); // cancellation must not trigger the retry loop
    }
}
