using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class CnpjTests
{
    private const string ValidNumericFormatted = "11.222.333/0001-81";
    private const string ValidNumericDigits = "11222333000181";
    private const string ValidAlphanumericFormatted = "12.ABC.345/01DE-35";
    private const string ValidAlphanumericDigits = "12ABC34501DE35";

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    [InlineData("45.723.174/0001-10")]
    [InlineData("45723174000110")]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12ABC34501DE35")]
    [InlineData("12.abc.345/01de-35")]
    [InlineData("12abc34501de35")]
    [InlineData("0A1B.2C3D/4E5F-23")]
    [InlineData("0a1b.2c3d/4e5f-23")]
    [InlineData("AAAA.AAAA/AAAA-45")]
    [InlineData("AAAAAAAAAAAA45")]
    [InlineData("ZZZZ.ZZZZ/ZZZZ-62")]
    [InlineData("BRAZIL2026DO07")]
    [InlineData("BR.AZI.L20/26DO-07")]
    [InlineData(" 11.222.333/0001-81 ")]
    [InlineData("11 222 333 0001 81")]
    [InlineData("1.1.2.2.2.3.3.3.0.0.0.1.8.1")]
    [InlineData("12/ABC/345/01DE/35")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(Cnpj.IsValid(value));
    }

    [Theory]
    [InlineData("12ABC34501DE36")]     // DV2 wrong
    [InlineData("12ABC34501DE34")]     // DV2 wrong
    [InlineData("12ABC34501DE45")]     // DV1 wrong
    [InlineData("12ABC34501DE00")]     // both wrong
    [InlineData("12ABC34501DE33")]     // both wrong
    [InlineData("12ABC34501DE95")]     // both wrong
    [InlineData("11222333000180")]     // DV2 wrong
    [InlineData("11222333000191")]     // DV1 wrong
    [InlineData("11222333000171")]     // DV1 wrong
    [InlineData("45723174000111")]     // DV1 wrong
    [InlineData("0A1B2C3D4E5F24")]     // DV2 wrong
    [InlineData("0A1B2C3D4E5F13")]     // DV1 wrong
    [InlineData("AAAAAAAAAAAA46")]     // DV2 wrong
    [InlineData("AAAAAAAAAAAA15")]     // DV1 wrong
    [InlineData("13ABC34501DE35")]     // base changed, DVs kept
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(Cnpj.IsValid(value));
    }

    [Theory]
    [InlineData("00.000.000/0000-00")]
    [InlineData("00000000000000")]
    [InlineData("11.111.111/1111-11")]
    [InlineData("11111111111111")]
    [InlineData("22222222222222")]
    [InlineData("33333333333333")]
    [InlineData("44444444444444")]
    [InlineData("55555555555555")]
    [InlineData("66666666666666")]
    [InlineData("77777777777777")]
    [InlineData("88888888888888")]
    [InlineData("99999999999999")]
    public void IsValid_Rejects_All_Zero_And_All_Same_Numeric_Sequences(string value)
    {
        Assert.False(Cnpj.IsValid(value));
    }

    [Fact]
    public void IsValid_Accepts_All_Same_Letter_Sequences()
    {
        // The all-same rejection is defined for numeric sequences; all-letter bases are legal
        // input for the alphanumeric format (DVs 45 and 62, verified against the official rule).
        Assert.True(Cnpj.IsValid("AAAAAAAAAAAA45"));
        Assert.True(Cnpj.IsValid("ZZZZ.ZZZZ/ZZZZ-62"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData("12ABC34501DE3")]      // 13 chars
    [InlineData("12ABC34501DE355")]    // 15 chars
    [InlineData("12ABC34501DE")]
    [InlineData("112223330001")]
    [InlineData("11.222.333/0001-810")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(Cnpj.IsValid(value));
    }

    [Theory]
    [InlineData("12.AB$.345/01DE-35")]
    [InlineData("12,ABC,345,01DE,35")]
    [InlineData("12_ABC_345_01DE_35")]
    [InlineData("(12) ABC 345/01DE-35")]
    [InlineData("12.ABC.345/01DE-A5")]   // letter in the DV1 position
    [InlineData("12.ABC.345/01DE-3B")]   // letter in the DV2 position
    [InlineData("12.ABC.345/01DE-ZZ")]
    [InlineData("12.ÁBC.345/01DE-35")]   // accented letter
    [InlineData("１２ABC34501DE35")]     // full-width digits
    [InlineData("12.ABC.345/01DE-35\n")]
    [InlineData("CEP 12.ABC.345/01DE-35")]
    [InlineData("12.ABC.345/01DE–35")]    // en dash U+2013
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(Cnpj.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Cnpj.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11.222.333/0001-81", "11222333000181", "11222333", "0001", false, true)]
    [InlineData("11222333000181", "11.222.333/0001-81", "11222333000181", "11222333", "0001", false, true)]
    [InlineData("45.723.174/0001-10", "45.723.174/0001-10", "45723174000110", "45723174", "0001", false, true)]
    [InlineData("12.ABC.345/01DE-35", "12.ABC.345/01DE-35", "12ABC34501DE35", "12ABC345", "01DE", true, false)]
    [InlineData("12.abc.345/01de-35", "12.ABC.345/01DE-35", "12ABC34501DE35", "12ABC345", "01DE", true, false)]
    [InlineData("0A1B.2C3D/4E5F-23", "0A.1B2.C3D/4E5F-23", "0A1B2C3D4E5F23", "0A1B2C3D", "4E5F", true, false)]
    [InlineData("aaaaaaaaaaaa45", "AA.AAA.AAA/AAAA-45", "AAAAAAAAAAAA45", "AAAAAAAA", "AAAA", true, false)]
    public void Parse_Normalizes_And_Exposes_The_Parts(
        string value, string expectedFormatted, string expectedDigits, string expectedRoot,
        string expectedBranch, bool expectedAlphanumeric, bool expectedHeadOffice)
    {
        Cnpj cnpj = Cnpj.Parse(value);
        Assert.Equal(expectedFormatted, cnpj.ToString());
        Assert.Equal(expectedDigits, cnpj.ToString("N"));
        Assert.Equal(expectedRoot, cnpj.Root);
        Assert.Equal(expectedBranch, cnpj.Branch);
        Assert.Equal(expectedAlphanumeric, cnpj.IsAlphanumeric);
        Assert.Equal(expectedHeadOffice, cnpj.IsHeadOffice);
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Cnpj.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("11222333000180")]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("12.ABC.345/01DE-A5")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Cnpj.Parse(value));
        Assert.Contains("CNPJ", exception.Message);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "12ABC34501DE35" + new string('9', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Cnpj.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 100, "the message must be truncated");
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
    [InlineData(" 12 ABC 345 01DE 35 ", "12ABC34501DE35")]
    [InlineData("BRAZIL2026DO07", "BRAZIL2026DO07")]
    public void TryParse_Valid_Outputs_The_Cnpj(string value, string expectedDigits)
    {
        Assert.True(Cnpj.TryParse(value, out Cnpj cnpj));
        Assert.Equal(expectedDigits, cnpj.ToString("N"));
        Assert.Equal(14, cnpj.ToString("N").Length);
        Assert.True(Cnpj.IsValid(cnpj.ToString("N")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("11222333000180")]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("12ABC34501DE3")]
    [InlineData("12.AB$.345/01DE-35")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Cnpj.TryParse(value, out Cnpj cnpj));
        Assert.Equal(default, cnpj);
        Assert.Equal("00.000.000/0000-00", cnpj.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("N")]
    [InlineData("g")]
    [InlineData("n")]
    public void ToString_Formats_Are_Resolved(string? format)
    {
        Cnpj cnpj = Cnpj.Parse(ValidNumericDigits);
        string expected = format is "N" or "n" ? ValidNumericDigits : ValidNumericFormatted;
        Assert.Equal(expected, cnpj.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData("F")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Cnpj cnpj = Cnpj.Parse(ValidNumericDigits);
        Assert.Throws<FormatException>(() => cnpj.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        Cnpj cnpj = Cnpj.Parse(ValidAlphanumericDigits);
        Span<char> buffer = stackalloc char[18];
        Assert.True(cnpj.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(18, written);
        Assert.Equal(ValidAlphanumericFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        Cnpj cnpj = Cnpj.Parse(ValidAlphanumericFormatted);
        Span<char> buffer = stackalloc char[14];
        Assert.True(cnpj.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(14, written);
        Assert.Equal(ValidAlphanumericDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(17)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        Cnpj cnpj = Cnpj.Parse(ValidAlphanumericDigits);
        char[] buffer = new char[18];
        buffer.AsSpan().Fill('x');
        Assert.False(cnpj.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 18), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        Cnpj cnpj = Cnpj.Parse(ValidAlphanumericDigits);
        char[] buffer = new char[14];
        buffer.AsSpan().Fill('x');
        Assert.False(cnpj.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 14), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Cnpj cnpj = Cnpj.Parse(ValidNumericDigits);
        Assert.Throws<FormatException>(() => cnpj.TryFormat(stackalloc char[18], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Cnpj expected = Cnpj.Parse(ValidAlphanumericDigits);
        Cnpj parsed = ParsableHelpers.ParseViaSpanParsable<Cnpj>(ValidAlphanumericFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Cnpj>(ValidAlphanumericDigits.AsSpan(), null, out Cnpj result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Cnpj expected = Cnpj.Parse(ValidNumericDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Cnpj>(ValidNumericFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Cnpj>(ValidNumericDigits, null, out Cnpj result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Cnpj>(null, null, out Cnpj result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Cnpj>("11222333000180".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Normalized_Characters()
    {
        Cnpj a = Cnpj.Parse("12.ABC.345/01DE-35");
        Cnpj b = Cnpj.Parse("12abc34501de35");
        Cnpj c = Cnpj.Parse("11.222.333/0001-81");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(((IEquatable<Cnpj>)a).Equals(b));
        Assert.False(((IEquatable<Cnpj>)a).Equals(c));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Characters_And_Invalid()
    {
        Cnpj @default = default;
        Assert.Equal("00.000.000/0000-00", @default.ToString());
        Assert.Equal("00000000000000", @default.ToString("N"));
        Assert.Equal("00000000", @default.Root);
        Assert.Equal("0000", @default.Branch);
        Assert.False(@default.IsAlphanumeric);
        Assert.False(@default.IsHeadOffice);
        Assert.False(Cnpj.IsValid(@default.ToString()));
    }

    [Fact]
    public void Generate_Returns_Valid_Numeric_Cnpjs()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Cnpj cnpj = Cnpj.Generate();
            string digits = cnpj.ToString("N");
            Assert.Equal(14, digits.Length);
            Assert.True(Cnpj.IsValid(digits), $"generated {digits} must be valid");
            Assert.False(cnpj.IsAlphanumeric);
            Assert.All(digits, c => Assert.True(char.IsAsciiDigit(c)));
            Assert.Equal(cnpj, Cnpj.Parse(digits));
            Assert.Equal(cnpj, Cnpj.Parse(cnpj.ToString()));
        }
    }

    [Fact]
    public void Generate_Produces_Multiple_Branches()
    {
        // With a seeded generator the run is deterministic per runtime; one million draws give
        // ~100 head-office results (branch 0001 has probability 1/10_000 per draw), so the
        // flake probability of a seeded miss is e^-100 ≈ 4e-44 — impossible in practice.
        var random = new Random(20260701);
        bool sawHeadOffice = false, sawOtherBranch = false;
        for (int i = 0; i < 1_000_000 && !(sawHeadOffice && sawOtherBranch); i++)
        {
            Cnpj cnpj = Cnpj.Generate(random);
            if (cnpj.IsHeadOffice) { sawHeadOffice = true; } else { sawOtherBranch = true; }
        }
        Assert.True(sawHeadOffice && sawOtherBranch, "generation must cover branch 0001 and other branches");
    }

    [Fact]
    public void GenerateAlphanumeric_Returns_Valid_Alphanumeric_Cnpjs()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Cnpj cnpj = Cnpj.GenerateAlphanumeric();
            string digits = cnpj.ToString("N");
            Assert.True(Cnpj.IsValid(digits), $"generated {digits} must be valid");
            Assert.True(cnpj.IsAlphanumeric, $"generated {digits} must contain a letter");
            Assert.Equal(8, cnpj.Root.Length);
            Assert.Equal(4, cnpj.Branch.Length);
            Assert.True(char.IsAsciiDigit(digits[12]));
            Assert.True(char.IsAsciiDigit(digits[13]));
            Assert.Contains(digits[..12], c => c >= 'A' && c <= 'Z');
            Assert.Equal(cnpj, Cnpj.Parse(digits));
            Assert.Equal(cnpj, Cnpj.Parse(cnpj.ToString()));
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        Cnpj[] Run(Random random) => Enumerable.Range(0, 100).Select(_ => Cnpj.Generate(random)).ToArray();

        Assert.Equal(Run(new Random(4242)), Run(new Random(4242)));
        Assert.NotEqual(Run(new Random(1)), Run(new Random(2)));
    }

    [Fact]
    public void GenerateAlphanumeric_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        Cnpj[] Run(Random random) => Enumerable.Range(0, 100).Select(_ => Cnpj.GenerateAlphanumeric(random)).ToArray();

        Assert.Equal(Run(new Random(777)), Run(new Random(777)));
        Assert.NotEqual(Run(new Random(3)), Run(new Random(4)));
    }

    [Fact]
    public void Parse_Is_Culture_Invariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Cnpj cnpj = Cnpj.Parse(ValidNumericDigits);
            Assert.Equal(ValidNumericFormatted, cnpj.ToString());
            Assert.Equal(ValidNumericDigits, cnpj.ToString("N"));
            Assert.Equal(ValidNumericFormatted, ((IFormattable)cnpj).ToString("G", CultureInfo.GetCultureInfo("ar-SA")));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
