using System.Globalization;

namespace BrDocs;

/// <summary>
/// A Pix BR Code payload (EMV® QRCPS-MPM, Banco Central do Brasil, "Manual de Padrões para
/// Iniciação do Pix") — the "Pix copia e cola" text scanned from a QR code or built for one.
/// </summary>
/// <remarks>
/// <para>
/// A payload is an ordered sequence of TLV data objects: the payload format indicator (00,
/// always "01"), the optional point of initiation method (01, "12" for single-use codes), a
/// merchant account information template (26–51) carrying the Pix GUI <c>br.gov.bcb.pix</c>,
/// the merchant category code (52, "0000" when not informed), the currency (53, "986" for the
/// Real), the optional amount (54), the country (58, "BR"), the merchant name (59, max 25) and
/// city (60, max 15), the additional data field template (62) with the txid Reference Label
/// (62-05, "***" when unused), the optional unreserved templates (80–99) and the CRC-16/
/// CCITT-FALSE (63). A <b>static</b> payload carries the receiver's Pix key (26-01); a
/// <b>dynamic</b> payload carries the location URL of the charge (26-25, no protocol prefix).
/// </para>
/// <para>
/// <see cref="Parse(string)"/> validates everything — structure, CRC, mandatory fields and the
/// Pix arrangement rules — and returns a typed object; <see cref="ToString"/> re-encodes the
/// payload and is guaranteed to reproduce the parsed text byte for byte, so unknown or extra
/// data objects survive a round trip. Fields outside the Pix arrangement (merchant account
/// templates of other payment systems, the tip/convenience indicators 55–57, the postal code
/// 61 and the language template 64) are preserved verbatim without semantic validation.
/// </para>
/// <para>
/// Use <see cref="CreateStatic(string, string, string)"/>/<see cref="CreateDynamic(string, string, string)"/>
/// plus the fluent options of <see cref="PixPayloadBuilder"/> to build payloads; the builder
/// normalizes the merchant name and the city (diacritics stripped, optional uppercasing,
/// allowed characters only), validates and canonicalizes the Pix key, formats the amount
/// invariantly and computes the CRC last.
/// </para>
/// <para>
/// The core package renders no QR code image: any QR library can encode
/// <see cref="ToString()"/> — QRCoder, for instance, with <c>QRCodeGenerator.CreateQrCode(payload, ECCLevel.M)</c>
/// — and the BCB payload is plain text, so the SPEC leaves image rendering out of BrDocs.
/// </para>
/// </remarks>
public sealed class PixPayload : IEquatable<PixPayload>
{
    private readonly List<PixTlv> _fields;
    private readonly PixTlv _merchantAccount;
    private readonly PixTlv? _initiation;
    private readonly PixTlv? _amount;
    private readonly PixTlv? _additionalData;

    private PixPayload(List<PixTlv> fields)
    {
        _fields = fields;
        _merchantAccount = FindPixMerchantAccount(fields) ?? throw new InvalidOperationException("A Pix payload requires a merchant account information template (26-51) with the 'br.gov.bcb.pix' GUI.");
        _initiation = FindField(fields, "01");
        _amount = FindField(fields, "54");
        _additionalData = FindField(fields, "62");
        EncodedLength = 0;
        foreach (PixTlv field in fields)
        {
            EncodedLength += field.EncodedLength;
        }
        UnreservedTemplates = FindUnreservedTemplates(fields);
    }

    /// <summary>The ordered root data objects of the payload, including the CRC (field 63).</summary>
    public IReadOnlyList<PixTlv> Fields => _fields;

    /// <summary>The sub-fields of the Pix merchant account information template (field 26–51).</summary>
    public IReadOnlyList<PixTlv> MerchantAccountFields => _merchantAccount.Children!;

    /// <summary>
    /// The sub-fields of the additional data field template (field 62); an empty list when the
    /// template is absent (only possible in dynamic payloads).
    /// </summary>
    public IReadOnlyList<PixTlv> AdditionalDataFields => _additionalData?.Children ?? (IReadOnlyList<PixTlv>)Array.Empty<PixTlv>();

    /// <summary>The unreserved templates (fields 80–99, the recurrence parameters of QR codes compostos).</summary>
    public IReadOnlyList<PixTlv> UnreservedTemplates { get; }

