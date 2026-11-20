using System.Globalization;

namespace BrDocs;

/// <summary>
/// CNH — the Carteira Nacional de Habilitação registration number, the 11-digit identifier of
/// the Brazilian driver's license.
/// </summary>
/// <remarks>
/// <para>
/// A CNH registration number has 11 digits — a 9-digit base assigned by the state Detran
/// followed by two check digits (DVs) — and is usually written as "NNNNNNNNN-DD"
/// (e.g. <c>026503064-61</c>). It is not the RENACH, the national lifetime registry number,
/// which follows the driver across renewals.
/// </para>
/// <para>
/// Check-digit rule (mod 11, as used by DENATRAN/SENATRAN; the layout "nove caracteres e dois
/// dígitos verificadores" is in Resolução CONTRAN nº 886/2021, art. 4º, and Resolução CONTRAN
/// nº 1.020/2025, art. 10 — neither publishes the weights, which follow the community
/// reference used by every major validator): DV1 multiplies the 9 base digits by the weights
/// <c>9, 8, 7, 6, 5, 4, 3, 2, 1</c> and takes <c>sum % 11</c> as the DV, except that a
/// remainder of 10 gives DV 0. When that happens (the "decrement rule"), DV2 is computed with
/// a compensation of 2: DV2 multiplies the same 9 digits by the weights
/// <c>1, 2, 3, 4, 5, 6, 7, 8, 9</c>, takes <c>(sum % 11) − 2</c>, adds 11 back when the
/// result is negative, and yields 0 when the adjusted value is 10. Without the compensation
/// roughly one CNH in eleven fails, so <c>012345678-07</c> is valid while <c>012345678-09</c>
/// is not. Verified against the known vectors <c>000000001-19</c> and <c>026503064-61</c>.
/// </para>
/// <para>
/// Sequences of 11 equal digits (such as <c>000000000-00</c> and <c>111111111-11</c>) are
/// rejected: they satisfy the DV arithmetic by construction but were never issued.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Cnh()"/> represents the
/// all-zero digits "000000000-00", which is never a valid CNH.
/// </para>
/// </remarks>
public readonly record struct Cnh : IEquatable<Cnh>, ISpanFormattable, ISpanParsable<Cnh>
{
    private const int DigitsLength = 11;
    private const int FormattedLength = 12;

    // mod-11 weights: DV1 over the 9-digit base (descending), DV2 over the same base (ascending).
    private static readonly int[] s_weightsDv1 = { 9, 8, 7, 6, 5, 4, 3, 2, 1 };
    private static readonly int[] s_weightsDv2 = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    // The 11 digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly long _digits;

    private Cnh(long digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a valid CNH registration number.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted CNH text; dots, dashes, slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 11 digits, is not an all-same-digit
    /// sequence and both check digits match (including the decrement rule).
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryValidate(value, digits);
    }

    /// <summary>Attempts to parse a CNH registration number, formatted or not.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed CNH when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CNH.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Cnh result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            result = default;
            return false;
        }
        result = new Cnh(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a CNH registration number, throwing when the text is not valid.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>The parsed CNH.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid CNH.</exception>
    public static Cnh Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a CNH from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; CNH parsing is culture-invariant.</param>
    /// <returns>The parsed CNH.</returns>
    /// <exception cref="FormatException">The text is not a valid CNH.</exception>
    public static Cnh Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Cnh IParsable<Cnh>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a CNH from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Cnh)"/>.
    /// </summary>
    static bool IParsable<Cnh>.TryParse(string? s, IFormatProvider? provider, out Cnh result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a CNH from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Cnh)"/>.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; CNH parsing is culture-invariant.</param>
    /// <param name="result">The parsed CNH when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CNH.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cnh result) => TryParse(s, out result);

    /// <summary>Returns the CNH formatted as "NNNNNNNNN-DD".</summary>
    /// <returns>The formatted CNH, e.g. "026503064-61".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the CNH.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NNNNNNNNN-DD";
    /// "N" produces the 11 digits only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted CNH.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the CNH; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" digits only.</param>
    /// <param name="formatProvider">Ignored; CNH formatting is culture-invariant.</param>
    /// <returns>The formatted CNH.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the CNH into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NNNNNNNNN-DD"; "N" for the digits only.</param>
    /// <param name="provider">Ignored; CNH formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bool formatted = ResolveFormat(format);
        int requiredLength = formatted ? FormattedLength : DigitsLength;
        if (destination.Length < requiredLength)
        {
            charsWritten = 0;
            return false;
        }
        charsWritten = Write(destination, formatted);
        return true;
    }

    /// <summary>Generates a random valid CNH registration number, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A CNH with a random 9-digit base (never all-same) and matching check digits.</returns>
    public static Cnh Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> digits = stackalloc char[DigitsLength];
        do
        {
            for (int i = 0; i < 9; i++)
            {
                digits[i] = (char)('0' + random.Next(10));
            }
        }
        while (IsAllSame(digits[..9]));
        WriteCheckDigits(digits);
        return new Cnh(PackDigits(digits));
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> digits)
    {
        if (!DocumentText.TryNormalizeDigits(value, digits, out int written) || written != DigitsLength)
        {
            return false;
        }
        if (IsAllSame(digits))
        {
            return false;
        }
        int remainder1 = CheckDigit.WeightedSum(digits[..9], s_weightsDv1) % 11;
        int dv1 = remainder1 > 9 ? 0 : remainder1;
        if (dv1 != digits[9] - '0')
        {
            return false;
        }
        // Decrement rule: when the first remainder is 10 the second DV is computed with a
        // compensation of 2 (r < 0 wraps back by 11; 10 still yields 0).
        int adjusted = (CheckDigit.WeightedSum(digits[..9], s_weightsDv2) % 11) - (remainder1 > 9 ? 2 : 0);
        if (adjusted < 0)
        {
            adjusted += 11;
        }
        return (adjusted > 9 ? 0 : adjusted) == digits[10] - '0';
    }

    private static Cnh ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid CNH.");
        }
        return new Cnh(PackDigits(digits));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a CNH; use \"G\" (formatted) or \"N\" (digits only)."),
    };

    private static void WriteCheckDigits(Span<char> digits)
    {
        int remainder1 = CheckDigit.WeightedSum(digits[..9], s_weightsDv1) % 11;
        int dv1 = remainder1 > 9 ? 0 : remainder1;
        int adjusted = (CheckDigit.WeightedSum(digits[..9], s_weightsDv2) % 11) - (remainder1 > 9 ? 2 : 0);
        if (adjusted < 0)
        {
            adjusted += 11;
        }
        digits[9] = (char)('0' + dv1);
        digits[10] = (char)('0' + (adjusted > 9 ? 0 : adjusted));
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

    private int Write(Span<char> destination, bool formatted)
    {
        long value = _digits;
        Span<char> digits = stackalloc char[DigitsLength];
        for (int i = DigitsLength - 1; i >= 0; i--)
        {
            digits[i] = (char)('0' + value % 10);
            value /= 10;
        }
        if (!formatted)
        {
            digits.CopyTo(destination);
            return DigitsLength;
        }
        digits[..9].CopyTo(destination);
        destination[9] = '-';
        digits[9..].CopyTo(destination[10..]);
        return FormattedLength;
    }
}
