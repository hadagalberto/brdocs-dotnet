using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class CpfTests
{
    private const string ValidCpfFormatted = "111.444.777-35";
    private const string ValidCpfDigits = "11144477735";

    // One valid CPF per fiscal region (9th digit 0..9), base "12345678X".

    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("529.982.247-25")]
    [InlineData("11144477735")]
    [InlineData("52998224725")]
    [InlineData("123.456.780-62")]
    [InlineData("12345678062")]
    [InlineData("123.456.781-43")]
    [InlineData("12345678143")]
    [InlineData("123.456.782-24")]
    [InlineData("12345678224")]
    [InlineData("123.456.783-05")]
    [InlineData("12345678305")]
    [InlineData("123.456.784-96")]
    [InlineData("12345678496")]
    [InlineData("123.456.785-77")]
    [InlineData("12345678577")]
    [InlineData("123.456.786-58")]
    [InlineData("12345678658")]
    [InlineData("123.456.787-39")]
    [InlineData("12345678739")]
    [InlineData("123.456.788-10")]
    [InlineData("12345678810")]
    [InlineData("123.456.789-09")]
    [InlineData("12345678909")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(Cpf.IsValid(value));
    }

    [Theory]
    [InlineData(" 111.444.777-35 ")]
    [InlineData("111 444 777 35")]
    [InlineData("1 1 1 4 4 4 7 7 7 3 5")]
    [InlineData("1.1.1.4.4.4.7.7.7.3.5")]
    [InlineData("1-1-1-4-4-4-7-7-7-3-5")]
    [InlineData("1/1/1/4/4/4/7/7/7/3/5")]
    [InlineData("  11144477735  ")]
    [InlineData("11.14/44.77/73-5")]
    [InlineData(".-.111.444.777-35.-.")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(Cpf.IsValid(value));
    }

    [Theory]
    [InlineData("11144477736")]       // DV2 wrong
    [InlineData("11144477745")]       // DV1 wrong
    [InlineData("11144477734")]       // DV2 wrong
    [InlineData("11144477753")]       // DV1 wrong
    [InlineData("11144477725")]       // both wrong
    [InlineData("11144477700")]       // both wrong
    [InlineData("52998224724")]       // DV2 wrong
    [InlineData("52998224715")]       // DV1 wrong
    [InlineData("12345678063")]       // DV2 wrong
    [InlineData("12345678900")]       // DV2 wrong
    [InlineData("12345678919")]       // DV1 wrong
    [InlineData("12345678906")]       // DV2 wrong
    [InlineData("123.456.780-62.5")]  // one extra digit
    [InlineData("91144477735")]      // base changed, DVs kept
    [InlineData("21144477735")]
    [InlineData("11144477744")]
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(Cpf.IsValid(value));
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("22222222222")]
    [InlineData("33333333333")]
    [InlineData("44444444444")]
    [InlineData("55555555555")]
    [InlineData("66666666666")]
    [InlineData("77777777777")]
    [InlineData("88888888888")]
    [InlineData("99999999999")]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    public void IsValid_Rejects_All_Same_Digit_Sequences(string value)
    {
        Assert.False(Cpf.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData(" - / ")]
    [InlineData("1")]
    [InlineData("12345678")]
    [InlineData("123456789")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("111444777355")]
    [InlineData("1114447773555")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(Cpf.IsValid(value));
    }

    [Theory]
    [InlineData("111,444,777,35")]
    [InlineData("111_444_777_35")]
    [InlineData("111:444:777:35")]
    [InlineData("(11) 444-777-35")]
    [InlineData("111;444;777;35")]
    [InlineData("1114447773a")]
    [InlineData("111.444.777-3x")]
    [InlineData("111.444.777-3X")]
    [InlineData("abc")]
    [InlineData("111.444.777–35")]    // en dash U+2013, not '-'
    [InlineData("١١١٤٤٤٧٧٧٣٥")]        // Arabic-Indic digits
    [InlineData("11144477735\n")]
    [InlineData("\t11144477735")]
    [InlineData("111 444 777\t35")]
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(Cpf.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Cpf.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("111.444.777-35", "111.444.777-35", "11144477735")]
    [InlineData("529.982.247-25", "529.982.247-25", "52998224725")]
    [InlineData(" 111 444 777 35 ", "111.444.777-35", "11144477735")]
    public void Parse_Returns_The_Cpf_For_Valid_Input(string value, string expectedFormatted, string expectedDigits)
    {
        Cpf cpf = Cpf.Parse(value);
        Assert.Equal(expectedFormatted, cpf.ToString());
        Assert.Equal(expectedDigits, cpf.ToString("N"));
        Assert.Equal(11, cpf.ToString("N").Length);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Cpf cpf = Cpf.Parse(ValidCpfDigits);
            Assert.Equal(ValidCpfFormatted, cpf.ToString());
            Assert.Equal(ValidCpfDigits, cpf.ToString("N"));
            Assert.Equal(ValidCpfDigits, ((IFormattable)cpf).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidCpfFormatted, ((IFormattable)cpf).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Cpf.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a cpf")]
    [InlineData("11144477736")]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Cpf.Parse(value));
        Assert.Contains("CPF", exception.Message);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "11144477735" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Cpf.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 100, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Cpf()
    {
        Assert.True(Cpf.TryParse(ValidCpfFormatted, out Cpf cpf));
        Assert.Equal(ValidCpfDigits, cpf.ToString("N"));
        Assert.True(Cpf.TryParse(ValidCpfDigits, out Cpf raw));
        Assert.Equal(cpf, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("11144477736")]
    [InlineData("111444777355")]
    [InlineData("111,444,777,35")]
    [InlineData("111.111.111-11")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Cpf.TryParse(value, out Cpf cpf));
        Assert.Equal(default, cpf);
        Assert.Equal("000.000.000-00", cpf.ToString());
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
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        string expected = format is "N" or "n" ? ValidCpfDigits : ValidCpfFormatted;
        Assert.Equal(expected, cpf.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        Assert.Throws<FormatException>(() => cpf.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        Span<char> buffer = stackalloc char[14];
        Assert.True(cpf.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(14, written);
        Assert.Equal(ValidCpfFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        Cpf cpf = Cpf.Parse(ValidCpfFormatted);
        Span<char> buffer = stackalloc char[11];
        Assert.True(cpf.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(11, written);
        Assert.Equal(ValidCpfDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        char[] buffer = new char[14];
        buffer.AsSpan().Fill('x');
        Assert.False(cpf.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 14), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        char[] buffer = new char[11];
        buffer.AsSpan().Fill('x');
        Assert.False(cpf.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 11), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Cpf cpf = Cpf.Parse(ValidCpfDigits);
        Assert.Throws<FormatException>(() => cpf.TryFormat(stackalloc char[14], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Cpf expected = Cpf.Parse(ValidCpfDigits);
        Cpf parsed = ParsableHelpers.ParseViaSpanParsable<Cpf>(ValidCpfFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Cpf>(ValidCpfDigits.AsSpan(), null, out Cpf result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Cpf expected = Cpf.Parse(ValidCpfDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Cpf>(ValidCpfFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Cpf>(ValidCpfDigits, null, out Cpf result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Cpf>(null, null, out Cpf result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Cpf>("12345678901".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Digits()
    {
        Cpf a = Cpf.Parse("11144477735");
        Cpf b = Cpf.Parse("111.444.777-35");
        Cpf c = Cpf.Parse("52998224725");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<Cpf>)a).Equals(b));
        Assert.False(((IEquatable<Cpf>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        Cpf original = Cpf.Parse("529.982.247-25");
        Cpf roundTripped = Cpf.Parse(original.ToString());
        Assert.Equal(original, roundTripped);
        Assert.Equal(original.ToString("N"), Cpf.Parse(original.ToString("N")).ToString("N"));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid()
    {
        Cpf @default = default;
        Assert.Equal("000.000.000-00", @default.ToString());
        Assert.Equal("00000000000", @default.ToString("N"));
        Assert.False(Cpf.IsValid(@default.ToString()));
        Assert.Equal(CpfFiscalRegion.Rs, @default.FiscalRegion);
    }

    [Theory]
    [InlineData("12345678062", CpfFiscalRegion.Rs, "RS")]
    [InlineData("12345678143", CpfFiscalRegion.DfGoMsMtTo, "DF, GO, MS, MT, TO")]
    [InlineData("12345678224", CpfFiscalRegion.AcAmApPaRoRr, "AC, AM, AP, PA, RO, RR")]
    [InlineData("12345678305", CpfFiscalRegion.CeMaPi, "CE, MA, PI")]
    [InlineData("12345678496", CpfFiscalRegion.AlPbPeRn, "AL, PB, PE, RN")]
    [InlineData("12345678577", CpfFiscalRegion.BaSe, "BA, SE")]
    [InlineData("12345678658", CpfFiscalRegion.Mg, "MG")]
    [InlineData("12345678739", CpfFiscalRegion.EsRj, "ES, RJ")]
    [InlineData("11144477735", CpfFiscalRegion.EsRj, "ES, RJ")]
    [InlineData("12345678810", CpfFiscalRegion.Sp, "SP")]
    [InlineData("12345678909", CpfFiscalRegion.PrSc, "PR, SC")]
    [InlineData("52998224725", CpfFiscalRegion.EsRj, "ES, RJ")]
    public void FiscalRegion_Maps_The_Ninth_Digit(string digits, CpfFiscalRegion region, string ufs)
    {
        Cpf cpf = Cpf.Parse(digits);
        Assert.Equal(region, cpf.FiscalRegion);
        Assert.Equal(ufs, cpf.FiscalRegion.Ufs());
    }

    [Fact]
    public void FiscalRegion_Extensions_Reject_Unknown_Values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CpfFiscalRegion)42).Ufs());
    }

    [Fact]
    public void Generate_Returns_Valid_Cpfs()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Cpf cpf = Cpf.Generate();
            string digits = cpf.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Cpf.IsValid(digits), $"generated {digits} must be valid");
            Assert.Equal(cpf, Cpf.Parse(digits));
            Assert.Equal(cpf, Cpf.Parse(cpf.ToString()));
        }
    }

    [Fact]
    public void Generate_Never_Produces_All_Same_Digit_Bases()
    {
        for (int i = 0; i < 1_000; i++)
        {
            string base9 = Cpf.Generate().ToString("N")[..9];
            Assert.False(base9.Distinct().Count() == 1, $"generated base {base9} must not be all-same");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        Cpf[] firstRun = Enumerable.Range(0, 100).Select(_ => Cpf.Generate(first)).ToArray();
        Cpf[] secondRun = Enumerable.Range(0, 100).Select(_ => Cpf.Generate(second)).ToArray();
        Cpf[] thirdRun = Enumerable.Range(0, 100).Select(_ => Cpf.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        Cpf cpf = Cpf.Generate();
        Assert.True(Cpf.IsValid(cpf.ToString("N")));
        Cpf also = Cpf.Generate(rng: null);
        Assert.True(Cpf.IsValid(also.ToString("N")));
    }

    [Fact]
    public void Generate_Covers_All_Fiscal_Regions()
    {
        var regions = new HashSet<CpfFiscalRegion>();
        for (int i = 0; i < 5_000; i++)
        {
            regions.Add(Cpf.Generate().FiscalRegion);
        }
        Assert.True(regions.Count >= 9, $"expected at least 9 distinct regions, got {regions.Count}");
    }
}
