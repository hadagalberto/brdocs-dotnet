namespace BrDocs.Tests;

/// <summary>
/// Generic helpers that invoke the static abstract interface members
/// (<see cref="IParsable{T}"/> / <see cref="ISpanParsable{T}"/>) through a type parameter,
/// which is the only way static abstract members can be called.
/// </summary>
internal static class ParsableHelpers
{
    internal static T ParseViaSpanParsable<T>(ReadOnlySpan<char> s, IFormatProvider? provider)
        where T : ISpanParsable<T>
        => T.Parse(s, provider);

    internal static bool TryParseViaSpanParsable<T>(ReadOnlySpan<char> s, IFormatProvider? provider, out T result)
        where T : ISpanParsable<T>
    {
        T? parsed = default;
        bool success = T.TryParse(s, provider, out parsed);
        result = parsed!;
        return success;
    }

    internal static T ParseViaParsable<T>(string s, IFormatProvider? provider)
        where T : IParsable<T>
        => T.Parse(s, provider);

    internal static bool TryParseViaParsable<T>(string? s, IFormatProvider? provider, out T result)
        where T : IParsable<T>
    {
        T? parsed = default;
        bool success = T.TryParse(s, provider, out parsed);
        result = parsed!;
        return success;
    }
}
