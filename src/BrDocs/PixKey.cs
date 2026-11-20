using System.Globalization;

namespace BrDocs;

/// <summary>
/// Pix keys (DICT keys): detection and validation of the five key types — CPF, CNPJ (numeric and
/// the IN RFB nº 2.229/2024 alphanumeric format), e-mail, mobile phone and chave aleatória (EVP) —
/// plus normalization of user input into the canonical BR Code form.
/// </summary>
/// <remarks>
/// <para>
/// The canonical codification of each key type follows the Banco Central do Brasil,
/// "Manual de Padrões para Iniciação do Pix", section 2.5.1: a CPF is 11 digits
/// (<c>12345678900</c>), a CNPJ is 14 alphanumeric characters without punctuation
/// (<c>00038166000105</c> or <c>12ABC34501DE35</c>), an e-mail is the address as registered
/// (<c>fulano_da_silva.recebedor@example.com</c>), a phone is the international form
/// (<c>+5561912345678</c>) and a chave aleatória is the hyphenated UUID
/// (<c>123e4567-e12b-12d1-a456-426655440000</c>).
/// </para>
/// <para>
/// <see cref="IsValid(ReadOnlySpan{char})"/> and <see cref="TryDetect(ReadOnlySpan{char}, out PixKeyKind)"/>
/// accept only those canonical forms — exactly what a BR Code field 26-01 (or a DICT response)
/// carries. <see cref="TryNormalize(ReadOnlySpan{char}, Span{char}, out int)"/> additionally
/// accepts common formatted user input (punctuated CPF/CNPJ, "(11) 91234-5678" style phones and
/// bare-country-code numbers) and produces the canonical key.
/// </para>
/// <para>
/// Shape-wise the types are unambiguous except that an 11-digit string is read as a CPF (the
/// documented CPF form) even when it could also spell a local mobile number; phone keys must
/// carry the "+55" prefix to be recognized. The UUID check verifies the canonical 8-4-4-4-12
/// hyphenated form but not the RFC 4122 version/variant nibbles: the BCB manual's own example
/// key (<c>123e4567-e12b-12d1-a456-426655440000</c>) does not follow them.
/// </para>
/// </remarks>
public static class PixKey
{
    /// <summary>The maximum length of a Pix key: 77 characters (the DICT limit, manual section 2.6).</summary>
    public const int MaxLength = 77;

    private const int EvpLength = 36;
    private const int CpfLength = 11;
    private const int CnpjLength = 14;
    private const int E164MobileLength = 14;      // "+55" + DDD + 9-digit mobile number.
    private const int NationalMobileLength = 11;  // DDD + 9-digit mobile subscriber ("N" format).

    /// <summary>
    /// Determines whether <paramref name="key"/> is a valid Pix key in its canonical form — a
    /// valid CPF, CNPJ, e-mail, "+55…" mobile phone or UUID chave aleatória.
    /// </summary>
    /// <param name="key">The key text, exactly as codified in a BR Code.</param>
    /// <returns><see langword="true"/> when the key is recognized and valid.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> key) => TryDetect(key, out _);

    /// <summary>
    /// Detects the type of a canonical Pix key. Detection is by shape: a leading "+" spells a
    /// phone, an "@" an e-mail, the 8-4-4-4-12 hyphenated form a chave aleatória, 11 digits a CPF
    /// and 14 alphanumeric characters a CNPJ.
    /// </summary>
    /// <param name="key">The key text, exactly as codified in a BR Code.</param>
    /// <returns>The key type, or <see langword="null"/> when the text is not a valid key.</returns>
    /// <remarks>This method returns a new boxed enum value only when the key is valid.</remarks>
    public static PixKeyKind? Detect(ReadOnlySpan<char> key) => TryDetect(key, out PixKeyKind kind) ? kind : null;

