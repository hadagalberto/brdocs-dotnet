using System.Globalization;

namespace BrDocs;

/// <summary>
/// CNPJ (Cadastro Nacional da Pessoa Jurídica) — the Brazilian legal entity registry number,
/// in the numeric format and in the new alphanumeric format (Receita Federal IN RFB nº
/// 2.229/2024, issued from July 2026).
/// </summary>
/// <remarks>
/// <para>
/// A CNPJ has 14 characters: a 12-character base — the 8-character registration root followed by
/// the 4-character branch, each an uppercase ASCII letter (A–Z) or a digit — followed by two
/// numeric check digits (DVs). It is formatted as "NN.NNN.NNN/NNNN-DD" (e.g.
/// <c>11.222.333/0001-81</c>, or <c>12.ABC.345/01DE-35</c> in the alphanumeric format).
/// Lowercase input is normalized to uppercase.
/// </para>
/// <para>
/// The check digits use each character's value <c>(int)c - 48</c> (so '0'..'9' contribute 0..9 and
/// 'A'..'Z' contribute 17..42), with weights <c>5,4,3,2,9,8,7,6,5,4,3,2</c> for DV1 and
/// <c>6,5,4,3,2,9,8,7,6,5,4,3,2</c> for DV2; the remainder rule is <c>r &lt; 2 → 0</c>, otherwise
/// <c>11 − r</c>. Fully numeric CNPJs never have 14 equal digits, which rejects
/// <c>00.000.000/0000-00</c> (the all-zero value that satisfies the pure DV arithmetic) and the
/// other all-same numeric sequences.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Cnpj()"/> represents the
/// all-zero characters "00.000.000/0000-00", which is never a valid CNPJ.
/// </para>
/// </remarks>
public readonly record struct Cnpj : IEquatable<Cnpj>, ISpanFormattable, ISpanParsable<Cnpj>
{
    private const int TotalLength = 14;    // 12 base characters + 2 numeric DVs.
    private const int BaseLength = 12;     // root (8) + branch (4).
    private const int RootLength = 8;
    private const int BranchLength = 4;
    private const int FormattedLength = 18;

    // mod-11 weights: DV1 over the 12-character base; DV2 over the base + DV1.
    private static readonly int[] s_weightsDv1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] s_weightsDv2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    // The 14 characters, 6 bits each: chars 0..9 in _lo, chars 10..13 in _hi.
    // A char maps to 0..9 for '0'..'9' and 10..35 for 'A'..'Z'.
    private readonly ulong _lo;
    private readonly ulong _hi;

    private Cnpj(ulong lo, ulong hi)
    {
        _lo = lo;
        _hi = hi;
    }

    /// <summary>
    /// Determines whether the given text represents a valid CNPJ, numeric or alphanumeric.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted CNPJ text; dots, dashes, slashes and spaces are ignored and
    /// lowercase letters are normalized to uppercase.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 14 valid characters, ends with two
    /// numeric check digits that match, and — when fully numeric — is not an all-same sequence.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> chars = stackalloc char[TotalLength];
        return TryValidate(value, chars);
    }

    /// <summary>Attempts to parse a CNPJ, formatted or not, numeric or alphanumeric.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored and lowercase letters are
    /// normalized to uppercase.
    /// </param>
    /// <param name="result">The parsed CNPJ when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CNPJ.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Cnpj result)
    {
        Span<char> chars = stackalloc char[TotalLength];
        if (!TryValidate(value, chars))
        {
            result = default;
            return false;
        }
        (ulong lo, ulong hi) = Pack(chars);
        result = new Cnpj(lo, hi);
        return true;
    }

    /// <summary>Parses a CNPJ, throwing when the text is not valid.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored and lowercase letters are
    /// normalized to uppercase.
    /// </param>
    /// <returns>The parsed CNPJ.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid CNPJ.</exception>
    public static Cnpj Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a CNPJ from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; CNPJ parsing is culture-invariant.</param>
    /// <returns>The parsed CNPJ.</returns>
    /// <exception cref="FormatException">The text is not a valid CNPJ.</exception>
    public static Cnpj Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Cnpj IParsable<Cnpj>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a CNPJ from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Cnpj)"/>.
    /// </summary>
    static bool IParsable<Cnpj>.TryParse(string? s, IFormatProvider? provider, out Cnpj result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a CNPJ from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Cnpj)"/>.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; CNPJ parsing is culture-invariant.</param>
    /// <param name="result">The parsed CNPJ when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid CNPJ.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cnpj result) => TryParse(s, out result);

    /// <summary>Returns the CNPJ formatted as "NN.NNN.NNN/NNNN-DD".</summary>
    /// <returns>The formatted CNPJ, e.g. "11.222.333/0001-81" or "12.ABC.345/01DE-35".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the CNPJ.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NN.NNN.NNN/NNNN-DD";
    /// "N" produces the 14 characters only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted CNPJ.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the CNPJ; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" characters only.</param>
    /// <param name="formatProvider">Ignored; CNPJ formatting is culture-invariant.</param>
    /// <returns>The formatted CNPJ.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(TotalLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the CNPJ into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NN.NNN.NNN/NNNN-DD"; "N" for the characters only.</param>
    /// <param name="provider">Ignored; CNPJ formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bool formatted = ResolveFormat(format);
        int requiredLength = formatted ? FormattedLength : TotalLength;
        if (destination.Length < requiredLength)
        {
            charsWritten = 0;
            return false;
        }
        charsWritten = Write(destination, formatted);
        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the CNPJ base (root + branch) contains at least one
    /// letter, i.e. it uses the alphanumeric format of IN RFB nº 2.229/2024.
    /// </summary>
    public bool IsAlphanumeric
    {
        get
        {
            for (int i = 0; i < BaseLength; i++)
            {
                if (CharAt(i) >= 'A')
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>The 8-character registration root (e.g. "12ABC345" for 12.ABC.345/01DE-35).</summary>
    public string Root
    {
        get
        {
            Span<char> chars = stackalloc char[RootLength];
            for (int i = 0; i < RootLength; i++)
            {
                chars[i] = CharAt(i);
            }
            return new string(chars);
        }
    }

    /// <summary>The 4-character branch/order (e.g. "01DE").</summary>
    public string Branch
    {
        get
        {
            Span<char> chars = stackalloc char[BranchLength];
            for (int i = 0; i < BranchLength; i++)
            {
                chars[i] = CharAt(RootLength + i);
            }
            return new string(chars);
        }
    }

    /// <summary>Returns <see langword="true"/> when the branch is "0001" (headquarters).</summary>
    public bool IsHeadOffice => CharAt(8) == '0' && CharAt(9) == '0' && CharAt(10) == '0' && CharAt(11) == '1';

    /// <summary>Generates a random valid numeric CNPJ, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A CNPJ with a random 12-digit base (never all-same) and matching check digits.</returns>
    public static Cnpj Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> chars = stackalloc char[TotalLength];
        do
        {
            for (int i = 0; i < BaseLength; i++)
            {
                chars[i] = (char)('0' + random.Next(10));
            }
        }
        while (IsAllSame(chars[..BaseLength]));
        WriteCheckDigits(chars);
        (ulong lo, ulong hi) = Pack(chars);
        return new Cnpj(lo, hi);
    }

    /// <summary>
    /// Generates a random valid alphanumeric CNPJ: the 12-character base is drawn from
    /// "0"–"9" and "A"–"Z" and always contains at least one letter, so
    /// <see cref="IsAlphanumeric"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>An alphanumeric CNPJ with matching check digits.</returns>
    public static Cnpj GenerateAlphanumeric(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> chars = stackalloc char[TotalLength];
        bool anyLetter = false;
        for (int i = 0; i < BaseLength; i++)
        {
            int symbol = random.Next(36);
            if (symbol < 10)
            {
                chars[i] = (char)('0' + symbol);
            }
            else
            {
                chars[i] = (char)('A' + symbol - 10);
                anyLetter = true;
            }
        }
        if (!anyLetter)
        {
            // The base came out fully numeric; force at least one letter so the result is
            // guaranteed to use the alphanumeric format.
            chars[random.Next(BaseLength)] = (char)('A' + random.Next(26));
        }
        WriteCheckDigits(chars);
        (ulong lo, ulong hi) = Pack(chars);
        return new Cnpj(lo, hi);
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> chars)
    {
        if (!DocumentText.TryNormalizeUpperAlphanumeric(value, chars, out int written) || written != TotalLength)
        {
            return false;
        }
        // The two DVs are always numeric, even in the alphanumeric format (IN RFB nº 2.229/2024).
        if (!char.IsAsciiDigit(chars[12]) || !char.IsAsciiDigit(chars[13]))
        {
            return false;
        }
        // Fully numeric CNPJs never have 14 equal digits (covers "00.000.000/0000-00").
        if (IsAllNumeric(chars) && IsAllSame(chars))
        {
            return false;
        }
        if (CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(chars[..BaseLength], s_weightsDv1)) != chars[12] - '0')
        {
            return false;
        }
        return CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(chars[..13], s_weightsDv2)) == chars[13] - '0';
    }

    private static Cnpj ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> chars = stackalloc char[TotalLength];
        if (!TryValidate(value, chars))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid CNPJ.");
        }
        (ulong lo, ulong hi) = Pack(chars);
        return new Cnpj(lo, hi);
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a CNPJ; use \"G\" (formatted) or \"N\" (digits only)."),
    };

    private static void WriteCheckDigits(Span<char> chars)
    {
        chars[12] = (char)('0' + CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(chars[..BaseLength], s_weightsDv1)));
        chars[13] = (char)('0' + CheckDigit.Mod11CheckDigit(CheckDigit.WeightedSum(chars[..13], s_weightsDv2)));
    }

    private static (ulong Lo, ulong Hi) Pack(ReadOnlySpan<char> chars)
    {
        ulong lo = 0, hi = 0;
        for (int i = 0; i < 10; i++)
        {
            lo |= (ulong)CharToIndex(chars[i]) << (6 * i);
        }
        for (int i = 10; i < TotalLength; i++)
        {
            hi |= (ulong)CharToIndex(chars[i]) << (6 * (i - 10));
        }
        return (lo, hi);
    }

    private static int CharToIndex(char c) => c <= '9' ? c - '0' : c - 'A' + 10;

    private static char IndexToChar(int index) => index < 10
        ? (char)('0' + index)
        : (char)('A' + index - 10);

    private char CharAt(int index) => IndexToChar((int)((index < 10 ? _lo >> (6 * index) : _hi >> (6 * (index - 10))) & 0x3FUL));

    private static bool IsAllNumeric(ReadOnlySpan<char> chars)
    {
        foreach (char c in chars)
        {
            if (c > '9')
            {
                return false;
            }
        }
        return true;
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
        if (!formatted)
        {
            for (int i = 0; i < TotalLength; i++)
            {
                destination[i] = CharAt(i);
            }
            return TotalLength;
        }
        destination[0] = CharAt(0);
        destination[1] = CharAt(1);
        destination[2] = '.';
        destination[3] = CharAt(2);
        destination[4] = CharAt(3);
        destination[5] = CharAt(4);
        destination[6] = '.';
        destination[7] = CharAt(5);
        destination[8] = CharAt(6);
        destination[9] = CharAt(7);
        destination[10] = '/';
        destination[11] = CharAt(8);
        destination[12] = CharAt(9);
        destination[13] = CharAt(10);
        destination[14] = CharAt(11);
        destination[15] = '-';
        destination[16] = CharAt(12);
        destination[17] = CharAt(13);
        return FormattedLength;
    }
}
