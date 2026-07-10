using System;
using System.Threading.Tasks;
using Xunit;

namespace Bitculator.Tests;

public class ErrorsTests
{
    private static async Task<ApiException> Failing(StubHandler handler)
    {
        // maxRetries: 0 — every status maps straight to its exception.
        using var client = TestClient.Create(handler, maxRetries: 0);
        return await Assert.ThrowsAsync<ApiException>(() => client.Coins.GetAsync("bitcoin"));
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(422)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task MapsStatusesToPredicates(int status)
    {
        var e = await Failing(new StubHandler().Error(status));
        Assert.Equal(status, e.Status);
        Assert.Equal($"err_{status}", e.Code);
        Assert.True(e is BitculatorException);
        Assert.Equal(status == 401, e.IsAuth);
        Assert.Equal(status == 403, e.IsPermission);
        Assert.Equal(status == 404, e.IsNotFound);
        Assert.Equal(status == 422, e.IsValidation);
        Assert.Equal(status == 429, e.IsRateLimited);
        Assert.Equal(status >= 500, e.IsServer);
    }

    [Fact]
    public async Task CarriesCodeMessageAndDetailsFromTheEnvelope()
    {
        var body = "{\"error\":{\"code\":\"validation_failed\",\"message\":\"Invalid parameters.\","
            + "\"details\":{\"per_page\":[\"must not be greater than 100\"]}}}";
        var e = await Failing(new StubHandler().Json(422, body));
        Assert.Equal("validation_failed", e.Code);
        Assert.Equal("Invalid parameters.", e.Message);
        Assert.Equal("must not be greater than 100", (string?)e.Details?["per_page"]?[0]);
    }

    [Fact]
    public async Task FallsBackToTopLevelMessageThenHttpStatus()
    {
        var withMessage = await Failing(new StubHandler().Json(500, "{\"message\":\"Whoops.\"}"));
        Assert.Equal("Whoops.", withMessage.Message);

        var rawText = await Failing(new StubHandler().Json(502, "Bad Gateway"));
        Assert.Equal("HTTP 502", rawText.Message);
        Assert.Null(rawText.Code);
    }

    [Fact]
    public async Task CapturesRequestIdAndRetryAfter()
    {
        var withId = await Failing(new StubHandler().Error(500, ("X-Request-Id", "req_123")));
        Assert.Equal("req_123", withId.RequestId);

        var limited = await Failing(new StubHandler().Error(429, ("Retry-After", "30")));
        Assert.Equal(30, limited.RetryAfter);

        var absent = await Failing(new StubHandler().Error(429));
        Assert.Null(absent.RetryAfter);
    }
}
