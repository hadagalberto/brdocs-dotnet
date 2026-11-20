using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class PlacaTests
{
    private const string ValidLegacyFormatted = "ABC-1234";
    private const string ValidLegacyChars = "ABC1234";
    private const string ValidMercosulChars = "ABC1D23";

    [Theory]
    [InlineData("ABC-1234")]      // legacy national format, LLLNNNN
    [InlineData("ABC1234")]
    [InlineData("abc-1234")]      // lowercase input is normalized
    [InlineData("abc1234")]
    [InlineData("zzz9999")]
    [InlineData("AAA-0000")]
    [InlineData("AAA0000")]
    [InlineData("FGH5T44")]      // hand-made Mercosul plate
    [InlineData("fgh5t44")]
    [InlineData("ABC1D23")]      // brazilian-utils valid Mercosul vector
    [InlineData("abc1d23")]
    [InlineData("BRA0A99")]
    [InlineData("ABC1K23")]      // natively issued Mercosul plates use letters beyond J
    [InlineData("ABC1Z99")]
    public void IsValid_Returns_True_For_Both_Formats(string value)
    {
        Assert.True(Placa.IsValid(value));
    }

    [Theory]
    [InlineData(" ABC-1234 ")]
    [InlineData("A B C 1 2 3 4")]
    [InlineData("a.b.c.1.2.3.4")]
    [InlineData("A-B-C-1-2-3-4")]
    [InlineData("A/B/C/1/D/2/3")]
    [InlineData("  ABC1D23   ")]
    [InlineData("...ABC-1234...")]
    [InlineData("AB C1 D23")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(Placa.IsValid(value));
    }

    [Theory]
    [InlineData("ABC12D3")]      // brazilian-utils invalid vector: letter in the sixth position
    [InlineData("AB123CD")]
    [InlineData("AB-1234")]      // only two letters
    [InlineData("A1B2C34")]
    [InlineData("1AB2345")]      // digit in the first position
    [InlineData("ABC-123")]       // too short
    [InlineData("ABC123")]        // too short
    [InlineData("ABC-12345")]     // too long
    [InlineData("ABC12345")]
    [InlineData("ABC-12D4")]      // letter in the seventh position of a legacy-looking plate
    [InlineData("ABCD123")]      // letter in the fourth position
    [InlineData("ABC123D")]      // letter in the last position
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ABC_1234")]
    [InlineData("ABC:1234")]
    [InlineData("(ABC) 1234")]
    [InlineData("ABC123a")]      // lowercase passes normalization, but the pattern is LLLNNNL
    [InlineData("ÁBC1234")]      // non-ASCII letter
    [InlineData("ABÇ1234")]
    [InlineData("ＡＢＣ1234")]    // fullwidth letters
    [InlineData("ABC123４")]      // fullwidth digit
    [InlineData("ABC-1234\n")]
    [InlineData("\tABC-1234")]
    public void IsValid_Returns_False_For_Invalid_Texts(string value)
    {
        Assert.False(Placa.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Placa.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("ABC-1234", "ABC-1234", "ABC1234", true, false)]
    [InlineData("ABC1234", "ABC-1234", "ABC1234", true, false)]
    [InlineData("abc 1234", "ABC-1234", "ABC1234", true, false)]
    [InlineData("ABC1D23", "ABC1D23", "ABC1D23", false, true)]
    [InlineData("abc1d23", "ABC1D23", "ABC1D23", false, true)]
    [InlineData("ABC1K23", "ABC1K23", "ABC1K23", false, true)]
    public void Parse_Returns_The_Placa_For_Valid_Input(
        string value, string expectedFormatted, string expectedChars, bool expectedLegacy, bool expectedMercosul)
    {
        Placa placa = Placa.Parse(value);
        Assert.Equal(expectedFormatted, placa.ToString());
        Assert.Equal(expectedChars, placa.ToString("N"));
        Assert.Equal(7, placa.ToString("N").Length);
        Assert.Equal(expectedLegacy, placa.IsLegacy);
        Assert.Equal(expectedMercosul, placa.IsMercosul);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Placa placa = Placa.Parse(ValidLegacyChars);
            Assert.Equal(ValidLegacyFormatted, placa.ToString());
            Assert.Equal(ValidLegacyChars, placa.ToString("N"));
            Assert.Equal(ValidLegacyChars, ((IFormattable)placa).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidLegacyFormatted, ((IFormattable)placa).ToString("G", CultureInfo.InvariantCulture));
            Placa mercosul = Placa.Parse(ValidMercosulChars);
            Assert.Equal(ValidMercosulChars, mercosul.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Placa.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a plate")]
    [InlineData("ABC12D3")]
    [InlineData("AB1234")]
    [InlineData("ABC123")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Placa.Parse(value));
        Assert.Contains("plate", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "ABC1234" + new string('X', 100);
        FormatException exception = Assert.Throws<FormatException>(() => Placa.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 120, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Placa()
    {
        Assert.True(Placa.TryParse(ValidLegacyFormatted, out Placa placa));
        Assert.Equal(ValidLegacyChars, placa.ToString("N"));
        Assert.True(Placa.TryParse(ValidLegacyChars, out Placa raw));
        Assert.Equal(placa, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC12D3")]
    [InlineData("ABC12345")]
    [InlineData("ABC_1234")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Placa.TryParse(value, out Placa placa));
        Assert.Equal(default, placa);
        Assert.Equal("000-0000", placa.ToString());
        Assert.True(placa.IsLegacy, "the all-zero default is a structurally valid legacy plate");
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
        Placa legacy = Placa.Parse(ValidLegacyChars);
        Placa mercosul = Placa.Parse(ValidMercosulChars);
        Assert.Equal(format is "N" or "n" ? ValidLegacyChars : ValidLegacyFormatted, legacy.ToString(format, CultureInfo.CurrentCulture));
        Assert.Equal(ValidMercosulChars, mercosul.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("GG")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Placa placa = Placa.Parse(ValidLegacyChars);
        Assert.Throws<FormatException>(() => placa.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Legacy_Form()
    {
        Placa placa = Placa.Parse(ValidLegacyChars);
        Span<char> buffer = stackalloc char[8];
        Assert.True(placa.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(8, written);
        Assert.Equal(ValidLegacyFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Mercosul_Form()
    {
        Placa placa = Placa.Parse(ValidMercosulChars);
        Span<char> buffer = stackalloc char[7];
        Assert.True(placa.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(7, written);
        Assert.Equal(ValidMercosulChars, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Chars_Only()
    {
        Placa legacy = Placa.Parse(ValidLegacyFormatted);
        Span<char> legacyBuffer = stackalloc char[7];
        Assert.True(legacy.TryFormat(legacyBuffer, out int legacyWritten, "N", null));
        Assert.Equal(7, legacyWritten);
        Assert.Equal(ValidLegacyChars, new string(legacyBuffer));

        Placa mercosul = Placa.Parse(ValidMercosulChars);
        Span<char> mercosulBuffer = stackalloc char[7];
        Assert.True(mercosul.TryFormat(mercosulBuffer, out int mercosulWritten, "N", null));
        Assert.Equal(7, mercosulWritten);
        Assert.Equal(ValidMercosulChars, new string(mercosulBuffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void TryFormat_Returns_False_When_The_Legacy_Destination_Is_Too_Small(int size)
    {
        Placa placa = Placa.Parse(ValidLegacyChars);
        char[] buffer = new char[8];
        buffer.AsSpan().Fill('x');
        Assert.False(placa.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 8), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void TryFormat_Returns_False_When_The_Mercosul_Destination_Is_Too_Small(int size)
    {
        Placa placa = Placa.Parse(ValidMercosulChars);
        char[] buffer = new char[7];
        buffer.AsSpan().Fill('x');
        Assert.False(placa.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 7), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Placa placa = Placa.Parse(ValidLegacyChars);
        Assert.Throws<FormatException>(() => placa.TryFormat(stackalloc char[8], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Placa expected = Placa.Parse(ValidLegacyChars);
        Placa parsed = ParsableHelpers.ParseViaSpanParsable<Placa>(ValidLegacyFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Placa>(ValidMercosulChars.AsSpan(), null, out Placa result));
        Assert.Equal(Placa.Parse(ValidMercosulChars), result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Placa expected = Placa.Parse(ValidLegacyChars);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Placa>(ValidLegacyFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Placa>(ValidMercosulChars, null, out Placa result));
        Assert.Equal(Placa.Parse(ValidMercosulChars), result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Placa>(null, null, out Placa result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Placa>("ABC12D3".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Characters()
    {
        Placa a = Placa.Parse("ABC1234");
        Placa b = Placa.Parse("abc-1234");
        Placa c = Placa.Parse("ABC1D23");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<Placa>)a).Equals(b));
        Assert.False(((IEquatable<Placa>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        Placa original = Placa.Parse("zzz-9999");
        Assert.Equal("ZZZ-9999", original.ToString());
        Assert.Equal(original, Placa.Parse(original.ToString()));
        Assert.Equal(original.ToString("N"), Placa.Parse(original.ToString("N")).ToString("N"));
        Placa mercosul = Placa.Parse("abc1d23");
        Assert.Equal("ABC1D23", mercosul.ToString());
        Assert.Equal(mercosul, Placa.Parse(mercosul.ToString()));
    }

    // The Resolução CONTRAN nº 969/2022, Anexo II, item 2 conversion: the fifth character
    // (the second digit of the legacy group) becomes the letter A–J for the digits 0–9.

    [Theory]
    [InlineData("AAA1023", "AAA1A23")]  // 0 → A
    [InlineData("AAA1123", "AAA1B23")]  // 1 → B
    [InlineData("ABC1234", "ABC1C34")]  // 2 → C (the brazilian-utils worked example)
    [InlineData("AAA1323", "AAA1D23")]  // 3 → D
    [InlineData("AAA1423", "AAA1E23")]  // 4 → E
    [InlineData("AAA1523", "AAA1F23")]  // 5 → F
    [InlineData("AAA1623", "AAA1G23")]  // 6 → G
    [InlineData("AAA1723", "AAA1H23")]  // 7 → H
    [InlineData("AAA1823", "AAA1I23")]  // 8 → I
    [InlineData("AAA9999", "AAA9J99")]  // 9 → J
    [InlineData("AAA0000", "AAA0A00")]  // 0 → A over zeros
    [InlineData("abc-1234", "ABC1C34")] // lowercase and punctuation on the way in
    public void ToMercosul_Converts_The_Fifth_Character_To_A_Letter(string legacy, string expected)
    {
        Placa placa = Placa.Parse(legacy);
        Placa mercosul = placa.ToMercosul();
        Assert.Equal(expected, mercosul.ToString());
        Assert.True(mercosul.IsMercosul);
        Assert.True(Placa.IsValid(mercosul.ToString()));
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("ABC1K23")]
    [InlineData("BRA0A99")]
    public void ToMercosul_Returns_The_Same_Plate_When_Already_Mercosul(string value)
    {
        Placa placa = Placa.Parse(value);
        Placa converted = placa.ToMercosul();
        Assert.Equal(placa, converted);
        Assert.Equal(value, converted.ToString());
    }

    [Fact]
    public void ToMercosul_Is_Idempotent()
    {
        Placa legacy = Placa.Parse("ABC1234");
        Placa once = legacy.ToMercosul();
        Placa twice = once.ToMercosul();
        Assert.Equal(once, twice);
        Assert.Equal("ABC1C34", twice.ToString());
    }

    [Fact]
    public void ToMercosul_Preserves_The_Other_Characters()
    {
        Placa converted = Placa.Parse("xyz-7654").ToMercosul();
        Assert.Equal("XYZ7G54", converted.ToString());
    }

    [Fact]
    public void Generate_Returns_Valid_Mercosul_Plates()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Placa placa = Placa.Generate();
            string chars = placa.ToString("N");
            Assert.Equal(7, chars.Length);
            Assert.True(Placa.IsValid(chars), $"generated {chars} must be valid");
            Assert.True(placa.IsMercosul);
            Assert.Equal(placa, Placa.Parse(chars));
            Assert.Equal(placa, Placa.Parse(placa.ToString()));
            Assert.Equal(placa, Placa.Parse(placa.ToMercosul().ToString()));
        }
    }

    [Fact]
    public void GenerateLegacy_Returns_Valid_Legacy_Plates()
    {
        for (int i = 0; i < 1_000; i++)
        {
            Placa placa = Placa.GenerateLegacy();
            string formatted = placa.ToString();
            Assert.Equal(8, formatted.Length);
            Assert.Equal('-', formatted[3]);
            Assert.True(Placa.IsValid(formatted), $"generated {formatted} must be valid");
            Assert.True(placa.IsLegacy);
            Assert.Equal(placa, Placa.Parse(formatted));
        }
    }

    [Fact]
    public void Generate_Covers_All_Letters_And_Digits_Over_Many_Samples()
    {
        var letters = new HashSet<char>();
        var digits = new HashSet<char>();
        for (int i = 0; i < 3_000; i++)
        {
            foreach (char c in Placa.Generate(new Random(i)).ToString("N"))
            {
                if (char.IsAsciiDigit(c))
                {
                    digits.Add(c);
                }
                else
                {
                    letters.Add(c);
                }
            }
        }
        for (char c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(letters.Contains(c), $"letter {c} must be generated");
        }
        for (char c = '0'; c <= '9'; c++)
        {
            Assert.True(digits.Contains(c), $"digit {c} must be generated");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        Placa[] firstRun = Enumerable.Range(0, 100).Select(_ => Placa.Generate(first)).ToArray();
        Placa[] secondRun = Enumerable.Range(0, 100).Select(_ => Placa.Generate(second)).ToArray();
        Placa[] thirdRun = Enumerable.Range(0, 100).Select(_ => Placa.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        Placa placa = Placa.Generate();
        Assert.True(Placa.IsValid(placa.ToString("N")));
        Placa also = Placa.Generate(rng: null);
        Assert.True(Placa.IsValid(also.ToString("N")));
    }
}
