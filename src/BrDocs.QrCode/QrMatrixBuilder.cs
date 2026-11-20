namespace BrDocs.QrCode;

/// <summary>Builds the final ISO/IEC 18004 module matrix: function patterns, interleaved data, masking.</summary>
internal static class QrMatrixBuilder
{
    public static (bool[,] Modules, int Mask) Build(string text, QrErrorCorrection level, int version, QrEncoding mode, int? forcedMask)
    {
        int size = (4 * version) + 17;
        bool[,] modules = new bool[size, size];
        bool[,] isFunction = new bool[size, size];

        DrawFunctionPatterns(modules, isFunction, version);
        byte[] codewords = BuildCodewords(text, level, version, mode);
        PlaceDataBits(modules, isFunction, codewords, version);

        if (forcedMask is int fixedMask)
        {
            ApplyMask(modules, isFunction, fixedMask);
            DrawFormatBits(modules, isFunction, level, fixedMask);
            return (modules, fixedMask);
        }

        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        bool[,]? bestMatrix = null;
        for (int mask = 0; mask < 8; mask++)
        {
            bool[,] candidate = (bool[,])modules.Clone();
            ApplyMask(candidate, isFunction, mask);
            DrawFormatBits(candidate, isFunction, level, mask);
            int penalty = Penalty(candidate);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
                bestMatrix = candidate;
            }
        }

