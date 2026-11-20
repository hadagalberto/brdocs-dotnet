using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Tests for the arrecadação/convenio payment code: the 44-digit código de barras starting
/// with <c>8</c>, the 48-digit linha digitável of four 11-digit blocks, the per-block DVs and
/// the general DV selected by the third digit (6/7 → módulo 10, 8/9 → módulo 11).
/// </summary>
/// <remarks>
/// The vectors are real-world codes (an energia/gás bill, a government guide, telecom bills)
/// cross-checked against independent implementations, plus synthetic reference-value codes
/// whose identifier digit was swapped to 7/9 with every check digit recomputed.
/// </remarks>
public sealed class ArrecadacaoTests
{
    // Energia elétrica e gás (segment 3), identifier 8: effective value, DV módulo 11, R$ 509,60.
    private const string EnergiaBarcode = "83860000005096000190000008017823000034306271";
    private const string EnergiaLinha = "838600000050096000190009000801782309000343062712";
    private const string EnergiaFormatted = "83860000005-0 09600019000-9 00080178230-9 00034306271-2";

    // Órgão governamental (segment 5), identifier 8: effective value, DV módulo 11, R$ 57,25.
    private const string GovernoBarcode = "85820000000572503282035607082021053959190446";
    private const string GovernoLinha = "858200000007572503282030560708202107539591904460";
    private const string GovernoFormatted = "85820000000-7 57250328203-0 56070820210-7 53959190446-0";

    // Telecomunicações (segment 4), identifier 6: effective value, DV módulo 10, R$ 29,99.
    private const string TelecomBarcode = "84630000000299902962020041013600000200644114";
    private const string TelecomLinha = "846300000003299902962024004101360008002006441147";
    private const string TelecomFormatted = "84630000000-3 29990296202-4 00410136000-8 00200644114-7";

    // A utility-bill barcode used in public "anatomia do código de barras" references: R$ 143,59.
    private const string UtilityBarcode = "84670000001435900240200240500024384221010811";

    // Synthetic codes: the telecom/energia vectors with the identifier swapped to a reference
    // value (7 → módulo 10, 9 → módulo 11); every DV was recomputed, so these exercise the
    // identifier rule rather than a broken check digit.
    private const string ReferenceMod10Barcode = "84710000000299902962020041013600000200644114";
    private const string ReferenceMod10Linha = "847100000003299902962024004101360008002006441147";
    private const string ReferenceMod11Barcode = "83940000005096000190000008017823000034306271";
    private const string ReferenceMod11Linha = "839400000055096000190009000801782309000343062712";

    // The identifier-5 and first-digit-7 codes below carry correctly recomputed módulo 10 DVs,
    // so their rejection comes from the structural rules, not from a check-digit accident.
    private const string Identifier5Linha = "845500000003299902962024004101360008002006441147";
    private const string FirstDigit7Linha = "746500000003299902962024004101360008002006441147";

    // The FEBRABAN-documented segments and the possible value identifiers.
    private static readonly int[] s_segments = { 1, 2, 3, 4, 5, 6, 7, 9 };
    private static readonly int[] s_identifiers = { 6, 7, 8, 9 };

