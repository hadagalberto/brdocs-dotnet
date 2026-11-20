using System.Globalization;

namespace BrDocs;

/// <summary>
/// Placa veicular — the Brazilian vehicle registration plate, in the legacy national format
/// and in the Mercosul format.
/// </summary>
/// <remarks>
/// <para>
/// A plate has 7 significant characters. The legacy national format is "LLLNNNN" — three
/// letters followed by four digits — written "AAA-9999"; it was replaced by the Mercosul
/// format "LLLNLNN", written without a separator (e.g. <c>ABC1D23</c>), per Resolução CONTRAN
/// nº 510/2014 (implementation delayed by Resolução CONTRAN nº 740/2018). Plates have no
/// check digit: validation is structural only, and any letter A–Z may appear in the fifth
/// position of a natively issued Mercosul plate.
/// </para>
/// <para>
/// <see cref="ToMercosul"/> converts a legacy plate to the Mercosul format per Resolução
/// CONTRAN nº 969/2022, Anexo II, item 2: the fifth character (the second digit of the
/// legacy group) becomes a letter, with <c>0</c>→A, <c>1</c>→B, …, <c>9</c>→J — so
/// <c>ABC1234</c> becomes <c>ABC1C34</c> and <c>AAA0000</c> becomes <c>AAA0A00</c>. The
/// conversion is one-way: a natively issued Mercosul plate whose fifth letter is not A–J
/// cannot be mapped back. Only the car formats are supported; the Mercosul motorcycle
/// layouts are out of scope.
/// </para>
/// <para>
/// Input may be formatted or not, in upper or lower case: dots, dashes, slashes and spaces
/// are ignored and lowercase letters are normalized to uppercase; any other character makes
/// the value invalid. All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Placa()"/> represents the
/// all-zero characters "0000000" (displayed "000-0000"); there is no reserved-sequence rule
/// for plates, so it is structurally valid as a legacy plate.
/// </para>
/// </remarks>
public readonly record struct Placa : IEquatable<Placa>, ISpanFormattable, ISpanParsable<Placa>
{
    private const int CharsLength = 7;
    private const int LegacyFormattedLength = 8;

    // The 7 characters, 6 bits each: chars 0..6 in a single value.
    // A char maps to 0..9 for '0'..'9' and 10..35 for 'A'..'Z'.
    private readonly ulong _chars;

    private Placa(ulong chars) => _chars = chars;

    /// <summary>
    /// Determines whether the given text represents a valid vehicle plate, in the legacy
    /// ("AAA-9999") or the Mercosul ("AAA9A99") format.
    /// </summary>
    /// <param name="value">
    /// Formatted or unformatted plate text; dots, dashes, slashes and spaces are ignored and
    /// lowercase letters are normalized to uppercase.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 7 valid characters: three letters,
    /// one digit, one letter or digit (letter = Mercosul, digit = legacy) and two digits.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> chars = stackalloc char[CharsLength];
        return TryValidate(value, chars);
    }

    /// <summary>Attempts to parse a plate, formatted or not, in either format.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored and lowercase letters
    /// are normalized to uppercase.
    /// </param>
    /// <param name="result">The parsed plate when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid plate.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Placa result)
    {
        Span<char> chars = stackalloc char[CharsLength];
        if (!TryValidate(value, chars))
        {
            result = default;
            return false;
        }
        result = new Placa(Pack(chars));
        return true;
    }

    /// <summary>Parses a plate, throwing when the text is not valid.</summary>
    /// <param name="value">
    /// The text to parse; dots, dashes, slashes and spaces are ignored and lowercase letters
    /// are normalized to uppercase.
    /// </param>
    /// <returns>The parsed plate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid plate.</exception>
    public static Placa Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a plate from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; plate parsing is culture-invariant.</param>
    /// <returns>The parsed plate.</returns>
    /// <exception cref="FormatException">The text is not a valid plate.</exception>
    public static Placa Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Placa IParsable<Placa>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a plate from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Placa)"/>.
    /// </summary>
    static bool IParsable<Placa>.TryParse(string? s, IFormatProvider? provider, out Placa result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a plate from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Placa)"/>.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; plate parsing is culture-invariant.</param>
    /// <param name="result">The parsed plate when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid plate.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Placa result) => TryParse(s, out result);

    /// <summary>
    /// Returns the plate in its display form: "AAA-9999" for the legacy format or "AAA9A99"
    /// for the Mercosul format.
    /// </summary>
    /// <returns>The formatted plate, e.g. "ABC-1234" or "ABC1C34".</returns>
    public override string ToString() => IsMercosul
        ? string.Create(CharsLength, this, static (buffer, state) => state.Write(buffer, legacy: false))
        : string.Create(LegacyFormattedLength, this, static (buffer, state) => state.Write(buffer, legacy: true));

    /// <summary>Formats the plate.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces the display form
    /// ("AAA-9999" for legacy plates, "AAA9A99" for Mercosul plates); "N" produces the 7
    /// characters without the legacy dash. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted plate.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the plate; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> display form, "N" characters only.</param>
    /// <param name="formatProvider">Ignored; plate formatting is culture-invariant.</param>
    /// <returns>The formatted plate.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(CharsLength, this, static (buffer, state) => state.Write(buffer, legacy: false));

    /// <summary>Writes the plate into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for the display form; "N" for the characters only.</param>
    /// <param name="provider">Ignored; plate formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bool formatted = ResolveFormat(format);
        bool legacy = formatted && !IsMercosul;
        int requiredLength = formatted && legacy ? LegacyFormattedLength : CharsLength;
        if (destination.Length < requiredLength)
        {
            charsWritten = 0;
            return false;
        }
        charsWritten = Write(destination, legacy);
        return true;
    }

    /// <summary>Returns <see langword="true"/> when the plate uses the Mercosul format (fifth character is a letter).</summary>
    public bool IsMercosul => CharAt(4) >= 'A';

    /// <summary>Returns <see langword="true"/> when the plate uses the legacy format (fifth character is a digit).</summary>
    public bool IsLegacy => !IsMercosul;

    /// <summary>
    /// Converts a legacy plate ("AAA-9999") to the Mercosul format ("AAA9A99") per Resolução
    /// CONTRAN nº 969/2022, Anexo II, item 2: the fifth character (the second digit of the
    /// legacy group) becomes the letter A–J for the digits 0–9.
    /// </summary>
    /// <returns>
    /// The Mercosul plate — e.g. <c>ABC1234</c> → <c>ABC1C34</c> — or this same plate when it
    /// already uses the Mercosul format.
    /// </returns>
    public Placa ToMercosul()
    {
        if (IsMercosul)
        {
            return this;
        }
        Span<char> chars = stackalloc char[CharsLength];
        for (int i = 0; i < CharsLength; i++)
        {
            chars[i] = CharAt(i);
        }
        chars[4] = (char)('A' + (chars[4] - '0'));
        return new Placa(Pack(chars));
    }

    /// <summary>
    /// Generates a random valid Mercosul plate, useful for tests and fake data.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A plate in the current national format, "LLLNLNN".</returns>
    public static Placa Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> chars = stackalloc char[CharsLength];
        chars[0] = RandomLetter(random);
        chars[1] = RandomLetter(random);
        chars[2] = RandomLetter(random);
        chars[3] = RandomDigit(random);
        chars[4] = RandomLetter(random);
        chars[5] = RandomDigit(random);
        chars[6] = RandomDigit(random);
        return new Placa(Pack(chars));
    }

    /// <summary>
    /// Generates a random valid plate in the legacy national format ("LLLNNNN"), useful for
    /// tests and fake data.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A plate in the legacy format, e.g. "AAA-9999".</returns>
    public static Placa GenerateLegacy(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> chars = stackalloc char[CharsLength];
        chars[0] = RandomLetter(random);
        chars[1] = RandomLetter(random);
        chars[2] = RandomLetter(random);
        chars[3] = RandomDigit(random);
        chars[4] = RandomDigit(random);
        chars[5] = RandomDigit(random);
        chars[6] = RandomDigit(random);
        return new Placa(Pack(chars));
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> chars)
    {
        if (!DocumentText.TryNormalizeUpperAlphanumeric(value, chars, out int written) || written != CharsLength)
        {
            return false;
        }
        if (!char.IsBetween(chars[0], 'A', 'Z') || !char.IsBetween(chars[1], 'A', 'Z') || !char.IsBetween(chars[2], 'A', 'Z'))
        {
            return false;
        }
        if (!char.IsAsciiDigit(chars[3]) || !char.IsAsciiDigit(chars[5]) || !char.IsAsciiDigit(chars[6]))
        {
            return false;
        }
        // The fifth character selects the format: digit = legacy, letter = Mercosul.
        return char.IsAsciiDigit(chars[4]) || char.IsBetween(chars[4], 'A', 'Z');
    }

    private static Placa ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> chars = stackalloc char[CharsLength];
        if (!TryValidate(value, chars))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid vehicle plate.");
        }
        return new Placa(Pack(chars));
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a vehicle plate; use \"G\" (formatted) or \"N\" (characters only)."),
    };

    private static ulong Pack(ReadOnlySpan<char> chars)
    {
        ulong packed = 0;
        for (int i = 0; i < CharsLength; i++)
        {
            packed |= (ulong)CharToIndex(chars[i]) << (6 * i);
        }
        return packed;
    }

    private static int CharToIndex(char c) => c <= '9' ? c - '0' : c - 'A' + 10;

    private static char IndexToChar(int index) => index < 10
        ? (char)('0' + index)
        : (char)('A' + index - 10);

    private char CharAt(int index) => IndexToChar((int)((_chars >> (6 * index)) & 0x3FUL));

    private static char RandomLetter(Random random) => (char)('A' + random.Next(26));

    private static char RandomDigit(Random random) => (char)('0' + random.Next(10));

    private int Write(Span<char> destination, bool legacy)
    {
        for (int i = 0; i < CharsLength; i++)
        {
            destination[i] = CharAt(i);
        }
        if (!legacy)
        {
            return CharsLength;
        }
        destination[3] = '-';
        destination[4] = CharAt(3);
        destination[5] = CharAt(4);
        destination[6] = CharAt(5);
        destination[7] = CharAt(6);
        return LegacyFormattedLength;
    }
}
