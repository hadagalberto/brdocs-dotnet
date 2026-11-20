using System.Text;

namespace BrDocs.QrCode;

/// <summary>Turns text into the data codeword stream of a QR symbol.</summary>
internal static class QrDataEncoder
{
    private const string AlphanumericAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>Returns -1 when the character cannot be represented in alphanumeric mode.</summary>
    public static int AlphanumericValue(char character)
    {
        if (character is >= '0' and <= '9')
        {
            return character - '0';
        }
        if (character is >= 'A' and <= 'Z')
        {
            return character - 'A' + 10;
        }
        int index = AlphanumericAlphabet.IndexOf(character);
        return index < 0 ? -1 : index;
    }

    public static bool CanEncodeAlphanumeric(ReadOnlySpan<char> text)
    {
        foreach (char character in text)
        {
            if (AlphanumericValue(character) < 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The number of bits used by the mode indicator, count indicator and payload.</summary>
    public static int SegmentBitLength(string text, QrEncoding mode, int version)
    {
        int countBits = QrTables.CharacterCountBits(mode, version);
        int count = mode == QrEncoding.Byte ? Encoding.UTF8.GetByteCount(text) : text.Length;
        if (count >= 1 << countBits)
        {
            return int.MaxValue;
        }
        int payloadBits = mode == QrEncoding.Byte
            ? count * 8
            : (text.Length / 2) * 11 + (text.Length % 2) * 6;
        return 4 + countBits + payloadBits;
    }

    /// <summary>Builds the complete data codeword block including terminator, byte padding and pad codewords.</summary>
    public static byte[] BuildDataCodewords(string text, QrEncoding mode, int version, QrErrorCorrection level)
    {
        int capacity = QrTables.DataCodewords(level, version);
        QrBitBuffer bits = new();

        bits.AppendBits(mode == QrEncoding.Byte ? 0b0100 : 0b0010, 4);
        int countBits = QrTables.CharacterCountBits(mode, version);
        int count = mode == QrEncoding.Byte ? Encoding.UTF8.GetByteCount(text) : text.Length;
        bits.AppendBits(count, countBits);

        if (mode == QrEncoding.Byte)
        {
            foreach (byte value in Encoding.UTF8.GetBytes(text))
            {
                bits.AppendBits(value, 8);
            }
        }
        else
        {
            for (int i = 0; i < text.Length; i += 2)
            {
                int first = AlphanumericValue(text[i]);
                if (i + 1 < text.Length)
                {
                    int second = AlphanumericValue(text[i + 1]);
                    bits.AppendBits((first * 45) + second, 11);
                }
                else
                {
                    bits.AppendBits(first, 6);
                }
            }
        }

        int capacityBits = capacity * 8;
        if (bits.BitLength > capacityBits)
        {
            throw new ArgumentException("The payload does not fit in the selected QR version.", nameof(text));
        }
        bits.AppendBits(0, Math.Min(4, capacityBits - bits.BitLength));
        while ((bits.BitLength % 8) != 0)
        {
            bits.AppendBit(0);
        }

        byte[] result = bits.ToBytes();
        for (int pad = 0; result.Length < capacity; pad++)
        {
            Array.Resize(ref result, result.Length + 1);
            result[^1] = (pad & 1) == 0 ? (byte)0xEC : (byte)0x11;
        }
        return result;
    }

    /// <summary>A minimal MSB-first bit buffer.</summary>
    private sealed class QrBitBuffer
    {
        private readonly List<byte> _bytes = [];
        private int _bitLength;

        public int BitLength => _bitLength;

        public void AppendBit(int bit)
        {
            int index = _bitLength >> 3;
            if (index == _bytes.Count)
            {
                _bytes.Add(0);
            }
            if (bit != 0)
            {
                _bytes[index] |= (byte)(1 << (7 - (_bitLength & 7)));
            }
            _bitLength++;
        }

        public void AppendBits(int value, int length)
        {
            for (int i = length - 1; i >= 0; i--)
            {
                AppendBit((value >> i) & 1);
            }
        }

        public byte[] ToBytes() => [.. _bytes];
    }
}
