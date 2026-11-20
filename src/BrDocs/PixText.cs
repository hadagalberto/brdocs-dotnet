namespace BrDocs;

/// <summary>
/// Normalization of the BR Code text fields 59 (merchant name) and 60 (merchant city):
/// diacritics are stripped, the optional uppercasing is applied, the EMV/BCB maximum lengths
/// are enforced and any character outside the allowed set — ASCII letters, digits and spaces —
/// is rejected.
/// </summary>
/// <remarks>
/// The EMV® QRCPS-MPM common character set limits these fields to simple text; the Banco
/// Central do Brasil examples ("Fulano de Tal", "BRASILIA", "SAO PAULO") confirm that accented
/// characters are expected to be normalized away, as Pix wallets reject diacritics in fields 59
/// and 60. Precomposed Latin-1 accented letters (À–ÿ) are mapped to their base ASCII letter and
/// combining diacritical marks (U+0300–U+036F) are dropped, so both "São Paulo" and
/// "S\u0303ao Paulo" normalize to "Sao Paulo". Letters without a single-letter ASCII base
/// (Æ, Ð, Ø, Þ, ß) are rejected, as are the multiplication and division signs.
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
    /// characters — ASCII letters, digits and spaces.
    /// </summary>
    /// <param name="source">The raw merchant name or city text.</param>
    /// <param name="maxLength">1 for the maximum accepted length (25 for names, 15 for cities).</param>
    /// <param name="uppercase">When <see langword="true"/>, ASCII letters are uppercased.</param>
    /// <param name="destination">The destination buffer; sized to <paramref name="maxLength"/>.</param>
    /// <param name="written">The number of characters written; <c>0</c> when <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="false"/> when the text is empty, holds a character outside the allowed set,
    /// or does not fit within <paramref name="maxLength"/> after normalization.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool TryNormalize(ReadOnlySpan<char> source, int maxLength, bool uppercase, Span<char> destination, out int written)
    {
        written = 0;
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
            destination[written++] = uppercase && mapped is >= 'a' and <= 'z' ? (char)(mapped - 32) : mapped;
        }
        return written > 0;
    }

    /// <summary>
    /// Maps one input character to its normalized form: ASCII letters, digits and spaces pass
    /// through; precomposed Latin-1 accented letters map to their base letter; everything else
    /// maps to '\0' (invalid).
    /// </summary>
    private static char MapChar(char c)
    {
        if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or ' ')
        {
            return c;
        }
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