    /// <summary>The total number of characters of the encoded payload.</summary>
    public int EncodedLength { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the point of initiation method (field 01) is "12" —
    /// the code must not be used for more than one payment.
    /// </summary>
    public bool IsSingleUse => _initiation is not null && _initiation.Value == "12";

    /// <summary>
    /// The point of initiation method (field 01) — "11" (reusable) or "12" (single-use) — or
    /// <see langword="null"/> when the field is absent (reusable by default).
    /// </summary>
    public string? InitiationMethod => _initiation?.Value;

    /// <summary>
    /// The merchant account GUI as carried in field 26-00: <c>br.gov.bcb.pix</c>, matched
    /// case-insensitively on parse (the manual, section 2.5: "O GUI – DNS reverso – é case
    /// insensitive"), so a payload that carries e.g. "BR.GOV.BCB.PIX" keeps its original form
    /// in both this property and <see cref="Fields"/>.
    /// </summary>
    public string Gui => FindChild(_merchantAccount, "00")!.Value;

    /// <summary>
    /// The receiver's Pix key (field 26-01) — a valid CPF, CNPJ, e-mail, "+55" phone or UUID —
    /// or <see langword="null"/> in a dynamic payload.
    /// </summary>
    /// <seealso cref="PixKey"/>
    public string? Key => FindChild(_merchantAccount, "01")?.Value;

    /// <summary>
    /// Returns <see langword="true"/> when the key (field 26-01) is present, i.e. the payload is
    /// a static QR code.
    /// </summary>
    public bool IsStatic => Key is not null;

    /// <summary>
    /// The payload location URL (field 26-25) of a dynamic QR code — the URL the payer's PSP
    /// fetches the charge from, without a protocol prefix — or <see langword="null"/> in a
    /// static payload.
    /// </summary>
    public string? Location => FindChild(_merchantAccount, "25")?.Value;

    /// <summary>Returns <see langword="true"/> when the location URL (field 26-25) is present, i.e. the payload is a dynamic QR code.</summary>
    public bool IsDynamic => Location is not null;

    /// <summary>
    /// The additional information of the merchant account template (field 26-02,
    /// "infoAdicional"), shown to the payer while tracking the payment; <see langword="null"/>
    /// when absent.
    /// </summary>
    public string? Description => FindChild(_merchantAccount, "02")?.Value;

    /// <summary>
    /// The merchant category code (field 52), "0000" when not informed.
    /// </summary>
    public string Mcc => FindField(_fields, "52")?.Value ?? "0000";

    /// <summary>The transaction currency (field 53): "986", the ISO 4217 code of the Real.</summary>
    public string Currency => FindField(_fields, "53")!.Value;

    /// <summary>
    /// The transaction amount (field 54) in reais, or <see langword="null"/> when the payload
    /// leaves the amount open for the payer to type. The encoded text (with the exact original
    /// formatting) is available through <see cref="Fields"/>.
    /// </summary>
    public decimal? Amount => _amount is not null && decimal.TryParse(_amount.Value.AsSpan(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount) ? amount : null;

    /// <summary>The country code (field 58): "BR".</summary>
    public string Country => FindField(_fields, "58")!.Value;

    /// <summary>The merchant name (field 59) — up to 25 letters, digits and spaces.</summary>
    public string Name => FindField(_fields, "59")!.Value;

    /// <summary>The merchant city (field 60) — up to 15 letters, digits and spaces.</summary>
    public string City => FindField(_fields, "60")!.Value;

    /// <summary>
    /// The txid (field 62-05, the Reference Label) — "***" when absent, which per EMV QRCPS-MPM
    /// section 4.8.1.2 means the payer's application generates it.
    /// </summary>
    public string Txid => FindChild(_additionalData, "05")?.Value ?? "***";

    /// <summary>The CRC-16/CCITT-FALSE of the payload (field 63), as four uppercase hex digits.</summary>
    public string Crc => FindField(_fields, "63")!.Value;

    /// <summary>
    /// Starts building a <b>static</b> payload — the receiver's Pix key, name and city are
    /// mandatory; the txid defaults to "***", the merchant category code to "0000" and the
    /// amount stays open unless <see cref="PixPayloadBuilder.WithAmount(decimal)"/> sets it.
    /// </summary>
    /// <param name="key">
    /// The receiver's Pix key: a CPF, CNPJ, e-mail, "+55" phone or UUID, in canonical or
    /// formatted form (see <see cref="PixKey.TryNormalize(ReadOnlySpan{char}, Span{char}, out int)"/>).
    /// </param>
    /// <param name="name">The merchant name, up to 25 characters; diacritics are stripped.</param>
    /// <param name="city">The merchant city, up to 15 characters; diacritics are stripped.</param>
    /// <returns>A builder for the static payload.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The key is not a valid Pix key, or the name or city is empty, too long or holds a
    /// character outside letters, digits and spaces.
    /// </exception>
    public static PixPayloadBuilder CreateStatic(string key, string name, string city)
    {
        ArgumentNullException.ThrowIfNull(key);
        string canonicalKey = NormalizeKey(key);
        (string normalizedName, string normalizedCity) = NormalizeNameAndCity(name, city);
        return new PixPayloadBuilder(canonicalKey, null, normalizedName, normalizedCity);
    }

    /// <summary>
    /// Starts building a <b>dynamic</b> payload — the charge is fetched from a location URL,
    /// so no key, amount or txid is carried in the code (the manual, section 2.7: "Os campos
    /// Valor e Identificador da Transação (txid) não devem ser preenchidos no QR Code dinâmico").
    /// </summary>
    /// <param name="location">
    /// The payload location URL of the charge, 1..77 RFC 3986 characters. A protocol prefix
    /// ("https://") is stripped when given, as the BR Code must not carry it.
    /// </param>
    /// <param name="name">The merchant name, up to 25 characters; diacritics are stripped.</param>
    /// <param name="city">The merchant city, up to 15 characters; diacritics are stripped.</param>
    /// <returns>A builder for the dynamic payload.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The location is invalid, or the name or city is empty, too long or holds a character
    /// outside letters, digits and spaces.
    /// </exception>
    public static PixPayloadBuilder CreateDynamic(string location, string name, string city)
    {
        ArgumentNullException.ThrowIfNull(location);
        ReadOnlySpan<char> stripped = PixLocation.StripProtocolPrefix(location.AsSpan());
        if (!PixLocation.IsValid(stripped))
        {
            throw new ArgumentException($"'{DocumentText.Preview(location)}' is not a valid payload location URL: 1..77 RFC 3986 characters, without a protocol prefix.", nameof(location));
        }
        (string normalizedName, string normalizedCity) = NormalizeNameAndCity(name, city);
        return new PixPayloadBuilder(null, new string(stripped), normalizedName, normalizedCity);
    }

    /// <summary>
    /// Determines whether <paramref name="payload"/> is a complete, valid Pix BR Code: the
    /// TLV structure, the CRC, the mandatory fields and the Pix arrangement rules (see the
    /// class remarks).
    /// </summary>
    /// <param name="payload">The BR Code text, exactly as scanned.</param>
    /// <returns><see langword="true"/> when the payload parses.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public static bool IsValid(ReadOnlySpan<char> payload) => PixBrCode.TryValidate(payload, out _, out _);

    /// <summary>
    /// Attempts to parse and validate a Pix BR Code payload; see <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="payload">The BR Code text; <see langword="null"/> never parses.</param>
    /// <param name="result">The typed payload; <see langword="null"/> when the parse fails.</param>
    /// <returns><see langword="true"/> when the payload is valid.</returns>
    public static bool TryParse(string? payload, out PixPayload? result)
    {
        if (payload is null)
        {
            result = null;
            return false;
        }
        return TryParse(payload.AsSpan(), out result);
    }

    /// <summary>Attempts to parse and validate a Pix BR Code payload; see <see cref="Parse(string)"/>.</summary>
    /// <param name="payload">The BR Code text, exactly as scanned.</param>
    /// <param name="result">The typed payload; <see langword="null"/> when the parse fails.</param>
    /// <returns><see langword="true"/> when the payload is valid.</returns>
    public static bool TryParse(ReadOnlySpan<char> payload, out PixPayload? result)
    {
        result = null;
        if (!PixBrCode.TryValidate(payload, out _, out _) || !PixTlv.TryParseSequence(payload, out IReadOnlyList<PixTlv>? fields) || fields is null)
        {
            return false;
        }
        PixPayload parsed = new(new List<PixTlv>(fields));
        // Structural self-check: the materialized tree must re-encode to the exact input.
        Span<char> reencoded = parsed.EncodedLength <= 1024 ? stackalloc char[1024] : new char[parsed.EncodedLength];
        if (!parsed.TryEncode(reencoded, out int written) || !reencoded[..written].SequenceEqual(payload))
        {
            return false;
        }
        result = parsed;
        return true;
    }

    /// <summary>
    /// Parses and validates a Pix BR Code payload, extracting every typed field.
    /// </summary>
    /// <param name="payload">The BR Code text, exactly as scanned from the QR code.</param>
    /// <returns>The typed payload; <see cref="ToString"/> reproduces the input exactly.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The payload fails any structural, CRC or Pix validation.</exception>
    public static PixPayload Parse(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!TryParse(payload.AsSpan(), out PixPayload? result) || result is null)
        {
            PixBrCode.TryValidate(payload.AsSpan(), out PixBrCode.Error error, out int position);
            throw new FormatException(PixBrCode.Describe(error, position, payload));
        }
        return result;
    }

    /// <summary>Encodes the payload back into its BR Code text, including the CRC.</summary>
    /// <returns>The "Pix copia e cola" payload, e.g. "00020126…63041D3D".</returns>
    public override string ToString() => string.Create(EncodedLength, this, static (buffer, state) => state.TryEncode(buffer, out _));

    /// <summary>Writes the encoded payload into a character span; see <see cref="ToString"/>.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">The number of characters written; <c>0</c> when <see langword="false"/>.</param>
    /// <returns><see langword="false"/> when <paramref name="destination"/> is too small.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryEncode(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (destination.Length < EncodedLength)
        {
            return false;
        }
        int position = 0;
        foreach (PixTlv field in _fields)
        {
            if (!field.TryEncode(destination[position..], out int written))
            {
                return false;
            }
            position += written;
        }
        charsWritten = position;
        return true;
    }

