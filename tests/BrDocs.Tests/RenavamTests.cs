using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class RenavamTests
{
    private const string ValidRenavamDigits = "00639884962";
    private const string ValidRenavamLegacy = "639884962";

    [Theory]
    [InlineData("00639884962")]      // brazilian-utils valid vector, new 11-digit format
    [InlineData("639884962")]         // the same number in the legacy 9-digit format
    [InlineData("0063988.4962")]      // brazilian-utils valid vector with a mask char
    [InlineData("12345678900")]       // brazilian-utils generator example: sum 231, resto 0, dv 0
    [InlineData("26827649960")]       // netconsultas worked example: sum 286, (286×10)%11 = 0
    [InlineData("08794266580")]       // brazilian-utils valid vector: sum 276, resto 1, dv 0
    [InlineData("63988496222")]      // hand-computed: base 6398849622, sum 328, resto 9, dv 2
    [InlineData("123456789-00")]      // 12345678900 with the DV split by a dash
    [InlineData("006 3988 4962")]
    [InlineData("00.639.884.962")]
    [InlineData(" 00639884962 ")]
    [InlineData("0063/9884/96-2")]
    [InlineData("0.0.6.3.9.8.8.4.9.6.2")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(Renavam.IsValid(value));
    }

    [Theory]
    [InlineData("12345678901")]      // brazilian-utils invalid vector: the DV is 0, not 1
    [InlineData("639884963")]        // legacy form with a wrong DV
    [InlineData("00639884961")]      // 11-digit form with a wrong DV
    [InlineData("00639884963")]
    [InlineData("26827649961")]
    [InlineData("08794266581")]
    [InlineData("10639884962")]      // base changed, DV kept
    [InlineData("00639884972")]
    [InlineData("12345678910")]
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(Renavam.IsValid(value));
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("0000000000")]
    [InlineData("11111111111")]
    [InlineData("22222222222")]
    [InlineData("33333333333")]
    [InlineData("44444444444")]
    [InlineData("55555555555")]
    [InlineData("66666666666")]
    [InlineData("77777777777")]
    [InlineData("88888888888")]
    [InlineData("99999999999")]
    public void IsValid_Rejects_All_Same_Digit_Sequences(string value)
    {
        // The all-zero 11-digit value satisfies the DV arithmetic (sum 0, DV 0), so the
        // rejection cannot rely on the DV; the whole all-same class is refused.
        Assert.False(Renavam.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData(" - / ")]
    [InlineData("1")]
    [InlineData("63988496")]         // 8 digits: neither the 9-digit legacy nor the 11-digit form
    [InlineData("6398849")]
    [InlineData("6398849622")]       // 10 digits: no such length exists
    [InlineData("006398849621")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(Renavam.IsValid(value));
    }

    [Theory]
    [InlineData("00639884,962")]
    [InlineData("00639884_962")]
    [InlineData("00639884:962")]
    [InlineData("renavam00639884962")]
    [InlineData("0063988496a")]
    [InlineData("0063988496X")]
    [InlineData("00639884–962")]     // en dash U+2013, not '-'
    [InlineData("٠٠٦٣٩٨٨٤٩٦٢")]        // Arabic-Indic digits
    [InlineData("00639884962\n")]
    [InlineData("\t00639884962")]
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(Renavam.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Renavam.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("00639884962", "00639884962")]
    [InlineData("639884962", "00639884962")]
    [InlineData("0063988.4962", "00639884962")]
    [InlineData(" 006 3988 4962 ", "00639884962")]
    [InlineData("12345678900", "12345678900")]
    [InlineData("26827649960", "26827649960")]
    public void Parse_Canonicalizes_To_The_Eleven_Digit_Form(string value, string expectedDigits)
    {
        Renavam renavam = Renavam.Parse(value);
        Assert.Equal(expectedDigits, renavam.ToString());
        Assert.Equal(expectedDigits, renavam.ToString("N"));
        Assert.Equal(11, renavam.ToString("N").Length);
    }

    [Fact]
    public void Parse_Legacy_And_Padded_Forms_Are_The_Same_Value()
    {
        Renavam legacy = Renavam.Parse(ValidRenavamLegacy);
        Renavam padded = Renavam.Parse(ValidRenavamDigits);
        Assert.Equal(legacy, padded);
        Assert.Equal(ValidRenavamDigits, legacy.ToString());
        Assert.True(Renavam.TryParse("639884962", out Renavam raw));
        Assert.Equal(padded, raw);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Renavam renavam = Renavam.Parse(ValidRenavamLegacy);
            Assert.Equal(ValidRenavamDigits, renavam.ToString());
            Assert.Equal(ValidRenavamDigits, renavam.ToString("N"));
            Assert.Equal(ValidRenavamDigits, ((IFormattable)renavam).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidRenavamDigits, ((IFormattable)renavam).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Renavam.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a renavam")]
    [InlineData("12345678901")]
    [InlineData("00000000000")]
    [InlineData("63988496")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Renavam.Parse(value));
        Assert.Contains("RENAVAM", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "00639884962" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Renavam.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 120, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Renavam()
    {
        Assert.True(Renavam.TryParse(ValidRenavamDigits, out Renavam renavam));
        Assert.Equal(ValidRenavamDigits, renavam.ToString("N"));
        Assert.True(Renavam.TryParse(ValidRenavamLegacy, out Renavam raw));
        Assert.Equal(renavam, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678901")]
    [InlineData("006398849621")]
    [InlineData("006,3988,4962")]
    [InlineData("00000000000")]
    [InlineData("63988496")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Renavam.TryParse(value, out Renavam renavam));
        Assert.Equal(default, renavam);
        Assert.Equal("00000000000", renavam.ToString());
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
        // RENAVAM has no official punctuation: both formats are the 11 digits.
        Renavam renavam = Renavam.Parse(ValidRenavamDigits);
        Assert.Equal(ValidRenavamDigits, renavam.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Renavam renavam = Renavam.Parse(ValidRenavamDigits);
        Assert.Throws<FormatException>(() => renavam.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Eleven_Digits()
    {
        Renavam renavam = Renavam.Parse(ValidRenavamLegacy);
        Span<char> buffer = stackalloc char[11];
        Assert.True(renavam.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(11, written);
        Assert.Equal(ValidRenavamDigits, new string(buffer));
        Assert.True(renavam.TryFormat(buffer, out int writtenN, "N", null));
        Assert.Equal(11, writtenN);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void TryFormat_Returns_False_When_The_Destination_Is_Too_Small(int size)
    {
        Renavam renavam = Renavam.Parse(ValidRenavamDigits);
        char[] buffer = new char[11];
        buffer.AsSpan().Fill('x');
        Assert.False(renavam.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 11), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Renavam renavam = Renavam.Parse(ValidRenavamDigits);
        Assert.Throws<FormatException>(() => renavam.TryFormat(stackalloc char[11], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Renavam expected = Renavam.Parse(ValidRenavamDigits);
        Renavam parsed = ParsableHelpers.ParseViaSpanParsable<Renavam>(ValidRenavamDigits.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Renavam>(ValidRenavamLegacy.AsSpan(), null, out Renavam result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Renavam expected = Renavam.Parse(ValidRenavamDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Renavam>(ValidRenavamLegacy, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Renavam>(ValidRenavamDigits, null, out Renavam result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Renavam>(null, null, out Renavam result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Renavam>("12345678901".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Canonical_Digits()
    {
        Renavam a = Renavam.Parse("639884962");
        Renavam b = Renavam.Parse("00639884962");
        Renavam c = Renavam.Parse("12345678900");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<Renavam>)a).Equals(b));
        Assert.False(((IEquatable<Renavam>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        Renavam original = Renavam.Parse(ValidRenavamLegacy);
        Renavam roundTripped = Renavam.Parse(original.ToString());
        Assert.Equal(original, roundTripped);
        Assert.Equal(original.ToString("N"), Renavam.Parse(original.ToString("N")).ToString("N"));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid()
    {
        Renavam @default = default;
        Assert.Equal("00000000000", @default.ToString());
        Assert.Equal("00000000000", @default.ToString("N"));
        Assert.False(Renavam.IsValid(@default.ToString()));
    }

    [Fact]
    public void Generate_Returns_Valid_Renavams()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Renavam renavam = Renavam.Generate();
            string digits = renavam.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Renavam.IsValid(digits), $"generated {digits} must be valid");
            Assert.Equal(renavam, Renavam.Parse(digits));
        }
    }

    [Fact]
    public void Generate_Never_Produces_All_Same_Digit_Bases()
    {
        for (int i = 0; i < 1_000; i++)
        {
            string base10 = Renavam.Generate().ToString("N")[..10];
            Assert.False(base10.Distinct().Count() == 1, $"generated base {base10} must not be all-same");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        Renavam[] firstRun = Enumerable.Range(0, 100).Select(_ => Renavam.Generate(first)).ToArray();
        Renavam[] secondRun = Enumerable.Range(0, 100).Select(_ => Renavam.Generate(second)).ToArray();
        Renavam[] thirdRun = Enumerable.Range(0, 100).Select(_ => Renavam.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        Renavam renavam = Renavam.Generate();
        Assert.True(Renavam.IsValid(renavam.ToString("N")));
        Renavam also = Renavam.Generate(rng: null);
        Assert.True(Renavam.IsValid(also.ToString("N")));
    }
}