    /// <summary>Attempts to detect the type of a canonical Pix key; see <see cref="Detect(ReadOnlySpan{char})"/>.</summary>
    /// <param name="key">The key text, exactly as codified in a BR Code.</param>
    /// <param name="kind">The detected type; <see langword="default"/> when the key is invalid.</param>
    /// <returns><see langword="true"/> when the text is a valid key of the detected type.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool TryDetect(ReadOnlySpan<char> key, out PixKeyKind kind)
    {
        kind = default;
        if (key.IsEmpty || key.Length > MaxLength)
        {
            return false;
        }
        if (key[0] == '+')
        {
            if (IsPhone(key))
            {
                kind = PixKeyKind.Phone;
                return true;
            }
            return false;
        }
        if (key.Contains('@'))
        {
            if (IsEmail(key))
            {
                kind = PixKeyKind.Email;
                return true;
            }
            return false;
        }
        if (IsEvp(key))
        {
            kind = PixKeyKind.Evp;
            return true;
        }
        if (key.Length == CpfLength && Cpf.IsValid(key))
        {
            kind = PixKeyKind.Cpf;
            return true;
        }
        if (key.Length == CnpjLength && IsCnpj(key))
        {
            kind = PixKeyKind.Cnpj;
            return true;
        }
        return false;
    }

    /// <summary>Determines whether <paramref name="key"/> is a valid CPF key: 11 digits.</summary>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsCpf(ReadOnlySpan<char> key) => key.Length == CpfLength && Cpf.IsValid(key);

