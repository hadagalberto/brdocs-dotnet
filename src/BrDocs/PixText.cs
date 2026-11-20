namespace BrDocs;

/// <summary>
/// Normalization of the BR Code text fields: field 59 (merchant name), field 60 (merchant city)
/// and the free-text fields (26-02 infoAdicional, 62 sub-fields). Diacritics are stripped, the
/// optional uppercasing is applied, maximum lengths are enforced, and every character is mapped
/// into the single-byte EMV/BCB character set so that TLV lengths and the CRC describe the same
/// bytes that a QR writer emits.
/// </summary>
/// <remarks>
/// The EMV® QRCPS-MPM common character set limits these fields to simple text; the Banco
/// Central do Brasil examples ("Fulano de Tal", "BRASILIA", "SAO PAULO") confirm that accented
/// characters are expected to be normalized away, as Pix wallets reject diacritics in fields 59
/// and 60. Precomposed Latin-1 accented letters (À–ÿ) are mapped to their base ASCII letter and
/// combining diacritical marks (U+0300–U+036F) are dropped, so both "São Paulo" and
/// "S\u0303ao Paulo" normalize to "Sao Paulo". Letters without a single-letter ASCII base
/// (Æ, Ð, Ø, Þ, ß) are rejected, as are the multiplication and division signs.
/// <para>
/// Name and city allow ASCII letters, digits and spaces and must contain at least one letter or
/// digit (a value of only spaces carries no merchant identity). Free-text fields additionally
/// allow ASCII punctuation, which the BCB manual defines as "conjunto livre de caracteres".
/// </para>
/// </remarks>
internal static class PixText
{
    /// <summary>The maximum length of the merchant name, field 59 (EMV QRCPS-MPM: max 25).</summary>
    internal const int MaxNameLength = 25;

    /// <summary>The maximum length of the merchant city, field 60 (EMV QRCPS-MPM: max 15).</summary>
    internal const int MaxCityLength = 15;

    /// <summary>
    /// Normalizes <paramref name="source"/> into <paramref name="destination"/>: combining marks
    /// are dropped, precomposed accented letters are mapped to their base letter, the value is
    /// optionally uppercased, and the result must hold 1..<paramref name="maxLength"/> allowed
    /// characters — ASCII letters, digits and spaces, at least one of which is not a space.
    /// </summary>
    /// <param name="source">The raw merchant name or city text.</param>
    /// <param name="maxLength">The maximum accepted length (25 for names, 15 for cities).</param>
    /// <param name="uppercase">When <see langword="true"/>, ASCII letters are uppercased.</param>
    /// <param name="destination">The destination buffer; sized to <paramref name="maxLength"/>.</param>
    /// <param name="written">The number of characters written; <c>0</c> when <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="false"/> when the text is empty, holds only spaces, holds a character
    /// outside the allowed set, or does not fit within <paramref name="maxLength"/> after
    /// normalization.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool TryNormalize(ReadOnlySpan<char> source, int maxLength, bool uppercase, Span<char> destination, out int written)
    {
        written = 0;
        bool hasLetterOrDigit = false;
        foreach (char c in source)
        {
            if (c is >= '\u0300' and <= '\u036F')
            {
                // Combining diacritical mark: dropped, so decomposed input also normalizes.
                continue;
            }
            char mapped = MapChar(c);
            if (mapped == '\0')
            {
                written = 0;
                return false;
            }
            if (written >= destination.Length || written >= maxLength)
            {
                written = 0;
                return false;
            }
            if (mapped != ' ')
            {
                hasLetterOrDigit = true;
            }
            destination[written++] = uppercase && mapped is >= 'a' and <= 'z' ? (char)(mapped - 32) : mapped;
        }
        // A name/city of only spaces is not a value: it must contain a letter or digit.
        return hasLetterOrDigit;
    }

