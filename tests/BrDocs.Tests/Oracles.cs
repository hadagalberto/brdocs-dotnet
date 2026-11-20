using System.Globalization;
using System.Text;

namespace BrDocs.Tests;

/// <summary>
/// Independent, string-based reference implementations used as oracles by the property-based
/// tests, plus deterministic fuzz helpers. They follow the same official rules as the library
/// but are implemented deliberately differently (StringBuilder/LINQ instead of spans).
/// </summary>
internal static class Oracles
{
    internal const string DigitsAlphabet = "0123456789";

    internal const string UpperAlphanumericAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal const string LowerAlphanumericAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    internal const string LettersAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal const string PlusDigitsAlphabet = "0123456789+";

    internal const string GarbageAlphabet = "0123456789.-/ ABCabc_.,;()Áé漢字١٢";

    internal static bool IsValidCpf(string? text)
    {
        if (!TryNormalizeDigits(text, 11, out string digits))
        {
            return false;
        }
        int[] d = digits.Select(c => c - '0').ToArray();
        if (d.Distinct().Count() == 1)
        {
            return false;
        }
        return CpfDv(d, 9, 10) == d[9] && CpfDv(d, 10, 11) == d[10];
    }

    internal static bool IsValidCnpj(string? text)
    {
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        foreach (char c in text)
        {
            if (c is '.' or '-' or '/' or ' ')
            {
                continue;
            }
            builder.Append(c is >= 'a' and <= 'z' ? char.ToUpperInvariant(c) : c);
        }
        string s = builder.ToString();
        if (s.Length != 14)
        {
            return false;
        }
        if (s.Any(c => c is (< '0' or > '9') and (< 'A' or > 'Z')))
        {
            return false;
        }
        if (s[12] is < '0' or > '9' || s[13] is < '0' or > '9')
        {
            return false;
        }
        if (s.All(c => c <= '9') && s.Distinct().Count() == 1)
        {
            return false;
        }
        int[] v = s.Select(c => c - 48).ToArray();
        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        if (Mod11(v, w1, 12) != v[12])
        {
            return false;
        }
        return Mod11(v, w2, 13) == v[13];
    }

    internal static bool IsValidCep(string? text) => TryNormalizeDigits(text, 8, out _);

    internal static bool IsValidPis(string? text)
    {
        if (!TryNormalizeDigits(text, 11, out string digits))
        {
            return false;
        }
        int[] d = digits.Select(c => c - '0').ToArray();
        if (d.Distinct().Count() == 1)
        {
            return false;
        }
        int[] weights = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = d.Take(10).Select((digit, i) => digit * weights[i]).Sum();
        int remainder = sum % 11;
        return (remainder < 2 ? 0 : 11 - remainder) == d[10];
    }

    internal static bool IsValidTituloEleitor(string? text)
    {
        if (!TryNormalizeDigits(text, 12, out string digits))
        {
            return false;
        }
        int ufCode = (digits[8] - '0') * 10 + (digits[9] - '0');
        if (ufCode is < 1 or > 28)
        {
            return false;
        }
        if (TituloDv(TituloSum(digits, 8) % 11, ufCode) != digits[10] - '0')
        {
            return false;
        }
        int sumDv2 = (digits[8] - '0') * 7 + (digits[9] - '0') * 8 + (digits[10] - '0') * 9;
        return TituloDv(sumDv2 % 11, ufCode) == digits[11] - '0';
    }

    /// <summary>The título DV rule: the remainder itself, 10 → 0, and 0 → 1 for SP (01) and MG (02).</summary>
    internal static int TituloDv(int remainder, int ufCode) => remainder switch
    {
        10 => 0,
        0 => ufCode is 1 or 2 ? 1 : 0,
        _ => remainder,
    };

    /// <summary>Σ digit[i] × (2 + i) over the first <paramref name="count"/> chars (título DV1 weights 2..9).</summary>
    private static int TituloSum(string digits, int count)
        => Enumerable.Range(0, count).Sum(i => (digits[i] - '0') * (2 + i));

    internal static bool IsValidCnh(string? text)
    {
        if (!TryNormalizeDigits(text, 11, out string digits))
        {
            return false;
        }
        int[] d = digits.Select(c => c - '0').ToArray();
        if (d.Distinct().Count() == 1)
        {
            return false;
        }
        int sum1 = d.Take(9).Select((digit, i) => digit * (9 - i)).Sum();
        int remainder1 = sum1 % 11;
        int dv1 = remainder1 > 9 ? 0 : remainder1;
        int decrement = remainder1 > 9 ? 2 : 0;
        int sum2 = d.Take(9).Select((digit, i) => digit * (i + 1)).Sum();
        int adjusted = sum2 % 11 - decrement;
        if (adjusted < 0)
        {
            adjusted += 11;
        }
        return dv1 == d[9] && (adjusted > 9 ? 0 : adjusted) == d[10];
    }

