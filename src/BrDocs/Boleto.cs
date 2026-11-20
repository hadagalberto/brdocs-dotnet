using System.Globalization;

namespace BrDocs;

/// <summary>
/// Boleto bancário (FEBRABAN cobrança) — a Brazilian bank payment slip, represented by its
/// 44-digit código de barras or by the equivalent 47-digit linha digitável printed on the slip.
/// </summary>
/// <remarks>
/// <para>
/// The 44 digits of the código de barras (Banco Central do Brasil, Carta-Circular
/// nº 2.926 de 25/07/2000; FEBRABAN "Layout - Código de Barras") are:
/// bank code 1–3, currency code 4 (<c>9</c> = Real), the general DV 5, the due-date factor
/// 6–9, the amount in centavos 10–19 and the free field 20–44.
/// </para>
/// <para>
/// The 47-digit linha digitável carries the same data as five fields:
/// <c>NNNNN.NNNNN NNNNN.NNNNNN NNNNN.NNNNNN N NNNNNNNNNNNNNN</c>, where the first three fields
/// end in a módulo 10 field DV, the single digit is the general DV (barcode position 5) and the
/// last 14 digits are the due-date factor followed by the amount.
/// </para>
/// <para>
/// The general DV uses módulo 11 with the weights 2, 3, …, 9 cycling from the right over the
/// other 43 digits: the result is <c>11 − resto</c>, and a computed 0, 1, 10 or 11 becomes 1.
/// </para>
/// <para>
/// The due-date factor (fator de vencimento) counts days from the FEBRABAN base date
/// 1997-10-07: factor 1 is 1997-10-08 and factor 9999 is 2025-02-21. On 2025-02-22 the factor
/// restarted at 1000 (FEBRABAN, comunicação sobre a virada do fator de vencimento) and will run
/// through 9999 again, repeating every 9000 factors. A factor is therefore ambiguous across
/// cycles and is resolved with a reference date (default: today) by choosing the candidate
/// date within the window from 3000 days before to 5500 days after the reference — see
/// <see cref="Decode(int, DateOnly)"/>. Factor 0000 means the boleto has no due date.
/// </para>
/// <para>
/// Input may be the código de barras or the linha digitável, formatted or not: dots, dashes,
/// slashes and spaces are ignored; any other character makes the value invalid. All formatting
/// is culture-invariant. Utility bills and government guides (arrecadação/convenio, 44/48
/// digits starting with <c>8</c>) are a separate structure, handled by <see cref="Arrecadacao"/>.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new Boleto()"/> represents the
/// all-zero código de barras "00000000000000000000000000000000000000000000", which is never a
/// valid boleto (its general DV would be 1, not 0).
/// </para>
/// </remarks>
public readonly record struct Boleto : IEquatable<Boleto>, ISpanFormattable, ISpanParsable<Boleto>
{
    private const int BarcodeLength = 44;
    private const int LinhaLength = 47;
    private const int FormattedLinhaLength = 54;   // 47 digits + 3 dots + 4 spaces.
    private const int DvLength = 43;              // the barcode minus the general DV position.
    private const int BankLength = 3;
    private const int FactorLength = 4;
    private const int AmountLength = 10;
    private const int FreeFieldLength = 25;

    // Barcode positions (0-based): bank 0–2, currency 3, general DV 4, factor 5–8,
    // amount 9–18, free field 19–43.
    private const int AmountStart = 9;
    private const int AmountEnd = 19;
    private const int FreeFieldStart = 19;

    // The largest amount a boleto can carry: 10 digits of centavos = R$ 99.999.999,99.
    private const decimal MaxAmount = 99_999_999.99m;

    // FEBRABAN due-date factor: base date, 2025-02-22 reset and the disambiguation window.
    private static readonly DateOnly s_factorBaseDate = new(1997, 10, 7);
    private static readonly DateOnly s_factorResetDate = new(2025, 2, 22);
    private const int FirstCycleMaxFactor = 9999;
    private const int ResetFactor = 1000;
    private const int CycleFactorCount = 9000;
    private const int WindowPastDays = 3000;
    private const int WindowFutureDays = 5500;

    // The 44 barcode digits: nibble-packed into three ulongs.
    private readonly ulong _lo;
    private readonly ulong _mid;
    private readonly ulong _hi;

    private Boleto(ulong lo, ulong mid, ulong hi)
    {
        _lo = lo;
        _mid = mid;
        _hi = hi;
    }

    /// <summary>
    /// Determines whether the given text is a valid bank-slip boleto: the 44-digit código de
    /// barras or the 47-digit linha digitável, formatted or not.
    /// </summary>
    /// <param name="value">Boleto text; dots, dashes, slashes and spaces are ignored.</param>
    /// <returns>
    /// <see langword="true"/> when the text has exactly 44 or 47 digits, the three field DVs
    /// (linha digitável only) and the general DV all match.
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        return TryValidate(value, barcode);
    }

    /// <summary>Attempts to parse a boleto from the código de barras or the linha digitável.</summary>
    /// <param name="value">Boleto text; dots, dashes, slashes and spaces are ignored.</param>
    /// <param name="result">The parsed boleto when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid boleto.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Boleto result)
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

    /// <summary>Parses a boleto, throwing when the text is not valid.</summary>
    /// <param name="value">The código de barras or the linha digitável, formatted or not.</param>
    /// <returns>The parsed boleto.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid boleto.</exception>
    public static Boleto Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a boleto from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The código de barras or the linha digitável, formatted or not.</param>
    /// <param name="provider">Ignored; boleto parsing is culture-invariant.</param>
    /// <returns>The parsed boleto.</returns>
    /// <exception cref="FormatException">The text is not a valid boleto.</exception>
    public static Boleto Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static Boleto IParsable<Boleto>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a boleto from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out Boleto)"/>.
    /// </summary>
    static bool IParsable<Boleto>.TryParse(string? s, IFormatProvider? provider, out Boleto result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a boleto from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out Boleto)"/>.</summary>
    /// <param name="s">The código de barras or the linha digitável, formatted or not.</param>
    /// <param name="provider">Ignored; boleto parsing is culture-invariant.</param>
    /// <param name="result">The parsed boleto when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid boleto.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Boleto result) => TryParse(s, out result);

    /// <summary>Returns the formatted linha digitável, "NNNNN.NNNNN NNNNN.NNNNNN NNNNN.NNNNNN N NNNNNNNNNNNNNNN".</summary>
    /// <returns>The formatted linha digitável, e.g. "10492.00650 61000.100042 00997.263900 9 89810000021403".</returns>
    public override string ToString() => string.Create(FormattedLinhaLength, this, static (buffer, state) => state.WriteFormatted(buffer));

    /// <summary>Formats the boleto.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces the formatted linha digitável;
    /// "N" produces the 47 linha digitável digits only; "B" produces the 44-digit código de
    /// barras. Matching is case-insensitive.
    /// </param>
    /// <returns>The formatted boleto.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G", "N" nor "B".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the boleto; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> formatted linha, "N" linha digits, "B" código de barras.</param>
    /// <param name="formatProvider">Ignored; boleto formatting is culture-invariant.</param>
    /// <returns>The formatted boleto.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G", "N" nor "B".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format) switch
    {
        0 => string.Create(FormattedLinhaLength, this, static (buffer, state) => state.WriteFormatted(buffer)),
        1 => string.Create(LinhaLength, this, static (buffer, state) => state.WriteLinha(buffer)),
        _ => string.Create(BarcodeLength, this, static (buffer, state) => state.WriteBarcode(buffer)),
    };

    /// <summary>Writes the boleto into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">
    /// "G"/empty for the formatted linha digitável; "N" for the 47 linha digits; "B" for the
    /// 44-digit código de barras.
    /// </param>
    /// <param name="provider">Ignored; boleto formatting is culture-invariant.</param>
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

    /// <summary>The FEBRABAN bank code (barcode positions 1–3), e.g. 104 for Caixa Econômica Federal.</summary>
    public int BankCode => Digit(0) * 100 + Digit(1) * 10 + Digit(2);

    /// <summary>The currency code (barcode position 4); 9 is the Real.</summary>
    public int CurrencyCode => Digit(3);

    /// <summary>The due-date factor (barcode positions 6–9); 0 means the boleto has no due date.</summary>
    /// <seealso cref="DueDate"/>
    public int DueDateFactor => Digit(5) * 1000 + Digit(6) * 100 + Digit(7) * 10 + Digit(8);

    /// <summary>
    /// The amount in reais (barcode positions 10–19 store whole centavos), invariant of any culture.
    /// </summary>
    public decimal Amount
    {
        get
        {
            long centavos = 0;
            for (int i = AmountStart; i < AmountEnd; i++)
            {
                centavos = centavos * 10 + Digit(i);
            }
            return centavos / 100m;
        }
    }

    /// <summary>
    /// The due date decoded from <see cref="DueDateFactor"/> with today as the reference date:
    /// <see langword="null"/> when the factor is 0000 (no due date) or when the factor cannot be
    /// resolved to a date inside the disambiguation window of the reference (see
    /// <see cref="Decode(int, DateOnly)"/>). Use <see cref="TryDecode(int, DateOnly, out DateOnly)"/>
    /// to resolve against a fixed reference date.
    /// </summary>
    public DateOnly? DueDate
    {
        get
        {
            int factor = DueDateFactor;
            if (factor == 0)
            {
                return null;
            }
            return TryDecode(factor, DateOnly.FromDateTime(DateTime.Today), out DateOnly dueDate) ? dueDate : null;
        }
    }

    /// <summary>The bank free field (barcode positions 20–44), 25 digits.</summary>
    public string FreeField => string.Create(FreeFieldLength, this, static (buffer, state) => state.WriteFreeField(buffer));

    /// <summary>The 44-digit código de barras.</summary>
    public string Barcode => string.Create(BarcodeLength, this, static (buffer, state) => state.WriteBarcode(buffer));

    /// <summary>The 47-digit linha digitável, without punctuation.</summary>
    public string LinhaDigitavel => string.Create(LinhaLength, this, static (buffer, state) => state.WriteLinha(buffer));

    /// <summary>
    /// Builds a valid boleto from its parts, computing the due-date factor (or 0000 for no due
    /// date) and the general DV.
    /// </summary>
    /// <param name="bank">The FEBRABAN bank code, 1–999 (zero-padded to 3 digits).</param>
    /// <param name="currency">The currency code digit, 0–9 (9 is the Real).</param>
    /// <param name="dueDate">The due date; <see langword="null"/> encodes factor 0000 (no due date).</param>
    /// <param name="amount">
    /// The amount in reais, between 0 and 99.999.999,99 with at most two decimal places (whole
    /// centavos).
    /// </param>
    /// <param name="freeField">
    /// Exactly 25 digits (dots, dashes, slashes and spaces are ignored) — the bank's free field.
    /// </param>
    /// <returns>A valid boleto whose <see cref="Barcode"/> and <see cref="LinhaDigitavel"/> are ready to use.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bank"/>, <paramref name="currency"/> or <paramref name="amount"/> is out
    /// of range, or <paramref name="dueDate"/> is before 1997-10-08 (the first encodable date).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="amount"/> has more than two decimal places, or <paramref name="freeField"/>
    /// does not hold exactly 25 digits.
    /// </exception>
    public static Boleto Create(int bank, int currency, DateOnly? dueDate, decimal amount, string freeField)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bank, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bank, 999);
        ArgumentOutOfRangeException.ThrowIfLessThan(currency, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(currency, 9);
        ArgumentNullException.ThrowIfNull(freeField);
        if (amount < 0m || amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                $"The amount must be between 0 and {MaxAmount.ToString(CultureInfo.InvariantCulture)}; the barcode stores 10 centavo digits.");
        }
        decimal centavos = amount * 100m;
        if (centavos - decimal.Truncate(centavos) != 0m)
        {
            throw new ArgumentException(
                $"The amount {amount.ToString(CultureInfo.InvariantCulture)} has more than two decimal places; boletos carry whole centavos.",
                nameof(amount));
        }

        Span<char> freeDigits = stackalloc char[FreeFieldLength];
        if (!DocumentText.TryNormalizeDigits(freeField.AsSpan(), freeDigits, out int freeWritten) || freeWritten != FreeFieldLength)
        {
            throw new ArgumentException(
                $"'{DocumentText.Preview(freeField)}' is not a valid free field: exactly 25 digits are required (dots, dashes, slashes and spaces are ignored).",
                nameof(freeField));
        }

        int factor = dueDate is null ? 0 : Encode(dueDate.Value);

        Span<char> barcode = stackalloc char[BarcodeLength];
        PackedDigits.WritePadded(bank, BankLength, barcode);
        barcode[3] = (char)('0' + currency);
        PackedDigits.WritePadded(factor, FactorLength, barcode[5..]);
        PackedDigits.WritePadded((long)centavos, AmountLength, barcode[AmountStart..]);
        freeDigits.CopyTo(barcode[FreeFieldStart..]);
        barcode[4] = (char)('0' + GeneralBarcodeDv(barcode));
        return Pack(barcode);
    }

    /// <summary>
    /// Encodes a due date as a FEBRABAN due-date factor: days since the base date 1997-10-07,
    /// restarting at 1000 on 2025-02-22 and repeating every 9000 factors.
    /// </summary>
    /// <param name="dueDate">The due date to encode, 1997-10-08 or later.</param>
    /// <returns>The factor, 1–9999: 9999 for 2025-02-21, 1000 for 2025-02-22, 1001 for 2025-02-23.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dueDate"/> is before 1997-10-08; factor 0000 is reserved for "no due date".
    /// </exception>
    public static int Encode(DateOnly dueDate)
    {
        if (dueDate.DayNumber < s_factorResetDate.DayNumber)
        {
            int factor = dueDate.DayNumber - s_factorBaseDate.DayNumber;
            if (factor < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dueDate),
                    dueDate,
                    "The first encodable due date is 1997-10-08 (factor 1); factor 0000 means no due date.");
            }
            return factor;
        }
        return ResetFactor + ((dueDate.DayNumber - s_factorResetDate.DayNumber) % CycleFactorCount);
    }

    /// <summary>
    /// Decodes a due-date factor to a date, resolving the cycle ambiguity with
    /// <paramref name="reference"/>: the candidate date is the one inside the window from
    /// <paramref name="reference"/> − 3000 days to <paramref name="reference"/> + 5500 days
    /// (candidates are 8999+ days apart, so at most one qualifies).
    /// </summary>
    /// <param name="factor">The due-date factor, 1–9999.</param>
    /// <param name="reference">The reference date that resolves the cycle; defaults to today for <see cref="DueDate"/>.</param>
    /// <returns>The due date the factor represents in the resolved cycle.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="factor"/> is 0000 (no due date), is outside 1–9999, or has no candidate
    /// date inside the disambiguation window.
    /// </exception>
    public static DateOnly Decode(int factor, DateOnly reference)
    {
        if (!TryDecode(factor, reference, out DateOnly dueDate))
        {
            throw new FormatException(
                factor == 0
                    ? "Due-date factor 0000 means the boleto has no due date; there is no date to decode."
                    : $"Due-date factor {factor} cannot be resolved to a date within {WindowPastDays} days before and {WindowFutureDays} days after the reference date {reference.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
        }
        return dueDate;
    }

    /// <summary>
    /// Attempts to decode a due-date factor; see <see cref="Decode(int, DateOnly)"/> for the
    /// cycle-resolution rule.
    /// </summary>
    /// <param name="factor">The due-date factor, 1–9999.</param>
    /// <param name="reference">The reference date that resolves the cycle.</param>
    /// <param name="dueDate">The resolved date; <see langword="default"/> when returning <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="false"/> when the factor is 0000, is outside 1–9999, or has no candidate
    /// date inside the disambiguation window.
    /// </returns>
    public static bool TryDecode(int factor, DateOnly reference, out DateOnly dueDate)
    {
        dueDate = default;
        if (factor is < 1 or > FirstCycleMaxFactor)
        {
            return false;
        }

        int referenceDay = reference.DayNumber;
        int windowLow = referenceDay - WindowPastDays;
        int windowHigh = referenceDay + WindowFutureDays;
        int bestCandidate = 0;
        int bestDistance = int.MaxValue;

        void Consider(int candidateDay)
        {
            if (candidateDay < windowLow || candidateDay > windowHigh)
            {
                return;
            }
            int distance = Math.Abs(candidateDay - referenceDay);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCandidate = candidateDay;
            }
        }

        // First cycle: factor 1 = 1997-10-08 … factor 9999 = 2025-02-21.
        Consider(s_factorBaseDate.DayNumber + factor);

        // Later cycles: factor 1000 = 2025-02-22 (then every 9000 factors) … factor 9999.
        if (factor >= ResetFactor)
        {
            int cycleStart = s_factorResetDate.DayNumber - ResetFactor;
            int candidate = cycleStart + factor;
            while (candidate <= windowHigh)
            {
                Consider(candidate);
                candidate += CycleFactorCount;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            return false;
        }
        dueDate = DateOnly.FromDayNumber(bestCandidate);
        return true;
    }

    /// <summary>Generates a random valid boleto, useful for tests and fake data.</summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>
    /// A boleto with a random FEBRABAN bank code, the Real currency (9), a due date within the
    /// next 90 days, a random amount up to R$ 999.999,99 and a random 25-digit free field.
    /// </returns>
    public static Boleto Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        int bank = random.Next(1, 1000);
        DateOnly dueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(random.Next(0, 91));
        decimal amount = random.Next(0, 100_000_000) / 100m;
        Span<char> freeField = stackalloc char[FreeFieldLength];
        for (int i = 0; i < FreeFieldLength; i++)
        {
            freeField[i] = (char)('0' + random.Next(10));
        }
        return Create(bank, 9, dueDate, amount, new string(freeField));
    }

    private static Boleto Pack(Span<char> barcode)
    {
        PackedDigits.Pack(barcode, out ulong lo, out ulong mid, out ulong hi);
        return new Boleto(lo, mid, hi);
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
                // The three field DVs are módulo 10 over fields 1–3 of the linha digitável.
                if (FebrabanCheckDigit.Mod10(digits[..9]) != digits[9] - '0'
                    || FebrabanCheckDigit.Mod10(digits[10..20]) != digits[20] - '0'
                    || FebrabanCheckDigit.Mod10(digits[21..31]) != digits[31] - '0')
                {
                    return false;
                }
                AssembleBarcode(digits, barcode);
                break;
            default:
                return false;
        }
        return GeneralBarcodeDv(barcode) == barcode[4] - '0';
    }

    private static void AssembleBarcode(ReadOnlySpan<char> linha, Span<char> barcode)
    {
        linha[..4].CopyTo(barcode);          // bank code + currency code
        barcode[4] = linha[32];              // general DV
        linha[33..37].CopyTo(barcode[5..]);  // due-date factor
        linha[37..].CopyTo(barcode[9..]);    // amount
        linha[4..9].CopyTo(barcode[19..]);   // free field, part 1
        linha[10..20].CopyTo(barcode[24..]); // free field, part 2
        linha[21..31].CopyTo(barcode[34..]); // free field, part 3
    }

    /// <summary>The general DV: módulo 11 over the 43 barcode digits other than position 5.</summary>
    private static int GeneralBarcodeDv(ReadOnlySpan<char> barcode)
    {
        Span<char> digits = stackalloc char[DvLength];
        barcode[..4].CopyTo(digits);
        barcode[5..].CopyTo(digits[4..]);
        return FebrabanCheckDigit.BankBarcodeDv(digits);
    }

    private static int ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => 0,
        1 when format[0] is 'G' or 'g' => 0,
        1 when format[0] is 'N' or 'n' => 1,
        1 when format[0] is 'B' or 'b' => 2,
        _ => throw new FormatException(
            $"Format '{DocumentText.Preview(format)}' is not valid for a boleto; use \"G\" (formatted linha digitável), \"N\" (linha digits) or \"B\" (código de barras)."),
    };

    private static Boleto ParseCore(ReadOnlySpan<char> value)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        if (!TryValidate(value, barcode))
        {
            throw new FormatException(
                $"'{DocumentText.Preview(value)}' is not a valid boleto (44-digit código de barras or 47-digit linha digitável).");
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
        barcode[..4].CopyTo(destination);
        barcode[19..24].CopyTo(destination[4..]);
        barcode[24..34].CopyTo(destination[10..]);
        barcode[34..].CopyTo(destination[21..]);
        destination[9] = (char)('0' + FebrabanCheckDigit.Mod10(destination[..9]));
        destination[20] = (char)('0' + FebrabanCheckDigit.Mod10(destination[10..20]));
        destination[31] = (char)('0' + FebrabanCheckDigit.Mod10(destination[21..31]));
        destination[32] = barcode[4];
        barcode[5..9].CopyTo(destination[33..]);
        barcode[9..19].CopyTo(destination[37..]);
        return LinhaLength;
    }

    private int WriteFormatted(Span<char> destination)
    {
        Span<char> linha = stackalloc char[LinhaLength];
        WriteLinha(linha);
        linha[..5].CopyTo(destination);
        destination[5] = '.';
        linha[5..10].CopyTo(destination[6..]);
        destination[11] = ' ';
        linha[10..15].CopyTo(destination[12..]);
        destination[17] = '.';
        linha[15..21].CopyTo(destination[18..]);
        destination[24] = ' ';
        linha[21..26].CopyTo(destination[25..]);
        destination[30] = '.';
        linha[26..32].CopyTo(destination[31..]);
        destination[37] = ' ';
        destination[38] = linha[32];
        destination[39] = ' ';
        linha[33..].CopyTo(destination[40..]);
        return FormattedLinhaLength;
    }

    private void WriteFreeField(Span<char> destination)
    {
        Span<char> barcode = stackalloc char[BarcodeLength];
        PackedDigits.Unpack(_lo, _mid, _hi, barcode);
        barcode[19..].CopyTo(destination);
    }
}
