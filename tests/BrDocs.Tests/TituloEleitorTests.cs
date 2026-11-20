using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class TituloEleitorTests
{
    private const string ValidTituloFormatted = "1023 8501 06 71";
    private const string ValidTituloDigits = "102385010671";

    [Theory]
    [InlineData("1023 8501 06 71")]    // cadcobol worked example (PR)
    [InlineData("102385010671")]
    [InlineData("0043 5687 09 06")]    // OBMEP worked example (SC)
    [InlineData("004356870906")]
    [InlineData("1234 5678 20 97")]     // bateCarimbo worked example (DF)
    [InlineData("123456782097")]
    [InlineData("1000 0007 03 02")]     // bateCarimbo: DV1 remainder 10 → 0 (RJ)
    [InlineData("100000070302")]
    [InlineData("1111 1111 01 16")]     // bateCarimbo: SP remainder 0 → DV1 = 1
    [InlineData("111111110116")]
    [InlineData("1111 1111 02 13")]     // bateCarimbo: MG remainder 0 → DV1 = 1
    [InlineData("111111110213")]
    [InlineData("6021 9483 20 03")]     // bateCarimbo: DF, DV1 remainder 0 → 0
    [InlineData("602194832003")]
    [InlineData("4801 2377 06 20")]     // bateCarimbo: PR, DV2 remainder 0 → 0
    [InlineData("480123770620")]
    [InlineData("4821 7305 06 04")]     // geradornv worked example (PR)
    [InlineData("482173050604")]
    [InlineData("6234 5678 06 80")]     // hand-computed: PR, DV2 remainder 10 → 0
    [InlineData("623456780680")]
    [InlineData("1234 5689 01 41")]     // hand-computed: SP, DV2 remainder 0 → 1
    [InlineData("123456890141")]
    [InlineData("1234 5689 03 45")]     // hand-computed: RJ, DV2 remainder 5
    [InlineData("123456890345")]
    public void IsValid_Returns_True_For_Known_Valid_Vectors(string value)
    {
        Assert.True(TituloEleitor.IsValid(value));
    }

    [Theory]
    [InlineData(" 1023 8501 06 71 ")]
    [InlineData("102385010671")]
    [InlineData("10 23 85 01 06 71")]
    [InlineData("1/0/2/3/8/5/0/1/0/6/7/1")]
    [InlineData("1023.8501.06-71")]
    [InlineData("  102385010671  ")]
    [InlineData("10-23-85-01-06-71")]
    [InlineData("...1023 8501 06 71...")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(TituloEleitor.IsValid(value));
    }

    [Theory]
    [InlineData("123456780199")]      // DV1 wrong (9 is the DV1 of seq 12345678, not 1)
    [InlineData("123456782098")]      // DV2 wrong
    [InlineData("123456782007")]      // both DVs wrong
    [InlineData("111111110106")]      // SP remainder-0 rule violated: DV1 must be 1
    [InlineData("111111110206")]      // MG remainder-0 rule violated
    [InlineData("111111110006")]      // SP DV1 with UF 00: invalid UF anyway
    [InlineData("1111111102133")]     // 13 digits are rejected
    [InlineData("1234567880191")]     // 13 digits
    [InlineData("12345678209")]       // too short
    [InlineData("1234567820977")]     // too long
    public void IsValid_Returns_False_For_Known_Invalid_Vectors(string value)
    {
        Assert.False(TituloEleitor.IsValid(value));
    }

    [Theory]
    [InlineData("123456780091")]      // UF 00 does not exist
    [InlineData("123456782994")]      // UF 29 does not exist
    [InlineData("123456783094")]      // UF 30 does not exist
    [InlineData("123456789990")]      // UF 99 does not exist
    [InlineData("12345678 00 91")]
    [InlineData("000000000000")]      // all-zero digits carry UF 00
    [InlineData("111111111111")]      // all-same digits carry UF 11, whose DVs do not match
    [InlineData("222222222222")]
    [InlineData("999999999999")]
    public void IsValid_Rejects_Unknown_Uf_Codes_And_All_Same_Sequences(string value)
    {
        Assert.False(TituloEleitor.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData(" - / ")]
    [InlineData("1")]
    [InlineData("1234567820")]
    [InlineData("12345678209777")]
    public void IsValid_Returns_False_For_Wrong_Lengths(string value)
    {
        Assert.False(TituloEleitor.IsValid(value));
    }

    [Theory]
    [InlineData("1234567,80209")]
    [InlineData("1234_5678_20_97")]
    [InlineData("1234:5678:20:97")]
    [InlineData("titulo 123456782097")]
    [InlineData("12345678209a")]
    [InlineData("12345678209X")]
    [InlineData("12345678–2097")]     // en dash U+2013, not '-'
    [InlineData("١٢٣٤٥٦٧٨٢٠٩٧")]        // Arabic-Indic digits
    [InlineData("123456782097\n")]
    [InlineData("\t123456782097")]
    public void IsValid_Returns_False_For_Invalid_Characters(string value)
    {
        Assert.False(TituloEleitor.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(TituloEleitor.IsValid((string?)null!));
    }

    // One valid título per UF code (01..28), all over the sequential 12345678 whose DV1 is 9
    // (240 mod 11); the DV2 of each row is 7×d9 + 8×d10 + 9×9 mod 11 with the remainder rules.
    // The DF row (20) matches the published example "1234 5678 20 97".

    [Theory]
    [InlineData("123456780191", 1, "SP")]
    [InlineData("123456780299", 2, "MG")]
    [InlineData("123456780396", 3, "RJ")]
    [InlineData("123456780493", 4, "RS")]
    [InlineData("123456780590", 5, "BA")]
    [InlineData("123456780698", 6, "PR")]
    [InlineData("123456780795", 7, "CE")]
    [InlineData("123456780892", 8, "PE")]
    [InlineData("123456780990", 9, "SC")]
    [InlineData("123456781090", 10, "GO")]
    [InlineData("123456781198", 11, "MA")]
    [InlineData("123456781295", 12, "PB")]
    [InlineData("123456781392", 13, "PA")]
    [InlineData("123456781490", 14, "ES")]
    [InlineData("123456781597", 15, "PI")]
    [InlineData("123456781694", 16, "RN")]
    [InlineData("123456781791", 17, "AL")]
    [InlineData("123456781899", 18, "MT")]
    [InlineData("123456781996", 19, "MS")]
    [InlineData("123456782097", 20, "DF")]
    [InlineData("123456782194", 21, "SE")]
    [InlineData("123456782291", 22, "AM")]
    [InlineData("123456782399", 23, "RO")]
    [InlineData("123456782496", 24, "AC")]
    [InlineData("123456782593", 25, "AP")]
    [InlineData("123456782690", 26, "RR")]
    [InlineData("123456782798", 27, "TO")]
    [InlineData("123456782895", 28, "ZZ")]
    public void Every_Uf_Code_01_To_28_Is_Valid_And_Maps_To_Its_Sigla(string value, int ufCode, string uf)
    {
        Assert.True(TituloEleitor.IsValid(value), $"{value} must be valid for UF code {ufCode}");
        TituloEleitor titulo = TituloEleitor.Parse(value);
        Assert.Equal(ufCode, titulo.UfCode);
        Assert.Equal(uf, titulo.Uf);
    }

    [Fact]
    public void UfCode_And_Uf_Are_Rejected_Outside_01_To_28_By_The_Oracle_Too()
    {
        // Cross-check a handful of the rows above against the independent oracle arithmetic.
        foreach ((string value, int ufCode) in new[] { ("123456780191", 1), ("123456781490", 14), ("123456782895", 28) })
        {
            Assert.True(Oracles.IsValidTituloEleitor(value));
            Assert.Equal(ufCode, TituloEleitor.Parse(value).UfCode);
        }
    }

    [Theory]
    [InlineData("1023 8501 06 71", "1023 8501 06 71", "102385010671")]
    [InlineData("102385010671", "1023 8501 06 71", "102385010671")]
    [InlineData(" 1023 8501 06 71 ", "1023 8501 06 71", "102385010671")]
    [InlineData("1023.8501.06.71", "1023 8501 06 71", "102385010671")]
    [InlineData("123456782097", "1234 5678 20 97", "123456782097")]
    public void Parse_Returns_The_Titulo_For_Valid_Input(string value, string expectedFormatted, string expectedDigits)
    {
        TituloEleitor titulo = TituloEleitor.Parse(value);
        Assert.Equal(expectedFormatted, titulo.ToString());
        Assert.Equal(expectedDigits, titulo.ToString("N"));
        Assert.Equal(12, titulo.ToString("N").Length);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
            Assert.Equal(ValidTituloFormatted, titulo.ToString());
            Assert.Equal(ValidTituloDigits, titulo.ToString("N"));
            Assert.Equal(ValidTituloDigits, ((IFormattable)titulo).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidTituloFormatted, ((IFormattable)titulo).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => TituloEleitor.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a titulo")]
    [InlineData("123456780199")]
    [InlineData("000000000000")]
    [InlineData("123456780091")]
    [InlineData("1234567880191")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => TituloEleitor.Parse(value));
        Assert.Contains("Título de Eleitor", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "102385010671" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => TituloEleitor.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 130, "the message must be truncated");
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Titulo()
    {
        Assert.True(TituloEleitor.TryParse(ValidTituloFormatted, out TituloEleitor titulo));
        Assert.Equal(ValidTituloDigits, titulo.ToString("N"));
        Assert.True(TituloEleitor.TryParse(ValidTituloDigits, out TituloEleitor raw));
        Assert.Equal(titulo, raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456780199")]
    [InlineData("1234567880191")]
    [InlineData("123,456,782,097")]
    [InlineData("000000000000")]
    [InlineData("123456780091")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(TituloEleitor.TryParse(value, out TituloEleitor titulo));
        Assert.Equal(default, titulo);
        Assert.Equal("0000 0000 00 00", titulo.ToString());
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
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        string expected = format is "N" or "n" ? ValidTituloDigits : ValidTituloFormatted;
        Assert.Equal(expected, titulo.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        Assert.Throws<FormatException>(() => titulo.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Formatted_Value()
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        Span<char> buffer = stackalloc char[15];
        Assert.True(titulo.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(15, written);
        Assert.Equal(ValidTituloFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Digits_Only()
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloFormatted);
        Span<char> buffer = stackalloc char[12];
        Assert.True(titulo.TryFormat(buffer, out int written, "N", null));
        Assert.Equal(12, written);
        Assert.Equal(ValidTituloDigits, new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void TryFormat_Returns_False_When_The_Formatted_Destination_Is_Too_Small(int size)
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        char[] buffer = new char[15];
        buffer.AsSpan().Fill('x');
        Assert.False(titulo.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 15), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void TryFormat_Returns_False_When_The_Numeric_Destination_Is_Too_Small(int size)
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        char[] buffer = new char[12];
        buffer.AsSpan().Fill('x');
        Assert.False(titulo.TryFormat(buffer.AsSpan(0, size), out int written, "N", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 12), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        TituloEleitor titulo = TituloEleitor.Parse(ValidTituloDigits);
        Assert.Throws<FormatException>(() => titulo.TryFormat(stackalloc char[15], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        TituloEleitor expected = TituloEleitor.Parse(ValidTituloDigits);
        TituloEleitor parsed = ParsableHelpers.ParseViaSpanParsable<TituloEleitor>(ValidTituloFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<TituloEleitor>(ValidTituloDigits.AsSpan(), null, out TituloEleitor result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        TituloEleitor expected = TituloEleitor.Parse(ValidTituloDigits);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<TituloEleitor>(ValidTituloFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<TituloEleitor>(ValidTituloDigits, null, out TituloEleitor result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<TituloEleitor>(null, null, out TituloEleitor result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<TituloEleitor>("123456780199".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Digits()
    {
        TituloEleitor a = TituloEleitor.Parse("102385010671");
        TituloEleitor b = TituloEleitor.Parse("1023 8501 06 71");
        TituloEleitor c = TituloEleitor.Parse("123456782097");
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.True(((IEquatable<TituloEleitor>)a).Equals(b));
        Assert.False(((IEquatable<TituloEleitor>)a).Equals(c));
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        TituloEleitor original = TituloEleitor.Parse("1234 5678 20 97");
        TituloEleitor roundTripped = TituloEleitor.Parse(original.ToString());
        Assert.Equal(original, roundTripped);
        Assert.Equal(original.ToString("N"), TituloEleitor.Parse(original.ToString("N")).ToString("N"));
        Assert.Equal("DF", roundTripped.Uf);
        Assert.Equal(20, roundTripped.UfCode);
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid_With_No_Uf()
    {
        TituloEleitor @default = default;
        Assert.Equal("0000 0000 00 00", @default.ToString());
        Assert.Equal("000000000000", @default.ToString("N"));
        Assert.False(TituloEleitor.IsValid(@default.ToString()));
        Assert.Equal(0, @default.UfCode);
        Assert.Null(@default.Uf);
    }

    [Fact]
    public void Uf_Covers_The_Exterior_Code()
    {
        TituloEleitor titulo = TituloEleitor.Parse("123456782895");
        Assert.Equal(28, titulo.UfCode);
        Assert.Equal("ZZ", titulo.Uf);
    }

    [Fact]
    public void Generate_Returns_Valid_Titulos()
    {
        for (int i = 0; i < 1_000; i++)
        {
            TituloEleitor titulo = TituloEleitor.Generate();
            string digits = titulo.ToString("N");
            Assert.Equal(12, digits.Length);
            Assert.True(TituloEleitor.IsValid(digits), $"generated {digits} must be valid");
            Assert.True(Oracles.IsValidTituloEleitor(digits), $"generated {digits} must satisfy the oracle too");
            Assert.Equal(titulo, TituloEleitor.Parse(digits));
            Assert.Equal(titulo, TituloEleitor.Parse(titulo.ToString()));
            Assert.InRange(titulo.UfCode, 1, 28);
            Assert.NotNull(titulo.Uf);
        }
    }

    [Fact]
    public void Generate_Covers_All_Uf_Codes()
    {
        var codes = new HashSet<int>();
        for (int i = 0; i < 5_000; i++)
        {
            codes.Add(TituloEleitor.Generate(new Random(i)).UfCode);
        }
        for (int code = 1; code <= 28; code++)
        {
            Assert.True(codes.Contains(code), $"UF code {code} must be generated");
        }
    }

    [Fact]
    public void Generate_Produces_The_Sp_Mg_Rule_When_The_Random_Uf_Is_01_Or_02()
    {
        int specialCases = 0;
        for (int i = 0; i < 20_000; i++)
        {
            TituloEleitor titulo = TituloEleitor.Generate(new Random(i));
            string digits = titulo.ToString("N");
            if (titulo.UfCode is 1 or 2)
            {
                int sum1 = digits.Take(8).Select((d, i2) => (d - '0') * (2 + i2)).Sum();
                int remainder = sum1 % 11;
                if (remainder == 0)
                {
                    // SP/MG with remainder 0 must carry DV1 = 1.
                    Assert.Equal('1', digits[10]);
                    specialCases++;
                }
            }
        }
        Assert.True(specialCases > 0, "the SP/MG remainder-zero rule must be exercised");
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        TituloEleitor[] firstRun = Enumerable.Range(0, 100).Select(_ => TituloEleitor.Generate(first)).ToArray();
        TituloEleitor[] secondRun = Enumerable.Range(0, 100).Select(_ => TituloEleitor.Generate(second)).ToArray();
        TituloEleitor[] thirdRun = Enumerable.Range(0, 100).Select(_ => TituloEleitor.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        TituloEleitor titulo = TituloEleitor.Generate();
        Assert.True(TituloEleitor.IsValid(titulo.ToString("N")));
        TituloEleitor also = TituloEleitor.Generate(rng: null);
        Assert.True(TituloEleitor.IsValid(also.ToString("N")));
    }
}
