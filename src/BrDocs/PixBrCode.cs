using System.Globalization;

namespace BrDocs;

/// <summary>
/// The Pix BR Code validation engine: a single, allocation-free walk over the payload that
/// checks the TLV structure, the CRC, the required EMV® QRCPS-MPM fields and the Pix rules of
/// the Banco Central do Brasil, "Manual de Padrões para Iniciação do Pix" (sections 2.5–2.8).
/// </summary>
/// <remarks>
/// The walk underlies <see cref="PixPayload.IsValid(ReadOnlySpan{char})"/> (no allocation at
/// all) and runs before the materializing decode in <c>PixPayload.TryParse</c>, which re-encodes
/// its result and compares it with the input as a self-check.
/// </remarks>
internal static class PixBrCode
{
    // Field 26-00: the Pix GUI, matched case-insensitively ("O GUI – DNS reverso – é case
    // insensitive", Manual de Padrões para Iniciação do Pix, section 2.5).
    internal const string Gui = "br.gov.bcb.pix";

    internal const int GuiLength = 14;
    internal const int MaxGuiLength = 32;        // EMV QRCPS-MPM: MAI sub-field 00 max 32.
    internal const int MaxDescriptionLength = 72;  // Manual section 2.6, footnote 24.
    internal const int WithdrawalFacilitatorLength = 8;  // ISPB, field 26-03 (fss).
    internal const int MccLength = 4;
    internal const int AmountMaxLength = 13;     // EMV QRCPS-MPM: field 54 max length 13.
    internal const int CountryLength = 2;
    internal const int CrcLength = 4;
    internal const int AdditionalDataMaxLength = 25;    // EMV QRCPS-MPM: field 62 sub-fields max 25.

    /// <summary>
    /// The full, ordered set of validation failures the walk can report, each mapped to a
    /// human-readable message by <see cref="Describe(Error, int, ReadOnlySpan{char})"/>.
    /// </summary>
    internal enum Error
    {
        None = 0,
        Empty,
        TruncatedHeader,
        InvalidId,
        InvalidLength,
        TruncatedValue,
        DuplicateId,
        FormatIndicatorNotFirst,
        FormatIndicatorMissing,
        FormatIndicatorInvalid,
        InitiationMethodInvalid,
        ReservedId,
        CrcMissing,
        CrcNotLast,
        CrcInvalidHex,
        CrcMismatch,
        MerchantAccountMissing,
        MerchantAccountGuiMissing,
        MerchantAccountGuiTooLong,
        MerchantAccountIncomplete,
        KeyAndLocationConflict,
        KeyInvalid,
        LocationInvalid,
        DescriptionInvalid,
        AdditionalDataInvalid,
        WithdrawalFacilitatorInvalid,
        MccMissing,
        MccInvalid,
        CurrencyMissing,
        CurrencyInvalid,
        AmountInvalid,
        CountryMissing,
        CountryInvalid,
        MerchantNameMissing,
        MerchantNameInvalid,
        MerchantCityMissing,
        MerchantCityInvalid,
        AdditionalDataIdInvalid,
        AdditionalDataTooLong,
        TxidInvalid,
        TxidMissing,
        UnreservedGuiInvalid,
        TemplateTrailingData,
    }

