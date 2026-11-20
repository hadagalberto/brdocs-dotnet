using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class CepTests
{
    private const string ValidCepFormatted = "01310-100";
    private const string ValidCepDigits = "01310100";

    [Theory]
    [InlineData("91906292")]
    [InlineData("91906-292")]
    [InlineData("00000000")]
    [InlineData("00000-000")]
    [InlineData("21749676")]
    [InlineData("21749-676")]
    [InlineData("41790070")]
    [InlineData("41790-070")]
    [InlineData("01310-100")]
    [InlineData("01310100")]
    [InlineData("92.500-000")]
    [InlineData("013 10 100")]
    [InlineData("01310.100")]
    [InlineData("01310/100")]
    [InlineData("0131/0100")]
    [InlineData("--0131 0100")]
    [InlineData("01310/100/")]
    [InlineData(" 01310-100 ")]
    [InlineData("01.310.100")]
    [InlineData("01 310 100")]
    public void IsValid_Returns_True_For_Eight_Digit_Texts(string value)
    {
        Assert.True(Cep.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("....")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("01310-10")]
    [InlineData("01310-1000")]
    [InlineData("013101000")]
    [InlineData("abc01310100")]
    [InlineData("01310-10a")]
    [InlineData("01310-1O0")]
    [InlineData("9250000A")]
    [InlineData("01310_100")]
    [InlineData("0131:0100")]
    [InlineData("CEP 01310-100")]
    [InlineData("01310–100")]       // en dash U+2013
    [InlineData("١٣١٠١٠٠")]          // Arabic-Indic digits
    [InlineData("\t01310-100")]
    [InlineData("01310-100\n")]
    public void IsValid_Returns_False_For_Invalid_Texts(string value)
    {
        Assert.False(Cep.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(Cep.IsValid((string?)null!));
    }

    [Theory]
    [InlineData("01310-100", "01310-100", "01310100")]
    [InlineData("01310100", "01310-100", "01310100")]
    [InlineData(" 013 10 100 ", "01310-100", "01310100")]
    [InlineData("01.310.100", "01310-100", "01310100")]
    public void Parse_Returns_The_Cep_For_Valid_Input(string value, string expectedFormatted, string expectedDigits)
    {
        Cep cep = Cep.Parse(value);
        Assert.Equal(expectedFormatted, cep.ToString());
        Assert.Equal(expectedDigits, cep.ToString("N"));
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Cep.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("01310-10a")]
    [InlineData("123456789")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Cep.Parse(value));
        Assert.Contains("CEP", exception.Message);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("01310-100")]
    [InlineData(" 0131/0100 ")]
    public void TryParse_Valid_Outputs_The_Cep(string value)
    {
        Assert.True(Cep.TryParse(value, out Cep cep));
        Assert.Equal(ValidCepDigits, cep.ToString("N"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("123456789")]
    [InlineData("abc")]
    [InlineData("01310_100")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Cep.TryParse(value, out Cep cep));
        Assert.Equal(default, cep);
        Assert.Equal("00000-000", cep.ToString());
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
        Cep cep = Cep.Parse(ValidCepDigits);
        string expected = format is "N" or "n" ? ValidCepDigits : ValidCepFormatted;
        Assert.Equal(expected, cep.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData("C")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Cep cep = Cep.Parse(ValidCepDigits);
        Assert.Throws<FormatException>(() => cep.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        Cep cep = Cep.Parse(ValidCepDigits);
        Span<char> buffer = stackalloc char[9];
        Assert.True(cep.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(9, written);
        Assert.Equal(ValidCepFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        Cep cep = Cep.Parse(ValidCepFormatted);
        Span<char> buffer = stackalloc char[8];
        Assert.True(cep.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(8, written);
        Assert.Equal(ValidCepDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        Cep cep = Cep.Parse(ValidCepDigits);
        char[] buffer = new char[9];
        buffer.AsSpan().Fill('x');
        Assert.False(cep.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 9), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        Cep cep = Cep.Parse(ValidCepDigits);
        char[] buffer = new char[8];
        buffer.AsSpan().Fill('x');
        Assert.False(cep.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 8), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Cep cep = Cep.Parse(ValidCepDigits);
        Assert.Throws<FormatException>(() => cep.TryFormat(stackalloc char[9], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Cep expected = Cep.Parse(ValidCepDigits);
        Cep parsed = ParsableHelpers.ParseViaSpanParsable<Cep>(ValidCepFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Cep>(ValidCepDigits.AsSpan(), null, out Cep result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Cep expected = Cep.Parse(ValidCepDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Cep>(ValidCepFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Cep>(ValidCepDigits, null, out Cep result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Cep>(null, null, out Cep result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Cep>("1234".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Digits()
    {
        Cep a = Cep.Parse("01310-100");
        Cep b = Cep.Parse("013 10 100");
        Cep c = Cep.Parse("99999-999");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(((IEquatable<Cep>)a).Equals(b));
        Assert.False(((IEquatable<Cep>)a).Equals(c));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_With_No_Uf()
    {
        Cep @default = default;
        Assert.Equal("00000-000", @default.ToString());
        Assert.Equal("00000000", @default.ToString("N"));
        Assert.Null(@default.Uf);
        Assert.True(Cep.IsValid(@default.ToString()));
    }

    // The Correios per-UF prefix ranges, asserting both ends of every range and the gaps.
    // Source: Correios "Busca Faixa de CEP" (per-state search), cross-checked with the
    // brazilian-utils CEP contract (SP single block 01000-19999; MT ends at 78899; 789xx unowned).
    [Theory]
    [InlineData("00000000", null)]   // below the first assigned prefix
    [InlineData("00999999", null)]   // end of the unassigned gap
    [InlineData("01000000", "SP")]   // SP: 01000-19999
    [InlineData("01001000", "SP")]   // first CEP of Brazil, Praça da Sé
    [InlineData("01310100", "SP")]
    [InlineData("10000000", "SP")]   // 10xxx is unused but still inside the SP block
    [InlineData("10999999", "SP")]
    [InlineData("19999999", "SP")]
    [InlineData("20000000", "RJ")]   // RJ: 20000-28999
    [InlineData("20040020", "RJ")]
    [InlineData("28999999", "RJ")]
    [InlineData("29000000", "ES")]   // ES: 29000-29999
    [InlineData("29056900", "ES")]
    [InlineData("29999999", "ES")]
    [InlineData("30000000", "MG")]   // MG: 30000-39999
    [InlineData("31270901", "MG")]
    [InlineData("39999999", "MG")]
    [InlineData("40000000", "BA")]   // BA: 40000-48999
    [InlineData("40140110", "BA")]
    [InlineData("48999999", "BA")]
    [InlineData("49000000", "SE")]   // SE: 49000-49999
    [InlineData("49010000", "SE")]
    [InlineData("49999999", "SE")]
    [InlineData("50000000", "PE")]   // PE: 50000-56999
    [InlineData("50030350", "PE")]
    [InlineData("56999999", "PE")]
    [InlineData("57000000", "AL")]   // AL: 57000-57999
    [InlineData("57010300", "AL")]
    [InlineData("57999999", "AL")]
    [InlineData("58000000", "PB")]   // PB: 58000-58999
    [InlineData("58010290", "PB")]
    [InlineData("58999999", "PB")]
    [InlineData("59000000", "RN")]   // RN: 59000-59999
    [InlineData("59020040", "RN")]
    [InlineData("59999999", "RN")]
    [InlineData("60000000", "CE")]   // CE: 60000-63999
    [InlineData("60010130", "CE")]
    [InlineData("63999999", "CE")]
    [InlineData("64000000", "PI")]   // PI: 64000-64999
    [InlineData("64001310", "PI")]
    [InlineData("64999999", "PI")]
    [InlineData("65000000", "MA")]   // MA: 65000-65999
    [InlineData("65005680", "MA")]
    [InlineData("65999999", "MA")]
    [InlineData("66000000", "PA")]   // PA: 66000-68899
    [InlineData("66033170", "PA")]
    [InlineData("68899999", "PA")]
    [InlineData("68900000", "AP")]   // AP: 68900-68999
    [InlineData("68900061", "AP")]
    [InlineData("68999999", "AP")]
    [InlineData("69000000", "AM")]   // AM: 69000-69299
    [InlineData("69005140", "AM")]
    [InlineData("69299999", "AM")]
    [InlineData("69300000", "RR")]   // RR: 69300-69399
    [InlineData("69301070", "RR")]
    [InlineData("69399999", "RR")]
    [InlineData("69400000", "AM")]   // AM: 69400-69899 (second block)
    [InlineData("69460000", "AM")]
    [InlineData("69899999", "AM")]
    [InlineData("69900000", "AC")]   // AC: 69900-69999
    [InlineData("69900680", "AC")]
    [InlineData("69999999", "AC")]
    [InlineData("70000000", "DF")]   // DF: 70000-72799
    [InlineData("70040010", "DF")]
    [InlineData("72799999", "DF")]
    [InlineData("72800000", "GO")]   // GO: 72800-72999 (entorno do DF)
    [InlineData("72855000", "GO")]
    [InlineData("72999999", "GO")]
    [InlineData("73000000", "DF")]   // DF: 73000-73699 (second block)
    [InlineData("73345000", "DF")]
    [InlineData("73699999", "DF")]
    [InlineData("73700000", "GO")]   // GO: 73700-76799 (second block)
    [InlineData("74000000", "GO")]
    [InlineData("74895000", "GO")]
    [InlineData("76799999", "GO")]
    [InlineData("76800000", "RO")]   // RO: 76800-76999
    [InlineData("76800300", "RO")]
    [InlineData("76999999", "RO")]
    [InlineData("77000000", "TO")]   // TO: 77000-77999
    [InlineData("77001000", "TO")]
    [InlineData("77999999", "TO")]
    [InlineData("78000000", "MT")]   // MT: 78000-78899
    [InlineData("78005010", "MT")]
    [InlineData("78899999", "MT")]
    [InlineData("78900000", null)]   // 789xx belongs to no state (legacy Porto Velho)
    [InlineData("78950000", null)]
    [InlineData("78999999", null)]
    [InlineData("79000000", "MS")]   // MS: 79000-79999
    [InlineData("79002120", "MS")]
    [InlineData("79999999", "MS")]
    [InlineData("80000000", "PR")]   // PR: 80000-87999
    [InlineData("80010100", "PR")]
    [InlineData("87999999", "PR")]
    [InlineData("88000000", "SC")]   // SC: 88000-89999
    [InlineData("88010300", "SC")]
    [InlineData("89999999", "SC")]
    [InlineData("90000000", "RS")]   // RS: 90000-99999
    [InlineData("90010470", "RS")]
    [InlineData("95555000", "RS")]
    [InlineData("99999999", "RS")]
    public void Uf_Resolves_The_Correios_Ranges(string digits, string? expectedUf)
    {
        Cep cep = Cep.Parse(digits);
        Assert.Equal(expectedUf, cep.Uf);
    }

    [Fact]
    public void Uf_Resolves_Formatted_Input_The_Same_Way()
    {
        Assert.Equal("SP", Cep.Parse("01310-100").Uf);
        Assert.Equal("DF", Cep.Parse("70.040-010").Uf);
        Assert.Null(Cep.Parse("78900-000").Uf);
    }

    [Fact]
    public void Generate_Returns_Valid_Ceps_Inside_Known_Ranges()
    {
        var ufs = new HashSet<string?>();
        for (int i = 0; i < 1_000; i++)
        {
            Cep cep = Cep.Generate();
            string digits = cep.ToString("N");
            Assert.Equal(8, digits.Length);
            Assert.True(Cep.IsValid(digits), $"generated {digits} must be valid");
            Assert.NotNull(cep.Uf);
            Assert.Equal(cep, Cep.Parse(digits));
            Assert.Equal(cep, Cep.Parse(cep.ToString()));
            ufs.Add(cep.Uf);
        }
        // Random CEPs over the weighted prefix space must cover a good part of the country.
        Assert.True(ufs.Count >= 12, $"expected at least 12 distinct UFs, got {ufs.Count}");
    }

    [Fact]
    public void Generate_Never_Produces_Unassigned_Prefixes()
    {
        for (int i = 0; i < 5_000; i++)
        {
            Cep cep = Cep.Generate();
            int prefix = int.Parse(cep.ToString("N")[..5], CultureInfo.InvariantCulture);
            Assert.True(prefix is >= 1000 and <= 99999, $"prefix {prefix} must be assigned");
            Assert.False(prefix is >= 78900 and <= 78999, $"prefix {prefix} must not be in the unowned 789xx block");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        Cep[] Run(Random random) => Enumerable.Range(0, 100).Select(_ => Cep.Generate(random)).ToArray();

        Assert.Equal(Run(new Random(31337)), Run(new Random(31337)));
        Assert.NotEqual(Run(new Random(1)), Run(new Random(2)));
    }

    [Fact]
    public void Parse_Is_Culture_Invariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Cep cep = Cep.Parse(ValidCepDigits);
            Assert.Equal(ValidCepFormatted, cep.ToString());
            Assert.Equal(ValidCepDigits, cep.ToString("N"));
            Assert.Equal(ValidCepFormatted, ((IFormattable)cep).ToString("G", CultureInfo.GetCultureInfo("ar-SA")));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