    internal static bool IsValidRenavam(string? text)
    {
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        foreach (char c in text)
        {
            if (c is not ('.' or '-' or '/' or ' '))
            {
                builder.Append(c);
            }
        }
        string s = builder.ToString();
        if (s.Any(c => c is < '0' or > '9'))
        {
            return false;
        }
        if (s.Length == 9)
        {
            s = s.PadLeft(11, '0');
        }
        if (s.Length != 11 || s.Distinct().Count() == 1)
        {
            return false;
        }
        int[] weights = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = s.Take(10).Select((c, i) => (c - '0') * weights[i]).Sum();
        int remainder = sum % 11;
        return (remainder < 2 ? 0 : 11 - remainder) == s[10] - '0';
    }

    internal static bool IsValidPlaca(string? text)
    {
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        foreach (char c in text)
        {
            if (c is '.' or '-' or '/' or ' ')
            {
                continue;
            }
            builder.Append(c is >= 'a' and <= 'z' ? char.ToUpperInvariant(c) : c);
        }
        string s = builder.ToString();
        if (s.Length != 7 || s.Any(c => c is (< '0' or > '9') and (< 'A' or > 'Z')))
        {
            return false;
        }
        return char.IsBetween(s[0], 'A', 'Z')
            && char.IsBetween(s[1], 'A', 'Z')
            && char.IsBetween(s[2], 'A', 'Z')
            && char.IsAsciiDigit(s[3])
            && (char.IsAsciiDigit(s[4]) || char.IsBetween(s[4], 'A', 'Z'))
            && char.IsAsciiDigit(s[5])
            && char.IsAsciiDigit(s[6]);
    }

    // The 67 ANATEL Códigos Nacionais, kept deliberately independent from AnatelDddTable.
    internal static readonly int[] ValidDdds =
    {
        11, 12, 13, 14, 15, 16, 17, 18, 19,
        21, 22, 24, 27, 28,
        31, 32, 33, 34, 35, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48, 49,
        51, 53, 54, 55,
        61, 62, 63, 64, 65, 66, 67, 68, 69,
        71, 73, 74, 75, 77, 79,
        81, 82, 83, 84, 85, 86, 87, 88, 89,
        91, 92, 93, 94, 95, 96, 97, 98, 99,
    };

    internal static bool IsValidTelefoneBr(string? text)
    {
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        bool plus = false;
        foreach (char c in text)
        {
            if (c is '.' or '-' or '/' or ' ' or '(' or ')')
            {
                continue;
            }
            if (c == '+')
            {
                if (plus || builder.Length != 0)
                {
                    return false;
                }
                plus = true;
                continue;
            }
            if (c is < '0' or > '9')
            {
                return false;
            }
            builder.Append(c);
        }
        string digits = builder.ToString();
        if (digits.Length is not (13 or 12 or 11 or 10))
        {
            return false;
        }
        int offset = digits.Length >= 12 ? 2 : 0;
        if (offset != 0 && !digits.StartsWith("55", StringComparison.Ordinal))
        {
            return false;
        }
        if (plus && offset == 0)
        {
            return false;
        }
        string national = digits[offset..];
        int ddd = int.Parse(national[..2], CultureInfo.InvariantCulture);
        if (!ValidDdds.Contains(ddd))
        {
            return false;
        }
        if (national.Length == 11)
        {
            return national[2] == '9';
        }
        return national[2] is >= '2' and <= '6';
    }

    // ---- Boleto bancário (FEBRABAN cobrança) -------------------------------

    /// <summary>
    /// Oracle for <see cref="Boleto"/>: the 44-digit código de barras or the 47-digit linha
    /// digitável, with the field DVs (módulo 10), the general DV (módulo 11 with the
    /// "0, 1, 10, 11 → 1" rule) and the barcode reassembly implemented with plain strings.
    /// </summary>
    internal static bool IsValidBoleto(string? text)
    {
        if (!TryNormalizeFEBRABAN(text, out string s) || s.Length is not (44 or 47))
        {
            return false;
        }
        return s.Length == 44 ? BankBarcodeIsValid(s) : BankLinhaIsValid(s);
    }

