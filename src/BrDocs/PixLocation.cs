namespace BrDocs;

/// <summary>
/// Validation of the payload location URLs of fields 26-25 (dynamic QR codes) and 80-25
/// (recurrence parameters, QR code composto).
/// </summary>
/// <remarks>
/// The URLs, which the BCB manual calls "locations", must not include a protocol prefix, are
/// limited to 77 characters, and must respect the RFC 3986 character set (the manual refers to
/// the RFC for the URL formation rules, "Manual de Padrões para Iniciação do Pix", section 2.5.2).
/// </remarks>
internal static class PixLocation
{
    /// <summary>The maximum length of a location URL (Manual de Padrões para Iniciação do Pix, section 2.5.2).</summary>
    internal const int MaxLength = 77;

    /// <summary>
    /// Determines whether <paramref name="location"/> is a valid BR Code location URL: 1..77
    /// characters of RFC 3986 URI characters (percent-escapes included) and no protocol prefix.
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool IsValid(ReadOnlySpan<char> location)
    {
        if (location.IsEmpty || location.Length > MaxLength || HasProtocolPrefix(location))
        {
            return false;
        }
        for (int i = 0; i < location.Length; i++)
        {
            char c = location[i];
            if (c == '%')
            {
                // A percent-escape takes two hexadecimal digits (RFC 3986 pct-encoded).
                if (i + 2 >= location.Length || !char.IsAsciiHexDigit(location[i + 1]) || !char.IsAsciiHexDigit(location[i + 2]))
                {
                    return false;
                }
                i += 2;
            }
            else if (!IsUriChar(c))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="location"/> starts with an RFC 3986
    /// scheme followed by "://" (e.g. "https://…"), which the BR Code must not carry.
    /// </summary>
    internal static bool HasProtocolPrefix(ReadOnlySpan<char> location)
    {
        int separator = location.IndexOf("://".AsSpan(), StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }
        if (!char.IsAsciiLetter(location[0]))
        {
            return false;
        }
        for (int i = 1; i < separator; i++)
        {
            if (!char.IsAsciiLetterOrDigit(location[i]) && location[i] is not ('+' or '-' or '.'))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Strips a leading RFC 3986 scheme and "://" from <paramref name="location"/> — the BR Code
    /// must not include the protocol prefix, so builders accept "https://pix.example.com/…" and
    /// encode "pix.example.com/…".
    /// </summary>
    internal static ReadOnlySpan<char> StripProtocolPrefix(ReadOnlySpan<char> location)
        => HasProtocolPrefix(location) ? location[(location.IndexOf("://".AsSpan(), StringComparison.Ordinal) + 3)..] : location;

    // RFC 3986: unreserved (A–Z a–z 0–9 - . _ ~), gen-delims (: / ? # [ ] @) and
    // sub-delims (! $ & ' ( ) * + , ; =); '%' is handled by the percent-escape rule above.
    private static bool IsUriChar(char c)
        => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~' or ':' or '/' or '?' or '#' or '[' or ']' or '@'
            or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=';
}
