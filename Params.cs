using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bitculator;

/// <summary>
/// A bag of query parameters. Keys mirror the API's names (snake_case); build it
/// fluently: <c>Params.Of().Set("per_page", 50).Set("sort", "-marketcap")</c>.
/// </summary>
public sealed class Params
{
    private readonly List<KeyValuePair<string, string>> _values = new();

    public Params() { }

    public static Params Of() => new();

    /// <summary>
    /// Set a parameter. A null value or empty sequence is skipped; sequences
    /// become CSV; a repeated key replaces the previous value.
    /// </summary>
    public Params Set(string key, object? value)
    {
        if (value is null)
        {
            return this;
        }

        string s;
        if (value is bool b)
        {
            s = b ? "true" : "false";
        }
        else if (value is string str)
        {
            s = str;
        }
        else if (value is double d)
        {
            // Convert.ToString would emit scientific notation (e.g. 1E+08, 1E-07);
            // "0.#################" gives a plain decimal string with no exponent,
            // trimming insignificant trailing zeros — kept plain for wire-format
            // consistency across the SDK family.
            s = d.ToString("0.#################", CultureInfo.InvariantCulture);
        }
        else if (value is float f)
        {
            s = ((double)f).ToString("0.#################", CultureInfo.InvariantCulture);
        }
        else if (value is IEnumerable enumerable)
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    parts.Add(Convert.ToString(item, CultureInfo.InvariantCulture) ?? "");
                }
            }
            if (parts.Count == 0)
            {
                return this;
            }
            s = string.Join(",", parts);
        }
        else
        {
            s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        _values.RemoveAll(kv => kv.Key == key);
        _values.Add(new KeyValuePair<string, string>(key, s));
        return this;
    }

    public bool IsEmpty => _values.Count == 0;

    internal Params Copy()
    {
        var copy = new Params();
        copy._values.AddRange(_values);
        return copy;
    }

    internal string ToQueryString()
    {
        if (_values.Count == 0)
        {
            return "";
        }
        var sb = new StringBuilder("?");
        bool first = true;
        foreach (var kv in _values)
        {
            if (!first)
            {
                sb.Append('&');
            }
            first = false;
            sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
        }
        return sb.ToString();
    }
}
