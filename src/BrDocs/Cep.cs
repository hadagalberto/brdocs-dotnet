using System.Globalization;

namespace BrDocs;

/// <summary>
/// CEP (Código de Endereçamento Postal) — the 8-digit Brazilian postal code managed by Correios.
/// </summary>
/// <remarks>
/// <para>
/// A CEP has 8 digits and is formatted as "NNNNN-DDD" (e.g. <c>01310-100</c>). It has no check
/// digits: any text with exactly 8 digits is structurally valid, including <c>00000-000</c>.
/// </para>
/// <para>
/// Input may be formatted or not: dots, dashes, slashes and spaces are ignored; any other
/// character makes the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// <see cref="Uf"/> resolves the federative unit that owns the 5-digit prefix per the Correios
/// ranges. Prefixes not assigned to any state (00000–00999 and 78900–78999) yield
/// <see langword="null"/>; SP owns the single block 01000–19999 even though no city uses 10xxx,
/// and MT ends at 78899.
/// </para>
/// </remarks>
public readonly record struct Cep : IEquatable<Cep>, ISpanFormattable, ISpanParsable<Cep>
{
    private const int DigitsLength = 8;
    private const int FormattedLength = 9;

    // The 8 digits as a single decimal-packed value; leading zeros are preserved by formatting.
    private readonly uint _digits;

    private Cep(uint digits) => _digits = digits;

    /// <summary>
    /// Determines whether the given text represents a structurally valid CEP: exactly 8 digits
    /// once the formatting characters are ignored.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted CEP text; dots, dashes, slashes and spaces are ignored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 8 digits. This does not mean the CEP is
    /// assigned to a real address; use <see cref="Uf"/> to check whether the prefix is inside a
    /// known Correios range.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        return TryNormalize(value, digits);
    }

    /// <summary>Attempts to parse a CEP, formatted or not.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed CEP when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a structurally valid CEP.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Cep result)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryNormalize(value, digits))
        {
            result = default;
            return false;
        }
        result = new Cep(PackDigits(digits));
        return true;
    }

    /// <summary>Parses a CEP, throwing when the text is not valid.</summary>
    /// <param name="value">The text to parse; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>The parsed CEP.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a structurally valid CEP.</exception>
    public static Cep Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a CEP from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; CEP parsing is culture-invariant.</param>
    /// <returns>The parsed CEP.</returns>
    /// <exception cref="FormatException">The text is not a structurally valid CEP.</exception>
    public static Cep Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Cep IParsable<Cep>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a CEP from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Cep)"/>.
    /// </summary>
    static bool IParsable<Cep>.TryParse(string? s, IFormatProvider? provider, out Cep result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a CEP from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Cep)"/>.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; CEP parsing is culture-invariant.</param>
    /// <param name="result">The parsed CEP when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a structurally valid CEP.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cep result) => TryParse(s, out result);

    /// <summary>Returns the CEP formatted as "NNNNN-DDD".</summary>
    /// <returns>The formatted CEP, e.g. "01310-100".</returns>
    public override string ToString() => string.Create(FormattedLength, this, static (buffer, state) => state.Write(buffer, formatted: true));

    /// <summary>Formats the CEP.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces "NNNNN-DDD";
    /// "N" produces the 8 digits only. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted CEP.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the CEP; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted, "N" digits only.</param>
    /// <param name="formatProvider">Ignored; CEP formatting is culture-invariant.</param>
    /// <returns>The formatted CEP.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(DigitsLength, this, static (buffer, state) => state.Write(buffer, formatted: false));

    /// <summary>Writes the CEP into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for "NNNNN-DDD"; "N" for the digits only.</param>
    /// <param name="provider">Ignored; CEP formatting is culture-invariant.</param>
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
    /// The federative unit (UF) that owns the CEP's 5-digit prefix, per the Correios ranges —
    /// e.g. "SP" for 01310-100 — or <see langword="null"/> when the prefix belongs to no state
    /// (00000–00999 and 78900–78999).
    /// </summary>
    public string? Uf => CepUfTable.LookupUf((int)(_digits / 1000));

    /// <summary>
    /// Generates a random CEP drawn uniformly from the prefixes assigned to the states, so
    /// <see cref="Uf"/> is never <see langword="null"/> for the result.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A random, structurally valid CEP inside a known Correios range.</returns>
    public static Cep Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        int index = random.Next(CepUfTable.TotalAssignedPrefixes);
        (int low, int start) = CepUfTable.RangeForIndex(index);
        int prefix = low + index - start;
        uint packed = (uint)(prefix * 1000) + (uint)random.Next(1000);
        return new Cep(packed);
    }

    private static bool TryNormalize(ReadOnlySpan<char> value, Span<char> digits)
        => DocumentText.TryNormalizeDigits(value, digits, out int written) && written == DigitsLength;

    private static Cep ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> digits = stackalloc char[DigitsLength];
        if (!TryNormalize(value, digits))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid CEP.");
        }
        return new Cep(PackDigits(digits));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a CEP; use \"G\" (formatted) or \"N\" (digits only)."),
    };

    private static uint PackDigits(ReadOnlySpan<char> digits)
    {
        uint packed = 0;
        foreach (char c in digits)
        {
            packed = packed * 10 + (uint)(c - '0');
        }
        return packed;
    }

    private int Write(Span<char> destination, bool formatted)
    {
        uint value = _digits;
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
        digits[..5].CopyTo(destination);
        destination[5] = '-';
        digits[5..].CopyTo(destination[6..]);
        return FormattedLength;
    }
}
