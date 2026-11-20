using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class VersionSelectionTests
{
    // Byte-mode capacities for level M: 14, 26, 42, 62, 84, 106, 122 bytes for versions 1–7.
    [Theory]
    [InlineData(14, 1)]
    [InlineData(15, 2)]
    [InlineData(26, 2)]
    [InlineData(27, 3)]
    [InlineData(42, 3)]
    [InlineData(43, 4)]
    [InlineData(62, 4)]
    [InlineData(63, 5)]
    [InlineData(84, 5)]
    [InlineData(85, 6)]
    [InlineData(106, 6)]
    [InlineData(107, 7)]
    public void ByteMode_SelectsSmallestFittingVersion(int length, int expectedVersion)
    {
        QrCode code = QrCode.Encode(new string('a', length));
        Assert.Equal(expectedVersion, code.Version);
        Assert.Equal(QrEncoding.Byte, code.Mode);
        Assert.Equal((4 * expectedVersion) + 17, code.Size);
    }

    // Alphanumeric capacities for level M: 20, 38, 61, 90 characters for versions 1–4.
    [Theory]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(38, 2)]
    [InlineData(39, 3)]
    [InlineData(61, 3)]
    [InlineData(62, 4)]
    [InlineData(90, 4)]
    [InlineData(91, 5)]
    public void AlphanumericMode_SelectsSmallestFittingVersion(int length, int expectedVersion)
    {
        QrCode code = QrCode.Encode(new string('A', length));
        Assert.Equal(expectedVersion, code.Version);
        Assert.Equal(QrEncoding.Alphanumeric, code.Mode);
    }

    [Fact]
    public void ModeSelection_ChoosesTheSmallerEncoding()
    {
        Assert.Equal(QrEncoding.Alphanumeric, QrCode.Encode("HELLO WORLD").Mode);
        Assert.Equal(QrEncoding.Byte, QrCode.Encode("hello world").Mode);
        Assert.Equal(QrEncoding.Byte, QrCode.Encode("PIX 123,45").Mode); // Comma is not alphanumeric.

        // At 15 bytes byte mode needs version 2; the same value in uppercase needs only version 1.
        Assert.Equal(2, QrCode.Encode(new string('a', 15)).Version);
        Assert.Equal(1, QrCode.Encode(new string('A', 15)).Version);
    }

    [Fact]
    public void MaximumVersionCapacity_IsEnforced()
    {
        QrCode max = QrCode.Encode(new string('a', 2331), QrErrorCorrection.M);
        Assert.Equal(40, max.Version);
        Assert.Equal(177, max.Size);

        Assert.Throws<ArgumentException>(() => QrCode.Encode(new string('a', 2332), QrErrorCorrection.M));
    }

    [Fact]
    public void InvalidInputs_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => QrCode.Encode(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.Encode("x", (QrErrorCorrection)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.Encode("x", QrErrorCorrection.M, new QrEncoderOptions { Version = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.Encode("x", QrErrorCorrection.M, new QrEncoderOptions { Version = 41 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.Encode("x", QrErrorCorrection.M, new QrEncoderOptions { Mask = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.Encode("x", QrErrorCorrection.M, new QrEncoderOptions { Mask = 8 }));
        Assert.Throws<ArgumentException>(() => QrCode.Encode(new string('a', 15), QrErrorCorrection.M, new QrEncoderOptions { Version = 1 }));
        Assert.Throws<ArgumentException>(() => QrCode.Encode("hello", QrErrorCorrection.M, new QrEncoderOptions { Encoding = QrEncoding.Alphanumeric }));
    }

    [Fact]
    public void AllErrorCorrectionLevels_ProduceValidVersions()
    {
        foreach (QrErrorCorrection level in Enum.GetValues<QrErrorCorrection>())
        {
            QrCode code = QrCode.Encode("HELLO WORLD", level);
            Assert.Equal(level, code.ErrorCorrection);
            Assert.InRange(code.Version, 1, 3);
            Assert.InRange(code.Mask, 0, 7);
            Assert.Equal(code.Size, code.Matrix.GetLength(0));
        }
    }
}
