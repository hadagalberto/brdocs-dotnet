namespace BrDocs;

/// <summary>
/// CRC-16/CCITT-FALSE — the checksum of the BR Code field 63: polynomial 0x1021, initial value
/// 0xFFFF, no reflection and no final XOR, written as four uppercase hexadecimal digits and
/// computed over every character of the payload up to and including the "6304" header.
/// </summary>
/// <remarks>
/// Source: Banco Central do Brasil, "Manual de Padrões para Iniciação do Pix", section 2.6.3
/// ("Polinômio 0x1021, C.I. 0xFFFF", with the checksum covering the payload including "6304"),
/// following the EMV® QRCPS-MPM annex.
/// </remarks>
internal static class Crc16
{
    /// <summary>
    /// Computes the CRC-16/CCITT-FALSE of the given characters, allocation-free.
    /// </summary>
    /// <remarks>
    /// Each character contributes its low byte. For the ASCII payloads that the BR Code
    /// mandates (the EMV QRCPS-MPM common character set) this is exactly the byte-wise checksum
    /// of the QR code content; characters above U+00FF are outside the specification and only
    /// their low byte participates, which keeps the result deterministic for any input.
    /// </remarks>
    internal static ushort Compute(ReadOnlySpan<char> value)
    {
        ushort crc = 0xFFFF;
        foreach (char c in value)
        {
            crc ^= (ushort)((c & 0xFF) << 8);
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }
        return crc;
    }

    /// <summary>Writes <paramref name="crc"/> as four uppercase hexadecimal digits, e.g. "1D3D".</summary>
    /// <remarks>Must not be called with a destination shorter than 4 characters.</remarks>
    internal static void WriteHex(Span<char> destination, ushort crc)
    {
        destination[0] = HexDigit(crc >> 12);
        destination[1] = HexDigit((crc >> 8) & 0xF);
        destination[2] = HexDigit((crc >> 4) & 0xF);
        destination[3] = HexDigit(crc & 0xF);
    }

    private static char HexDigit(int value) => (char)(value < 10 ? '0' + value : 'A' + value - 10);
}
