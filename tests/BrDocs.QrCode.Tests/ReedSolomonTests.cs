using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class ReedSolomonTests
{
    [Fact]
    public void GfMultiplication_UsesQrPrimitivePolynomial()
    {
        // α^8 = 0x1D, α^9 = 0x3A, 0x10 * 0x20 = α^4 * α^5 = α^9.
        Assert.Equal((byte)0x1D, ReedSolomon.Multiply(0x80, 0x02));
        Assert.Equal((byte)0x3A, ReedSolomon.Multiply(0x10, 0x20));
        Assert.Equal((byte)0x00, ReedSolomon.Multiply(0x00, 0xA5));
        Assert.Equal((byte)0x1D, ReedSolomon.Divide(0x3A, 0x02));
        Assert.Equal((byte)0x01, ReedSolomon.Divide(0x3A, 0x3A));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.Divide(1, 0));
    }

    [Fact]
    public void GeneratorPolynomial_MatchesIsoAnnexA()
    {
        // Coefficients after the leading 1, in descending powers (ISO/IEC 18004 Annex A, Thonky's
        // "generator polynomial tool"). Degree 2: x^2 + 3x + 2. Degree 3: x^3 + 7x^2 + 14x + α^3 (8).
        Assert.Equal(new byte[] { 3, 2 }, ReedSolomon.GeneratorPolynomial(2));
        Assert.Equal(new byte[] { 7, 14, 8 }, ReedSolomon.GeneratorPolynomial(3));
        Assert.Equal(
            new byte[] { 216, 194, 159, 111, 199, 94, 95, 113, 157, 193 },
            ReedSolomon.GeneratorPolynomial(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.GeneratorPolynomial(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.GeneratorPolynomial(69));
    }

    [Fact]
    public void ErrorCorrectionCodewords_MatchKnownVectors()
    {
        // A 1-M HELLO WORLD block: the 16 data codewords published by Thonky must produce these
        // 10 remainder codewords (independently computed with the log/antilog division algorithm).
        byte[] helloWorld =
        [
            32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17,
        ];
        Assert.Equal(
            new byte[] { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 },
            ReedSolomon.ComputeErrorCorrectionCodewords(helloWorld, 10));

        Assert.Equal(
            new byte[] { 231, 167 },
            ReedSolomon.ComputeErrorCorrectionCodewords(new byte[] { 0x40, 0x00, 0x00 }, 2));
    }

    [Fact]
    public void ErrorCorrectionCodewords_AreZeroForAllZeroData()
    {
        Assert.Equal(new byte[] { 0, 0, 0 }, ReedSolomon.ComputeErrorCorrectionCodewords(new byte[5], 3));
    }
}