    [Theory]
    [InlineData(EnergiaBarcode)]
    [InlineData(EnergiaLinha)]
    [InlineData(EnergiaFormatted)]
    [InlineData(GovernoBarcode)]
    [InlineData(GovernoLinha)]
    [InlineData(GovernoFormatted)]
    [InlineData(TelecomBarcode)]
    [InlineData(TelecomLinha)]
    [InlineData(TelecomFormatted)]
    [InlineData(UtilityBarcode)]
    [InlineData(ReferenceMod10Barcode)]
    [InlineData(ReferenceMod10Linha)]
    [InlineData(ReferenceMod11Barcode)]
    [InlineData(ReferenceMod11Linha)]
    [InlineData(" 85820000000-7 57250328203-0 56070820210-7 53959190446-0 ")]
    [InlineData("83860000005.0 09600019000.9 00080178230.9 00034306271.2")]
    [InlineData("8386 0000005-0 09600019000-9 00080178230-9 00034306271-2")]
    public void IsValid_Returns_True_For_Valid_Codes_In_Every_Form(string value)
    {
        Assert.True(Arrecadacao.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("8386000000509600019000000801782300003430627")]    // 43 digits
    [InlineData(EnergiaBarcode + "0")]                            // 45 digits
    [InlineData("34198532419429701941940804574073716770000123456")] // 47-digit bank linha
    [InlineData(EnergiaLinha + "0")]                              // 49 digits
    [InlineData("83870000005096000190000008017823000034306271")]   // general DV corrupted
    [InlineData("838600000051096000190009000801782309000343062712")] // block DV1 corrupted
    [InlineData("838600000050096000190008000801782309000343062712")] // block DV2 corrupted
    [InlineData("838600000050096000190009000801782308000343062712")] // block DV3 corrupted
    [InlineData("838600000050096000190009000801782309000343062711")] // block DV4 corrupted
    [InlineData("34197167700001234568532494297019414080457407")]   // bank-slip barcode
    [InlineData("10499898100000214032006561000100040099726390")]   // bank-slip barcode (Caixa)
    [InlineData(Identifier5Linha)]                                 // identifier 5 rejected
    [InlineData(FirstDigit7Linha)]                                 // first digit 7 rejected
    [InlineData("00000000000000000000000000000000000000000000")]    // 44 zeros
    [InlineData("88888888888888888888888888888888888888888888")]     // 44 eights
    [InlineData("000000000000000000000000000000000000000000000000")] // 48 zeros
    [InlineData("83860000005009600019000900080178230900034306271a")]
    [InlineData("838600000050096000190009000801782309000343062712\n")]
    public void IsValid_Returns_False_For_Invalid_Codes(string value)
    {
        Assert.False(Arrecadacao.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        Assert.False(Arrecadacao.IsValid((string?)null!));
    }

    [Fact]
    public void Parsing_Either_Form_Produces_The_Same_Code()
    {
        Arrecadacao fromBarcode = Arrecadacao.Parse(EnergiaBarcode);
        Assert.Equal(fromBarcode, Arrecadacao.Parse(EnergiaLinha));
        Assert.Equal(fromBarcode, Arrecadacao.Parse(EnergiaFormatted));
        Assert.True(fromBarcode == Arrecadacao.Parse(EnergiaLinha));
        Assert.False(fromBarcode != Arrecadacao.Parse(EnergiaFormatted));
    }

    [Fact]
    public void Parse_Extracts_The_Energia_Fields()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        Assert.Equal(3, arrecadacao.Segment);
        Assert.Equal(ArrecadacaoValueType.EffectiveValueMod11, arrecadacao.ValueType);
        Assert.True(arrecadacao.IsEffectiveValue);
        Assert.Equal(509.60m, arrecadacao.Value);
        Assert.Equal("0000008017823000034306271", arrecadacao.FreeField);
        Assert.Equal(EnergiaBarcode, arrecadacao.Barcode);
        Assert.Equal(EnergiaLinha, arrecadacao.LinhaDigitavel);
        Assert.Equal(EnergiaFormatted, arrecadacao.ToString());
    }

    [Fact]
    public void Parse_Extracts_The_Governo_Fields()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(GovernoLinha);
        Assert.Equal(5, arrecadacao.Segment);
        Assert.Equal(ArrecadacaoValueType.EffectiveValueMod11, arrecadacao.ValueType);
        Assert.Equal(57.25m, arrecadacao.Value);
        Assert.Equal("2035607082021053959190446", arrecadacao.FreeField);
        Assert.Equal(GovernoBarcode, arrecadacao.Barcode);
        Assert.Equal(GovernoFormatted, arrecadacao.ToString());
    }

