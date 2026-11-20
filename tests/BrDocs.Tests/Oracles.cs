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
}
