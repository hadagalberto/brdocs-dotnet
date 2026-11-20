namespace BrDocs.QrCode;

/// <summary>
/// ISO/IEC 18004 tables and formulas. The two error-correction tables give, for every version and
/// level, the number of error correction codewords per block and the number of blocks; the number of
/// data codewords follows from the total raw data modules of the version.
/// </summary>
internal static class QrTables
{
    private const int FormatGenerator = 0x537;    // x^10 + x^8 + x^5 + x^4 + x^2 + x + 1
    private const int FormatMask = 0x5412;
    private const int VersionGenerator = 0x1F25; // x^12 + x^11 + x^10 + x^9 + x^8 + x^5 + x^2 + 1

    /// <summary>Index: error correction level (L=0, M=1, Q=2, H=3), then version 1–40.</summary>
    private static readonly int[][] EccPerBlock =
    [
        // L
        [0, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
        // M
        [0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28],
        // Q
        [0, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
        // H
        [0, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
    ];

    /// <summary>Index: error correction level (L=0, M=1, Q=2, H=3), then version 1–40.</summary>
    private static readonly int[][] NumBlocks =
    [
        // L
        [0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25],
        // M
        [0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49],
        // Q
        [0, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68],
        // H
        [0, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81],
    ];

    /// <summary>The two-bit error correction indicator used in format information.</summary>
    public static int FormatIndicator(QrErrorCorrection level) => level switch
    {
        QrErrorCorrection.L => 0b01,
        QrErrorCorrection.M => 0b00,
        QrErrorCorrection.Q => 0b11,
        QrErrorCorrection.H => 0b10,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown error correction level."),
    };

    /// <summary>Raw data modules of a version (finder, timing, alignment and format areas removed).</summary>
    public static int RawDataModules(int version)
    {
        int result = ((16 * version) + 128) * version + 64;
        if (version >= 2)
        {
            int alignmentPatterns = (version / 7) + 2;
            result -= ((25 * alignmentPatterns) - 10) * alignmentPatterns - 55;
            if (version >= 7)
            {
                result -= 36;
            }
        }
        return result;
    }

    public static int RemainderBits(int version) => RawDataModules(version) % 8;

    public static int TotalCodewords(int version) => RawDataModules(version) / 8;

    public static int EccPerBlockCodewords(QrErrorCorrection level, int version) => EccPerBlock[(int)level][version];

    public static int BlockCount(QrErrorCorrection level, int version) => NumBlocks[(int)level][version];

    public static int DataCodewords(QrErrorCorrection level, int version) =>
        TotalCodewords(version) - (EccPerBlockCodewords(level, version) * BlockCount(level, version));

    /// <summary>The number of bits in the character count indicator for a version group.</summary>
    public static int CharacterCountBits(QrEncoding mode, int version)
    {
        int group = version switch
        {
            <= 9 => 0,
            <= 26 => 1,
            _ => 2,
        };
        return (mode, group) switch
        {
            (QrEncoding.Alphanumeric, 0) => 9,
            (QrEncoding.Alphanumeric, 1) => 11,
            (QrEncoding.Alphanumeric, 2) => 13,
            (QrEncoding.Byte, 0) => 8,
            (QrEncoding.Byte, 1) => 16,
            (QrEncoding.Byte, 2) => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown encoding mode."),
        };
    }

    /// <summary>The centres of the alignment patterns, in ascending order (empty for version 1).</summary>
    public static int[] AlignmentPositions(int version)
    {
        if (version == 1)
        {
            return [];
        }

        int count = (version / 7) + 2;
        // Version 32 is the single exception to the regular spacing formula (ISO/IEC 18004, §6.3.4).
        int step = version == 32 ? 26 : (((version * 4) + (count * 2) + 1) / ((count * 2) - 2)) * 2;
        int[] result = new int[count];
        result[0] = 6;
        int position = (4 * version) + 10; // size - 7
        for (int i = count - 1; i >= 1; i--, position -= step)
        {
            result[i] = position;
        }
        return result;
    }

    /// <summary>The 15-bit format information value (bit 14 first) for an EC level and mask.</summary>
    public static int FormatInformationBits(QrErrorCorrection level, int mask)
    {
        int data = (FormatIndicator(level) << 3) | mask;
        int remainder = data << 10;
        while (BitLength(remainder) > 10)
        {
            remainder ^= FormatGenerator << (BitLength(remainder) - 11);
        }
        return ((data << 10) | remainder) ^ FormatMask;
    }

    /// <summary>The 18-bit version information value (bit 17 first) for version 7 or higher.</summary>
    public static int VersionInformationBits(int version)
    {
        int remainder = version << 12;
        while (BitLength(remainder) > 12)
        {
            remainder ^= VersionGenerator << (BitLength(remainder) - 13);
        }
        return (version << 12) | remainder;
    }

    public static string Binary(int value, int width)
    {
        char[] result = new char[width];
        for (int i = 0; i < width; i++)
        {
            result[width - 1 - i] = (char)('0' + ((value >> i) & 1));
        }
        return new string(result);
    }

    private static int BitLength(int value)
    {
        int length = 0;
        while (value != 0)
        {
            length++;
            value >>= 1;
        }
        return length;
    }
}