    /// <summary>
    /// Determines whether <paramref name="key"/> is a valid CNPJ key: 14 uppercase alphanumeric
    /// characters, numeric or in the alphanumeric format of IN RFB nº 2.229/2024.
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsCnpj(ReadOnlySpan<char> key)
    {
        if (key.Length != CnpjLength)
        {
            return false;
        }
        foreach (char c in key)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }
        return Cnpj.IsValid(key);
    }

    /// <summary>
    /// Determines whether <paramref name="key"/> is a valid e-mail key: one "@", a local part
    /// of practical RFC 5322 atext characters and a dotted domain with a TLD of at least two
    /// letters, within the 77-character DICT limit.
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsEmail(ReadOnlySpan<char> key)
    {
        if (key.IsEmpty || key.Length > MaxLength)
        {
            return false;
        }
        int at = key.IndexOf('@');
        if (at <= 0)
        {
            return false;
        }
        ReadOnlySpan<char> local = key[..at];
        ReadOnlySpan<char> domain = key[(at + 1)..];
        if (local.Length > 64 || domain.IsEmpty || domain[^1] is '.' or '-')
        {
            return false;
        }
        if (local[0] is '.' || local[^1] is '.')
        {
            return false;
        }
        char previous = '\0';
        foreach (char c in local)
        {
            if (c == '.' && previous == '.')
            {
                return false;
            }
            previous = c;
            if (!IsAtext(c))
            {
                return false;
            }
        }
        return IsValidDomain(domain);
    }

    /// <summary>
    /// Determines whether <paramref name="key"/> is a valid phone key: "+55" followed by a valid
    /// ANATEL DDD and a 9-digit mobile number, e.g. "+5561912345678".
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsPhone(ReadOnlySpan<char> key)
        => key.Length == E164MobileLength && key[0] == '+' && TelefoneBr.TryParse(key, out TelefoneBr phone) && phone.IsMobile;

    /// <summary>
    /// Determines whether <paramref name="key"/> is a valid chave aleatória (EVP): a UUID in the
    /// canonical 8-4-4-4-12 hyphenated form. The version and variant nibbles are not enforced
    /// (the BCB manual's own example key does not follow RFC 4122 v4).
    /// </summary>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsEvp(ReadOnlySpan<char> key)
    {
        if (key.Length != EvpLength)
        {
            return false;
        }
        for (int i = 0; i < EvpLength; i++)
        {
            if (i is 8 or 13 or 18 or 23)
            {
                if (key[i] != '-')
                {
                    return false;
                }
            }
            else if (!char.IsAsciiHexDigit(key[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Normalizes <paramref name="key"/> into its canonical BR Code form, accepting formatted
    /// user input: punctuated CPFs/CNPJs (lowercase CNPJs are uppercased), phones in local,
    /// display or bare-country-code forms (normalized to "+55…"), and uppercased UUIDs
    /// (lowercased, per the RFC 4122 output convention). E-mails are used exactly as given.
    /// </summary>
    /// <param name="key">The raw key text.</param>
    /// <param name="destination">
    /// The destination buffer; a buffer of <see cref="MaxLength"/> characters never overflows.
    /// </param>
    /// <param name="written">The number of characters written; <c>0</c> when <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="false"/> when the text is not a valid key in any accepted form, or when
    /// <paramref name="destination"/> is too small.
    /// </returns>
    /// <remarks>
    /// <para>This method performs no allocations.</para>
    /// <para>
    /// An 11-digit string that is a valid CPF is normalized as a CPF; an invalid 11-digit string
    /// that parses as a local mobile number is normalized as a phone. Keys longer than the
    /// canonical forms are rejected.
    /// </para>
    /// </remarks>
    public static bool TryNormalize(ReadOnlySpan<char> key, Span<char> destination, out int written)
    {
        written = 0;
        if (TryDetect(key, out PixKeyKind kind))
        {
            return kind == PixKeyKind.Evp
                ? TryCopyLower(key, destination, out written)
                : TryCopy(key, destination, out written);
        }

        // Formatted CPF/CNPJ input: strip the ignored punctuation (lowercase CNPJs are uppercased).
        Span<char> alphanumeric = stackalloc char[CnpjLength + 4];
        if (DocumentText.TryNormalizeUpperAlphanumeric(key, alphanumeric, out int count) && count is CpfLength or CnpjLength)
        {
            ReadOnlySpan<char> digits = alphanumeric[..count];
            if (count == CpfLength && Cpf.IsValid(digits))
            {
                return TryCopy(digits, destination, out written);
            }
            if (count == CnpjLength && IsCnpj(digits))
            {
                return TryCopy(digits, destination, out written);
            }
        }

        // Phone input in local, display or bare-country-code form; only mobiles are Pix keys.
        if (key.Length <= E164MobileLength + 6 && TelefoneBr.TryParse(key, out TelefoneBr phone) && phone.IsMobile)
        {
            Span<char> national = stackalloc char[NationalMobileLength];
            if (!phone.TryFormat(national, out int nationalDigits, "N", CultureInfo.InvariantCulture))
            {
                return false;
            }
            if (destination.Length < 3 + nationalDigits)
            {
                return false;
            }
            destination[0] = '+';
            destination[1] = '5';
            destination[2] = '5';
            national[..nationalDigits].CopyTo(destination[3..]);
            written = 3 + nationalDigits;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Normalizes <paramref name="key"/> into its canonical BR Code form; see
    /// <see cref="TryNormalize(ReadOnlySpan{char}, Span{char}, out int)"/>.
    /// </summary>
    /// <param name="key">The raw key text.</param>
    /// <returns>The canonical key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a valid Pix key in any accepted form.</exception>
    public static string Normalize(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Span<char> buffer = stackalloc char[MaxLength];
        if (!TryNormalize(key.AsSpan(), buffer, out int written))
        {
            throw new FormatException($"'{DocumentText.Preview(key)}' is not a valid Pix key (expected a CPF, CNPJ, e-mail, +55 phone or UUID).");
        }
        return new string(buffer[..written]);
    }

    private static bool TryCopy(ReadOnlySpan<char> source, Span<char> destination, out int written)
    {
        if (destination.Length < source.Length)
        {
            written = 0;
            return false;
        }
        source.CopyTo(destination);
        written = source.Length;
        return true;
    }

    private static bool TryCopyLower(ReadOnlySpan<char> source, Span<char> destination, out int written)
    {
        if (destination.Length < source.Length)
        {
            written = 0;
            return false;
        }
        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = char.IsAsciiLetter(source[i]) ? char.ToLowerInvariant(source[i]) : source[i];
        }
        written = source.Length;
        return true;
    }

    // RFC 5322 atext plus the dot separator (the practical set accepted for local parts).
    private static bool IsAtext(char c)
        => char.IsAsciiLetterOrDigit(c) || c is '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '/' or '=' or '?' or '^' or '_' or '`' or '{' or '|' or '}' or '~' or '.';

    private static bool IsValidDomain(ReadOnlySpan<char> domain)
    {
        if (domain[0] is '.' or '-')
        {
            return false;
        }
        int labelLength = 0;
        for (int i = 0; i < domain.Length; i++)
        {
            char c = domain[i];
            if (c == '.')
            {
                // Empty label ("a..b") or a hyphen-terminated label ("a-.b") is invalid.
                if (labelLength == 0 || domain[i - 1] == '-')
                {
                    return false;
                }
                labelLength = 0;
                continue;
            }
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
            {
                return false;
            }
            if (++labelLength > 63)
            {
                return false;
            }
        }
        // At least one dot (a TLD of at least 2 characters), with no trailing hyphen.
        return domain.Contains('.') && labelLength >= 2 && domain[^1] != '-';
    }
}