    private static bool BankBarcodeIsValid(string barcode)
        => BankDvOracle(barcode[..4] + barcode[5..]) == barcode[4] - '0';

    private static bool BankLinhaIsValid(string linha)
    {
        if (Mod10Oracle(linha[..9]) != linha[9] - '0'
            || Mod10Oracle(linha.Substring(10, 10)) != linha[20] - '0'
            || Mod10Oracle(linha.Substring(21, 10)) != linha[31] - '0')
        {
            return false;
        }
        string barcode = linha[..4] + linha[32]
            + linha.Substring(33, 14)
            + linha.Substring(4, 5)
            + linha.Substring(10, 10)
            + linha.Substring(21, 10);
        return BankDvOracle(barcode[..4] + barcode[5..]) == barcode[4] - '0';
    }

    /// <summary>
    /// Oracle for <see cref="Boleto.Encode(DateOnly)"/>: days since 1997-10-07, restarting at
    /// 1000 on 2025-02-22 and repeating every 9000 factors.
    /// </summary>
    internal static int EncodeFator(DateOnly dueDate)
    {
        var resetDate = new DateOnly(2025, 2, 22);
        if (dueDate < resetDate)
        {
            return dueDate.DayNumber - new DateOnly(1997, 10, 7).DayNumber;
        }
        return 1000 + ((dueDate.DayNumber - resetDate.DayNumber) % 9000);
    }

    /// <summary>
    /// Oracle for <see cref="Boleto.TryDecode(int, DateOnly, out DateOnly)"/>: enumerates the
    /// candidate date of every cycle, keeps the ones inside the [-3000, +5500] day window and
    /// picks the closest to the reference.
    /// </summary>
    internal static bool TryDecodeFator(int factor, DateOnly reference, out DateOnly dueDate)
    {
        dueDate = default;
        if (factor is < 1 or > 9999)
        {
            return false;
        }
        var candidates = new List<DateOnly> { DateOnly.FromDayNumber(new DateOnly(1997, 10, 7).DayNumber + factor) };
        if (factor >= 1000)
        {
            int cycleStart = new DateOnly(2025, 2, 22).DayNumber - 1000;
            for (int cycle = 0; ; cycle++)
            {
                int day = cycleStart + 9000 * cycle + factor;
                if (day > reference.DayNumber + 5500)
                {
                    break;
                }
                candidates.Add(DateOnly.FromDayNumber(day));
            }
        }
        DateOnly? best = candidates
            .Where(d => d.DayNumber >= reference.DayNumber - 3000 && d.DayNumber <= reference.DayNumber + 5500)
            .OrderBy(d => Math.Abs(d.DayNumber - reference.DayNumber))
            .Cast<DateOnly?>()
            .FirstOrDefault();
        if (best is null)
        {
            return false;
        }
        dueDate = best.Value;
        return true;
    }

    // ---- Arrecadação / convênio ---------------------------------------------

    /// <summary>
    /// Oracle for <see cref="Arrecadacao"/>: the 44-digit código de barras or the 48-digit
    /// linha digitável of four 11-digit blocks, with per-block DVs and the general DV selected
    /// by the third digit (6/7 → módulo 10, 8/9 → módulo 11 with the "0/1 → 0, 10 → 1" rule).
    /// </summary>
    internal static bool IsValidArrecadacao(string? text)
    {
        if (!TryNormalizeFEBRABAN(text, out string s) || s.Length is not (44 or 48))
        {
            return false;
        }
        if (s[0] != '8' || s[2] is < '6' or > '9')
        {
            return false;
        }
        bool mod11 = s[2] is '8' or '9';
        string barcode;
        if (s.Length == 44)
        {
            barcode = s;
        }
        else
        {
            var blocks = new List<string>();
            for (int block = 0; block < 4; block++)
            {
                string digits = s.Substring(block * 12, 11);
                int expected = mod11 ? ArrDvOracle(digits) : Mod10Oracle(digits);
                if (expected != s[block * 12 + 11] - '0')
                {
                    return false;
                }
                blocks.Add(digits);
            }
            barcode = string.Concat(blocks[0], blocks[1], blocks[2], blocks[3]);
        }
        string dv43 = barcode[..3] + barcode[4..];
        int general = mod11 ? ArrDvOracle(dv43) : Mod10Oracle(dv43);
        return general == barcode[3] - '0';
    }