    /// <summary>Two payloads are equal when their BR Code texts are equal.</summary>
    public bool Equals(PixPayload? other) => other is not null && ToString() == other.ToString();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PixPayload other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Builds the root field list of a payload from its parts (used by
    /// <see cref="PixPayloadBuilder"/>); the CRC (field 63) must be part of
    /// <paramref name="fields"/>.
    /// </summary>
    internal static PixPayload FromFields(List<PixTlv> fields) => new(fields);

    private static PixTlv? FindField(List<PixTlv> fields, string id)
    {
        foreach (PixTlv field in fields)
        {
            if (field.Id == id)
            {
                return field;
            }
        }
        return null;
    }

    private static PixTlv? FindChild(PixTlv? template, string id)
    {
        if (template?.Children is null)
        {
            return null;
        }
        foreach (PixTlv child in template.Children)
        {
            if (child.Id == id)
            {
                return child;
            }
        }
        return null;
    }

    private static PixTlv? FindPixMerchantAccount(List<PixTlv> fields)
    {
        foreach (PixTlv field in fields)
        {
            if (PixTlv.IsTemplateId(TemplateId(field)) && IsPixMerchantAccount(field))
            {
                return field;
            }
        }
        return null;
    }

    private static bool IsPixMerchantAccount(PixTlv template)
    {
        if (template.Children is null)
        {
            return false;
        }
        foreach (PixTlv child in template.Children)
        {
            if (child.Id == "00" && MemoryExtensions.Equals(child.Value.AsSpan(), PixBrCode.Gui, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static List<PixTlv> FindUnreservedTemplates(List<PixTlv> fields)
    {
        List<PixTlv> templates = new();
        foreach (PixTlv field in fields)
        {
            if (TemplateId(field) is >= 80 and <= 99)
            {
                templates.Add(field);
            }
        }
        return templates;
    }

    private static int TemplateId(PixTlv field) => (field.Id[0] - '0') * 10 + (field.Id[1] - '0');

    private static string NormalizeKey(string key)
    {
        Span<char> buffer = stackalloc char[PixKey.MaxLength];
        if (!PixKey.TryNormalize(key.AsSpan(), buffer, out int written))
        {
            throw new ArgumentException($"'{DocumentText.Preview(key)}' is not a valid Pix key (expected a CPF, CNPJ, e-mail, +55 phone or UUID).", nameof(key));
        }
        return new string(buffer[..written]);
    }

    private static (string Name, string City) NormalizeNameAndCity(string name, string city)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(city);
        return (NormalizeText(name, PixText.MaxNameLength, uppercase: false, nameof(name)),
            NormalizeText(city, PixText.MaxCityLength, uppercase: false, nameof(city)));
    }

    internal static string NormalizeText(string text, int maxLength, bool uppercase, string paramName)
    {
        Span<char> buffer = stackalloc char[maxLength];
        if (!PixText.TryNormalize(text.AsSpan(), maxLength, uppercase, buffer, out int written))
        {
            throw new ArgumentException(
                $"'{DocumentText.Preview(text)}' is not valid BR Code text: 1..{maxLength.ToString(CultureInfo.InvariantCulture)} letters, digits and spaces (accents are stripped).",
                paramName);
        }
        return new string(buffer[..written]);
    }

    /// <summary>
    /// Normalizes free-text fields (26-02, 62 sub-fields) into the single-byte character set so
    /// that the TLV length and the CRC are computed over the same bytes that a QR writer emits.
    /// </summary>
    internal static string NormalizeFreeText(string text, int maxLength, string paramName)
    {
        Span<char> buffer = stackalloc char[maxLength];
        if (!PixText.TryNormalizeFreeText(text.AsSpan(), buffer, out int written))
        {
            throw new ArgumentException(
                $"'{DocumentText.Preview(text)}' is not valid BR Code free text: 1..{maxLength.ToString(CultureInfo.InvariantCulture)} single-byte characters (accents are stripped).",
                paramName);
        }
        return new string(buffer[..written]);
    }
}
