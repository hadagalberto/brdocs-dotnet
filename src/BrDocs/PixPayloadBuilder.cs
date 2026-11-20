using System.Globalization;

namespace BrDocs;

/// <summary>
/// Fluent builder for Pix BR Code payloads, started from
/// <see cref="PixPayload.CreateStatic(string, string, string)"/> (a payload with the receiver's
/// key) or <see cref="PixPayload.CreateDynamic(string, string, string)"/> (a payload with a
/// charge location URL). Call <see cref="Build"/> to obtain the encoded payload with its CRC.
/// </summary>
/// <remarks>
/// <para>
/// The builder emits the data objects in ascending ID order and computes the CRC-16/CCITT-FALSE
/// (field 63) last, over the whole payload including "6304". Defaults follow the Banco Central
/// do Brasil examples: MCC "0000", currency "986", country "BR", txid "***" (absent usage)
/// and the amount open for the payer to type.
/// </para>
/// <para>
/// The name and the city are normalized when the builder starts: diacritics are stripped
/// ("Brasília" → "BRASILIA" with <see cref="WithCity(string, bool)"/>'s uppercasing, or
/// "Brasilia" without) and any character outside ASCII letters, digits and spaces is rejected.
/// Dynamic payloads refuse a txid and an amount — the manual, section 2.7: "Os campos Valor e
/// Identificador da Transação (txid) não devem ser preenchidos no QR Code dinâmico".
/// </para>
/// </remarks>
public sealed class PixPayloadBuilder
{
    private const string DefaultMcc = "0000";
    private const string DefaultTxid = "***";
    private static readonly decimal s_maxAmount = 9_999_999_999.99m;   // 13 characters: 10 digits + '.' + 2.

    private readonly string? _key;        // static payloads only.
    private readonly string? _location;   // dynamic payloads only.
    private string _name;
    private string _city;
    private readonly bool _dynamic;

    private bool _singleUse;
    private string _mcc = DefaultMcc;
    private string? _description;
    private string? _txid;                  // static only; defaults to "***".
    private decimal? _amount;               // static only.
    private string? _withdrawalFacilitator; // static only (field 26-03, fss).
    private readonly List<PixTlv> _additionalData = new();
    private readonly List<PixTlv> _unreserved = new();

    internal PixPayloadBuilder(string? key, string? location, string name, string city)
    {
        _key = key;
        _location = location;
        _dynamic = location is not null;
        _name = name;
        _city = city;
    }

    /// <summary>
    /// Overrides the merchant name (field 59). The text is normalized: diacritics are stripped
    /// and it is uppercased when <paramref name="uppercase"/> is <see langword="true"/>
    /// ("Fulano de Tal" is preserved as-is, matching the BCB example).
    /// </summary>
    /// <param name="name">The merchant name, 1..25 characters.</param>
    /// <param name="uppercase">Upper-cases the normalized text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The name is empty, too long or holds an invalid character.</exception>
    public PixPayloadBuilder WithName(string name, bool uppercase = false)
    {
        _name = PixPayload.NormalizeText(name, PixText.MaxNameLength, uppercase, nameof(name));
        return this;
    }

    /// <summary>
    /// Overrides the merchant city (field 60); see <see cref="WithName(string, bool)"/> for the
    /// normalization rules.
    /// </summary>
    /// <param name="city">The merchant city, 1..15 characters.</param>
    /// <param name="uppercase">Upper-cases the normalized text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="city"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The city is empty, too long or holds an invalid character.</exception>
    public PixPayloadBuilder WithCity(string city, bool uppercase = false)
    {
        _city = PixPayload.NormalizeText(city, PixText.MaxCityLength, uppercase, nameof(city));
        return this;
    }