    /// <summary>Módulo 10, computed left-to-right with a distance-derived weight.</summary>
    private static int Mod10Oracle(string digits)
    {
        int sum = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            int product = (digits[i] - '0') * ((digits.Length - i) % 2 == 1 ? 2 : 1);
            sum += (product / 10) + (product % 10);
        }
        return (10 - sum % 10) % 10;
    }

    /// <summary>Módulo 11 remainder, computed left-to-right with a LINQ-weighted sum.</summary>
    private static int Mod11RemainderOracle(string digits)
        => digits.Select((c, i) => (c - '0') * (((digits.Length - 1 - i) % 8) + 2)).Sum() % 11;

    private static int BankDvOracle(string digits)
    {
        int dv = 11 - Mod11RemainderOracle(digits);
        return dv > 9 ? 1 : dv;
    }

    private static int ArrDvOracle(string digits)
    {
        int remainder = Mod11RemainderOracle(digits);
        return remainder <= 1 ? 0 : 11 - remainder;
    }

    /// <summary>Strips the ignored punctuation and requires ASCII digits only.</summary>
    private static bool TryNormalizeFEBRABAN(string? text, out string digits)
    {
        digits = string.Empty;
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        foreach (char c in text)
        {
            if (c is '.' or '-' or '/' or ' ')
            {
                continue;
            }
            if (c is < '0' or > '9')
            {
                return false;
            }
            builder.Append(c);
        }
        digits = builder.ToString();
        return true;
    }

    /// <summary>Deterministically derives a string of <paramref name="length"/> chars from <paramref name="seed"/>.</summary>
    internal static string RandomString(int seed, int length, string alphabet)
    {
        uint state = (uint)seed | 1u;
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            state = state * 1664525u + 1013904223u;
            chars[i] = alphabet[(int)(state % (uint)alphabet.Length)];
        }
        return new string(chars);
    }

    /// <summary>Scatters the ignored punctuation ( '.', '-', '/', ' ' ) around the characters of <paramref name="core"/>.</summary>
    internal static string ScatterIgnored(int seed, string core)
    {
        uint state = (uint)seed | 1u;
        var builder = new StringBuilder();
        foreach (char c in core)
        {
            state = state * 1664525u + 1013904223u;
            if (state % 3u == 0u)
            {
                builder.Append(" .-/"[(int)((state >> 16) % 4u)]);
            }
            builder.Append(c);
        }
        state = state * 1664525u + 1013904223u;
        if (state % 2u == 0u)
        {
            builder.Append(' ');
        }
        return builder.ToString();
    }

    private static int CpfDv(int[] digits, int count, int startWeight)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += digits[i] * (startWeight - i);
        }
        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int Mod11(int[] values, int[] weights, int count)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += values[i] * weights[i];
        }
        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool TryNormalizeDigits(string? text, int expectedLength, out string digits)
    {
        digits = string.Empty;
        if (text is null)
        {
            return false;
        }
        var builder = new StringBuilder();
        foreach (char c in text)
        {
            if (c is '.' or '-' or '/' or ' ')
            {
                continue;
            }
            if (c is < '0' or > '9')
            {
                return false;
            }
            builder.Append(c);
        }
        digits = builder.ToString();
        return digits.Length == expectedLength;
    }

    // ---- Pix BR Code ---------------------------------------------------------

    /// <summary>
    /// CRC-16/CCITT-FALSE via a precomputed 256-entry table — an implementation deliberately
    /// different from the library's bit-by-bit shift (polynomial 0x1021, init 0xFFFF, no
    /// reflection, no final XOR, per the BCB manual section 2.6.3).
    /// </summary>
    internal static string Crc16Oracle(string text)
    {
        ushort[] table = new ushort[256];
        for (int value = 0; value < 256; value++)
        {
            ushort current = (ushort)(value << 8);
            for (int bit = 0; bit < 8; bit++)
            {
                current = (current & 0x8000) != 0 ? (ushort)((current << 1) ^ 0x1021) : (ushort)(current << 1);
            }
            table[value] = current;
        }
        ushort crc = 0xFFFF;
        foreach (char c in text)
        {
            crc = (ushort)((crc << 8) ^ table[(crc >> 8) ^ (c & 0xFF)]);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Recomputes the CRC of a payload that already carries one: the last four characters are
    /// replaced by the checksum of everything up to and including "6304".
    /// </summary>
    internal static string FixCrc(string payload) => payload[..^4] + Crc16Oracle(payload[..^4]);

    /// <summary>Encodes one TLV: ID + two-digit length + value (Substring/LINQ style).</summary>
    internal static string Tlv(string id, string value)
        => id + value.Length.ToString("00", CultureInfo.InvariantCulture) + value;

    /// <summary>An independent TLV tree node, for comparing against <see cref="PixTlv"/>.</summary>
    internal sealed record TlvNode(string Id, string Value, List<TlvNode>? Children);

    /// <summary>
    /// Parses a TLV stream with the same structural rules as the library but implemented with
    /// Substring/LINQ: unique IDs per level, full consumption, and templates (26-51, 62,
    /// 80-99) decomposed into leaf children.
    /// </summary>
    internal static List<TlvNode>? TryParseTlvStreamOracle(string text)
    {
        List<TlvNode>? nodes = ParseLevelOracle(text, allowTemplates: true);
        return nodes is { Count: > 0 } ? nodes : null;
    }

    private static List<TlvNode>? ParseLevelOracle(string text, bool allowTemplates)
    {
        List<TlvNode> nodes = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        int position = 0;
        while (position < text.Length)
        {
            if (position + 4 > text.Length || !text.Substring(position, 2).All(char.IsDigit)
                || !text.Substring(position + 2, 2).All(char.IsDigit))
            {
                return null;
            }
            string id = text.Substring(position, 2);
            int length = int.Parse(text.AsSpan(position + 2, 2), CultureInfo.InvariantCulture);
            if (position + 4 + length > text.Length || !seen.Add(id))
            {
                return null;
            }
            string value = text.Substring(position + 4, length);
            int numericId = int.Parse(id, CultureInfo.InvariantCulture);
            bool isTemplate = numericId is >= 26 and <= 51 or 62 or (>= 80 and <= 99);
            if (isTemplate && allowTemplates)
            {
                List<TlvNode>? children = ParseLevelOracle(value, allowTemplates: false);
                if (children is null)
                {
                    return null;
                }
                nodes.Add(new TlvNode(id, string.Empty, children));
            }
            else
            {
                nodes.Add(new TlvNode(id, value, null));
            }
            position += 4 + length;
        }
        return nodes;
    }

    /// <summary>Compares an oracle TLV tree with a <see cref="PixTlv"/> tree, recursively.</summary>
    internal static bool TlvTreesEqual(List<TlvNode>? oracle, IReadOnlyList<PixTlv>? actual)
    {
        if (oracle is null || actual is null)
        {
            return oracle is null && actual is null;
        }
        if (oracle.Count != actual.Count)
        {
            return false;
        }
        for (int i = 0; i < oracle.Count; i++)
        {
            TlvNode expected = oracle[i];
            PixTlv node = actual[i];
            if (expected.Id != node.Id || expected.Value != node.Value)
            {
                return false;
            }
            if (expected.Children is null)
            {
                if (node.Children is not null)
                {
                    return false;
                }
            }
            else if (!TlvTreesEqual(expected.Children, node.Children))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Builds a complete static Pix BR Code independently of the library: fields in ascending
    /// order with the CRC computed by the oracle table.
    /// </summary>
    internal static string BuildStaticPayloadOracle(
        string key, string name, string city, string txid, string mcc, string? amountText,
        string? description, (string Id, string Value)? withdrawalFacilitator,
        IReadOnlyList<(string Id, string Value)>? additional)
    {
        string merchantAccount = Tlv("00", "br.gov.bcb.pix") + Tlv("01", key);
        if (description is not null)
        {
            merchantAccount += Tlv("02", description);
        }
        if (withdrawalFacilitator is { } fss)
        {
            merchantAccount += Tlv(fss.Id, fss.Value);
        }
        string additionalData = Tlv("05", txid);
        if (additional is not null)
        {
            foreach ((string id, string value) in additional)
            {
                additionalData += Tlv(id, value);
            }
        }
        string payload = Tlv("00", "01") + Tlv("26", merchantAccount) + Tlv("52", mcc)
            + Tlv("53", "986")
            + (amountText is null ? string.Empty : Tlv("54", amountText))
            + Tlv("58", "BR") + Tlv("59", name) + Tlv("60", city) + Tlv("62", additionalData)
            + "6304";
        return payload + Crc16Oracle(payload);
    }

    /// <summary>Builds a complete dynamic Pix BR Code independently of the library.</summary>
    internal static string BuildDynamicPayloadOracle(
        string location, string name, string city, bool singleUse,
        IReadOnlyList<(string Id, string Value)>? additional)
    {
        string merchantAccount = Tlv("00", "br.gov.bcb.pix") + Tlv("25", location);
        string additionalData = Tlv("05", "***");
        if (additional is not null)
        {
            foreach ((string id, string value) in additional)
            {
                additionalData += Tlv(id, value);
            }
        }
        string payload = Tlv("00", "01") + (singleUse ? Tlv("01", "12") : string.Empty)
            + Tlv("26", merchantAccount) + Tlv("52", "0000") + Tlv("53", "986")
            + Tlv("58", "BR") + Tlv("59", name) + Tlv("60", city) + Tlv("62", additionalData)
            + "6304";
        return payload + Crc16Oracle(payload);
    }

    /// <summary>Deterministically applies one small mutation to <paramref name="text"/>: a replaced, inserted or deleted character.</summary>
    internal static string Mutate(int seed, string text)
    {
        Random random = new(seed);
        if (text.Length == 0)
        {
            return "0".ToString(CultureInfo.InvariantCulture);
        }
        int position = random.Next(text.Length + 1);
        int kind = random.Next(3);
        if (kind == 0)
        {
            // Replace one character with a random payload-ish character.
            string alphabet = "0123456789ABCDEFBRbr.***govcbpix";
            char replacement = alphabet[random.Next(alphabet.Length)];
            return text[..position] + replacement + text[(position + 1)..];
        }
        if (kind == 1 && position < text.Length)
        {
            return text.Remove(position, 1);
        }
        return text.Insert(position, ((char)('0' + random.Next(10))).ToString());
    }

    /// <summary>
    /// An independent Pix key validity check: canonical shapes only, reusing the CPF/CNPJ
    /// oracles above and reimplementing the e-mail and UUID rules. Phone keys are checked only
    /// for their shape ("+55" + DDD + 9-digit mobile): the DDD itself must be cross-checked
    /// through <see cref="TelefoneBr"/>, so agreement properties generate phones from
    /// <see cref="TelefoneBr.Generate"/> instead of arbitrary digits.
    /// </summary>
    internal static bool IsValidPixKeyOracle(string key)
    {
        if (key.Length is 0 or > 77)
        {
            return false;
        }
        if (key.StartsWith('+'))
        {
            // "+55" + DDD + 9-digit mobile; the DDD must be one of the ANATEL codes 11..99.
            if (key.Length != 14 || !key[1..].All(char.IsDigit) || !key.StartsWith("+55", StringComparison.Ordinal))
            {
                return false;
            }
            int ddd = int.Parse(key.AsSpan(3, 2), CultureInfo.InvariantCulture);
            return ddd is >= 11 and <= 99 && key[5] == '9';
        }
        if (key.Count(c => c == '@') == 1)
        {
            string local = key.Split('@')[0];
            string domain = key.Split('@')[1];
            bool localOk = local.Length is > 0 and <= 64
                && !local.StartsWith('.') && !local.EndsWith('.')
                && !local.Contains("..", StringComparison.Ordinal)
                && local.All(c => char.IsLetterOrDigit(c) && c < 128 || "._%+-!#$&'*/=?^`{|}~".Contains(c));
            bool domainOk = domain.Length > 0
                && domain.Contains('.', StringComparison.Ordinal) && !domain.StartsWith('.')
                && !domain.StartsWith('-') && !domain.EndsWith('.')
                && !domain.EndsWith('-') && !domain.Contains("..", StringComparison.Ordinal)
                && domain.Split('.')
                    .All(label => label.Length is > 0 and <= 63 && !label.StartsWith('-')
                        && !label.EndsWith('-') && label.All(c => char.IsLetterOrDigit(c) || c == '-'))
                && domain.Split('.').Last().Length >= 2;
            return localOk && domainOk;
        }
        if (key.Length == 36)
        {
            string[] groups = key.Split('-');
            return groups.Length == 5
                && groups[0].Length == 8 && groups[1].Length == 4 && groups[2].Length == 4
                && groups[3].Length == 4 && groups[4].Length == 12
                && key.Replace("-", string.Empty).ToLowerInvariant().All(Uri.IsHexDigit);
        }
        if (key.Length == 11 && key.All(char.IsDigit))
        {
            return IsValidCpf(key);
        }
        if (key.Length == 14 && key.All(c => c is (>= '0' and <= '9') or (>= 'A' and <= 'Z')))
        {
            return IsValidCnpj(key);
        }
        return false;
    }
}
