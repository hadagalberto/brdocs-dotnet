using System.Globalization;

namespace BrDocs;

/// <summary>
/// CPF (Cadastro de Pessoas Físicas) — the Brazilian individual taxpayer registry number.
/// </summary>
/// <remarks>
/// <para>
/// A CPF has 11 digits — a 9-digit base followed by two check digits (DVs) computed with the
/// mod-11 algorithm — and is formatted as "NNN.NNN.NNN-DD" (e.g. <c>111.444.777-35</c>).
/// Sequences of 11 equal digits (such as <c>111.111.111-11</c>) are rejected even though
/// <c>000.000.000-00</c> satisfies the pure DV arithmetic: they are not valid documents.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Cpf()"/> represents the
/// all-zero digits "000.000.000-00", which is never a valid CPF.
/// </para>
/// </remarks>
public readonly record struct Cpf : IEquatable<Cpf>, ISpanFormattable, ISpanParsable<Cpf>
{
    private const int DigitsLength = 11;
    private const int FormattedLength = 14;

    // mod-11 weights: DV1 over the 9-digit base; DV2 over the base + DV1.
    private static readonly int[] s_weightsDv1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] s_weightsDv2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

    // Powers of ten used to unpack single digits from the packed value.
    private static readonly long[] s_pow10 =
    {
        1L, 10L, 100L, 1_000L, 10_000L, 100_000L, 1_000_000L, 10_000_000L, 100_000_000L,
        1_000_000_000L, 10_000_000_000L,
    };

    // The 11 digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly long _digits;

    private Cpf(long digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a valid CPF.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted CPF text; dots, dashes, slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 11 digits, is not an all-same-digit
    /// sequence and both check digits match.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryValidate(value, digits);
    }

    /// <summary>Attempts to parse a CPF, formatted or not.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed CPF when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CPF.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Cpf result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            result = default;
            return false;
        }
        result = new Cpf(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a CPF, throwing when the text is not valid.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>The parsed CPF.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid CPF.</exception>
    public static Cpf Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a CPF from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; CPF parsing is culture-invariant.</param>
    /// <returns>The parsed CPF.</returns>
    /// <exception cref="FormatException">The text is not a valid CPF.</exception>
    public static Cpf Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Cpf IParsable<Cpf>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a CPF from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Cpf)"/>.
    /// </summary>
    static bool IParsable<Cpf>.TryParse(string? s, IFormatProvider? provider, out Cpf result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a CPF from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Cpf)"/>.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; CPF parsing is culture-invariant.</param>
    /// <param name="result">The parsed CPF when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CPF.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cpf result) => TryParse(s, out result);

    /// <summary>Returns the CPF formatted as "NNN.NNN.NNN-DD".</summary>
    /// <returns>The formatted CPF, e.g. "111.444.777-35".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the CPF.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NNN.NNN.NNN-DD";
    /// "N" produces the 11 digits only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted CPF.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the CPF; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" digits only.</param>
    /// <param name="formatProvider">Ignored; CPF formatting is culture-invariant.</param>
    /// <returns>The formatted CPF.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the CPF into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NNN.NNN.NNN-DD"; "N" for the digits only.</param>
    /// <param name="provider">Ignored; CPF formatting is culture-invariant.</param>
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

    /// <summary>
    /// The fiscal region encoded by the 9th digit, per the Receita Federal table:
    /// 0=RS; 1=DF/GO/MS/MT/TO; 2=AC/AM/AP/PA/RO/RR; 3=CE/MA/PI; 4=AL/PB/PE/RN; 5=BA/SE;
    /// 6=MG; 7=ES/RJ; 8=SP; 9=PR/SC.
    /// </summary>
    /// <remarks>
    /// Since CPF assignments became national (2020s), the region digit is historical/indicative
    /// rather than the actual state of registration.
    /// </remarks>
    public CpfFiscalRegion FiscalRegion => (CpfFiscalRegion)((int)(_digits / s_pow10[2]) % 10);

    /// <summary>Generates a random valid CPF, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A CPF with a random 9-digit base (never all-same) and matching check digits.</returns>
    public static Cpf Generate(Random? rng = null)
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
        digits[9] = (char)('0' + CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..9], s_weightsDv1)));
        digits[10] = (char)('0' + CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..10], s_weightsDv2)));
        return new Cpf(PackDigits(digits));
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
        if (CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..9], s_weightsDv1)) != digits[9] - '0')
        {
            return false;
        }
        return CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..10], s_weightsDv2)) == digits[10] - '0';
    }

    private static Cpf ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid CPF.");
        }
        return new Cpf(PackDigits(digits));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a CPF; use \"G\" (formatted) or \"N\" (digits only)."),
    };

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
        digits[..3].CopyTo(destination);
        destination[3] = '.';
        digits[3..6].CopyTo(destination[4..]);
        destination[7] = '.';
        digits[6..9].CopyTo(destination[8..]);
        destination[11] = '-';
        digits[9..].CopyTo(destination[12..]);
        return FormattedLength;
    }
}