    /// <summary>
    /// Sets the additional information of the merchant account template (field 26-02,
    /// "infoAdicional") — free text shown to the payer while tracking the payment.
    /// Passing <see langword="null"/> clears it. Diacritics are stripped so the text stays inside
    /// the single-byte BR Code character set ("Café" is encoded as "Cafe").
    /// </summary>
    /// <param name="description">1..72 characters, or <see langword="null"/> to omit the field.</param>
    /// <exception cref="ArgumentException">
    /// The normalized description is empty, longer than 72 characters or holds a character
    /// outside the single-byte character set.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The payload is dynamic: the payer reads the charge data from the location URL, and a
    /// dynamic code carries only the GUI and the location.
    /// </exception>
    public PixPayloadBuilder WithDescription(string? description)
    {
        if (_dynamic)
        {
            throw new InvalidOperationException("A dynamic Pix BR Code carries only the GUI and the location URL in its merchant account template; the payer reads the charge data from the location (Manual de Padrões para Iniciação do Pix, section 2.7).");
        }
        if (description is null || description.Length == 0)
        {
            _description = null;
            return this;
        }
        // Field 26-02 is "conjunto livre de caracteres" (Manual section 2.6, footnote 24), but
        // the BR Code character set is single-byte: strip diacritics so the TLV length and the
        // CRC describe the exact bytes a QR writer emits (independent review finding 2).
        _description = PixPayload.NormalizeFreeText(description, PixBrCode.MaxDescriptionLength, nameof(description));
        return this;
    }