    /// <summary>
    /// Validates a whole BR Code payload: TLV structure, CRC-16/CCITT-FALSE, the mandatory
    /// QRCPS-MPM fields and the Pix arrangement rules (static key / dynamic location, txid,
    /// unreserved templates).
    /// </summary>
    /// <param name="payload">The BR Code text, exactly as scanned from a QR code.</param>
    /// <param name="error">The first failure found; <see cref="Error.None"/> when valid.</param>
    /// <param name="position">
    /// The position of the offending data object; <c>-1</c> for payload-level failures
    /// (missing fields) and when the payload is valid.
    /// </param>
    /// <returns><see langword="true"/> when the payload is a valid Pix BR Code.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    internal static bool TryValidate(ReadOnlySpan<char> payload, out Error error, out int position)
    {
        error = Error.None;
        position = -1;
        if (payload.IsEmpty)
        {
            error = Error.Empty;
            return false;
        }

        Span<bool> seenRoot = stackalloc bool[100];
        bool formatIndicatorSeen = false;
        bool mccSeen = false;
        bool currencySeen = false;
        bool countrySeen = false;
        bool nameSeen = false;
        bool citySeen = false;
        bool txidSeen = false;
        bool crcSeen = false;
        bool pixMerchantAccountSeen = false;
        bool staticPixMerchantAccount = false;
        bool first = true;
        int cursor = 0;

        while (cursor < payload.Length)
        {
            if (crcSeen)
            {
                // Nothing — not even a well-formed data object — may follow the CRC.
                position = cursor;
                error = Error.CrcNotLast;
                return false;
            }
            int remaining = payload.Length - cursor;
            if (remaining < 4)
            {
                position = cursor;
                error = Error.TruncatedHeader;
                return false;
            }
            ReadOnlySpan<char> header = payload.Slice(cursor, 4);
            if (!char.IsAsciiDigit(header[0]) || !char.IsAsciiDigit(header[1]))
            {
                position = cursor;
                error = Error.InvalidId;
                return false;
            }
            if (!char.IsAsciiDigit(header[2]) || !char.IsAsciiDigit(header[3]))
            {
                position = cursor;
                error = Error.InvalidLength;
                return false;
            }
            int id = (header[0] - '0') * 10 + (header[1] - '0');
            int length = (header[2] - '0') * 10 + (header[3] - '0');
            if (remaining - 4 < length)
            {
                position = cursor;
                error = Error.TruncatedValue;
                return false;
            }
            if (seenRoot[id])
            {
                position = cursor;
                error = Error.DuplicateId;
                return false;
            }
            seenRoot[id] = true;
            ReadOnlySpan<char> value = payload.Slice(cursor + 4, length);
            if (first)
            {
                // "Tag 00 comes first and tag 63 comes last" (EMV QRCPS-MPM section 4.2/4.3).
                first = false;
                if (id != 0)
                {
                    position = cursor;
                    error = Error.FormatIndicatorNotFirst;
                    return false;
                }
            }

            switch (id)
            {
                case 0:
                    formatIndicatorSeen = true;
                    if (!value.SequenceEqual("01"))
                    {
                        position = cursor;
                        error = Error.FormatIndicatorInvalid;
                        return false;
                    }
                    break;
                case 1:
                    if (value is not ("11" or "12"))
                    {
                        position = cursor;
                        error = Error.InitiationMethodInvalid;
                        return false;
                    }
                    break;
                case int when id is >= 2 and <= 25:
                    // Merchant Account Information of other payment systems (Visa 02-03,
                    // Mastercard 04-05, …): outside the Pix arrangement; preserved verbatim.
                    break;
                case int when id is >= 26 and <= 51:
                    if (!TryWalkMerchantAccount(value, ref pixMerchantAccountSeen, ref staticPixMerchantAccount, out Error maiError))
                    {
                        position = cursor;
                        error = maiError;
                        return false;
                    }
                    break;
                case 52:
                    mccSeen = true;
                    if (length != MccLength || !IsAllDigits(value))
                    {
                        position = cursor;
                        error = Error.MccInvalid;
                        return false;
                    }
                    break;
                case 53:
                    currencySeen = true;
                    if (!value.SequenceEqual("986"))
                    {
                        position = cursor;
                        error = Error.CurrencyInvalid;
                        return false;
                    }
                    break;
                case 54:
                    if (!IsValidAmount(value))
                    {
                        position = cursor;
                        error = Error.AmountInvalid;
                        return false;
                    }
                    break;
                case 55 or 56 or 57 or 61 or 64:
                    // Tip/convenience indicators, postal code and the language template:
                    // valid MPM objects, unused by the Pix arrangement; preserved verbatim.
                    break;
                case 58:
                    countrySeen = true;
                    if (!value.SequenceEqual("BR"))
                    {
                        position = cursor;
                        error = Error.CountryInvalid;
                        return false;
                    }
                    break;
                case 59:
                    nameSeen = true;
                    if (length == 0)
                    {
                        position = cursor;
                        error = Error.MerchantNameMissing;
                        return false;
                    }
                    if (length > PixText.MaxNameLength || !PixText.IsAllowedText(value))
                    {
                        position = cursor;
                        error = Error.MerchantNameInvalid;
                        return false;
                    }
                    break;
                case 60:
                    citySeen = true;
                    if (length == 0)
                    {
                        position = cursor;
                        error = Error.MerchantCityMissing;
                        return false;
                    }
                    if (length > PixText.MaxCityLength || !PixText.IsAllowedText(value))
                    {
                        position = cursor;
                        error = Error.MerchantCityInvalid;
                        return false;
                    }
                    break;
                case 62:
                    if (!TryWalkAdditionalData(value, ref txidSeen, out Error additionalError))
                    {
                        position = cursor;
                        error = additionalError;
                        return false;
                    }
                    break;
                case 63:
                    crcSeen = true;
                    if (length != CrcLength || !IsUppercaseHex(value))
                    {
                        position = cursor;
                        error = Error.CrcInvalidHex;
                        return false;
                    }
                    // CRC-16/CCITT-FALSE over everything up to and including "6304".
                    if (ParseHex16(value) != Crc16.Compute(payload[..(cursor + 4)]))
                    {
                        position = cursor;
                        error = Error.CrcMismatch;
                        return false;
                    }
                    break;
                case int when id is >= 65 and <= 79:
                    // Reserved for future use by EMVCo (QRCPS-MPM section 4.3).
                    position = cursor;
                    error = Error.ReservedId;
                    return false;
                case int when id is >= 80 and <= 99:
                    if (!TryWalkUnreserved(value, out Error unreservedError))
                    {
                        position = cursor;
                        error = unreservedError;
                        return false;
                    }
                    break;
                default:
                    position = cursor;
                    error = Error.InvalidId;
                    return false;
            }
            cursor += 4 + length;
        }

        // Payload-level post-conditions, in a deterministic order so that a given payload
        // always reports the same first failure.
        if (!formatIndicatorSeen)
        {
            error = Error.FormatIndicatorMissing;
            return false;
        }
        if (!pixMerchantAccountSeen)
        {
            error = Error.MerchantAccountMissing;
            return false;
        }
        if (!mccSeen)
        {
            error = Error.MccMissing;
            return false;
        }
        if (!currencySeen)
        {
            error = Error.CurrencyMissing;
            return false;
        }
        if (!countrySeen)
        {
            error = Error.CountryMissing;
            return false;
        }
        if (!nameSeen)
        {
            error = Error.MerchantNameMissing;
            return false;
        }
        if (!citySeen)
        {
            error = Error.MerchantCityMissing;
            return false;
        }
        // Static codes always carry the Reference Label (*** when the txid is not used);
        // dynamic codes take their txid from the payload fetched at the location URL.
        if (staticPixMerchantAccount && !txidSeen)
        {
            error = Error.TxidMissing;
            return false;
        }
        if (!crcSeen)
        {
            error = Error.CrcMissing;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Renders the failure as the message of the <see cref="FormatException"/> thrown by
    /// <c>PixPayload.Parse</c>.
    /// </summary>
    internal static string Describe(Error error, int position, ReadOnlySpan<char> payload)
    {
        string detail = error switch
        {
            Error.Empty => "the payload is empty",
            Error.TruncatedHeader => "it ends in the middle of a TLV header",
            Error.InvalidId => "a data object ID is not exactly two digits",
            Error.InvalidLength => "a data object length is not exactly two digits",
            Error.TruncatedValue => "a declared value is longer than the payload",
            Error.DuplicateId => "the same data object ID appears twice at the same level",
            Error.FormatIndicatorNotFirst => "the payload format indicator (field 00) must be the first data object",
            Error.FormatIndicatorMissing => "the payload format indicator (field 00) is missing",
            Error.FormatIndicatorInvalid => "the payload format indicator (field 00) must be '01'",
            Error.InitiationMethodInvalid => "the point of initiation method (field 01) must be '11' or '12'",
            Error.ReservedId => "the data object ID is reserved for future use (65-79)",
            Error.CrcMissing => "the CRC (field 63) is missing",
            Error.CrcNotLast => "the CRC (field 63) must be the last data object",
            Error.CrcInvalidHex => "the CRC (field 63) must be exactly four uppercase hexadecimal digits",
            Error.CrcMismatch => "the CRC-16/CCITT-FALSE (field 63) does not match the payload",
            Error.MerchantAccountMissing => "no merchant account information template (26-51) carries the 'br.gov.bcb.pix' GUI",
            Error.MerchantAccountGuiMissing => "a merchant account information template (26-51) has no GUI sub-field 00",
            Error.MerchantAccountGuiTooLong => "a GUI sub-field 00 is longer than the 32-character limit",
            Error.MerchantAccountIncomplete => "the Pix merchant account information must carry either a key (sub-field 01) or a payload location URL (sub-field 25)",
            Error.KeyAndLocationConflict => "the Pix merchant account information cannot carry both a key (sub-field 01) and a payload location URL (sub-field 25)",
            Error.KeyInvalid => "the Pix key (sub-field 01) is not a valid CPF, CNPJ, e-mail, +55 phone or UUID",
            Error.LocationInvalid => "the payload location URL (sub-field 25) must be 1..77 RFC 3986 characters without a protocol prefix",
            Error.DescriptionInvalid => "the additional information (sub-field 02) must hold 1..72 characters",
            Error.AdditionalDataInvalid => "an additional data sub-field (field 62) must hold single-byte characters",
            Error.WithdrawalFacilitatorInvalid => "the withdrawal facilitator (sub-field 03, fss) must be exactly 8 digits (the ISPB)",
            Error.MccMissing => "the merchant category code (field 52) is missing",
            Error.MccInvalid => "the merchant category code (field 52) must be exactly 4 digits",
            Error.CurrencyMissing => "the transaction currency (field 53) is missing",
            Error.CurrencyInvalid => "the transaction currency (field 53) must be '986' (BRL)",
            Error.AmountInvalid => "the transaction amount (field 54) must be up to 10 digits with an optional 2-digit decimal part (max 13 characters)",
            Error.CountryMissing => "the country code (field 58) is missing",
            Error.CountryInvalid => "the country code (field 58) must be 'BR'",
            Error.MerchantNameMissing => "the merchant name (field 59) is missing",
            Error.MerchantNameInvalid => "the merchant name (field 59) must hold 1..25 letters, digits and spaces",
            Error.MerchantCityMissing => "the merchant city (field 60) is missing",
            Error.MerchantCityInvalid => "the merchant city (field 60) must hold 1..15 letters, digits and spaces",
            Error.AdditionalDataIdInvalid => "the additional data template (field 62) has an invalid sub-field ID (01-09 and 50-99 only)",
            Error.AdditionalDataTooLong => "an additional data sub-field (field 62) exceeds the 25-character limit",
            Error.TxidInvalid => "the txid (sub-field 05) must hold 1..25 letters or digits, or '***'",
            Error.TxidMissing => "a static payload must carry the Reference Label (field 62 sub-field 05, '***' when unused)",
            Error.UnreservedGuiInvalid => "an unreserved template (80-99) must start with the 'br.gov.bcb.pix' GUI sub-field 00",
            Error.TemplateTrailingData => "a template value is not fully consumed by its child data objects",
            _ => "it is not a valid Pix BR Code",
        };
        string where = position >= 0 ? $" at position {position.ToString(CultureInfo.InvariantCulture)}" : string.Empty;
        return $"'{DocumentText.Preview(payload)}' is not a valid Pix BR Code payload: {detail}{where}.";
    }

    /// <summary>
    /// Walks a merchant account information template (field 26–51): unique sub-fields, full
    /// consumption, and — when the GUI is <c>br.gov.bcb.pix</c> — the Pix arrangement rules for
    /// the key, the payload location URL, the additional information and the withdrawal
    /// facilitator.
    /// </summary>
    private static bool TryWalkMerchantAccount(ReadOnlySpan<char> value, ref bool pixMerchantAccountSeen, ref bool staticPixMerchantAccount, out Error error)
    {
        error = Error.None;
        Span<bool> seen = stackalloc bool[100];
        ReadOnlySpan<char> gui = default;
        bool guiSeen = false;
        ReadOnlySpan<char> key = default;
        bool keySeen = false;
        ReadOnlySpan<char> location = default;
        bool locationSeen = false;
        int cursor = 0;
        while (cursor < value.Length)
        {
            int remaining = value.Length - cursor;
            if (remaining < 4)
            {
                error = Error.TemplateTrailingData;
                return false;
            }
            ReadOnlySpan<char> header = value.Slice(cursor, 4);
            if (!TryReadHeader(header, out int subId, out int subLength))
            {
                error = char.IsAsciiDigit(header[0]) && char.IsAsciiDigit(header[1]) ? Error.InvalidLength : Error.InvalidId;
                return false;
            }
            if (remaining - 4 < subLength)
            {
                error = Error.TruncatedValue;
                return false;
            }
            if (seen[subId])
            {
                error = Error.DuplicateId;
                return false;
            }
            seen[subId] = true;
            ReadOnlySpan<char> subValue = value.Slice(cursor + 4, subLength);
            switch (subId)
            {
                case 0:
                    guiSeen = true;
                    gui = subValue;
                    break;
                case 1:
                    keySeen = true;
                    key = subValue;
                    break;
                case 2:
                    // infoAdicional, "conjunto livre de caracteres", 1..72 (section 2.6, footnote 24).
                    if (subLength is 0 or > MaxDescriptionLength || !PixText.IsAllowedFreeText(subValue))
                    {
                        error = Error.DescriptionInvalid;
                        return false;
                    }
                    break;
                case 3:
                    // fss: the ISPB of the withdrawal service facilitator (Pix Saque), 8 digits.
                    if (subLength != WithdrawalFacilitatorLength || !IsAllDigits(subValue))
                    {
                        error = Error.WithdrawalFacilitatorInvalid;
                        return false;
                    }
                    break;
                case 25:
                    locationSeen = true;
                    location = subValue;
                    break;
                default:
                    // Other sub-fields are defined "conforme PIX" per use case; preserved verbatim.
                    break;
            }
            cursor += 4 + subLength;
        }

        if (!guiSeen || gui.IsEmpty)
        {
            error = Error.MerchantAccountGuiMissing;
            return false;
        }
        if (gui.Length > MaxGuiLength)
        {
            error = Error.MerchantAccountGuiTooLong;
            return false;
        }
        if (!MemoryExtensions.Equals(gui, Gui, StringComparison.OrdinalIgnoreCase))
        {
            // Another arrangement's merchant account template; no Pix semantics apply.
            return true;
        }
        pixMerchantAccountSeen = true;
        if (keySeen && locationSeen)
        {
            error = Error.KeyAndLocationConflict;
            return false;
        }
        if (keySeen)
        {
            staticPixMerchantAccount = true;
            if (!PixKey.IsValid(key))
            {
                error = Error.KeyInvalid;
                return false;
            }
        }
        else if (locationSeen)
        {
            if (!PixLocation.IsValid(location))
            {
                error = Error.LocationInvalid;
                return false;
            }
        }
        else
        {
            error = Error.MerchantAccountIncomplete;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Walks the additional data field template (field 62): unique sub-fields 01–09 and 50–99
    /// of up to 25 characters — 10–49 are reserved for future use — with the txid rules for the
    /// Reference Label (sub-field 05).
    /// </summary>
    private static bool TryWalkAdditionalData(ReadOnlySpan<char> value, ref bool txidSeen, out Error error)
    {
        error = Error.None;
        Span<bool> seen = stackalloc bool[100];
        int cursor = 0;
        while (cursor < value.Length)
        {
            int remaining = value.Length - cursor;
            if (remaining < 4)
            {
                error = Error.TemplateTrailingData;
                return false;
            }
            ReadOnlySpan<char> header = value.Slice(cursor, 4);
            if (!TryReadHeader(header, out int subId, out int subLength))
            {
                error = char.IsAsciiDigit(header[0]) && char.IsAsciiDigit(header[1]) ? Error.InvalidLength : Error.InvalidId;
                return false;
            }
            if (remaining - 4 < subLength)
            {
                error = Error.TruncatedValue;
                return false;
            }
            if (seen[subId])
            {
                error = Error.DuplicateId;
                return false;
            }
            seen[subId] = true;
            ReadOnlySpan<char> subValue = value.Slice(cursor + 4, subLength);
            if (subId is 0 or (>= 10 and <= 49))
            {
                error = Error.AdditionalDataIdInvalid;
                return false;
            }
            if (subValue.Length > AdditionalDataMaxLength)
            {
                error = Error.AdditionalDataTooLong;
                return false;
            }
            if (!PixText.IsAllowedFreeText(subValue))
            {
                error = Error.AdditionalDataInvalid;
                return false;
            }
            if (subId == 5)
            {
                txidSeen = true;
                if (!IsValidTxid(subValue))
                {
                    error = Error.TxidInvalid;
                    return false;
                }
            }
            cursor += 4 + subLength;
        }
        return true;
    }

    /// <summary>
    /// Walks an unreserved template (field 80–99, the recurrence parameters of QR codes
    /// compostos): unique sub-fields, full consumption and — when not empty — the
    /// <c>br.gov.bcb.pix</c> GUI sub-field 00.
    /// </summary>
    private static bool TryWalkUnreserved(ReadOnlySpan<char> value, out Error error)
    {
        error = Error.None;
        Span<bool> seen = stackalloc bool[100];
        bool guiSeen = false;
        ReadOnlySpan<char> gui = default;
        int cursor = 0;
        while (cursor < value.Length)
        {
            int remaining = value.Length - cursor;
            if (remaining < 4)
            {
                error = Error.TemplateTrailingData;
                return false;
            }
            ReadOnlySpan<char> header = value.Slice(cursor, 4);
            if (!TryReadHeader(header, out int subId, out int subLength))
            {
                error = char.IsAsciiDigit(header[0]) && char.IsAsciiDigit(header[1]) ? Error.InvalidLength : Error.InvalidId;
                return false;
            }
            if (remaining - 4 < subLength)
            {
                error = Error.TruncatedValue;
                return false;
            }
            if (seen[subId])
            {
                error = Error.DuplicateId;
                return false;
            }
            seen[subId] = true;
            if (subId == 0)
            {
                guiSeen = true;
                gui = value.Slice(cursor + 4, subLength);
            }
            cursor += 4 + subLength;
        }
        // A non-empty unreserved template of the Pix arrangement always starts with the GUI
        // (Manual section 2.5: the 80-99 range carries "00 (GUI) obrigatório").
        if (!value.IsEmpty && (!guiSeen || !MemoryExtensions.Equals(gui, Gui, StringComparison.OrdinalIgnoreCase) || gui.Length > MaxGuiLength))
        {
            error = Error.UnreservedGuiInvalid;
            return false;
        }
        return true;
    }

    /// <summary>Reads a TLV header: two ID digits and two length digits.</summary>
    private static bool TryReadHeader(ReadOnlySpan<char> header, out int id, out int length)
    {
        id = -1;
        length = -1;
        if (!char.IsAsciiDigit(header[0]) || !char.IsAsciiDigit(header[1])
            || !char.IsAsciiDigit(header[2]) || !char.IsAsciiDigit(header[3]))
        {
            return false;
        }
        id = (header[0] - '0') * 10 + (header[1] - '0');
        length = (header[2] - '0') * 10 + (header[3] - '0');
        return true;
    }

    /// <summary>
    /// Validates a transaction amount (field 54): 1..10 digits with an optional "." and 1..2
    /// decimal digits — at most 13 characters (EMV QRCPS-MPM section 4.3).
    /// </summary>
    private static bool IsValidAmount(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || value.Length > AmountMaxLength)
        {
            return false;
        }
        int dot = value.IndexOf('.');
        if (dot < 0)
        {
            return IsAllDigits(value);
        }
        if (dot is 0 or > 10)
        {
            return false;
        }
        ReadOnlySpan<char> fraction = value[(dot + 1)..];
        if (fraction.IsEmpty || fraction.Length > 2 || !IsAllDigits(fraction))
        {
            return false;
        }
        return IsAllDigits(value[..dot]);
    }

    /// <summary>
    /// Validates a txid (field 62 sub-field 05): "***" — the Reference Label for "no txid",
    /// per EMV QRCPS-MPM section 4.8.1.2 — or 1..25 letters/digits (Manual section 2.6.2).
    /// </summary>
    private static bool IsValidTxid(ReadOnlySpan<char> value)
    {
        if (value.SequenceEqual("***"))
        {
            return true;
        }
        if (value.IsEmpty || value.Length > AdditionalDataMaxLength)
        {
            return false;
        }
        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsAllDigits(ReadOnlySpan<char> value)
    {
        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsUppercaseHex(ReadOnlySpan<char> value)
    {
        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c) && c is not ('A' or 'B' or 'C' or 'D' or 'E' or 'F'))
            {
                return false;
            }
        }
        return true;
    }

    private static ushort ParseHex16(ReadOnlySpan<char> value)
    {
        int result = 0;
        foreach (char c in value)
        {
            result = (result << 4) | (char.IsAsciiDigit(c) ? c - '0' : c - 'A' + 10);
        }
        return (ushort)result;
    }
}
