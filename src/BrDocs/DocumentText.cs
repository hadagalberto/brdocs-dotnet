namespace BrDocs;

/// <summary>
/// Shared, span-based text normalization used by every BrDocs document type.
/// </summary>
/// <remarks>
/// BrDocs accepts formatted or unformatted input: the standard punctuation (dot, dash, slash and
/// space) is stripped in place; any other character makes the input invalid. All helpers are
/// allocation-free: the caller provides a stack-allocated destination buffer sized to the expected
/// normalized length, and inputs longer than that are rejected without scanning the whole string.
/// </remarks>
internal static class DocumentText
{
    private const char Invalid = '\0';

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="c"/> is one of the ignored characters:
    /// '.', '-', '/' or ' '.
    /// </summary>
    internal static bool IsIgnored(char c) => c is '.' or '-' or '/' or ' ';

    /// <summary>
    /// Strips the ignored punctuation from <paramref name="source"/> into
    /// <paramref name="destination"/>, requiring every remaining character to be an ASCII digit.
    /// </summary>
    /// <param name="source">The raw input, formatted or not.</param>
    /// <param name="destination">
    /// Destination buffer sized to the expected normalized length; extra significant characters
    /// make the input invalid.
    /// </param>
    /// <param name="written">
    /// The number of characters written to <paramref name="destination"/>;
    /// <c>0</c> when <see langword="false"/> is returned.
    /// </param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="source"/> contains a non-digit character other
    /// than the ignored punctuation, or more significant characters than
    /// <paramref name="destination"/> can hold.
    /// </returns>
    internal static bool TryNormalizeDigits(ReadOnlySpan<char> source, Span<char> destination, out int written)
    {
        written = 0;
        foreach (char c in source)
        {
            if (IsIgnored(c))
            {
                continue;
            }
            if ((uint)(c - '0') > 9u)
            {
                written = 0;
                return false;
            }
            if (written >= destination.Length)
            {
                written = 0;
                return false;
            }
            destination[written++] = c;
        }
        return true;
    }

    /// <summary>
    /// Strips the ignored punctuation from <paramref name="source"/> into
    /// <paramref name="destination"/>, requiring every remaining character to be an ASCII digit or
    /// an ASCII letter. Lowercase letters are normalized to uppercase.
    /// </summary>
    /// <param name="source">The raw input, formatted or not.</param>
    /// <param name="destination">
    /// Destination buffer sized to the expected normalized length; extra significant characters
    /// make the input invalid.
    /// </param>
    /// <param name="written">
    /// The number of characters written to <paramref name="destination"/>;
    /// <c>0</c> when <see langword="false"/> is returned.
    /// </param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="source"/> contains a character that is neither
    /// a digit, an ASCII letter, nor ignored punctuation, or more significant characters than
    /// <paramref name="destination"/> can hold.
    /// </returns>
    internal static bool TryNormalizeUpperAlphanumeric(ReadOnlySpan<char> source, Span<char> destination, out int written)
    {
        written = 0;
        foreach (char c in source)
        {
            if (IsIgnored(c))
            {
                continue;
            }
            char normalized = NormalizeAsciiUpper(c);
            if (normalized == Invalid)
            {
                written = 0;
                return false;
            }
            if (written >= destination.Length)
            {
                written = 0;
                return false;
            }
            destination[written++] = normalized;
        }
        return true;
    }

    /// <summary>Returns a short, human-readable preview of <paramref name="value"/> for exception messages.</summary>
    internal static string Preview(ReadOnlySpan<char> value)
    {
        if (value.Length <= 32)
        {
            return new string(value);
        }
        return string.Concat(new string(value[..16]), "...", new string(value[^8..]));
    }

    private static char NormalizeAsciiUpper(char c)
    {
        if ((uint)(c - '0') <= 9u)
        {
            return c;
        }
        if ((uint)(c - 'A') <= 25u)
        {
            return c;
        }
        if ((uint)(c - 'a') <= 25u)
        {
            return (char)(c - 32);
        }
        return Invalid;
    }
}
