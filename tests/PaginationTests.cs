using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Bitculator.Tests;

public class PaginationTests
{
    private static string PageBody(int page, int last)
    {
        int first = (page - 1) * 2;
        string next = page < last ? $"\"/api/v1/coins?page={page + 1}\"" : "null";
        return $"{{\"data\":[{{\"id\":{first + 1}}},{{\"id\":{first + 2}}}],"
            + $"\"meta\":{{\"current_page\":{page},\"last_page\":{last},\"per_page\":2,\"total\":{last * 2}}},"
            + $"\"links\":{{\"next\":{next}}}}}";
    }

    [Fact]
    public async Task PageExposesDataMetaAndHasMore()
    {
        var handler = new StubHandler().Json(200, PageBody(1, 3));
        using var client = TestClient.Create(handler);
        Page page = await client.Coins.ListAsync(Params.Of().Set("per_page", 2));
        Assert.Equal(2, page.Data.Count);
        Assert.Equal(1, page.Meta.CurrentPage);
        Assert.Equal(3, page.Meta.LastPage);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task NextPageRequestsTheFollowingPageAndPreservesTheQuery()
    {
        var handler = new StubHandler().Json(200, PageBody(1, 3)).Json(200, PageBody(2, 3));
        using var client = TestClient.Create(handler);
        Page first = await client.Coins.ListAsync(Params.Of().Set("per_page", 2).Set("sort", "rank"));
        Page? second = await first.NextPageAsync();
        Assert.NotNull(second);
        Assert.Equal(3, (int?)second!.Data[0]?["id"]);
        var request = handler.Calls[1];
        foreach (var pair in new[] { "page=2", "per_page=2", "sort=rank" })
        {
            Assert.True(request.HasQueryPair(pair), $"second query missing {pair}: {request.Uri.Query}");
        }
    }

    [Fact]
    public async Task NextPageIsNullOnTheLastPage()
    {
        var handler = new StubHandler().Json(200, PageBody(3, 3));
        using var client = TestClient.Create(handler);
        Page page = await client.Coins.ListAsync();
        Assert.False(page.HasMore);
        Assert.Null(await page.NextPageAsync());
    }

    [Fact]
    public async Task AutoPagingWalksEveryPageInOrder()
    {
        var handler = new StubHandler().Json(200, PageBody(1, 3)).Json(200, PageBody(2, 3)).Json(200, PageBody(3, 3));
        using var client = TestClient.Create(handler);
        Page page = await client.Coins.ListAsync(Params.Of().Set("per_page", 2));
        var seen = new List<int>();
        await foreach (JsonNode? row in page.AutoPagingAsync())
        {
            seen.Add((int)row!["id"]!);
        }
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, seen);
        Assert.Equal(3, handler.Hits);
    }

    [Fact]
    public async Task EnvelopeWithoutMetaOrLinksIsASafeFinalPage()
    {
        var handler = new StubHandler().Json(200, "{\"data\":[{\"id\":9}]}");
        using var client = TestClient.Create(handler);
        Page page = await client.Coins.ListAsync();
        Assert.Single(page.Data);
        Assert.False(page.HasMore);
        Assert.Null(await page.NextPageAsync());
    }

    [Fact]
    public async Task NonArrayDataIsAnEmptyPage()
    {
        var handler = new StubHandler().Json(200, "{\"data\":null,\"meta\":{\"current_page\":1,\"last_page\":1}}");
        using var client = TestClient.Create(handler);
        Page page = await client.Coins.ListAsync();
        Assert.Empty(page.Data);
        Assert.False(page.HasMore);
    }
}
