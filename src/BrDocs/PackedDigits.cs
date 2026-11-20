namespace BrDocs;

/// <summary>
/// Nibble packing of the 44 digits of a boleto/arrecadação código de barras into three
/// <see cref="ulong"/> values (4 bits per decimal digit), so the value objects keep their
/// digits without any string field and stay allocation-free on every hot path.
/// </summary>
/// <remarks>
/// Digits 0–15 live in the low <see cref="ulong"/>, digits 16–31 in the middle one and digits
/// 32–43 in the high one; within each value, digit <c>i</c> of the range occupies nibble
/// <c>i</c> (shift <c>4 × i</c>).
/// </remarks>
internal static class PackedDigits
{
    internal const int DigitsCount = 44;
    private const int LoCount = 16;    // digits 0..15
    private const int MidCount = 16;   // digits 16..31
    private const int HiCount = 12;    // digits 32..43

    /// <summary>Packs the 44 decimal digits <paramref name="digits"/> into three <see cref="ulong"/> values.</summary>
    /// <param name="digits">Exactly 44 ASCII digit characters.</param>
    /// <param name="lo">Receives digits 0–15.</param>
    /// <param name="mid">Receives digits 16–31.</param>
    /// <param name="hi">Receives digits 32–43.</param>
    internal static void Pack(ReadOnlySpan<char> digits, out ulong lo, out ulong mid, out ulong hi)
    {
        lo = PackRange(digits, 0, LoCount);
        mid = PackRange(digits, LoCount, MidCount);
        hi = PackRange(digits, LoCount + MidCount, HiCount);
    }

    /// <summary>Unpacks the 44 digits back into <paramref name="destination"/> as ASCII digit characters.</summary>
    /// <param name="lo">Digits 0–15.</param>
    /// <param name="mid">Digits 16–31.</param>
    /// <param name="hi">Digits 32–43.</param>
    /// <param name="destination">A span of at least 44 characters.</param>
    internal static void Unpack(ulong lo, ulong mid, ulong hi, Span<char> destination)
    {
        WriteRange(lo, destination, 0, LoCount);
        WriteRange(mid, destination, LoCount, MidCount);
        WriteRange(hi, destination, LoCount + MidCount, HiCount);
    }

    /// <summary>Returns the decimal value (0–9) of the digit at <paramref name="index"/> (0–43).</summary>
    internal static int Digit(ulong lo, ulong mid, ulong hi, int index)
    {
        return index switch
        {
            < LoCount => (int)((lo >> (index * 4)) & 0xFul),
            < LoCount + MidCount => (int)((mid >> ((index - LoCount) * 4)) & 0xFul),
            _ => (int)((hi >> ((index - LoCount - MidCount) * 4)) & 0xFul),
        };
    }

    /// <summary>
    /// Writes <paramref name="value"/> into <paramref name="destination"/> as zero-padded decimal
    /// digits (least significant digit last), as the boleto layouts require.
    /// </summary>
    /// <param name="value">The value to write; must be non-negative and fit in <paramref name="length"/> digits.</param>
    /// <param name="length">The exact number of digits to write.</param>
    /// <param name="destination">A span of at least <paramref name="length"/> characters.</param>
    internal static void WritePadded(long value, int length, Span<char> destination)
    {
        for (int i = length - 1; i >= 0; i--)
        {
            destination[i] = (char)('0' + value % 10);
            value /= 10;
        }
    }

    private static ulong PackRange(ReadOnlySpan<char> digits, int start, int count)
    {
        ulong packed = 0;
        for (int i = start + count - 1; i >= start; i--)
        {
            packed = (packed << 4) | (uint)(digits[i] - '0');
        }
        return packed;
    }

    private static void WriteRange(ulong packed, Span<char> destination, int start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            destination[start + i] = (char)('0' + (int)((packed >> (i * 4)) & 0xFul));
        }
    }
}
