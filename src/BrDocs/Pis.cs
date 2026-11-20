using System.Globalization;

namespace BrDocs;

/// <summary>
/// PIS/PASEP/NIT — the Brazilian worker registration number (Programa de Integração Social,
/// Programa de Formação do Patrimônio do Servidor Público or Número de Identificação do
/// Trabalhador). PIS, PASEP, NIT and NIS are four names for the same 11-digit number.
/// </summary>
/// <remarks>
/// <para>
/// A PIS/PASEP/NIT has 11 digits — a 10-digit base followed by one check digit (DV) — and is
/// formatted as "NNN.NNNNN.NN-D" (e.g. <c>120.70458.82-4</c>). PIS numbers are registered by
/// Caixa Econômica Federal, PASEP by Banco do Brasil and NIT/NIS by INSS; the format and the
/// check-digit algorithm are identical.
/// </para>
/// <para>
/// Check-digit rule (mod 11): each of the first 10 digits is multiplied by the weights
/// <c>3, 2, 9, 8, 7, 6, 5, 4, 3, 2</c> (left to right) and the products are summed;
/// <c>r = sum % 11</c>; the DV is <c>0</c> when <c>r &lt; 2</c> and <c>11 − r</c> otherwise.
/// No official publication spells out the weights: the eSocial Manual de Orientação and the
/// SIRC manual state only that the number has 11 digits with a mod-11 check digit, and Caixa
/// layouts ask for a "Número de PIS/PASEP válido" without the arithmetic; the weights above
/// are the ones used by every major community reference (brutils, brazilian-utils, COBOL
/// payroll systems) and are verified by known vectors such as <c>120.5641.284-7</c>.
/// </para>
/// <para>
/// Sequences of 11 equal digits (such as <c>000.00000.00-0</c>) are rejected: the all-zero
/// value satisfies the DV arithmetic but was never issued, mirroring the reserved CPF and
/// CNPJ sequences.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Pis()"/> represents the
/// all-zero digits "000.00000.00-0", which is never a valid PIS.
/// </para>
/// </remarks>
public readonly record struct Pis : IEquatable<Pis>, ISpanFormattable, ISpanParsable<Pis>
{
    private const int DigitsLength = 11;
    private const int FormattedLength = 14;

    // mod-11 weights over the 10-digit base, left to right.
    private static readonly int[] s_weights = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    // The 11 digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly long _digits;

    private Pis(long digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a valid PIS/PASEP/NIT.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted PIS text; dots, dashes, slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 11 digits, is not an all-same-digit
    /// sequence and the check digit matches.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryValidate(value, digits);
    }

    /// <summary>Attempts to parse a PIS/PASEP/NIT, formatted or not.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed PIS when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid PIS.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Pis result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            result = default;
            return false;
        }
        result = new Pis(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a PIS/PASEP/NIT, throwing when the text is not valid.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>The parsed PIS.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid PIS.</exception>
    public static Pis Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a PIS/PASEP/NIT from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; PIS parsing is culture-invariant.</param>
    /// <returns>The parsed PIS.</returns>
    /// <exception cref="FormatException">The text is not a valid PIS.</exception>
    public static Pis Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Pis IParsable<Pis>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a PIS from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Pis)"/>.
    /// </summary>
    static bool IParsable<Pis>.TryParse(string? s, IFormatProvider? provider, out Pis result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a PIS from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Pis)"/>.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; PIS parsing is culture-invariant.</param>
    /// <param name="result">The parsed PIS when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid PIS.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Pis result) => TryParse(s, out result);

    /// <summary>Returns the PIS formatted as "NNN.NNNNN.NN-D".</summary>
    /// <returns>The formatted PIS, e.g. "120.70458.82-4".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the PIS.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NNN.NNNNN.NN-D";
    /// "N" produces the 11 digits only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted PIS.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the PIS; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" digits only.</param>
    /// <param name="formatProvider">Ignored; PIS formatting is culture-invariant.</param>
    /// <returns>The formatted PIS.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the PIS into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NNN.NNNNN.NN-D"; "N" for the digits only.</param>
    /// <param name="provider">Ignored; PIS formatting is culture-invariant.</param>
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

    /// <summary>Generates a random valid PIS, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A PIS with a random 10-digit base (never all-same) and a matching check digit.</returns>
    public static Pis Generate(Random? rng = null)
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
        return new Pis(PackDigits(digits));
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
        return CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(digits[..10], s_weights)) == digits[10] - '0';
    }

    private static Pis ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid PIS/PASEP/NIT.");
        }
        return new Pis(PackDigits(digits));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a PIS; use \"G\" (formatted) or \"N\" (digits only)."),
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
        digits[3..8].CopyTo(destination[4..]);
        destination[9] = '.';
        digits[8..10].CopyTo(destination[10..]);
        destination[12] = '-';
        destination[13] = digits[10];
        return FormattedLength;
    }
}
