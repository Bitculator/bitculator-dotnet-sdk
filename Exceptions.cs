using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace Bitculator;

/// <summary>Base class for every exception thrown by the SDK.</summary>
public class BitculatorException : Exception
{
    public BitculatorException(string message) : base(message) { }

    public BitculatorException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// A non-2xx response, carrying the API's <c>{ error: { code, message, details } }</c>
/// envelope. Branch on the <c>IsXxx</c> predicates.
/// </summary>
public sealed class ApiException : BitculatorException
{
    public int Status { get; }
    public string? Code { get; }
    public JsonNode? Details { get; }
    public string? RequestId { get; }

    /// <summary>Seconds to wait before retrying (429 only), or null.</summary>
    public int? RetryAfter { get; }

    public ApiException(int status, string? code, string message, JsonNode? details, string? requestId, int? retryAfter)
        : base(message)
    {
        Status = status;
        Code = code;
        Details = details;
        RequestId = requestId;
        RetryAfter = retryAfter;
    }

    public bool IsAuth => Status == 401;
    public bool IsPermission => Status == 403;
    public bool IsNotFound => Status == 404;
    public bool IsValidation => Status == 422;
    public bool IsRateLimited => Status == 429;
    public bool IsServer => Status >= 500;

    internal static ApiException From(int status, JsonNode? body, HttpResponseMessage resp)
    {
        string? code = null;
        string message = $"HTTP {status}";
        JsonNode? details = null;

        if (body is JsonObject obj)
        {
            if (obj["error"] is JsonObject err)
            {
                code = AsString(err["code"]);
                var m = AsString(err["message"]);
                if (m != null)
                {
                    message = m;
                }
                details = err["details"]?.DeepClone();
            }
            else
            {
                var m = AsString(obj["message"]);
                if (m != null)
                {
                    message = m;
                }
            }
        }

        string? requestId = resp.Headers.TryGetValues("x-request-id", out var rid) ? rid.FirstOrDefault() : null;
        int? retryAfter = null;
        if (status == 429 && resp.Headers.TryGetValues("retry-after", out var ra) && int.TryParse(ra.FirstOrDefault(), out var secs))
        {
            retryAfter = secs;
        }

        return new ApiException(status, code, message, details, requestId, retryAfter);
    }

    private static string? AsString(JsonNode? n) =>
        n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
}

/// <summary>The request exceeded the configured timeout (after retries).</summary>
public sealed class BitculatorTimeoutException : BitculatorException
{
    public BitculatorTimeoutException(string message) : base(message) { }
}

/// <summary>A network-level failure prevented the request from completing.</summary>
public sealed class BitculatorConnectionException : BitculatorException
{
    public BitculatorConnectionException(string message) : base(message) { }
}
