using System.Globalization;

namespace BrDocs;

/// <summary>
/// Título de Eleitor — the Brazilian voter registration number, assigned by the Justiça
/// Eleitoral (TSE).
/// </summary>
/// <remarks>
/// <para>
/// A título has 12 digits: an 8-digit sequential number (digits 1–8), the federative-unit
/// code of the inscrição (digits 9–10, <c>01</c>–<c>28</c>, where 28 is "ZZ" for voters
/// registered abroad) and two check digits (digits 11–12). It is displayed with the grouping
/// "NNNN NNNN NN DD" (e.g. <c>1234 5678 20 97</c>). Only the 12-digit form is accepted: a few
/// historical São Paulo/Minas Gerais titles carry a 9-digit sequential (13 digits in total),
/// and old printed cards omit leading zeros — left-pad such values to 12 digits before
/// parsing.
/// </para>
/// <para>
/// Check-digit rule (Resolução TSE nº 23.659/2021, art. 36, which fixes the layout and the
/// two "módulo 11" check digits; the weights and the state rule below are the ones used by
/// the Justiça Eleitoral systems, per the community-documented algorithm): DV1 multiplies
/// the sequential digits by the weights <c>2, 3, 4, 5, 6, 7, 8, 9</c> (left to right) and is
/// the remainder itself, except that a remainder of 10 yields 0 — and, for São Paulo
/// (<c>01</c>) and Minas Gerais (<c>02</c>), a remainder of 0 yields 1 instead of 0. DV2
/// multiplies the two UF digits and DV1 by the weights <c>7, 8, 9</c> and follows the same
/// remainder rule, including the São Paulo/Minas Gerais exception. Verified against the
/// known vectors <c>1023 8501 06 71</c> and <c>1111 1111 01 16</c>.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new TituloEleitor()"/>
/// represents the all-zero digits "0000 0000 00 00", which is never a valid título (the UF
/// code 00 does not exist).
/// </para>
/// </remarks>
public readonly record struct TituloEleitor : IEquatable<TituloEleitor>, ISpanFormattable, ISpanParsable<TituloEleitor>
{
    private const int DigitsLength = 12;
    private const int FormattedLength = 15;

    // mod-11 weights: DV1 over the 8-digit sequential; DV2 over the UF code + DV1.
    private static readonly int[] s_weightsDv1 = { 2, 3, 4, 5, 6, 7, 8, 9 };
    private static readonly int[] s_weightsDv2 = { 7, 8, 9 };

    // The 12 digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly long _digits;

    private TituloEleitor(long digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a valid Título de Eleitor.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted título text; dots, dashes, slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 12 digits, the UF code (digits 9–10)
    /// is in the range 01–28 and both check digits match, including the São Paulo/Minas
    /// Gerais remainder-zero rule.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryValidate(value, digits);
    }

    /// <summary>Attempts to parse a Título de Eleitor, formatted or not.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed título when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid título.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out TituloEleitor result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            result = default;
            return false;
        }
        result = new TituloEleitor(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a Título de Eleitor, throwing when the text is not valid.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>The parsed título.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid título.</exception>
    public static TituloEleitor Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a título from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; título parsing is culture-invariant.</param>
    /// <returns>The parsed título.</returns>
    /// <exception cref="FormatException">The text is not a valid título.</exception>
    public static TituloEleitor Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static TituloEleitor IParsable<TituloEleitor>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a título from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see
    /// <see cref="TryParse(ReadOnlySpan{char}, out TituloEleitor)"/>.
    /// </summary>
    static bool IParsable<TituloEleitor>.TryParse(string? s, IFormatProvider? provider, out TituloEleitor result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a título from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out TituloEleitor)"/>.</summary>
    /// <param name="s">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="provider">Ignored; título parsing is culture-invariant.</param>
    /// <param name="result">The parsed título when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid título.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TituloEleitor result) => TryParse(s, out result);

    /// <summary>Returns the título formatted as "NNNN NNNN NN DD".</summary>
    /// <returns>The formatted título, e.g. "1234 5678 20 97".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the título.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NNNN NNNN NN DD";
    /// "N" produces the 12 digits only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted título.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the título; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" digits only.</param>
    /// <param name="formatProvider">Ignored; título formatting is culture-invariant.</param>
    /// <returns>The formatted título.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the título into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NNNN NNNN NN DD"; "N" for the digits only.</param>
    /// <param name="provider">Ignored; título formatting is culture-invariant.</param>
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
    /// The numeric UF code of the inscrição (digits 9–10), from 1 (SP) to 28 (ZZ, exterior).
    /// </summary>
    public int UfCode => (int)((_digits / 100) % 100);

    /// <summary>
    /// The UF sigla of the inscrição, per the Justiça Eleitoral table — e.g. "SP" for code 01,
    /// "DF" for 20 and "ZZ" (exterior) for 28 — or <see langword="null"/> when the instance
    /// does not carry a valid UF code (only <see langword="default"/> or an invalid value).
    /// </summary>
    public string? Uf => TituloEleitorUfTable.TryGetUf(UfCode, out string? uf) ? uf : null;

    /// <summary>Generates a random valid título, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>
    /// A título with a random 8-digit sequential, a random UF code in 1..28 and matching
    /// check digits (including the São Paulo/Minas Gerais rule when applicable).
    /// </returns>
    public static TituloEleitor Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> digits = stackalloc char[DigitsLength];
        for (int i = 0; i < 8; i++)
        {
            digits[i] = (char)('0' + random.Next(10));
        }
        int ufCode = random.Next(TituloEleitorUfTable.MinCode, TituloEleitorUfTable.MaxCode + 1);
        digits[8] = (char)('0' + ufCode / 10);
        digits[9] = (char)('0' + ufCode % 10);
        WriteCheckDigits(digits);
        return new TituloEleitor(PackDigits(digits));
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> digits)
    {
        if (!DocumentText.TryNormalizeDigits(value, digits, out int written) || written != DigitsLength)
        {
            return false;
        }
        int ufCode = (digits[8] - '0') * 10 + (digits[9] - '0');
        if (ufCode is < TituloEleitorUfTable.MinCode or > TituloEleitorUfTable.MaxCode)
        {
            return false;
        }
        int dv1 = TituloDv(CheckDigit.WeightedSum(digits[..8], s_weightsDv1) % 11, ufCode);
        if (dv1 != digits[10] - '0')
        {
            return false;
        }
        return TituloDv(CheckDigit.WeightedSum(digits[8..11], s_weightsDv2) % 11, ufCode) == digits[11] - '0';
    }

    // The título DV is the remainder itself; remainder 10 yields 0, and for São Paulo (01)
    // and Minas Gerais (02) remainder 0 yields 1 (Resolução TSE nº 23.659/2021, art. 36).
    private static int TituloDv(int remainder, int ufCode) => remainder switch
    {
        10 => 0,
        0 => ufCode is 1 or 2 ? 1 : 0,
        _ => remainder,
    };

    private static TituloEleitor ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryValidate(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid Título de Eleitor.");
        }
        return new TituloEleitor(PackDigits(digits));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a Título de Eleitor; use \"G\" (formatted) or \"N\" (digits only)."),
    };

    private static void WriteCheckDigits(Span<char> digits)
    {
        int ufCode = (digits[8] - '0') * 10 + (digits[9] - '0');
        int dv1 = TituloDv(CheckDigit.WeightedSum(digits[..8], s_weightsDv1) % 11, ufCode);
        digits[10] = (char)('0' + dv1);
        int dv2 = TituloDv(CheckDigit.WeightedSum(digits[8..11], s_weightsDv2) % 11, ufCode);
        digits[11] = (char)('0' + dv2);
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
        digits[..4].CopyTo(destination);
        destination[4] = ' ';
        digits[4..8].CopyTo(destination[5..]);
        destination[9] = ' ';
        digits[8..10].CopyTo(destination[10..]);
        destination[12] = ' ';
        digits[10..].CopyTo(destination[13..]);
        return FormattedLength;
    }
}
