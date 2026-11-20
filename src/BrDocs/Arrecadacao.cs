using System.Globalization;

namespace BrDocs;

/// <summary>
/// Boleto de arrecadação/convenio (FEBRABAN) — a utility bill or government guide payment code:
/// a 44-digit código de barras starting with <c>8</c>, or the equivalent 48-digit linha
/// digitável of four 11-digit blocks, each closed by its own check digit.
/// </summary>
/// <remarks>
/// <para>
/// The 44 digits of the código de barras (FEBRABAN, "Layout - Código de Barras") are:
/// the constant <c>8</c> at position 1, the segment at position 2 (commonly documented as
/// 1 prefeituras, 2 saneamento, 3 energia elétrica e gás, 4 telecomunicações,
/// 5 órgãos governamentais, 6 carnês e assemelhados, 7 taxas de trânsito, 9 outros), the
/// value identifier at position 3 (<c>6</c>/<c>7</c> for quantidade de moeda with DV módulo 10,
/// <c>8</c>/<c>9</c> for valor efetivo with DV módulo 11), the general DV at position 4, the
/// value in centavos (or the reference quantity) at positions 5–15, the company/agency
/// identification at positions 16–19 and the free field at positions 20–44.
/// </para>
/// <para>
/// The 48-digit linha digitável splits the same 44 digits into four blocks of 11 and appends a
/// DV to each: <c>NNNNNNNNNNN-D NNNNNNNNNNN-D NNNNNNNNNNN-D NNNNNNNNNNN-D</c>. Note that the
/// first block contains the general DV of position 4.
/// </para>
/// <para>
/// Every DV — the general one and the four block ones — is computed per the third digit:
/// módulo 10 for identifiers 6/7, módulo 11 for 8/9. The módulo 11 rule differs from the bank
/// cobrança general DV: a remainder of 0 or 1 produces DV 0, and a remainder of 10 produces
/// DV 1 (FEBRABAN, "Layout - Código de Barras").
/// </para>
/// <para>
/// The value field only carries an actual amount in reais when the identifier is 6 or 8
/// (<see cref="IsEffectiveValue"/>); identifiers 7 and 9 carry a reference quantity and
/// <see cref="Value"/> is <see langword="null"/>. Unlike bank boletos, arrecadação codes have
/// no due-date factor — a due date, when present, is printed in the free field.
/// </para>
/// <para>
/// Input may be the código de barras or the linha digitável, formatted or not: dots, dashes,
/// slashes and spaces are ignored; any other character makes the value invalid. Bank boletos
/// (cobrança) are a separate structure, handled by <see cref="Boleto"/>. All formatting is
/// culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Arrecadacao()"/> represents
/// the all-zero digits, which are never a valid code (they do not start with <c>8</c>).
/// </para>
/// </remarks>
public readonly record struct Arrecadacao : IEquatable<Arrecadacao>, ISpanFormattable, ISpanParsable<Arrecadacao>
{
    private const int BarcodeLength = 44;
    private const int LinhaLength = 48;
    private const int FormattedLinhaLength = 55;   // 4 blocks of "NNNNNNNNNNN-D" separated by spaces.
    private const int DvLength = 43;              // the barcode minus the general DV position.
    private const int BlockCount = 4;
    private const int BlockLength = 12;           // 11 digits + 1 DV in the linha digitável.
    private const int BlockDigitCount = 11;
    private const int ValueLength = 11;
    private const int FreeFieldLength = 25;

    // Barcode positions (0-based): constant 8 at 0, segment 1, identifier 2, general DV 3,
    // value 4–14, company/agency identification 15–18, free field 19–43.
    private const int SegmentIndex = 1;
    private const int IdentifierIndex = 2;
    private const int GeneralDvIndex = 3;
    private const int ValueStart = 4;
    private const int ValueEnd = 15;
    private const int FreeFieldStart = 19;

    // The FEBRABAN-documented segments used by Generate.
    private static readonly int[] s_segments = { 1, 2, 3, 4, 5, 6, 7, 9 };
    private static readonly int[] s_identifiers = { 6, 7, 8, 9 };

    // The 44 barcode digits: nibble-packed into three ulongs.
    private readonly ulong _lo;
    private readonly ulong _mid;
    private readonly ulong _hi;

    private Arrecadacao(ulong lo, ulong mid, ulong hi)
    {
        _lo = lo;
        _mid = mid;
        _hi = hi;
    }

    /// <summary>
    /// Determines whether the given text is a valid arrecadação/convenio code: the 44-digit
    /// código de barras or the 48-digit linha digitável, formatted or not.
    /// </summary>
    /// <param name="value">Code text; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 44 or 48 digits, starts with <c>8</c>,
    /// has a value identifier of 6, 7, 8 or 9, and the four block DVs (linha digitável only)
    /// and the general DV all match.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        return TryValidate(value, barcode);
    }

    /// <summary>Attempts to parse an arrecadação/convenio code.</summary>
    /// <param name="value">Code text; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed code when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid arrecadação code.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Arrecadacao result)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        if (!TryValidate(value, barcode))
        {
            result = default;
            return false;
        }
        result = Pack(barcode);
        return true;
    }

    /// <summary>Parses an arrecadação/convenio code, throwing when the text is not valid.</summary>
    /// <param name="value">The código de barras or the linha digitável, formatted or not.</param>
    /// <returns>The parsed code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid arrecadação code.</exception>
    public static Arrecadacao Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses an arrecadação/convenio code from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The código de barras or the linha digitável, formatted or not.</param>
    /// <param name="provider">Ignored; arrecadação parsing is culture-invariant.</param>
    /// <returns>The parsed code.</returns>
    /// <exception cref="FormatException">The text is not a valid arrecadação code.</exception>
    public static Arrecadacao Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Arrecadacao IParsable<Arrecadacao>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse an arrecadação code from a string. Returns <see langword="false"/> for
    /// a <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Arrecadacao)"/>.
    /// </summary>
    static bool IParsable<Arrecadacao>.TryParse(string? s, IFormatProvider? provider, out Arrecadacao result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse an arrecadação code from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Arrecadacao)"/>.</summary>
    /// <param name="s">The código de barras or the linha digitável, formatted or not.</param>
    /// <param name="provider">Ignored; arrecadação parsing is culture-invariant.</param>
    /// <param name="result">The parsed code when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid arrecadação code.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Arrecadacao result) => TryParse(s, out result);

    /// <summary>
    /// Returns the formatted linha digitável,
    /// "NNNNNNNNNNN-D NNNNNNNNNNN-D NNNNNNNNNNN-D NNNNNNNNNNN-D".
    /// </summary>
    /// <returns>The formatted linha digitável, e.g. "85820000000-7 57250328203-0 56070820210-7 53959190446-0".</returns>
    public override string ToString() => string.Create(FormattedLinhaLength, this, static (buffer, state) => state.WriteFormatted(buffer));

    /// <summary>Formats the code.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces the formatted linha digitável;
    /// "N" produces the 48 linha digitável digits only; "B" produces the 44-digit código de
    /// barras. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted code.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G", "N" nor "B".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the code; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted linha, "N" linha digits, "B" código de barras.</param>
    /// <param name="formatProvider">Ignored; arrecadação formatting is culture-invariant.</param>
    /// <returns>The formatted code.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G", "N" nor "B".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format) switch
    {
        0 => string.Create(FormattedLinhaLength, this, static (buffer, state) => state.WriteFormatted(buffer)),
        1 => string.Create(LinhaLength, this, static (buffer, state) => state.WriteLinha(buffer)),
        _ => string.Create(BarcodeLength, this, static (buffer, state) => state.WriteBarcode(buffer)),
    };

    /// <summary>Writes the code into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">
    /// "G"/empty for the formatted linha digitável; "N" for the 48 linha digits; "B" for the
    /// 44-digit código de barras.
    /// </param>
    /// <param name="provider">Ignored; arrecadação formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G", "N" nor "B".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        switch (ResolveFormat(format))
        {
            case 0:
                if (destination.Length < FormattedLinhaLength)
                {
                    charsWritten = 0;
                    return false;
                }
                charsWritten = WriteFormatted(destination);
                return true;
            case 1:
                if (destination.Length < LinhaLength)
                {
                    charsWritten = 0;
                    return false;
                }
                charsWritten = WriteLinha(destination);
                return true;
            default:
                if (destination.Length < BarcodeLength)
                {
                    charsWritten = 0;
                    return false;
                }
                charsWritten = WriteBarcode(destination);
                return true;
        }
    }

    /// <summary>
    /// The "identificação do segmento" digit (barcode position 2): commonly documented as
    /// 1 prefeituras, 2 saneamento, 3 energia elétrica e gás, 4 telecomunicações,
    /// 5 órgãos governamentais, 6 carnês e assemelhados, 7 taxas de trânsito and 9 outros.
    /// Unassigned digits are passed through without validation.
    /// </summary>
    public int Segment => Digit(SegmentIndex);

    /// <summary>
    /// The value identifier (barcode position 3): what the 11-digit value field means and which
    /// module every DV of the code uses.
    /// </summary>
    public ArrecadacaoValueType ValueType => (ArrecadacaoValueType)Digit(IdentifierIndex);

    /// <summary>
    /// <see langword="true"/> when the value field carries an amount in reais (identifier 6 or 8);
    /// <see langword="false"/> when it carries a reference quantity (identifier 7 or 9).
    /// </summary>
    public bool IsEffectiveValue => ValueType is ArrecadacaoValueType.EffectiveValueMod10 or ArrecadacaoValueType.EffectiveValueMod11;

    /// <summary>
    /// The value in reais (barcode positions 5–15 store whole centavos) when
    /// <see cref="IsEffectiveValue"/> is <see langword="true"/>; <see langword="null"/> when the
    /// code carries a reference quantity instead.
    /// </summary>
    public decimal? Value
    {
        get
        {
            if (!IsEffectiveValue)
            {
                return null;
            }
            long centavos = 0;
            for (int i = ValueStart; i < ValueEnd; i++)
            {
                centavos = centavos * 10 + Digit(i);
            }
            return centavos / 100m;
        }
    }

    /// <summary>The free field (barcode positions 20–44), 25 digits.</summary>
    public string FreeField => string.Create(FreeFieldLength, this, static (buffer, state) => state.WriteFreeField(buffer));

    /// <summary>The 44-digit código de barras.</summary>
    public string Barcode => string.Create(BarcodeLength, this, static (buffer, state) => state.WriteBarcode(buffer));

    /// <summary>The 48-digit linha digitável, without punctuation.</summary>
    public string LinhaDigitavel => string.Create(LinhaLength, this, static (buffer, state) => state.WriteLinha(buffer));

    /// <summary>Generates a random valid arrecadação/convenio code, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>
    /// A code with a random documented segment, a random value identifier (6, 7, 8 or 9), a
    /// random 11-digit value and random identification/free-field digits, with every check
    /// digit computed.
    /// </returns>
    public static Arrecadacao Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        Span<char> barcode = stackalloc char[BarcodeLength];
        barcode[0] = '8';
        barcode[SegmentIndex] = (char)('0' + s_segments[random.Next(s_segments.Length)]);
        barcode[IdentifierIndex] = (char)('0' + s_identifiers[random.Next(s_identifiers.Length)]);
        PackedDigits.WritePadded(random.NextInt64(0, 10_000_000_000L), ValueLength, barcode[ValueStart..]);
        for (int i = ValueEnd; i < BarcodeLength; i++)
        {
            barcode[i] = (char)('0' + random.Next(10));
        }
        barcode[GeneralDvIndex] = (char)('0' + GeneralDv(barcode));
        return Pack(barcode);
    }

    private static Arrecadacao Pack(Span<char> barcode)
    {
        PackedDigits.Pack(barcode, out ulong lo, out ulong mid, out ulong hi);
        return new Arrecadacao(lo, mid, hi);
    }

    private static bool TryValidate(ReadOnlySpan<char> value, Span<char> barcode)
    {
        Span<char> digits = stackalloc char[LinhaLength];
        if (!DocumentText.TryNormalizeDigits(value, digits, out int written))
        {
            return false;
        }
        switch (written)
        {
            case BarcodeLength:
                digits[..BarcodeLength].CopyTo(barcode);
                break;
            case LinhaLength:
                bool mod11 = UsesMod11(digits[IdentifierIndex]);
                for (int block = 0; block < BlockCount; block++)
                {
                    ReadOnlySpan<char> blockDigits = digits.Slice(block * BlockLength, BlockDigitCount);
                    if (BlockDv(blockDigits, mod11) != digits[block * BlockLength + BlockDigitCount] - '0')
                    {
                        return false;
                    }
                    blockDigits.CopyTo(barcode.Slice(block * BlockDigitCount, BlockDigitCount));
                }
                break;
            default:
                return false;
        }
        if (barcode[0] != '8' || barcode[IdentifierIndex] is < '6' or > '9')
        {
            return false;
        }
        return GeneralDv(barcode) == barcode[GeneralDvIndex] - '0';
    }

    /// <summary>
    /// The general DV (barcode position 4), computed per the value identifier over the other
    /// 43 digits: módulo 11 for identifiers 8/9, módulo 10 for 6/7.
    /// </summary>
    private static int GeneralDv(ReadOnlySpan<char> barcode)
    {
        Span<char> digits = stackalloc char[DvLength];
        barcode[..GeneralDvIndex].CopyTo(digits);
        barcode[(GeneralDvIndex + 1)..].CopyTo(digits[GeneralDvIndex..]);
        return UsesMod11(barcode[IdentifierIndex])
            ? FebrabanCheckDigit.ArrecadacaoDv(digits)
            : FebrabanCheckDigit.Mod10(digits);
    }

    private static int BlockDv(ReadOnlySpan<char> blockDigits, bool mod11)
        => mod11 ? FebrabanCheckDigit.ArrecadacaoDv(blockDigits) : FebrabanCheckDigit.Mod10(blockDigits);

    private static bool UsesMod11(char identifier) => identifier is '8' or '9';

    private static int ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => 0,
        1 when format[0] is 'G' or 'g' => 0,
        1 when format[0] is 'N' or 'n' => 1,
        1 when format[0] is 'B' or 'b' => 2,
        _ => throw new FormatException(
            $"Format '{DocumentText.Preview(format)}' is not valid for an arrecadação code; use \"G\" (formatted linha digitável), \"N\" (linha digits) or \"B\" (código de barras)."),
    };

    private static Arrecadacao ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        if (!TryValidate(value, barcode))
        {
            throw new FormatException(
                $"'{DocumentText.Preview(value)}' is not a valid arrecadação code (44-digit código de barras or 48-digit linha digitável).");
        }
        return Pack(barcode);
    }

    private int Digit(int index) => PackedDigits.Digit(_lo, _mid, _hi, index);

    private int WriteBarcode(Span<char> destination)
    {
        PackedDigits.Unpack(_lo, _mid, _hi, destination);
        return BarcodeLength;
    }

    private int WriteLinha(Span<char> destination)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        PackedDigits.Unpack(_lo, _mid, _hi, barcode);
        bool mod11 = UsesMod11(barcode[IdentifierIndex]);
        for (int block = 0; block < BlockCount; block++)
        {
            ReadOnlySpan<char> blockDigits = barcode.Slice(block * BlockDigitCount, BlockDigitCount);
            blockDigits.CopyTo(destination.Slice(block * BlockLength, BlockDigitCount));
            destination[block * BlockLength + BlockDigitCount] = (char)('0' + BlockDv(blockDigits, mod11));
        }
        return LinhaLength;
    }

    private int WriteFormatted(Span<char> destination)
    {
        Span<char> linha = stackalloc char[LinhaLength];
        WriteLinha(linha);
        const int formattedBlockLength = BlockLength + 2; // 11 digits + '-' + DV (+ separator).
        for (int block = 0; block < BlockCount; block++)
        {
            int source = block * BlockLength;
            int target = block * formattedBlockLength;
            linha.Slice(source, BlockLength).CopyTo(destination.Slice(target, BlockLength));
            destination[target + BlockDigitCount] = '-';
            destination[target + BlockDigitCount + 1] = linha[source + BlockDigitCount];
            if (block < BlockCount - 1)
            {
                destination[target + BlockLength + 1] = ' ';
            }
        }
        return FormattedLinhaLength;
    }

    private void WriteFreeField(Span<char> destination)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        PackedDigits.Unpack(_lo, _mid, _hi, barcode);
        barcode[FreeFieldStart..].CopyTo(destination);
    }
}
