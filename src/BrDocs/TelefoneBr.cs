using System.Globalization;

namespace BrDocs;

/// <summary>
/// Telefone BR — a Brazilian geographic phone number: an ANATEL Código Nacional (DDD) plus an
/// 8-digit fixed-line or 9-digit mobile subscriber number.
/// </summary>
/// <remarks>
/// <para>
/// The number follows ITU-T E.164 with country code <c>+55</c> (Resolução ANATEL
/// nº 749/2022 — Plano de Numeração / Regulamento de Numeração dos Serviços de
/// Telecomunicações): a 2-digit Código Nacional (the DDD, validated against the 67 codes of
/// the ANATEL PGCN, Despacho Decisório nº 20/2026/PRRE/SPR) followed by the subscriber
/// number. A mobile number has 9 digits starting with <c>9</c> (art. 12); a fixed-line
/// number has 8 digits starting with <c>2</c>–<c>6</c> (art. 11 — the STFC/SCM range; from
/// 2027-03-01 Resolução ANATEL nº 777/2025 restricts STFC to 2–5).
/// </para>
/// <para>
/// Accepted input forms, in every combination of the ignored punctuation
/// (dots, dashes, slashes, spaces and the parentheses of the display form):
/// the local form "DDD + subscriber number" (<c>11912345678</c>), the display form
/// (<c>(11) 91234-5678</c>), the same with an explicit country code
/// (<c>+5511912345678</c>, E.164) and the same with a bare "55" country code
/// (<c>5511987654321</c>). The bare "55" is
/// only read as a country code when 12 or 13 digits remain, so the DDD 55 (Rio Grande do Sul)
/// is never confused with it: <c>55987654321</c> is DDD 55 + a mobile number. A leading
/// <c>+</c> is only valid together with the country code; the legacy <c>00</c> dialing
/// prefix is not accepted.
/// </para>
/// <para>
/// Formatting: the display form is "(11) 91234-5678" for mobile and "(11) 3123-4567" for
/// fixed lines; "N" produces the national digits only (DDD + subscriber number); and
/// <see cref="ToE164"/> produces the full international form, e.g. "+5511912345678".
/// All formatting is culture-invariant.
/// </para>
/// <para>
/// The default value <see langword="default"/>/<see langword="new TelefoneBr()"/> represents
/// the all-zero digits "(00) 0000-0000", which is never a valid phone number (DDD 00 does
/// not exist).
/// </para>
/// </remarks>
public readonly record struct TelefoneBr : IEquatable<TelefoneBr>, ISpanFormattable, ISpanParsable<TelefoneBr>
{
    private const int NationalMaxLength = 11;    // DDD (2) + 9-digit mobile subscriber.
    private const int E164MobileLength = 14;     // "+55" + DDD + 9 digits.
    private const int E164FixedLength = 13;      // "+55" + DDD + 8 digits.
    private const int FormattedMobileLength = 15; // "(11) 91234-5678"
    private const int FormattedFixedLength = 14;  // "(11) 3123-4567"

    // DDD (11..99) + subscriber (up to 9 digits) + the mobile/fixed split.
    private readonly byte _ddd;
    private readonly uint _subscriber;
    private readonly bool _mobile;

    private TelefoneBr(byte ddd, uint subscriber, bool mobile)
    {
        _ddd = ddd;
        _subscriber = subscriber;
        _mobile = mobile;
    }

    /// <summary>
    /// Determines whether the given text represents a valid Brazilian geographic phone number.
    /// </summary>
    /// <param name="value">
    /// Local ("DDD + number"), E.164 ("+55…") or bare-country-code ("55…") phone text; dots,
    /// dashes, slashes and spaces are ignored and a single leading <c>+</c> is allowed.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the DDD is one of the 67 ANATEL codes, a 9-digit
    /// subscriber number starts with <c>9</c> (mobile) and an 8-digit one starts with
    /// <c>2</c>–<c>6</c> (fixed).
    /// </returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> value) => TryResolve(value, out _, out _, out _);

    /// <summary>Attempts to parse a phone number, in every accepted input form.</summary>
    /// <param name="value">
    /// Local, E.164 or bare-country-code phone text; dots, dashes, slashes and spaces are
    /// ignored and a single leading <c>+</c> is allowed.
    /// </param>
    /// <param name="result">The parsed phone when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid phone number.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out TelefoneBr result)
    {
        if (!TryResolve(value, out byte ddd, out uint subscriber, out bool mobile))
        {
            result = default;
            return false;
        }
        result = new TelefoneBr(ddd, subscriber, mobile);
        return true;
    }

    /// <summary>Parses a phone number, throwing when the text is not valid.</summary>
    /// <param name="value">
    /// Local, E.164 or bare-country-code phone text; dots, dashes, slashes and spaces are
    /// ignored and a single leading <c>+</c> is allowed.
    /// </param>
    /// <returns>The parsed phone.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid phone number.</exception>
    public static TelefoneBr Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ParseCore(value.AsSpan());
    }

    /// <summary>Parses a phone number from a character span, throwing when the text is not valid.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; phone parsing is culture-invariant.</param>
    /// <returns>The parsed phone.</returns>
    /// <exception cref="FormatException">The text is not a valid phone number.</exception>
    public static TelefoneBr Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => ParseCore(s);

    /// <inheritdoc cref="Parse(string)"/>
    static TelefoneBr IParsable<TelefoneBr>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a phone from a string. Returns <see langword="false"/> for a
    /// <see langword="null"/> input; see <see cref="TryParse(ReadOnlySpan{char}, out TelefoneBr)"/>.
    /// </summary>
    static bool IParsable<TelefoneBr>.TryParse(string? s, IFormatProvider? provider, out TelefoneBr result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), out result);
    }

    /// <summary>Attempts to parse a phone from a character span; see <see cref="TryParse(ReadOnlySpan{char}, out TelefoneBr)"/>.</summary>
    /// <param name="s">The text to parse; formatting characters are ignored.</param>
    /// <param name="provider">Ignored; phone parsing is culture-invariant.</param>
    /// <param name="result">The parsed phone when valid; <see langword="default"/> otherwise.</param>
    /// <returns><see langword="true"/> when the text is a valid phone number.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TelefoneBr result) => TryParse(s, out result);

    /// <summary>
    /// Returns the phone in its display form: "(11) 91234-5678" for mobile or "(11) 3123-4567"
    /// for fixed lines.
    /// </summary>
    /// <returns>The formatted phone, e.g. "(11) 91234-5678".</returns>
    public override string ToString() => IsMobile
        ? string.Create(FormattedMobileLength, this, static (buffer, state) => state.Write(buffer, digitsOnly: false))
        : string.Create(FormattedFixedLength, this, static (buffer, state) => state.Write(buffer, digitsOnly: false));

    /// <summary>Formats the phone.</summary>
    /// <param name="format">
    /// "G" (or an empty/<see langword="null"/> format) produces the display form;
    /// "N" produces the national digits only (DDD + subscriber number). Matching is
    /// case-insensitive.
    /// </param>
    /// <returns>The formatted phone.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the phone; see <see cref="ToString(string?)"/>.</summary>
    /// <param name="format">"G"/empty/<see langword="null"/> display form, "N" national digits.</param>
    /// <param name="formatProvider">Ignored; phone formatting is culture-invariant.</param>
    /// <returns>The formatted phone.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ResolveFormat(format)
        ? ToString()
        : string.Create(IsMobile ? NationalMaxLength : NationalMaxLength - 1, this, static (buffer, state) => state.Write(buffer, digitsOnly: true));

    /// <summary>Writes the phone into a character span.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// The number of characters written; <c>0</c> when the method returns <see langword="false"/>.
    /// </param>
    /// <param name="format">"G"/empty for the display form; "N" for the national digits only.</param>
    /// <param name="provider">Ignored; phone formatting is culture-invariant.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="destination"/> is too small for the output.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="format"/> is neither "G" nor "N".</exception>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bool formatted = ResolveFormat(format);
        int requiredLength = formatted
            ? (IsMobile ? FormattedMobileLength : FormattedFixedLength)
            : (IsMobile ? NationalMaxLength : NationalMaxLength - 1);
        if (destination.Length < requiredLength)
        {
            charsWritten = 0;
            return false;
        }
        charsWritten = Write(destination, digitsOnly: !formatted);
        return true;
    }

    /// <summary>The Código Nacional (DDD) of the number, e.g. 11 for São Paulo.</summary>
    public int Ddd => _ddd;

    /// <summary>Returns <see langword="true"/> when the subscriber number has 9 digits starting with 9 (mobile).</summary>
    public bool IsMobile => _mobile;

    /// <summary>Returns <see langword="true"/> when the subscriber number has 8 digits starting with 2–6 (fixed line).</summary>
    public bool IsFixed => !_mobile;

    /// <summary>
    /// The subscriber number without the DDD — 9 digits for mobile ("91234 5678" as
    /// "912345678") or 8 digits for fixed lines ("31234567").
    /// </summary>
    public string Subscriber => string.Create(IsMobile ? 9 : 8, this, static (buffer, state) => state.WriteSubscriber(buffer));

    /// <summary>
    /// Returns the number in the international E.164 form: "+55" followed by the DDD and the
    /// subscriber number, e.g. "+5511912345678".
    /// </summary>
    /// <returns>The E.164 representation of the number.</returns>
    public string ToE164() => string.Create(IsMobile ? E164MobileLength : E164FixedLength, this, static (buffer, state) => state.WriteE164(buffer));

    /// <summary>
    /// Generates a random valid mobile phone number, useful for tests and fake data.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>A phone with a random ANATEL DDD and a 9-digit subscriber number starting with 9.</returns>
    public static TelefoneBr Generate(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        byte ddd = (byte)AnatelDddTable.GetRandom(random);
        uint subscriber = 900_000_000u + (uint)random.Next(100_000_000);
        return new TelefoneBr(ddd, subscriber, mobile: true);
    }

    /// <summary>
    /// Generates a random valid fixed-line phone number, useful for tests and fake data.
    /// </summary>
    /// <param name="rng">Optional source of randomness; defaults to <see cref="Random.Shared"/>.</param>
    /// <returns>
    /// A phone with a random ANATEL DDD and an 8-digit subscriber number starting with 2–5,
    /// the range of the fixed lines in actual STFC use.
    /// </returns>
    public static TelefoneBr GenerateFixed(Random? rng = null)
    {
        Random random = rng ?? Random.Shared;
        byte ddd = (byte)AnatelDddTable.GetRandom(random);
        // 8 digits starting with 2–5, the range of the fixed lines in actual STFC use.
        uint subscriber = (uint)(20_000_000 + random.Next(0, 4) * 10_000_000 + random.Next(10_000_000));
        return new TelefoneBr(ddd, subscriber, mobile: false);
    }

    private static bool TryResolve(ReadOnlySpan<char> value, out byte ddd, out uint subscriber, out bool mobile)
    {
        ddd = 0;
        subscriber = 0;
        mobile = false;

        // Collect the digits, allowing a single '+' before the first digit (E.164 marker);
        // parentheses are part of the display form and are ignored like the other punctuation.
        Span<char> digits = stackalloc char[13];
        bool plus = false;
        int written = 0;
        foreach (char c in value)
        {
            if (DocumentText.IsIgnored(c) || c is '(' or ')')
            {
                continue;
            }
            if (c == '+')
            {
                if (plus || written != 0)
                {
                    return false;
                }
                plus = true;
                continue;
            }
            if ((uint)(c - '0') > 9u)
            {
                return false;
            }
            if (written >= digits.Length)
            {
                return false;
            }
            digits[written++] = c;
        }

        // 12 or 13 digits carry a country code ("55"); 10 or 11 are the local national form.
        if (written is not (13 or 12 or 11 or 10))
        {
            return false;
        }
        int offset = written >= 12 ? 2 : 0;
        if (offset != 0 && (digits[0] != '5' || digits[1] != '5'))
        {
            return false;
        }
        ReadOnlySpan<char> national = digits.Slice(offset, written - offset);

        // A '+' requires the country code: only "+55…" is valid E.164 for Brazil.
        if (plus && written is not (12 or 13))
        {
            return false;
        }

        int code = (national[0] - '0') * 10 + (national[1] - '0');
        if (!AnatelDddTable.Contains(code))
        {
            return false;
        }
        ddd = (byte)code;

        if (national.Length == NationalMaxLength)
        {
            // Mobile: 9 digits starting with 9 (Resolução ANATEL nº 749/2022, art. 12).
            if (national[2] != '9')
            {
                return false;
            }
            mobile = true;
        }
        else
        {
            // Fixed line: 8 digits starting with 2–6 (art. 11 — the STFC/SCM range).
            if (national[2] is < '2' or > '6')
            {
                return false;
            }
        }
        subscriber = 0;
        for (int i = 2; i < national.Length; i++)
        {
            subscriber = subscriber * 10 + (uint)(national[i] - '0');
        }
        return true;
    }

    private static TelefoneBr ParseCore(ReadOnlySpan<char> value)
    {
        if (!TryResolve(value, out byte ddd, out uint subscriber, out bool mobile))
        {
            throw new FormatException($"'{DocumentText.Preview(value)}' is not a valid Brazilian phone number.");
        }
        return new TelefoneBr(ddd, subscriber, mobile);
    }

    private static bool ResolveFormat(ReadOnlySpan<char> format) => format.Length switch
    {
        0 => true,
        1 when format[0] is 'G' or 'g' => true,
        1 when format[0] is 'N' or 'n' => false,
        _ => throw new FormatException($"Format '{DocumentText.Preview(format)}' is not valid for a Brazilian phone number; use \"G\" (formatted) or \"N\" (national digits only)."),
    };

    private void WriteSubscriber(Span<char> destination)
    {
        uint value = _subscriber;
        int length = _mobile ? 9 : 8;
        for (int i = length - 1; i >= 0; i--)
        {
            destination[i] = (char)('0' + value % 10);
            value /= 10;
        }
    }

    private int Write(Span<char> destination, bool digitsOnly)
    {
        if (digitsOnly)
        {
            destination[0] = (char)('0' + _ddd / 10);
            destination[1] = (char)('0' + _ddd % 10);
            WriteSubscriber(destination[2..]);
            return _mobile ? NationalMaxLength : NationalMaxLength - 1;
        }
        destination[0] = '(';
        destination[1] = (char)('0' + _ddd / 10);
        destination[2] = (char)('0' + _ddd % 10);
        destination[3] = ')';
        destination[4] = ' ';
        WriteSubscriber(destination[5..]);
        if (_mobile)
        {
            // "(11) 91234-5678": the dash goes after the first 5 subscriber digits, so the
            // last 4 digits move one char to the right (copy from the end backwards).
            destination[14] = destination[13];
            destination[13] = destination[12];
            destination[12] = destination[11];
            destination[11] = destination[10];
            destination[10] = '-';
            return FormattedMobileLength;
        }
        // "(11) 3123-4567": the dash goes after the first 4 subscriber digits, so the last
        // 4 digits move one char to the right (copy from the end backwards).
        destination[13] = destination[12];
        destination[12] = destination[11];
        destination[11] = destination[10];
        destination[10] = destination[9];
        destination[9] = '-';
        return FormattedFixedLength;
    }

    private int WriteE164(Span<char> destination)
    {
        destination[0] = '+';
        destination[1] = '5';
        destination[2] = '5';
        destination[3] = (char)('0' + _ddd / 10);
        destination[4] = (char)('0' + _ddd % 10);
        WriteSubscriber(destination[5..]);
        return _mobile ? E164MobileLength : E164FixedLength;
    }
}
