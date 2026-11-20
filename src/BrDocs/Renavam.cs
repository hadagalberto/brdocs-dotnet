using System.Globalization;

namespace BrDocs;

/// <summary>
/// RENAVAM — the Registro Nacional de Veículos Automotores, the Brazilian national vehicle
/// registry number.
/// </summary>
/// <remarks>
/// <para>
/// The current RENAVAM has 11 digits: a 10-digit base followed by one check digit (DV). The
/// legacy 9-digit format (8-digit base + DV) is accepted on input and left-padded with zeros
/// to 11 digits, as every Detran integration does; the padded form is the canonical
/// representation, so <c>Renavam.Parse("639884962")</c> equals <c>Renavam.Parse("00639884962")</c>.
/// </para>
/// <para>
/// Check-digit rule (mod 11, Portaria DENATRAN nº 27/2013, art. 1º — "10 dígitos e um dígito
/// verificador, calculado através do módulo 11, peso 9"): the 10 digits of the base are
/// multiplied, from right to left, by the cycle 2, 3, 4, 5, 6, 7, 8, 9 restarting at 2 —
/// which is the same as applying the weights <c>3, 2, 9, 8, 7, 6, 5, 4, 3, 2</c> left to
/// right, the cycle read backwards over the reversed first 10 digits. <c>r = sum % 11</c>;
/// the DV is <c>0</c> when <c>r &lt; 2</c> and <c>11 − r</c> otherwise. Verified against the
/// known vectors <c>00639884962</c> (the padded form of the legacy <c>639884962</c>) and
/// <c>12345678900</c>.
/// </para>
/// <para>
/// Sequences of 11 equal digits (such as <c>00000000000</c>) are rejected: the all-zero value
/// satisfies the DV arithmetic but was never issued.
/// </para>
/// <para>
/// Input may be formatted or not, with 9 or 11 significant digits: dots, dashes, slashes and
/// spaces are ignored; any other character makes the value invalid. All formatting is
/// culture-invariant.
/// </para>
/// <para>
/// RENAVAM has no official punctuation; both the "G" and "N" formats return the 11 canonical
/// digits (e.g. <c>00639884962</c>).
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Renavam()"/> represents the
/// all-zero digits "00000000000", which is never a valid RENAVAM.
/// </para>
/// </remarks>
public readonly record struct Renavam : IEquatable<Renavam>, ISpanFormattable, ISpanParsable<Renavam>
{
    private const int DigitsLength = 11;
    private const int LegacyDigitsLength = 9;

    // mod-11 weights over the 10-digit base, left to right; equivalent to the 2–9 cycle
    // applied from right to left.
    private static readonly int[] s_weights = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    // The 11 canonical digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly long _digits;

    private Renavam(long digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a valid RENAVAM.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted RENAVAM text with 9 (legacy) or 11 digits; dots, dashes,
    /// slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has 9 or 11 digits, is not an all-same-digit
    /// sequence and the check digit matches.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryValidate(value, digits);
    }

    /// <summary>Attempts to parse a RENAVAM, formatted or not, in the 9- or 11-digit format.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored. A 9-digit value is
    /// padded with leading zeros to the 11-digit canonical form.
    /// </param>
    /// <param name="result">The parsed RENAVAM when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid RENAVAM.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Renavam result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            result = default;
            return false;
        }
        result = new Renavam(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a RENAVAM, throwing when the text is not valid.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored. A 9-digit value is
    /// padded with leading zeros to the 11-digit canonical form.
    /// </param>
    /// <returns>The parsed RENAVAM.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid RENAVAM.</exception>
    public static Renavam Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a RENAVAM from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; RENAVAM parsing is culture-invariant.</param>
    /// <returns>The parsed RENAVAM.</returns>
    /// <exception cref="FormatException">The text is not a valid RENAVAM.</exception>
    public static Renavam Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Renavam IParsable<Renavam>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a RENAVAM from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Renavam)"/>.
    /// </summary>
    static bool IParsable<Renavam>.TryParse(string? s, IFormatProvider? provider, out Renavam result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a RENAVAM from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Renavam)"/>.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; RENAVAM parsing is culture-invariant.</param>
    /// <param name="result">The parsed RENAVAM when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid RENAVAM.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Renavam result) => TryParse(s, out result);

    /// <summary>Returns the RENAVAM's 11 canonical digits.</summary>
    /// <returns>The 11 digits, e.g. "00639884962".</returns>
    /// <remarks>RENAVAM has no official punctuation, so "G" and "N" produce the same output.</remarks>
    public override string ToString() => ToString("N", CultureInfo.InvariantCulture);

    /// <summary>Formats the RENAVAM.</summary>
    /// <param name="format">
    /// "G"/"N" (or an empty/<see langword="null"/> format) produce the 11 digits; RENAVAM has
    /// no official punctuation. Matching is case-insensitive.
    /// </param>
    /// <returns>The 11 canonical digits.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the RENAVAM; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/"N"/empty/<see langword="null"/>, all producing the 11 digits.</param>
    /// <param name="formatProvider">Ignored; RENAVAM formatting is culture-invariant.</param>
    /// <returns>The 11 canonical digits.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        ResolveFormat(format);
        return string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer));
    }

    /// <summary>Writes the RENAVAM into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/"N"/empty, all producing the 11 digits.</param>
    /// <param name="provider">Ignored; RENAVAM formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        ResolveFormat(format);
        if (destination.Length < DigitsLength)
        {
            charsWritten = 0;
            return false;
        }
        charsWritten = Write(destination);
        return true;
    }

    /// <summary>Generates a random valid RENAVAM, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A RENAVAM with a random 10-digit base (never all-same) and a matching check digit.</returns>
    public static Renavam Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> digits = stackalloc char[DigitsLength];
        do
        {
            for (int i = 0; i < 10; i++)
            {
                digits[i] = (char)('0' + random.Next(10));
            }
        }
        while (IsAllSame(digits[..10]));
        digits[10] = (char)('0' + CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..10], s_weights)));
        return new Renavam(PackDigits(digits));
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> digits)
    {
        if (!DocumentText.TryNormalizeDigits(value, digits, out int written))
        {
            return false;
        }
        if (written == LegacyDigitsLength)
        {
            // Legacy 9-digit RENAVAM: pad with two leading zeros to the 11-digit form.
            digits[..LegacyDigitsLength].CopyTo(digits[2..]);
            digits[0] = '0';
            digits[1] = '0';
        }
        else if (written != DigitsLength)
        {
            return false;
        }
        if (IsAllSame(digits))
        {
            return false;
        }
        return CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..10], s_weights)) == digits[10] - '0';
    }

    private static Renavam ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid RENAVAM.");
        }
        return new Renavam(PackDigits(digits));
    }

    private static void ResolveFormat(ReadOnlySpan<char> format)
    {
        bool valid = format.Length switch
        {
            0 => true,
            1 => format[0] is 'G' or 'g' or 'N' or 'n',
            _ => false,
        };
        if (!valid)
        {
            throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a RENAVAM; use \"G\" or \"N\" (the 11 digits).");
        }
    }

    private static long PackDigits(ReadOnlySpan<char> digits)
    {
        long packed = 0;
        foreach (char c in digits)
        {
            packed = packed * 10 + (c - '0');
        }
        return packed;
    }

    private static bool IsAllSame(ReadOnlySpan<char> chars)
    {
        char first = chars[0];
        foreach (char c in chars)
        {
            if (c != first)
            {
                return false;
            }
        }
        return true;
    }

    private int Write(Span<char> destination)
    {
        long value = _digits;
        Span<char> digits = stackalloc char[DigitsLength];
        for (int i = DigitsLength - 1; i >= 0; i--)
        {
            digits[i] = (char)('0' + value % 10);
            value /= 10;
        }
        digits.CopyTo(destination);
        return DigitsLength;
    }
}