        return (bestMatrix!, bestMask);
    }

    private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, int version)
    {
        int size = modules.GetLength(0);

        // Timing patterns; the finder patterns drawn next overwrite their overlapping ends.
        for (int i = 0; i < size; i++)
        {
            SetFunction(modules, isFunction, 6, i, i % 2 == 0);
            SetFunction(modules, isFunction, i, 6, i % 2 == 0);
        }

        DrawFinderPattern(modules, isFunction, 3, 3);
        DrawFinderPattern(modules, isFunction, size - 4, 3);
        DrawFinderPattern(modules, isFunction, 3, size - 4);

        int[] positions = QrTables.AlignmentPositions(version);
        for (int i = 0; i < positions.Length; i++)
        {
            for (int j = 0; j < positions.Length; j++)
            {
                bool finderCorner = (i == 0 && j == 0)
                    || (i == 0 && j == positions.Length - 1)
                    || (i == positions.Length - 1 && j == 0);
                if (!finderCorner)
                {
                    DrawAlignmentPattern(modules, isFunction, positions[j], positions[i]);
                }
            }
        }

        // Reserve (and initially clear) the format areas; the real format bits are written after masking.
        DrawFormatBits(modules, isFunction, QrErrorCorrection.M, 0);
        if (version >= 7)
        {
            DrawVersionBits(modules, isFunction, version);
        }
    }

    private static void SetFunction(bool[,] modules, bool[,] isFunction, int row, int col, bool dark)
    {
        modules[row, col] = dark;
        isFunction[row, col] = true;
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int centerRow, int centerCol)
    {
        int size = modules.GetLength(0);
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int row = centerRow + dy;
                int col = centerCol + dx;
                if (row < 0 || row >= size || col < 0 || col >= size)
                {
                    continue;
                }
                int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                SetFunction(modules, isFunction, row, col, distance != 2 && distance != 4);
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int centerRow, int centerCol)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                SetFunction(modules, isFunction, centerRow + dy, centerCol + dx, distance != 1);
            }
        }
    }

    private static void DrawFormatBits(bool[,] modules, bool[,] isFunction, QrErrorCorrection level, int mask)
    {
        int size = modules.GetLength(0);
        int bits = QrTables.FormatInformationBits(level, mask);

        void Set(int row, int col, int index) =>
            SetFunction(modules, isFunction, row, col, ((bits >> index) & 1) != 0);

        // First copy, around the top-left finder pattern.
        for (int i = 0; i <= 5; i++)
        {
            Set(i, 8, i);
        }
        Set(7, 8, 6);
        Set(8, 8, 7);
        Set(8, 7, 8);
        for (int i = 9; i < 15; i++)
        {
            Set(8, 14 - i, i);
        }

        // Second copy: one strip below the top-right finder, one strip right of the bottom-left finder.
        for (int i = 0; i < 8; i++)
        {
            Set(8, size - 1 - i, i);
        }
        for (int i = 8; i < 15; i++)
        {
            Set(size - 15 + i, 8, i);
        }

        // The dark module is always dark and is not part of either copy.
        SetFunction(modules, isFunction, size - 8, 8, true);
    }

    private static void DrawVersionBits(bool[,] modules, bool[,] isFunction, int version)
    {
        int size = modules.GetLength(0);
        int bits = QrTables.VersionInformationBits(version);
        for (int i = 0; i < 18; i++)
        {
            bool dark = ((bits >> i) & 1) != 0;
            int a = size - 11 + (i % 3);
            int b = i / 3;
            SetFunction(modules, isFunction, b, a, dark);   // Top-right 6x3 block.
            SetFunction(modules, isFunction, a, b, dark);   // Bottom-left 3x6 block.
        }
    }

    private static byte[] BuildCodewords(string text, QrErrorCorrection level, int version, QrEncoding mode)
    {
        byte[] data = QrDataEncoder.BuildDataCodewords(text, mode, version, level);
        int blockCount = QrTables.BlockCount(level, version);
        int eccPerBlock = QrTables.EccPerBlockCodewords(level, version);
        int shortLength = data.Length / blockCount;
        int shortBlocks = blockCount - (data.Length % blockCount);

        byte[][] dataBlocks = new byte[blockCount][];
        byte[][] eccBlocks = new byte[blockCount][];
        int offset = 0;
        for (int block = 0; block < blockCount; block++)
        {
            int length = shortLength + (block < shortBlocks ? 0 : 1);
            dataBlocks[block] = data.AsSpan(offset, length).ToArray();
            offset += length;
            eccBlocks[block] = ReedSolomon.ComputeErrorCorrectionCodewords(dataBlocks[block], eccPerBlock);
        }

        List<byte> result = new(data.Length + (eccPerBlock * blockCount));
        for (int i = 0; i <= shortLength; i++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                if (i != shortLength || block >= shortBlocks)
                {
                    result.Add(dataBlocks[block][i]);
                }
            }
        }
        for (int i = 0; i < eccPerBlock; i++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                result.Add(eccBlocks[block][i]);
            }
        }
        return [.. result];
    }

    private static void PlaceDataBits(bool[,] modules, bool[,] isFunction, byte[] codewords, int version)
    {
        int size = modules.GetLength(0);
        List<bool> bits = new(codewords.Length * 8 + 8);
        foreach (byte codeword in codewords)
        {
            for (int i = 7; i >= 0; i--)
            {
                bits.Add(((codeword >> i) & 1) != 0);
            }
        }
        for (int i = 0; i < QrTables.RemainderBits(version); i++)
        {
            bits.Add(false);
        }

        int bitIndex = 0;
        bool upward = true;
        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }
            for (int vertical = 0; vertical < size; vertical++)
            {
                int row = upward ? size - 1 - vertical : vertical;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int col = right - columnOffset;
                    if (!isFunction[row, col])
                    {
                        modules[row, col] = bitIndex < bits.Count && bits[bitIndex];
                        bitIndex++;
                    }
                }
            }
            upward = !upward;
        }
    }

    private static void ApplyMask(bool[,] modules, bool[,] isFunction, int mask)
    {
        int size = modules.GetLength(0);
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (!isFunction[row, col] && MaskBit(mask, col, row))
                {
                    modules[row, col] = !modules[row, col];
                }
            }
        }
    }

    private static bool MaskBit(int mask, int x, int y) => mask switch
    {
        0 => (x + y) % 2 == 0,
        1 => y % 2 == 0,
        2 => x % 3 == 0,
        3 => (x + y) % 3 == 0,
        4 => ((x / 3) + (y / 2)) % 2 == 0,
        5 => (((x * y) % 2) + ((x * y) % 3)) == 0,
        6 => ((((x * y) % 2) + ((x * y) % 3)) % 2) == 0,
        7 => ((((x + y) % 2) + ((x * y) % 3)) % 2) == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(mask), mask, "The mask must be between 0 and 7."),
    };

    private static int Penalty(bool[,] modules)
    {
        int size = modules.GetLength(0);
        int result = 0;

        // Rules 1 and 3, rows then columns.
        for (int y = 0; y < size; y++)
        {
            bool runColor = false;
            int runLength = 0;
            int[] history = new int[7];
            for (int x = 0; x < size; x++)
            {
                if (modules[y, x] == runColor)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        result += 3;
                    }
                    else if (runLength > 5)
                    {
                        result++;
                    }
                }
                else
                {
                    AddRunHistory(runLength, history, size);
                    if (!runColor)
                    {
                        result += CountFinderPatterns(history) * 40;
                    }
                    runColor = modules[y, x];
                    runLength = 1;
                }
            }
            result += TerminateRun(runColor, runLength, history, size) * 40;
        }

        for (int x = 0; x < size; x++)
        {
            bool runColor = false;
            int runLength = 0;
            int[] history = new int[7];
            for (int y = 0; y < size; y++)
            {
                if (modules[y, x] == runColor)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        result += 3;
                    }
                    else if (runLength > 5)
                    {
                        result++;
                    }
                }
                else
                {
                    AddRunHistory(runLength, history, size);
                    if (!runColor)
                    {
                        result += CountFinderPatterns(history) * 40;
                    }
                    runColor = modules[y, x];
                    runLength = 1;
                }
            }
            result += TerminateRun(runColor, runLength, history, size) * 40;
        }

        // Rule 2: overlapping 2x2 blocks of a single colour, 3 points each.
        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                bool color = modules[y, x];
                if (color == modules[y, x + 1] && color == modules[y + 1, x] && color == modules[y + 1, x + 1])
                {
                    result += 3;
                }
            }
        }

        // Rule 4: deviation of the dark-module ratio from 50%, in 5% steps, 10 points each.
        int dark = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (modules[y, x])
                {
                    dark++;
                }
            }
        }
        int total = size * size;
        int steps = ((Math.Abs((dark * 20) - (total * 10)) + total - 1) / total) - 1;
        return result + (steps * 10);
    }

    private static void AddRunHistory(int runLength, int[] history, int size)
    {
        if (history[6] == 0)
        {
            runLength += size; // Treat the area before the first run as an infinite light border.
        }
        Array.Copy(history, 1, history, 0, 6);
        history[6] = runLength;
    }

    private static int TerminateRun(bool runColor, int runLength, int[] history, int size)
    {
        if (runColor)
        {
            AddRunHistory(runLength, history, size);
            runLength = 0;
        }
        AddRunHistory(runLength + size, history, size);
        return CountFinderPatterns(history);
    }

    private static int CountFinderPatterns(int[] history)
    {
        int n = history[1];
        bool core = n > 0 && history[2] == n && history[3] == n * 3 && history[4] == n && history[5] == n;
        return (core && history[0] >= n * 4 && history[6] >= n ? 1 : 0)
             + (core && history[6] >= n * 4 && history[0] >= n ? 1 : 0);
    }
}
