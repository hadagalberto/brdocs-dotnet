using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class PisTests
{
    private const string ValidPisFormatted = "120.70458.82-4";
    private const string ValidPisDigits = "12070458824";

    [Theory]
    [InlineData("120.70458.82-4")]       // geradornv worked example
    [InlineData("12070458824")]
    [InlineData("120.5641.284-7")]       // brazilian-utils valid vector
    [InlineData("12056412847")]
    [InlineData("123.45678.90-0")]        // base 1234567890, sum 231, resto 0
    [InlineData("12345678900")]
    [InlineData("100.27230.88-8")]        // cadcobol worked example
    [InlineData("10027230888")]
    [InlineData("565.43219.87-4")]       // hand-computed: base 5654321987, resto 7, dv 4
    [InlineData("56543219874")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(Pis.IsValid(value));
    }

    [Theory]
    [InlineData(" 120.70458.82-4 ")]
    [InlineData("120 70458 82 4")]
    [InlineData("1 2 0 7 0 4 5 8 8 2 4")]
    [InlineData("1.2.0.7.0.4.5.8.8.2.4")]
    [InlineData("1-2-0-7-0-4-5-8-8-2-4")]
    [InlineData("1/2/0/7/0/4/5/8/8/2/4")]
    [InlineData("  12070458824  ")]
    [InlineData("12.0704/58.82/-4")]
    [InlineData(".-.120.70458.82-4.-.")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(Pis.IsValid(value));
    }

    [Theory]
    [InlineData("12070458825")]       // DV wrong (5 instead of 4)
    [InlineData("12070458814")]       // DV wrong
    [InlineData("12070458834")]       // DV wrong
    [InlineData("12070458804")]       // DV wrong
    [InlineData("12070458844")]       // DV wrong
    [InlineData("12056412547")]       // brazilian-utils invalid vector
    [InlineData("12345678901")]       // the DV of base 1234567890 is 0, not 1
    [InlineData("10027230889")]       // DV wrong
    [InlineData("56543219875")]       // DV wrong
    [InlineData("22070458824")]       // base changed, DV kept
    [InlineData("02070458824")]
    [InlineData("12070458894")]       // base changed, DV kept
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(Pis.IsValid(value));
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
    [InlineData("000.00000.00-0")]
    [InlineData("111.11111.11-1")]
    public void IsValid_Rejects_All_Same_Digit_Sequences(string value)
    {
        // Only the all-zero sequence satisfies the DV arithmetic, but the whole class is
        // rejected as reserved, like the CPF/CNPJ sequences.
        Assert.False(Pis.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData(" - / ")]
    [InlineData("1")]
    [InlineData("1234567890")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    [InlineData("120704588244")]
    [InlineData("1207045882444")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(Pis.IsValid(value));
    }

    [Theory]
    [InlineData("120,70458,82,4")]
    [InlineData("120_70458_82_4")]
    [InlineData("120:70458:82:4")]
    [InlineData("(120) 70458-82-4")]
    [InlineData("120;70458;82;4")]
    [InlineData("1207045882a")]
    [InlineData("120.70458.82-4x")]
    [InlineData("abc")]
    [InlineData("120·70458·82·4")]    // middle dot U+00B7
    [InlineData("١٢٠٧٠٤٥٨٨٢٤")]        // Arabic-Indic digits
    [InlineData("12070458824\n")]
    [InlineData("\t12070458824")]
    [InlineData("120 70458\t82 4")]
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(Pis.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Pis.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("120.70458.82-4", "120.70458.82-4", "12070458824")]
    [InlineData("12070458824", "120.70458.82-4", "12070458824")]
    [InlineData(" 120 70458 82 4 ", "120.70458.82-4", "12070458824")]
    [InlineData("12.07045.882-4", "120.70458.82-4", "12070458824")]
    [InlineData("123.45678.90-0", "123.45678.90-0", "12345678900")]
    [InlineData("100.27230.88-8", "100.27230.88-8", "10027230888")]
    public void Parse_Returns_The_Pis_For_Valid_Input(string value, string expectedFormatted, string expectedDigits)
    {
        Pis pis = Pis.Parse(value);
        Assert.Equal(expectedFormatted, pis.ToString());
        Assert.Equal(expectedDigits, pis.ToString("N"));
        Assert.Equal(11, pis.ToString("N").Length);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Pis pis = Pis.Parse(ValidPisDigits);
            Assert.Equal(ValidPisFormatted, pis.ToString());
            Assert.Equal(ValidPisDigits, pis.ToString("N"));
            Assert.Equal(ValidPisDigits, ((IFormattable)pis).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidPisFormatted, ((IFormattable)pis).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Pis.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a pis")]
    [InlineData("12070458825")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Pis.Parse(value));
        Assert.Contains("PIS", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "12070458824" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Pis.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 120, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Pis()
    {
        Assert.True(Pis.TryParse(ValidPisFormatted, out Pis pis));
        Assert.Equal(ValidPisDigits, pis.ToString("N"));
        Assert.True(Pis.TryParse(ValidPisDigits, out Pis raw));
        Assert.Equal(pis, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12070458825")]
    [InlineData("120704588244")]
    [InlineData("120,70458,82,4")]
    [InlineData("11111111111")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Pis.TryParse(value, out Pis pis));
        Assert.Equal(default, pis);
        Assert.Equal("000.00000.00-0", pis.ToString());
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
        Pis pis = Pis.Parse(ValidPisDigits);
        string expected = format is "N" or "n" ? ValidPisDigits : ValidPisFormatted;
        Assert.Equal(expected, pis.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Pis pis = Pis.Parse(ValidPisDigits);
        Assert.Throws<FormatException>(() => pis.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        Pis pis = Pis.Parse(ValidPisDigits);
        Span<char> buffer = stackalloc char[14];
        Assert.True(pis.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(14, written);
        Assert.Equal(ValidPisFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        Pis pis = Pis.Parse(ValidPisFormatted);
        Span<char> buffer = stackalloc char[11];
        Assert.True(pis.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(11, written);
        Assert.Equal(ValidPisDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        Pis pis = Pis.Parse(ValidPisDigits);
        char[] buffer = new char[14];
        buffer.AsSpan().Fill('x');
        Assert.False(pis.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 14), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        Pis pis = Pis.Parse(ValidPisDigits);
        char[] buffer = new char[11];
        buffer.AsSpan().Fill('x');
        Assert.False(pis.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 11), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Pis pis = Pis.Parse(ValidPisDigits);
        Assert.Throws<FormatException>(() => pis.TryFormat(stackalloc char[14], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Pis expected = Pis.Parse(ValidPisDigits);
        Pis parsed = ParsableHelpers.ParseViaSpanParsable<Pis>(ValidPisFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Pis>(ValidPisDigits.AsSpan(), null, out Pis result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Pis expected = Pis.Parse(ValidPisDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Pis>(ValidPisFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Pis>(ValidPisDigits, null, out Pis result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Pis>(null, null, out Pis result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Pis>("12345678901".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Digits()
    {
        Pis a = Pis.Parse("12070458824");
        Pis b = Pis.Parse("120.70458.82-4");
        Pis c = Pis.Parse("12345678900");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<Pis>)a).Equals(b));
        Assert.False(((IEquatable<Pis>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        Pis original = Pis.Parse("120.70458.82-4");
        Pis roundTripped = Pis.Parse(original.ToString());
        Assert.Equal(original, roundTripped);
        Assert.Equal(original.ToString("N"), Pis.Parse(original.ToString("N")).ToString("N"));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid()
    {
        Pis @default = default;
        Assert.Equal("000.00000.00-0", @default.ToString());
        Assert.Equal("00000000000", @default.ToString("N"));
        Assert.False(Pis.IsValid(@default.ToString()));
    }

    [Fact]
    public void Generate_Returns_Valid_Pis()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Pis pis = Pis.Generate();
            string digits = pis.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Pis.IsValid(digits), $"generated {digits} must be valid");
            Assert.Equal(pis, Pis.Parse(digits));
            Assert.Equal(pis, Pis.Parse(pis.ToString()));
        }
    }

    [Fact]
    public void Generate_Never_Produces_All_Same_Digit_Bases()
    {
        for (int i = 0; i < 1_000; i++)
        {
            string base10 = Pis.Generate().ToString("N")[..10];
            Assert.False(base10.Distinct().Count() == 1, $"generated base {base10} must not be all-same");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        Pis[] firstRun = Enumerable.Range(0, 100).Select(_ => Pis.Generate(first)).ToArray();
        Pis[] secondRun = Enumerable.Range(0, 100).Select(_ => Pis.Generate(second)).ToArray();
        Pis[] thirdRun = Enumerable.Range(0, 100).Select(_ => Pis.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        Pis pis = Pis.Generate();
        Assert.True(Pis.IsValid(pis.ToString("N")));
        Pis also = Pis.Generate(rng: null);
        Assert.True(Pis.IsValid(also.ToString("N")));
    }
}