    /// <summary>
    /// Sets the transaction amount (field 54), encoded invariantly with two decimal places,
    /// e.g. <c>100.50m</c> → "100.50". The amount stays open for the payer when never set.
    /// </summary>
    /// <param name="amount">The amount in reais, 0..9.999.999.999,99 with at most two decimal places.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative or above the 13-character encoding limit.</exception>
    /// <exception cref="ArgumentException"><paramref name="amount"/> has more than two decimal places.</exception>
    /// <exception cref="InvalidOperationException">
    /// The payload is dynamic: a dynamic code must not carry the amount, which the payer's PSP
    /// reads from the charge fetched at the location URL.
    /// </exception>
    public PixPayloadBuilder WithAmount(decimal amount)
    {
        if (_dynamic)
        {
            throw new InvalidOperationException("A dynamic Pix BR Code must not carry the amount (field 54): the amount comes from the charge fetched at the location URL (Manual de Padrões para Iniciação do Pix, section 2.7).");
        }
        if (amount < 0m || amount > s_maxAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                $"The amount must be between 0 and {s_maxAmount.ToString(CultureInfo.InvariantCulture)}.");
        }
        if (decimal.Round(amount, 2, MidpointRounding.ToZero) != amount)
        {
            throw new ArgumentException(
                $"The amount {amount.ToString(CultureInfo.InvariantCulture)} has more than two decimal places; the BR Code encodes whole centavos.",
                nameof(amount));
        }
        _amount = amount;
        return this;
    }

    /// <summary>
    /// Sets the txid (field 62-05, the Reference Label) used for conciliation. The default
    /// "***" marks the absence of a txid, per EMV QRCPS-MPM section 4.8.1.2.
    /// </summary>
    /// <param name="txid">1..25 letters/digits, or "***".</param>
    /// <exception cref="ArgumentException"><paramref name="txid"/> holds other characters or is too long.</exception>
    /// <exception cref="InvalidOperationException">
    /// The payload is dynamic: a dynamic code must not carry a txid, which the payer's PSP reads
    /// from the charge fetched at the location URL.
    /// </exception>
    public PixPayloadBuilder WithTxid(string txid)
    {
        if (_dynamic)
        {
            throw new InvalidOperationException("A dynamic Pix BR Code must not carry a txid (field 62-05): the txid comes from the charge fetched at the location URL (Manual de Padrões para Iniciação do Pix, section 2.7).");
        }
        ArgumentNullException.ThrowIfNull(txid);
        if (txid != DefaultTxid)
        {
            if (txid.Length is 0 or > 25)
            {
                throw new ArgumentException("The txid holds 1..25 characters, or '***'.", nameof(txid));
            }
            foreach (char c in txid)
            {
                if (!char.IsAsciiLetterOrDigit(c))
                {
                    throw new ArgumentException($"The txid '{txid}' must hold only letters and digits (or be '***').", nameof(txid));
                }
            }
        }
        _txid = txid;
        return this;
    }

    /// <summary>
    /// Sets the merchant category code (field 52), four digits; the default is "0000"
    /// (not informed).
    /// </summary>
    /// <param name="mcc">The MCC, e.g. "5812" for restaurants.</param>
    /// <exception cref="ArgumentException"><paramref name="mcc"/> is not exactly four digits.</exception>
    public PixPayloadBuilder WithMcc(string mcc)
    {
        ArgumentNullException.ThrowIfNull(mcc);
        if (mcc.Length != PixBrCode.MccLength)
        {
            throw new ArgumentException($"The merchant category code '{mcc}' must be exactly four digits.", nameof(mcc));
        }
        foreach (char c in mcc)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new ArgumentException($"The merchant category code '{mcc}' must be exactly four digits.", nameof(mcc));
            }
        }
        _mcc = mcc;
        return this;
    }

    /// <summary>
    /// Marks the payload as Pix Saque by setting the ISPB of the withdrawal service facilitator
    /// (field 26-03, "fss"), a feature of static codes.
    /// </summary>
    /// <param name="ispb">The 8-digit ISPB of the facilitator.</param>
    /// <exception cref="ArgumentException"><paramref name="ispb"/> is not exactly eight digits.</exception>
    /// <exception cref="InvalidOperationException">The payload is dynamic: Pix Saque facilitation in dynamic codes is declared in the charge, not in the BR Code.</exception>
    public PixPayloadBuilder WithWithdrawalFacilitator(string ispb)
    {
        if (_dynamic)
        {
            throw new InvalidOperationException("The withdrawal facilitator (field 26-03) is declared in the charge JSON of dynamic codes, not in the BR Code.");
        }
        ArgumentNullException.ThrowIfNull(ispb);
        if (ispb.Length != PixBrCode.WithdrawalFacilitatorLength)
        {
            throw new ArgumentException($"The ISPB '{ispb}' must be exactly eight digits.", nameof(ispb));
        }
        foreach (char c in ispb)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new ArgumentException($"The ISPB '{ispb}' must be exactly eight digits.", nameof(ispb));
            }
        }
        _withdrawalFacilitator = ispb;
        return this;
    }

    /// <summary>
    /// Adds one sub-field to the additional data template (field 62) — the bill number (01),
    /// mobile number (02), store label (03), loyalty number (04), customer label (06), terminal
    /// label (07), purpose (08), additional consumer data request (09) or payment-system
    /// specific data (50–99). Use <see cref="WithTxid(string)"/> for the Reference Label (05).
    /// </summary>
    /// <param name="id">The two-digit sub-field ID: 01–09 (except 05) or 50–99; 00 and 10–49 are invalid.</param>
    /// <param name="value">The sub-field value, up to 25 characters.</param>
    /// <exception cref="ArgumentException">
    /// The ID is invalid or already used, or the value is empty or longer than 25 characters.
    /// </exception>
    public PixPayloadBuilder WithAdditionalDataField(string id, string value)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(value);
        int numericId = -1;
        if (id.Length != 2 || !char.IsAsciiDigit(id[0]) || !char.IsAsciiDigit(id[1])
            || (numericId = (id[0] - '0') * 10 + (id[1] - '0')) is 0 or 5 or (>= 10 and <= 49))
        {
            throw new ArgumentException($"'{id}' is not a valid additional data sub-field ID (01-09 except the txid 05, and 50-99).", nameof(id));
        }
        // The same single-byte invariant applies to every free-text sub-field of field 62.
        string normalized = PixPayload.NormalizeFreeText(value, PixBrCode.AdditionalDataMaxLength, nameof(value));
        if (_additionalData.Exists(field => field.Id == id))
        {
            throw new ArgumentException($"The additional data sub-field {id} is already set.", nameof(id));
        }
        _additionalData.Add(PixTlv.CreateChild(id, normalized));
        return this;
    }

    /// <summary>
    /// Adds an unreserved template (field 80–99) — the recurrence parameters of a QR code
    /// composto, carrying the <c>br.gov.bcb.pix</c> GUI and the location URL of the parameters.
    /// </summary>
    /// <param name="template">
    /// The template, built with <see cref="PixTlv.CreateTemplate(string, IEnumerable{PixTlv})"/>;
    /// its ID must be 80–99 and it must start with the GUI sub-field 00.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="template"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The template ID is not 80–99, it has no <c>br.gov.bcb.pix</c> GUI sub-field 00, or its
    /// ID is already in use.
    /// </exception>
    public PixPayloadBuilder WithUnreservedTemplate(PixTlv template)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (!template.IsTemplate || TemplateId(template) is not (>= 80 and <= 99))
        {
            throw new ArgumentException($"The unreserved template ID '{template.Id}' must be one of 80-99.", nameof(template));
        }
        bool hasGui = false;
        foreach (PixTlv child in template.Children!)
        {
            if (child.Id == "00" && MemoryExtensions.Equals(child.Value.AsSpan(), PixBrCode.Gui, StringComparison.OrdinalIgnoreCase))
            {
                hasGui = true;
                break;
            }
        }
        if (!hasGui)
        {
            throw new ArgumentException("An unreserved template must start with the 'br.gov.bcb.pix' GUI sub-field 00 (Manual de Padrões para Iniciação do Pix, section 2.5).", nameof(template));
        }
        if (_unreserved.Exists(existing => existing.Id == template.Id))
        {
            throw new ArgumentException($"The unreserved template {template.Id} is already set.", nameof(template));
        }
        _unreserved.Add(template);
        return this;
    }

    /// <summary>
    /// Marks the code as single-use: the point of initiation method (field 01) is set to "12",
    /// telling the payer's application not to start more than one payment with this code.
    /// Passing <see langword="false"/> keeps the field absent (reusable, the default).
    /// </summary>
    public PixPayloadBuilder SingleUse(bool singleUse = true)
    {
        _singleUse = singleUse;
        return this;
    }

    /// <summary>
    /// Assembles the payload: the data objects in ascending ID order with the CRC-16/
    /// CCITT-FALSE (field 63) computed last, over the whole payload including "6304".
    /// </summary>
    /// <returns>The encoded, ready-to-use <see cref="PixPayload"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// A template exceeds the 99-character content limit (e.g. a description too long for the
    /// remaining space of the merchant account template).
    /// </exception>
    public PixPayload Build()
    {
        List<PixTlv> merchantChildren = new()
        {
            PixTlv.Create("00", PixBrCode.Gui),
        };
        if (_dynamic)
        {
            merchantChildren.Add(PixTlv.Create("25", _location!));
        }
        else
        {
            merchantChildren.Add(PixTlv.Create("01", _key!));
            if (_description is not null)
            {
                merchantChildren.Add(PixTlv.Create("02", _description));
            }
            if (_withdrawalFacilitator is not null)
            {
                merchantChildren.Add(PixTlv.Create("03", _withdrawalFacilitator));
            }
        }
        PixTlv merchantAccount = AssembleTemplate("26", merchantChildren, "merchant account information");

        List<PixTlv> additionalChildren = new();
        additionalChildren.Add(PixTlv.Create("05", _dynamic ? DefaultTxid : _txid ?? DefaultTxid));
        additionalChildren.AddRange(_additionalData);
        PixTlv additionalData = AssembleTemplate("62", additionalChildren, "additional data");

        List<PixTlv> fields = new()
        {
            PixTlv.Create("00", "01"),
        };
        if (_singleUse)
        {
            fields.Add(PixTlv.Create("01", "12"));
        }
        fields.Add(merchantAccount);
        fields.Add(PixTlv.Create("52", _mcc));
        fields.Add(PixTlv.Create("53", "986"));
        if (_amount is not null)
        {
            fields.Add(PixTlv.Create("54", _amount.Value.ToString("0.00", CultureInfo.InvariantCulture)));
        }
        fields.Add(PixTlv.Create("58", "BR"));
        fields.Add(PixTlv.Create("59", _name));
        fields.Add(PixTlv.Create("60", _city));
        fields.Add(additionalData);
        _unreserved.Sort(static (left, right) => string.CompareOrdinal(left.Id, right.Id));
        fields.AddRange(_unreserved);

        // The CRC-16/CCITT-FALSE covers everything up to and including "6304".
        int contentLength = 0;
        foreach (PixTlv field in fields)
        {
            contentLength += field.EncodedLength;
        }
        Span<char> crcInput = contentLength + 4 <= 1024 ? stackalloc char[1024] : new char[contentLength + 4];
        int position = 0;
        foreach (PixTlv field in fields)
        {
            field.TryEncode(crcInput[position..], out int written);
            position += written;
        }
        "6304".AsSpan().CopyTo(crcInput[position..]);
        position += 4;
        Span<char> hex = stackalloc char[4];
        Crc16.WriteHex(hex, Crc16.Compute(crcInput[..position]));
        fields.Add(PixTlv.Create("63", new string(hex)));

        return PixPayload.FromFields(fields);
    }

    private static PixTlv AssembleTemplate(string id, List<PixTlv> children, string description)
        => children.Sum(child => child.EncodedLength) > 99
            ? throw new InvalidOperationException($"The {description} template exceeds the 99-character content limit of a two-digit length.")
            : PixTlv.CreateTemplate(id, children);

    private static int TemplateId(PixTlv template) => (template.Id[0] - '0') * 10 + (template.Id[1] - '0');
}
