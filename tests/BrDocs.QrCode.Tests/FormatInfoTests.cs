using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class FormatInfoTests
{
    // ISO/IEC 18004 Table C.1 — all 32 (level, mask) format information strings, most significant
    // bit first. L/mask 4 (0x662F) is the worked example in Thonky's tutorial.
    [Theory]
    [InlineData(QrErrorCorrection.L, 0, 0x77C4)]
    [InlineData(QrErrorCorrection.L, 1, 0x72F3)]
    [InlineData(QrErrorCorrection.L, 2, 0x7DAA)]
    [InlineData(QrErrorCorrection.L, 3, 0x789D)]
    [InlineData(QrErrorCorrection.L, 4, 0x662F)]
    [InlineData(QrErrorCorrection.L, 5, 0x6318)]
    [InlineData(QrErrorCorrection.L, 6, 0x6C41)]
    [InlineData(QrErrorCorrection.L, 7, 0x6976)]
    [InlineData(QrErrorCorrection.M, 0, 0x5412)]
    [InlineData(QrErrorCorrection.M, 1, 0x5125)]
    [InlineData(QrErrorCorrection.M, 2, 0x5E7C)]
    [InlineData(QrErrorCorrection.M, 3, 0x5B4B)]
    [InlineData(QrErrorCorrection.M, 4, 0x45F9)]
    [InlineData(QrErrorCorrection.M, 5, 0x40CE)]
    [InlineData(QrErrorCorrection.M, 6, 0x4F97)]
    [InlineData(QrErrorCorrection.M, 7, 0x4AA0)]
    [InlineData(QrErrorCorrection.Q, 0, 0x355F)]
    [InlineData(QrErrorCorrection.Q, 1, 0x3068)]
    [InlineData(QrErrorCorrection.Q, 2, 0x3F31)]
    [InlineData(QrErrorCorrection.Q, 3, 0x3A06)]
    [InlineData(QrErrorCorrection.Q, 4, 0x24B4)]
    [InlineData(QrErrorCorrection.Q, 5, 0x2183)]
    [InlineData(QrErrorCorrection.Q, 6, 0x2EDA)]
    [InlineData(QrErrorCorrection.Q, 7, 0x2BED)]
    [InlineData(QrErrorCorrection.H, 0, 0x1689)]
    [InlineData(QrErrorCorrection.H, 1, 0x13BE)]
    [InlineData(QrErrorCorrection.H, 2, 0x1CE7)]
    [InlineData(QrErrorCorrection.H, 3, 0x19D0)]
    [InlineData(QrErrorCorrection.H, 4, 0x0762)]
    [InlineData(QrErrorCorrection.H, 5, 0x0255)]
    [InlineData(QrErrorCorrection.H, 6, 0x0D0C)]
    [InlineData(QrErrorCorrection.H, 7, 0x083B)]
    public void FormatInformation_MatchesStandardTable(QrErrorCorrection level, int mask, int expected) =>
        Assert.Equal(expected, QrCode.GetFormatInformationBits(level, mask));

    [Theory]
    [InlineData(7, 0x07C94)]
    [InlineData(8, 0x085BC)]
    [InlineData(14, 0x0E60D)]
    [InlineData(21, 0x15683)]
    [InlineData(28, 0x1CC1A)]
    [InlineData(40, 0x28C69)]
    public void VersionInformation_MatchesStandardTable(int version, int expected) =>
        Assert.Equal(expected, QrCode.GetVersionInformationBits(version));

    [Fact]
    public void Information_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.GetFormatInformationBits(QrErrorCorrection.M, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.GetFormatInformationBits(QrErrorCorrection.M, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.GetFormatInformationBits((QrErrorCorrection)99, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.GetVersionInformationBits(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCode.GetVersionInformationBits(41));
    }
}
