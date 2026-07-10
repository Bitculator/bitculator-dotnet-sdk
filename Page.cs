using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Bitculator;

/// <summary>
/// One page of a paginated list, plus lazy forward navigation. <see cref="Data"/>
/// is the current page's rows; <see cref="AutoPagingAsync"/> walks every following
/// page. Rows are <see cref="JsonNode"/>s — call <c>node.Deserialize&lt;T&gt;()</c>
/// for POJOs.
/// </summary>
public sealed class Page
{
    private readonly HttpTransport _http;
    private readonly string _path;
    private readonly Params? _params;

    public IReadOnlyList<JsonNode?> Data { get; }
    public PageMeta Meta { get; }
    public PageLinks Links { get; }

    internal Page(HttpTransport http, string path, Params? p, JsonNode? envelope)
    {
        _http = http;
        _path = path;
        _params = p;

        List<JsonNode?> rows = [];
        if (envelope is JsonObject obj && obj["data"] is JsonArray array)
        {
            foreach (var item in array)
            {
                rows.Add(item);
            }
        }
        Data = rows;
        Meta = PageMeta.Parse((envelope as JsonObject)?["meta"]);
        Links = PageLinks.Parse((envelope as JsonObject)?["links"]);
    }

    /// <summary>True if at least one more page exists.</summary>
    public bool HasMore =>
        !string.IsNullOrEmpty(Links.Next)
        || (Meta.CurrentPage != 0 && Meta.LastPage != 0 && Meta.CurrentPage < Meta.LastPage);

    /// <summary>Fetch the next page, or null if this is the last one.</summary>
    public async Task<Page?> NextPageAsync(CancellationToken ct = default)
    {
        if (!HasMore)
        {
            return null;
        }
        var next = (_params ?? new Params()).Copy().Set("page", Meta.CurrentPage + 1);
        return await _http.PageAsync(_path, next, ct).ConfigureAwait(false);
    }

    /// <summary>Enumerate every row across this and all following pages, fetching lazily.</summary>
    public async IAsyncEnumerable<JsonNode?> AutoPagingAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        Page? page = this;
        while (page != null)
        {
            foreach (var row in page.Data)
            {
                yield return row;
            }
            page = await page.NextPageAsync(ct).ConfigureAwait(false);
        }
    }
}

/// <summary>Laravel paginator metadata.</summary>
public sealed record PageMeta(int CurrentPage, int? From, int LastPage, int PerPage, int? To, long Total)
{
    internal static PageMeta Parse(JsonNode? n)
    {
        if (n is not JsonObject o)
        {
            return new PageMeta(0, null, 0, 0, null, 0);
        }
        return new PageMeta(GetInt(o, "current_page"), GetIntN(o, "from"), GetInt(o, "last_page"),
            GetInt(o, "per_page"), GetIntN(o, "to"), GetLong(o, "total"));
    }

    private static int GetInt(JsonObject o, string k) => o[k] is JsonValue v && v.TryGetValue<int>(out var i) ? i : 0;
    private static int? GetIntN(JsonObject o, string k) => o[k] is JsonValue v && v.TryGetValue<int>(out var i) ? i : null;
    private static long GetLong(JsonObject o, string k) => o[k] is JsonValue v && v.TryGetValue<long>(out var l) ? l : 0;
}

/// <summary>Page navigation links.</summary>
public sealed record PageLinks(string? First, string? Last, string? Prev, string? Next)
{
    internal static PageLinks Parse(JsonNode? n)
    {
        if (n is not JsonObject o)
        {
            return new PageLinks(null, null, null, null);
        }
        return new PageLinks(Str(o, "first"), Str(o, "last"), Str(o, "prev"), Str(o, "next"));
    }

    private static string? Str(JsonObject o, string k) => o[k] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
}
