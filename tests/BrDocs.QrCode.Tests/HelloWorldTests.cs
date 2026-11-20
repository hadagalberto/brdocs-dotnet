using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class HelloWorldTests
{
    // The canonical "HELLO WORLD" example (ISO/IEC 18004 / Thonky's QR Code tutorial): version 1,
    // error correction M, alphanumeric mode, mask 0. Independently generated with segno
    // (segno.make('HELLO WORLD', error='m', mode='alphanumeric', mask=0, micro=False, boost_error=False)).
    private static readonly string[] ExpectedMatrix =
    [
        "111111100010101111111",
        "100000101110001000001",
        "101110100010101011101",
        "101110100010101011101",
        "101110101011101011101",
        "100000100111001000001",
        "111111101010101111111",
        "000000000000000000000",
        "101010100100100010010",
        "011110001001000010001",
        "000111111101001011000",
        "111101011001110101110",
        "010011110101001110101",
        "000000001010001000101",
        "111111100000100101100",
        "100000100110001101000",
        "101110101100101111111",
        "101110100011010100010",
        "101110101111011101001",
        "100000100001110001011",
        "111111101101011100001",
    ];

    [Fact]
    public void HelloWorld_ProducesTheCanonicalMatrix()
    {
        QrCode code = QrCode.Encode("HELLO WORLD", QrErrorCorrection.M);

        Assert.Equal(1, code.Version);
        Assert.Equal(21, code.Size);
        Assert.Equal(QrEncoding.Alphanumeric, code.Mode);
        Assert.Equal(QrErrorCorrection.M, code.ErrorCorrection);
        Assert.Equal(0, code.Mask);
        Assert.Equal(16, code.DataCodewordCount);
        Assert.Equal(10, code.ErrorCorrectionCodewordCount);
        Assert.Equal(1, code.BlockCount);

        bool[,] matrix = code.Matrix;
        Assert.Equal(21, matrix.GetLength(0));
        Assert.Equal(21, matrix.GetLength(1));
        for (int row = 0; row < 21; row++)
        {
            string actual = new(Enumerable.Range(0, 21).Select(col => matrix[row, col] ? '1' : '0').ToArray());
            Assert.Equal(ExpectedMatrix[row], actual);
        }
    }

    [Fact]
    public void ForcedMaskZero_MatchesAutomaticallySelectedMask()
    {
        QrCode automatic = QrCode.Encode("HELLO WORLD");
        QrCode forced = QrCode.Encode("HELLO WORLD", QrErrorCorrection.M, new QrEncoderOptions { Mask = 0 });

        Assert.Equal(automatic.Mask, forced.Mask);
        Assert.Equal(ToString(automatic.Matrix), ToString(forced.Matrix));
    }

    [Fact]
    public void EmbeddedFormatBits_MatchTheMZeroFormatString()
    {
        bool[,] matrix = QrCode.Encode("HELLO WORLD").Matrix;
        int value = 0;
        for (int i = 0; i <= 5; i++)
        {
            value |= (matrix[i, 8] ? 1 : 0) << i;
        }
        value |= (matrix[7, 8] ? 1 : 0) << 6;
        value |= (matrix[8, 8] ? 1 : 0) << 7;
        value |= (matrix[8, 7] ? 1 : 0) << 8;
        for (int i = 9; i < 15; i++)
        {
            value |= (matrix[8, 14 - i] ? 1 : 0) << i;
        }

        Assert.Equal(0x5412, value);
        Assert.True(matrix[13, 8]); // The dark module at (size - 8, 8).
    }

    private static string ToString(bool[,] matrix)
    {
        char[] chars = new char[matrix.Length];
        int index = 0;
        foreach (bool module in matrix)
        {
            chars[index++] = module ? '1' : '0';
        }
        return new string(chars);
    }
}