    [Fact]
    public void Parse_Extracts_The_Telecom_Fields()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(TelecomFormatted);
        Assert.Equal(4, arrecadacao.Segment);
        Assert.Equal(ArrecadacaoValueType.EffectiveValueMod10, arrecadacao.ValueType);
        Assert.Equal(29.99m, arrecadacao.Value);
        Assert.Equal(TelecomBarcode, arrecadacao.Barcode);
        Assert.Equal(TelecomLinha, arrecadacao.LinhaDigitavel);
    }

    [Fact]
    public void Parse_Extracts_The_Utility_Bill_Fields()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(UtilityBarcode);
        Assert.Equal(4, arrecadacao.Segment);
        Assert.Equal(ArrecadacaoValueType.EffectiveValueMod10, arrecadacao.ValueType);
        Assert.Equal(143.59m, arrecadacao.Value);
    }

    [Fact]
    public void Reference_Values_Are_Not_Amounts()
    {
        Arrecadacao reference10 = Arrecadacao.Parse(ReferenceMod10Linha);
        Assert.Equal(ArrecadacaoValueType.ReferenceValueMod10, reference10.ValueType);
        Assert.False(reference10.IsEffectiveValue);
        Assert.Null(reference10.Value);
        Assert.Equal(ReferenceMod10Barcode, reference10.Barcode);

        Arrecadacao reference11 = Arrecadacao.Parse(ReferenceMod11Barcode);
        Assert.Equal(ArrecadacaoValueType.ReferenceValueMod11, reference11.ValueType);
        Assert.False(reference11.IsEffectiveValue);
        Assert.Null(reference11.Value);
        Assert.Equal(ReferenceMod11Linha, reference11.LinhaDigitavel);
    }

    [Fact]
    public void Parse_From_The_Linha_Digitavel_Produces_The_Known_Barcode_And_Vice_Versa()
    {
        Assert.Equal(EnergiaBarcode, Arrecadacao.Parse(EnergiaLinha).Barcode);
        Assert.Equal(EnergiaLinha, Arrecadacao.Parse(EnergiaBarcode).LinhaDigitavel);
        Assert.Equal(GovernoBarcode, Arrecadacao.Parse(GovernoLinha).Barcode);
        Assert.Equal(TelecomBarcode, Arrecadacao.Parse(TelecomLinha).Barcode);
    }

    [Theory]
    [InlineData("838600000050096000190009000801782309000343062713")]
    [InlineData("83860000005009600019000900080178230900034306271a")]
    [InlineData("84")]
    public void Parse_Invalid_Throws_Format_Exception_With_A_Clear_Message(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Arrecadacao.Parse(value));
        Assert.Contains("arrecadação", exception.Message, StringComparison.Ordinal);
        Assert.True(exception.Message.Contains(value, StringComparison.Ordinal)
            || exception.Message.Contains(value[..10], StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Arrecadacao.Parse(null!));
    }

    [Theory]
    [InlineData(EnergiaBarcode)]
    [InlineData(EnergiaLinha)]
    [InlineData(EnergiaFormatted)]
    [InlineData(GovernoBarcode)]
    [InlineData(TelecomLinha)]
    public void TryParse_Valid_Outputs_The_Code(string value)
    {
        Assert.True(Arrecadacao.TryParse(value, out Arrecadacao arrecadacao));
        Assert.Equal(Arrecadacao.Parse(value), arrecadacao);
        Assert.True(Arrecadacao.IsValid(arrecadacao.ToString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("838600000050096000190009000801782309000343062713")]
    [InlineData(FirstDigit7Linha)]
    [InlineData(Identifier5Linha)]
    [InlineData("34197167700001234568532494297019414080457407")]
    [InlineData("abc")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Arrecadacao.TryParse(value, out Arrecadacao arrecadacao));
        Assert.Equal(default, arrecadacao);
        Assert.Equal("00000000000-0 00000000000-0 00000000000-0 00000000000-0", arrecadacao.ToString());
        Assert.False(Arrecadacao.IsValid(arrecadacao.ToString()));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Is_Never_Valid()
    {
        Arrecadacao @default = default;
        Assert.Equal("00000000000-0 00000000000-0 00000000000-0 00000000000-0", @default.ToString());
        Assert.Equal(new string('0', 48), @default.ToString("N"));
        Assert.Equal(new string('0', 44), @default.ToString("B"));
        Assert.Equal(0, @default.Segment);
        Assert.Equal(ArrecadacaoValueType.Unknown, @default.ValueType);
        Assert.False(@default.IsEffectiveValue);
        Assert.Null(@default.Value);
        Assert.Equal(new string('0', 25), @default.FreeField);
        Assert.False(Arrecadacao.IsValid(@default.Barcode));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("g")]
    [InlineData("N")]
    [InlineData("n")]
    [InlineData("B")]
    [InlineData("b")]
    public void ToString_Formats_Are_Resolved(string? format)
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        string expected = format is "N" or "n" ? EnergiaLinha : format is "B" or "b" ? EnergiaBarcode : EnergiaFormatted;
        Assert.Equal(expected, arrecadacao.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData("C")]
    [InlineData("b n")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        Assert.Throws<FormatException>(() => arrecadacao.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_Every_Form()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        char[] buffer = new char[56];

        Assert.True(arrecadacao.TryFormat(buffer, out int writtenG, "G", null));
        Assert.Equal(55, writtenG);
        Assert.Equal(EnergiaFormatted, new string(buffer, 0, 55));

        Assert.True(arrecadacao.TryFormat(buffer, out int writtenN, "N", null));
        Assert.Equal(48, writtenN);
        Assert.Equal(EnergiaLinha, new string(buffer, 0, 48));

        Assert.True(arrecadacao.TryFormat(buffer, out int writtenB, "B", null));
        Assert.Equal(44, writtenB);
        Assert.Equal(EnergiaBarcode, new string(buffer, 0, 44));
    }

    [Theory]
    [InlineData("G", 0)]
    [InlineData("G", 54)]
    [InlineData("N", 0)]
    [InlineData("N", 47)]
    [InlineData("B", 0)]
    [InlineData("B", 43)]
    public void TryFormat_Returns_False_When_The_Destination_Is_Too_Small(string format, int size)
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        char[] buffer = new char[55];
        buffer.AsSpan().Fill('x');
        Assert.False(arrecadacao.TryFormat(buffer.AsSpan(0, size), out int written, format, null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 55), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaBarcode);
        Assert.Throws<FormatException>(() => arrecadacao.TryFormat(stackalloc char[55], out _, "X", null));
    }

    [Fact]
    public void Every_Format_Parses_Back_To_The_Same_Code()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse(GovernoBarcode);
        Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.ToString()));
        Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.ToString("N")));
        Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.ToString("B")));
        Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.Barcode));
        Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.LinhaDigitavel));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Arrecadacao expected = Arrecadacao.Parse(EnergiaBarcode);
        Arrecadacao parsed = ParsableHelpers.ParseViaSpanParsable<Arrecadacao>(EnergiaLinha.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Arrecadacao>(EnergiaFormatted.AsSpan(), null, out Arrecadacao result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Arrecadacao expected = Arrecadacao.Parse(EnergiaBarcode);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Arrecadacao>(EnergiaLinha, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Arrecadacao>(EnergiaFormatted, null, out Arrecadacao result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Arrecadacao>(null, null, out Arrecadacao result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Arrecadacao>("1234".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Barcode()
    {
        Arrecadacao a = Arrecadacao.Parse(EnergiaBarcode);
        Arrecadacao b = Arrecadacao.Parse(" 83860000005-0 09600019000-9 00080178230-9 00034306271-2 ");
        Arrecadacao c = Arrecadacao.Parse(GovernoBarcode);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(((IEquatable<Arrecadacao>)a).Equals(b));
        Assert.False(((IEquatable<Arrecadacao>)a).Equals(c));
    }

    [Fact]
    public void Generate_Produces_Valid_Codes_That_Round_Trip()
    {
        var segments = new HashSet<int>();
        for (int i = 0; i < 500; i++)
        {
            Arrecadacao arrecadacao = Arrecadacao.Generate(new Random(i));
            Assert.True(Arrecadacao.IsValid(arrecadacao.Barcode), $"generated {arrecadacao.Barcode} must be valid");
            Assert.True(Arrecadacao.IsValid(arrecadacao.LinhaDigitavel), "the generated linha digitável must be valid");
            Assert.True(Oracles.IsValidArrecadacao(arrecadacao.Barcode), "the oracle must accept the generated code");
            Assert.Equal(44, arrecadacao.Barcode.Length);
            Assert.Equal(48, arrecadacao.LinhaDigitavel.Length);
            Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.Barcode));
            Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.LinhaDigitavel));
            Assert.Equal(arrecadacao, Arrecadacao.Parse(arrecadacao.ToString()));
            Assert.Contains(arrecadacao.Segment, s_segments);
            Assert.Contains((int)arrecadacao.ValueType, s_identifiers);
            Assert.Equal(arrecadacao.IsEffectiveValue, arrecadacao.Value is not null);
            segments.Add(arrecadacao.Segment);
        }
        Assert.True(segments.Count >= 6, $"expected at least 6 distinct segments, got {segments.Count}");
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        Arrecadacao[] Run(Random random) => Enumerable.Range(0, 25).Select(_ => Arrecadacao.Generate(random)).ToArray();

        Assert.Equal(Run(new Random(1337)), Run(new Random(1337)));
        Assert.NotEqual(Run(new Random(1)), Run(new Random(2)));
    }

    [Fact]
    public void Parse_Is_Culture_Invariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Arrecadacao arrecadacao = Arrecadacao.Parse(EnergiaFormatted);
            Assert.Equal(EnergiaFormatted, arrecadacao.ToString());
            Assert.Equal(EnergiaLinha, arrecadacao.ToString("N"));
            Assert.Equal(EnergiaBarcode, arrecadacao.ToString("B"));
            Assert.Equal(509.60m, arrecadacao.Value);
            Assert.Equal(EnergiaFormatted, ((IFormattable)arrecadacao).ToString("G", CultureInfo.GetCultureInfo("ar-SA")));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
