using System;

namespace Bitculator;

/// <summary>Shared base for every resource group: holds the transport and encodes path segments.</summary>
public abstract class ResourceBase
{
    private protected readonly HttpTransport Http;

    private protected ResourceBase(HttpTransport http) => Http = http;

    /// <summary>Percent-encode a dynamic path segment (slug or id) so it is URL-safe.</summary>
    private protected static string Enc(object? value) => Uri.EscapeDataString(value?.ToString() ?? "");
}
