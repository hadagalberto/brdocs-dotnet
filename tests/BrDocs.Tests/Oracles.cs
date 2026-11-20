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
