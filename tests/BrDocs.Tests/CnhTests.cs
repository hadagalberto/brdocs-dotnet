using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class CnhTests
{
    private const string ValidCnhFormatted = "026503064-61";
    private const string ValidCnhDigits = "02650306461";

    [Theory]
    [InlineData("026503064-61")]       // brazilian-utils worked example
    [InlineData("02650306461")]
    [InlineData("000000001-19")]      // brazilian-utils valid vector
    [InlineData("00000000119")]
    [InlineData("048814725-05")]      // hand-computed: sums 187/203, dv 0/5
    [InlineData("04881472505")]
    [InlineData("012345678-07")]      // decrement case: r1 = 10, dv2 = 9 - 2 = 7
    [InlineData("01234567807")]
    [InlineData("123456789-00")]      // dv2 remainder 10 case: r1 = 0, r2 = 10 → 0
    [InlineData("12345678900")]
    [InlineData("112344568-09")]      // decrement + wrap: r1 = 10, r2 = 0 - 2 → +11 = 9
    [InlineData("11234456809")]
    [InlineData("527988023-10")]      // hand-computed: sums 243/197, dv 1/0
    [InlineData("52798802310")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(Cnh.IsValid(value));
    }

    [Theory]
    [InlineData(" 026503064-61 ")]
    [InlineData("026 503 064 61")]
    [InlineData("0 2 6 5 0 3 0 6 4 6 1")]
    [InlineData("0.2.6.5.0.3.0.6.4.6.1")]
    [InlineData("0-2-6-5-0-3-0-6-4-6-1")]
    [InlineData("0/2/6/5/0/3/0/6/4/6/1")]
    [InlineData("  02650306461  ")]
    [InlineData("026.5030/64-61")]
    [InlineData(".-.026503064-61.-.")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(Cnh.IsValid(value));
    }

    [Theory]
    [InlineData("012345678-09")]      // the value a validator without the decrement rule accepts
    [InlineData("123456789-09")]      // the value the inverted-weights variant accepts
    [InlineData("112344568-00")]      // the value a validator without the negative wrap accepts
    [InlineData("026503064-60")]      // DV2 wrong
    [InlineData("026503064-51")]      // both DVs wrong
    [InlineData("000000001-18")]      // DV2 wrong
    [InlineData("000000001-09")]      // both wrong
    [InlineData("000000001-29")]      // DV1 wrong
    [InlineData("048814725-15")]      // DV1 wrong
    [InlineData("048814725-04")]      // DV2 wrong
    [InlineData("527988023-11")]      // DV2 wrong
    [InlineData("926503064-61")]      // base changed, DVs kept
    [InlineData("026503065-61")]      // base changed, DVs kept
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(Cnh.IsValid(value));
    }

    [Theory]
    [InlineData("000000000-00")]
    [InlineData("111111111-11")]
    [InlineData("222222222-22")]
    [InlineData("333333333-33")]
    [InlineData("444444444-44")]
    [InlineData("555555555-55")]
    [InlineData("666666666-66")]
    [InlineData("777777777-77")]
    [InlineData("888888888-88")]
    [InlineData("999999999-99")]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    public void IsValid_Rejects_All_Same_Digit_Sequences(string value)
    {
        // Every all-same sequence is self-consistent under the CNH arithmetic (the base sum
        // 45d gives remainder d for both DVs), so the rejection cannot rely on the DVs.
        Assert.False(Cnh.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData(" - / ")]
    [InlineData("1")]
    [InlineData("026503064")]
    [InlineData("0265030646")]
    [InlineData("026503064611")]
    [InlineData("0265030646111")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(Cnh.IsValid(value));
    }

    [Theory]
    [InlineData("026503064,61")]
    [InlineData("026503064_61")]
    [InlineData("026503064:61")]
    [InlineData("(026) 503-064-61")]
    [InlineData("026503064a61")]
    [InlineData("026503064-6a")]
    [InlineData("026503064-6X")]
    [InlineData("abc")]
    [InlineData("026503064–61")]     // en dash U+2013, not '-'
    [InlineData("٠٢٦٥٠٣٠٦٤٦١")]        // Arabic-Indic digits
    [InlineData("02650306461\n")]
    [InlineData("\t02650306461")]
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(Cnh.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Cnh.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("026503064-61", "026503064-61", "02650306461")]
    [InlineData("02650306461", "026503064-61", "02650306461")]
    [InlineData(" 026 503 064 61 ", "026503064-61", "02650306461")]
    [InlineData("02.65030.646-1", "026503064-61", "02650306461")]
    [InlineData("000000001-19", "000000001-19", "00000000119")]
    [InlineData("012345678-07", "012345678-07", "01234567807")]
    public void Parse_Returns_The_Cnh_For_Valid_Input(string value, string expectedFormatted, string expectedDigits)
    {
        Cnh cnh = Cnh.Parse(value);
        Assert.Equal(expectedFormatted, cnh.ToString());
        Assert.Equal(expectedDigits, cnh.ToString("N"));
        Assert.Equal(11, cnh.ToString("N").Length);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Cnh cnh = Cnh.Parse(ValidCnhDigits);
            Assert.Equal(ValidCnhFormatted, cnh.ToString());
            Assert.Equal(ValidCnhDigits, cnh.ToString("N"));
            Assert.Equal(ValidCnhDigits, ((IFormattable)cnh).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidCnhFormatted, ((IFormattable)cnh).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Cnh.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a cnh")]
    [InlineData("02650306460")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Cnh.Parse(value));
        Assert.Contains("CNH", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "02650306461" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Cnh.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 120, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Cnh()
    {
        Assert.True(Cnh.TryParse(ValidCnhFormatted, out Cnh cnh));
        Assert.Equal(ValidCnhDigits, cnh.ToString("N"));
        Assert.True(Cnh.TryParse(ValidCnhDigits, out Cnh raw));
        Assert.Equal(cnh, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("02650306460")]
    [InlineData("026503064611")]
    [InlineData("026,503064,61")]
    [InlineData("11111111111")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Cnh.TryParse(value, out Cnh cnh));
        Assert.Equal(default, cnh);
        Assert.Equal("000000000-00", cnh.ToString());
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
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        string expected = format is "N" or "n" ? ValidCnhDigits : ValidCnhFormatted;
        Assert.Equal(expected, cnh.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        Assert.Throws<FormatException>(() => cnh.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        Span<char> buffer = stackalloc char[12];
        Assert.True(cnh.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(12, written);
        Assert.Equal(ValidCnhFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        Cnh cnh = Cnh.Parse(ValidCnhFormatted);
        Span<char> buffer = stackalloc char[11];
        Assert.True(cnh.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(11, written);
        Assert.Equal(ValidCnhDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(11)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        char[] buffer = new char[12];
        buffer.AsSpan().Fill('x');
        Assert.False(cnh.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 12), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        char[] buffer = new char[11];
        buffer.AsSpan().Fill('x');
        Assert.False(cnh.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 11), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Cnh cnh = Cnh.Parse(ValidCnhDigits);
        Assert.Throws<FormatException>(() => cnh.TryFormat(stackalloc char[12], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Cnh expected = Cnh.Parse(ValidCnhDigits);
        Cnh parsed = ParsableHelpers.ParseViaSpanParsable<Cnh>(ValidCnhFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Cnh>(ValidCnhDigits.AsSpan(), null, out Cnh result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Cnh expected = Cnh.Parse(ValidCnhDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Cnh>(ValidCnhFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Cnh>(ValidCnhDigits, null, out Cnh result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Cnh>(null, null, out Cnh result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Cnh>("12345678901".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Digits()
    {
        Cnh a = Cnh.Parse("02650306461");
        Cnh b = Cnh.Parse("026503064-61");
        Cnh c = Cnh.Parse("00000000119");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<Cnh>)a).Equals(b));
        Assert.False(((IEquatable<Cnh>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        Cnh original = Cnh.Parse("026503064-61");
        Cnh roundTripped = Cnh.Parse(original.ToString());
        Assert.Equal(original, roundTripped);
        Assert.Equal(original.ToString("N"), Cnh.Parse(original.ToString("N")).ToString("N"));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid()
    {
        Cnh @default = default;
        Assert.Equal("000000000-00", @default.ToString());
        Assert.Equal("00000000000", @default.ToString("N"));
        Assert.False(Cnh.IsValid(@default.ToString()));
    }

    [Fact]
    public void Generate_Returns_Valid_Cnhs()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Cnh cnh = Cnh.Generate();
            string digits = cnh.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Cnh.IsValid(digits), $"generated {digits} must be valid");
            Assert.Equal(cnh, Cnh.Parse(digits));
            Assert.Equal(cnh, Cnh.Parse(cnh.ToString()));
        }
    }

    [Fact]
    public void Generate_Covers_The_Decrement_Rule_Branch()
    {
        // The remainder-1 == 10 branch (DV1 forced to 0, DV2 computed with the compensation
        // of 2) must be exercised: over many bases it appears roughly once in eleven.
        int hits = 0;
        for (int i = 0; i < 2_000; i++)
        {
            Cnh cnh = Cnh.Generate(new Random(i));
            if (Oracles.IsValidCnh(cnh.ToString("N")))
            {
                // Recompute the first remainder through the oracle shape: the generated CNH
                // is valid by construction, so only count the ones whose DV1 digit is 0 and
                // whose base yields remainder 10 under the descending weights.
                string digits = cnh.ToString("N");
                int sum1 = digits.Take(9).Select((d, i2) => (d - '0') * (9 - i2)).Sum();
                if (sum1 % 11 == 10)
                {
                    hits++;
                    Assert.Equal('0', digits[9]);
                }
            }
        }
        Assert.True(hits > 0, "the decrement rule branch must be reached by Generate");
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        Cnh[] firstRun = Enumerable.Range(0, 100).Select(_ => Cnh.Generate(first)).ToArray();
        Cnh[] secondRun = Enumerable.Range(0, 100).Select(_ => Cnh.Generate(second)).ToArray();
        Cnh[] thirdRun = Enumerable.Range(0, 100).Select(_ => Cnh.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        Cnh cnh = Cnh.Generate();
        Assert.True(Cnh.IsValid(cnh.ToString("N")));
        Cnh also = Cnh.Generate(rng: null);
        Assert.True(Cnh.IsValid(also.ToString("N")));
    }
}
