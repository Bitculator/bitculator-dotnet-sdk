using System.Collections.Generic;

namespace Bitculator;

/// <summary>Quota headers (X-Quota-*) from the most recent response. A field is null when the header was absent.</summary>
public sealed record Quota(int? Limit, int? Remaining, int? Reset, IReadOnlyDictionary<string, string> Raw);