    /// <summary>
    /// Normalizes free text (field 26-02 and field 62 sub-fields) into the single-byte character
    /// set: diacritics are stripped, ASCII printable characters (0x20–0x7E) are kept as-is and
    /// every other character is rejected. The result is non-empty and fits the destination.
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool TryNormalizeFreeText(ReadOnlySpan<char> source, Span<char> destination, out int written)
    {
        written = 0;
        foreach (char c in source)
        {
            if (c is >= '\u0300' and <= '\u036F')
            {
                continue;
            }
            char mapped;
            if (c < 0x80)
            {
                if (c is < '\u0020' or > '\u007E')
                {
                    written = 0;
                    return false;
                }
                mapped = c;
            }
            else
            {
                mapped = MapAccentedLetter(c);
                if (mapped == '\0')
                {
                    written = 0;
                    return false;
                }
            }
            if (written >= destination.Length)
            {
                written = 0;
                return false;
            }
            destination[written++] = mapped;
        }
        return written > 0;
    }

    /// <summary>
    /// Validates that already-encoded free text uses only single-byte printable ASCII — the
    /// invariant that <see cref="TryNormalizeFreeText"/> establishes when the builder emits it.
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool IsAllowedFreeText(ReadOnlySpan<char> value)
    {
        foreach (char c in value)
        {
            if (c is < '\u0020' or > '\u007E')
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates an already-encoded merchant name/city: ASCII letters, digits and spaces with at
    /// least one letter or digit (mirrors <see cref="TryNormalize"/> without uppercasing).
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool IsAllowedText(ReadOnlySpan<char> value)
    {
        bool hasLetterOrDigit = false;
        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != ' ')
            {
                return false;
            }
            if (c != ' ')
            {
                hasLetterOrDigit = true;
            }
        }
        return hasLetterOrDigit;
    }

    /// <summary>
    /// Maps one name/city input character to its normalized form: ASCII letters, digits and
    /// spaces pass through; precomposed Latin-1 accented letters map to their base letter;
    /// everything else maps to '\0' (invalid).
    /// </summary>
    private static char MapChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or ' '
            ? c
            : MapAccentedLetter(c);

    /// <summary>Maps a precomposed Latin-1 accented letter to its single-letter ASCII base.</summary>
    private static char MapAccentedLetter(char c)
    {
        if (c < '\u00C0' || c > '\u00FF')
        {
            return '\0';
        }
        // U+00C0..U+00FF: the precomposed Latin-1 supplement letters. Accented vowels, Ç/ç and
        // Ñ/ñ map to their base letter; Æ Ð × Ø Þ ß ð ÷ ø þ have no single-letter ASCII base and
        // are rejected.
        return c switch
        {
            >= '\u00C0' and <= '\u00C5' => 'A',  // À Á Â Ã Ä Å
            '\u00C7' => 'C',                     // Ç
            >= '\u00C8' and <= '\u00CB' => 'E',  // È É Ê Ë
            >= '\u00CC' and <= '\u00CF' => 'I',  // Ì Í Î Ï
            '\u00D1' => 'N',                     // Ñ
            >= '\u00D2' and <= '\u00D6' => 'O',  // Ò Ó Ô Õ Ö
            >= '\u00D9' and <= '\u00DC' => 'U',  // Ù Ú Û Ü
            '\u00DD' => 'Y',                     // Ý
            >= '\u00E0' and <= '\u00E5' => 'a',  // à á â ã ä å
            '\u00E7' => 'c',                     // ç
            >= '\u00E8' and <= '\u00EB' => 'e',  // è é ê ë
            >= '\u00EC' and <= '\u00EF' => 'i',  // ì í î ï
            '\u00F1' => 'n',                     // ñ
            >= '\u00F2' and <= '\u00F6' => 'o',  // ò ó ô õ ö
            >= '\u00F9' and <= '\u00FC' => 'u',  // ù ú û ü
            '\u00FD' or '\u00FF' => 'y',          // ý ÿ
            _ => '\0',
        };
    }
}
