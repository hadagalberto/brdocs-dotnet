using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Tests for the bank-slip boleto (FEBRABAN cobrança): 44-digit código de barras, 47-digit
/// linha digitável, field/general DVs, field extraction, the 2025 factor reset and the
/// <see cref="Boleto.Create"/> builder.
/// </summary>
/// <remarks>
/// The primary vectors are real-world boletos validated against independent implementations:
/// the Caixa vector comes from the boleto-utils test suite and the Itaú vector from the
/// validation-br documentation (both cross-checked by hand against the FEBRABAN rules).
/// </remarks>
public sealed class BoletoTests
{
    // Caixa Econômica Federal, bank 104, currency 9 (Real), factor 8981 (2022-05-10), R$ 214,03.
    private const string CaixaBarcode = "10499898100000214032006561000100040099726390";
    private const string CaixaLinha = "10492006506100010004200997263900989810000021403";
    private const string CaixaLinhaFormatted = "10492.00650 61000.100042 00997.263900 9 89810000021403";

    // Itaú, bank 341, currency 9, factor 1677 (2002-05-11 in the first cycle), R$ 1.234,56.
    private const string ItauBarcode = "34197167700001234568532494297019414080457407";
    private const string ItauLinha = "34198532419429701941940804574073716770000123456";
    private const string ItauLinhaFormatted = "34198.53241 94297.019419 40804.574073 7 16770000123456";

    // A reference date whose [-3000, +5500] day window is [2017-03-15, 2040-06-22]:
    // cycle-1 factors 7099–9999 and cycle-2 factors 1000–6599 resolve; 6600–7098 fall in the gap.
    private static readonly DateOnly s_reference = new(2025, 6, 1);

    [Theory]
    [InlineData(CaixaBarcode)]
    [InlineData(CaixaLinha)]
    [InlineData(CaixaLinhaFormatted)]
    [InlineData(ItauBarcode)]
    [InlineData(ItauLinha)]
    [InlineData(ItauLinhaFormatted)]
    [InlineData("34196790600001000002220000005566385101214000")]   // boleto-utils Itaú sample
    [InlineData(" 10492.00650 61000.100042 00997.263900 9 89810000021403 ")]
    [InlineData("10492.00650-61000/100042 00997.263900 9 89810000021403")]
    [InlineData("10.49.20.06.50.61.00.01.00.04.20.09.97.26.39.00.9.89.81.00.00.02.14.03")]
    [InlineData("1049 9898 1000 0021 4032 0065 6100 0100 0400 9972 6390")]
    [InlineData("1049200650 6100010004 20099726390 0989810000021403")]
    public void IsValid_Returns_True_For_Valid_Boletos_In_Every_Form(string value)
    {
        Assert.True(Boleto.IsValid(value));
    }

    [Theory]
    [InlineData("")]                                       // empty
    [InlineData("   ")]                                    // punctuation only
    [InlineData("abc")]
    [InlineData("10499898100000214032006561000100040099726")]   // 43 digits
    [InlineData(CaixaBarcode + "0")]                       // 45 digits
    [InlineData(CaixaBarcode + "00")]                      // 46 digits
    [InlineData("846300000003299902962024004101360008002006441147")] // 48-digit convenio linha
    [InlineData("10498898100000214032006561000100040099726390")]    // general DV corrupted
    [InlineData("10492006516100010004200997263900989810000021403")] // field DV1 corrupted
    [InlineData("10492006506100010004300997263900989810000021403")] // field DV2 corrupted
    [InlineData("10492006506100010004200997263901989810000021403")] // field DV3 corrupted
    [InlineData("10492006506100010004200997263900889810000021403")] // general DV corrupted (linha)
    [InlineData("10492006506100010004200997263900989810000021503")] // amount digit corrupted
    [InlineData("34197790600001000002220000005566385101214000")]    // Itaú barcode with a wrong general DV
    [InlineData("84670000001435900240200240500024384221010811")]    // convenio barcode
    [InlineData("83860000005096000190000008017823000034306271")]    // convenio barcode (mod 11)
    [InlineData("1049989810000021403200656100010004009972639a")]    // trailing letter
    [InlineData("1049a98100000214032006561000100040099726390")]      // embedded letter
    [InlineData("10499898100000214032006561000100040099726390\n")]
    [InlineData("\t10499898100000214032006561000100040099726390")]
    [InlineData("١٠٤٩٩٨٩٨١٠٠٠٠٠٢١٤٠٣٢٠٠٦٥٦١٠٠٠١٠٠٠٤٠٠٩٩٧٢٦٣٩٠")]     // Arabic-Indic digits
    [InlineData("00000000000000000000000000000000000000000000")]     // 44 zeros
    [InlineData("11111111111111111111111111111111111111111111")]     // 44 ones
    [InlineData("99999999999999999999999999999999999999999999")]     // 44 nines
    [InlineData("00000000000000000000000000000000000000000000000")]  // 47 zeros
    public void IsValid_Returns_False_For_Invalid_Boletos(string value)
    {
        Assert.False(Boleto.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        Assert.False(Boleto.IsValid((string?)null!));
    }

    [Fact]
    public void Parsing_Either_Form_Produces_The_Same_Boleto()
    {
        Boleto fromBarcode = Boleto.Parse(CaixaBarcode);
        Assert.Equal(fromBarcode, Boleto.Parse(CaixaLinha));
        Assert.Equal(fromBarcode, Boleto.Parse(CaixaLinhaFormatted));
        Assert.True(fromBarcode == Boleto.Parse(CaixaLinha));
        Assert.False(fromBarcode != Boleto.Parse(CaixaLinhaFormatted));
    }

    [Fact]
    public void Parse_Extracts_Every_Caixa_Field()
    {
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        Assert.Equal(104, boleto.BankCode);
        Assert.Equal(9, boleto.CurrencyCode);
        Assert.Equal(8981, boleto.DueDateFactor);
        Assert.Equal(214.03m, boleto.Amount);
        Assert.Equal("2006561000100040099726390", boleto.FreeField);
        Assert.Equal(CaixaBarcode, boleto.Barcode);
        Assert.Equal(CaixaLinha, boleto.LinhaDigitavel);
    }

    [Fact]
    public void Parse_Extracts_Every_Itau_Field()
    {
        Boleto boleto = Boleto.Parse(ItauLinhaFormatted);
        Assert.Equal(341, boleto.BankCode);
        Assert.Equal(9, boleto.CurrencyCode);
        Assert.Equal(1677, boleto.DueDateFactor);
        Assert.Equal(1234.56m, boleto.Amount);
        Assert.Equal("8532494297019414080457407", boleto.FreeField);
        Assert.Equal(ItauBarcode, boleto.Barcode);
        Assert.Equal(ItauLinha, boleto.LinhaDigitavel);
    }

    [Fact]
    public void Parse_From_The_Linha_Digitavel_Produces_The_Known_Barcode_And_Vice_Versa()
    {
        Assert.Equal(CaixaBarcode, Boleto.Parse(CaixaLinha).Barcode);
        Assert.Equal(CaixaLinha, Boleto.Parse(CaixaBarcode).LinhaDigitavel);
        Assert.Equal(ItauBarcode, Boleto.Parse(ItauLinha).Barcode);
        Assert.Equal(ItauLinha, Boleto.Parse(ItauBarcode).LinhaDigitavel);
    }

    [Theory]
    [InlineData("10499898100000214032006561000100040099726391")]
    [InlineData("1049a98100000214032006561000100040099726390")]
    [InlineData("1049200650610001000420099726390098981000002140")]
    public void Parse_Invalid_Throws_Format_Exception_With_A_Clear_Message(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Boleto.Parse(value));
        Assert.Contains("boleto", exception.Message, StringComparison.Ordinal);
        // The message previews long values (first 16 chars + ellipsis), so assert on a prefix.
        Assert.Contains(value[..10], exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => Boleto.Parse(null!));
    }

    [Theory]
    [InlineData(CaixaBarcode)]
    [InlineData(CaixaLinha)]
    [InlineData(CaixaLinhaFormatted)]
    [InlineData(ItauBarcode)]
    [InlineData(ItauLinha)]
    public void TryParse_Valid_Outputs_The_Boleto(string value)
    {
        Assert.True(Boleto.TryParse(value, out Boleto boleto));
        Assert.Equal(Boleto.Parse(value), boleto);
        Assert.True(Boleto.IsValid(boleto.ToString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("10499898100000214032006561000100040099726391")]
    [InlineData("10492006516100010004200997263900989810000021403")]
    [InlineData("846300000003299902962024004101360008002006441147")]
    [InlineData("abc")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(Boleto.TryParse(value, out Boleto boleto));
        Assert.Equal(default, boleto);
        Assert.Equal("00000.00000 00000.000000 00000.000000 0 00000000000000", boleto.ToString());
        Assert.False(Boleto.IsValid(boleto.ToString()));
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Is_Never_Valid()
    {
        Boleto @default = default;
        Assert.Equal("00000.00000 00000.000000 00000.000000 0 00000000000000", @default.ToString());
        Assert.Equal(new string('0', 47), @default.ToString("N"));
        Assert.Equal(new string('0', 44), @default.ToString("B"));
        Assert.Equal(0, @default.BankCode);
        Assert.Equal(0, @default.CurrencyCode);
        Assert.Equal(0, @default.DueDateFactor);
        Assert.Equal(0m, @default.Amount);
        Assert.Equal(new string('0', 25), @default.FreeField);
        Assert.Null(@default.DueDate);
        Assert.False(Boleto.IsValid(@default.Barcode));
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
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        string expected = format is "N" or "n" ? CaixaLinha : format is "B" or "b" ? CaixaBarcode : CaixaLinhaFormatted;
        Assert.Equal(expected, boleto.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("NN")]
    [InlineData("C")]
    [InlineData("b n")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        Assert.Throws<FormatException>(() => boleto.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_Every_Form()
    {
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        char[] buffer = new char[55];

        Assert.True(boleto.TryFormat(buffer, out int writtenG, "G", null));
        Assert.Equal(54, writtenG);
        Assert.Equal(CaixaLinhaFormatted, new string(buffer, 0, 54));

        Assert.True(boleto.TryFormat(buffer, out int writtenN, "N", null));
        Assert.Equal(47, writtenN);
        Assert.Equal(CaixaLinha, new string(buffer, 0, 47));

        Assert.True(boleto.TryFormat(buffer, out int writtenB, "B", null));
        Assert.Equal(44, writtenB);
        Assert.Equal(CaixaBarcode, new string(buffer, 0, 44));
    }

    [Theory]
    [InlineData("G", 0)]
    [InlineData("G", 53)]
    [InlineData("N", 0)]
    [InlineData("N", 46)]
    [InlineData("B", 0)]
    [InlineData("B", 43)]
    public void TryFormat_Returns_False_When_The_Destination_Is_Too_Small(string format, int size)
    {
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        char[] buffer = new char[54];
        buffer.AsSpan().Fill('x');
        Assert.False(boleto.TryFormat(buffer.AsSpan(0, size), out int written, format, null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 54), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        Assert.Throws<FormatException>(() => boleto.TryFormat(stackalloc char[54], out _, "X", null));
    }

    [Fact]
    public void Every_Format_Parses_Back_To_The_Same_Boleto()
    {
        Boleto boleto = Boleto.Parse(ItauLinhaFormatted);
        Assert.Equal(boleto, Boleto.Parse(boleto.ToString()));
        Assert.Equal(boleto, Boleto.Parse(boleto.ToString("N")));
        Assert.Equal(boleto, Boleto.Parse(boleto.ToString("B")));
        Assert.Equal(boleto, Boleto.Parse(boleto.Barcode));
        Assert.Equal(boleto, Boleto.Parse(boleto.LinhaDigitavel));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        Boleto expected = Boleto.Parse(CaixaBarcode);
        Boleto parsed = ParsableHelpers.ParseViaSpanParsable<Boleto>(CaixaLinha.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<Boleto>(CaixaLinhaFormatted.AsSpan(), null, out Boleto result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        Boleto expected = Boleto.Parse(CaixaBarcode);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<Boleto>(CaixaLinha, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<Boleto>(CaixaLinhaFormatted, null, out Boleto result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<Boleto>(null, null, out Boleto result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<Boleto>("1234".AsSpan(), null));
    }

    [Fact]
    public void Equality_Follows_The_Barcode()
    {
        Boleto a = Boleto.Parse(CaixaBarcode);
        Boleto b = Boleto.Parse(" 10492.00650 61000.100042 00997.263900 9 89810000021403 ");
        Boleto c = Boleto.Parse(ItauBarcode);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(((IEquatable<Boleto>)a).Equals(b));
        Assert.False(((IEquatable<Boleto>)a).Equals(c));
    }

    // ---- Create ------------------------------------------------------------

    [Fact]
    public void Create_Reproduces_The_Caixa_Vector()
    {
        Boleto created = Boleto.Create(104, 9, new DateOnly(2022, 5, 10), 214.03m, "2006561000100040099726390");
        Assert.Equal(Boleto.Parse(CaixaBarcode), created);
        Assert.Equal(CaixaBarcode, created.Barcode);
        Assert.Equal(CaixaLinha, created.LinhaDigitavel);
        Assert.Equal(CaixaLinhaFormatted, created.ToString());
    }

    [Fact]
    public void Create_Reproduces_The_Itau_Vector()
    {
        Boleto created = Boleto.Create(341, 9, new DateOnly(2002, 5, 11), 1234.56m, "8532494297019414080457407");
        Assert.Equal(Boleto.Parse(ItauBarcode), created);
        Assert.Equal(ItauBarcode, created.Barcode);
        Assert.Equal(ItauLinha, created.LinhaDigitavel);
    }

    [Fact]
    public void Create_With_No_Due_Date_Encodes_Factor_Zero()
    {
        Boleto created = Boleto.Create(1, 9, null, 150m, new string('7', 25));
        Assert.Equal(0, created.DueDateFactor);
        Assert.Null(created.DueDate);
        Assert.Equal(150m, created.Amount);
        Assert.True(Boleto.IsValid(created.Barcode));
        Assert.True(Boleto.TryParse(created.ToString(), out Boleto reparsed));
        Assert.Equal(created, reparsed);
    }

    [Fact]
    public void Create_Ignores_Punctuation_In_The_Free_Field()
    {
        Boleto created = Boleto.Create(104, 9, new DateOnly(2022, 5, 10), 214.03m, "20065 61000/10004 00997-26390");
        Assert.Equal(Boleto.Parse(CaixaBarcode), created);
    }

    [Fact]
    public void Create_Round_Trips_Its_Parts()
    {
        Boleto created = Boleto.Create(748, 9, new DateOnly(2030, 3, 1), 8_765.43m, "0123456789012345678901234");
        Assert.Equal(748, created.BankCode);
        Assert.Equal(9, created.CurrencyCode);
        Assert.Equal(Boleto.Encode(new DateOnly(2030, 3, 1)), created.DueDateFactor);
        Assert.Equal(8_765.43m, created.Amount);
        Assert.Equal("0123456789012345678901234", created.FreeField);
        Assert.Equal(created, Boleto.Parse(created.Barcode));
        Assert.Equal(created, Boleto.Parse(created.LinhaDigitavel));
        Assert.Equal(created, Boleto.Parse(created.ToString()));
    }

    [Fact]
    public void Create_Allows_The_Extreme_Amounts()
    {
        Boleto zero = Boleto.Create(1, 9, null, 0m, new string('1', 25));
        Assert.Equal(0m, zero.Amount);
        Assert.True(Boleto.IsValid(zero.Barcode));

        Boleto max = Boleto.Create(999, 9, null, 99_999_999.99m, new string('1', 25));
        Assert.Equal(99_999_999.99m, max.Amount);
        Assert.True(Boleto.IsValid(max.Barcode));
        Assert.Equal("9999999999", max.Barcode.Substring(9, 10));
    }

    [Fact]
    public void Create_Validates_Its_Arguments()
    {
        DateOnly? dueDate = new DateOnly(2030, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(0, 9, dueDate, 1m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1000, 9, dueDate, 1m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1, -1, dueDate, 1m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1, 10, dueDate, 1m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1, 9, dueDate, -0.01m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1, 9, dueDate, 100_000_000m, new string('1', 25)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Create(1, 9, new DateOnly(1997, 10, 7), 1m, new string('1', 25)));
        Assert.Throws<ArgumentException>(() => Boleto.Create(1, 9, dueDate, 1.005m, new string('1', 25)));
        Assert.Throws<ArgumentException>(() => Boleto.Create(1, 9, dueDate, 1m, new string('1', 24)));
        Assert.Throws<ArgumentException>(() => Boleto.Create(1, 9, dueDate, 1m, new string('1', 26)));
        Assert.Throws<ArgumentException>(() => Boleto.Create(1, 9, dueDate, 1m, "not-25-digitsxxxxxxxxx"));
        Assert.Throws<ArgumentNullException>(() => Boleto.Create(1, 9, dueDate, 1m, null!));
    }

    [Fact]
    public void Create_Accepts_The_First_Encodable_Date()
    {
        Boleto created = Boleto.Create(1, 9, new DateOnly(1997, 10, 8), 1m, new string('1', 25));
        Assert.Equal(1, created.DueDateFactor);
    }

    // ---- Due-date factor: the 2025-02-22 reset ------------------------------

    [Theory]
    [InlineData("2025-02-20", 9998)]
    [InlineData("2025-02-21", 9999)]   // end of the first cycle
    [InlineData("2025-02-22", 1000)]   // the factor restarts at 1000
    [InlineData("2025-02-23", 1001)]
    [InlineData("1997-10-08", 1)]      // the first encodable date
    [InlineData("2002-05-11", 1677)]
    [InlineData("2022-05-10", 8981)]
    [InlineData("2024-12-31", 9947)]
    [InlineData("2049-10-13", 9999)]   // end of the second cycle
    [InlineData("2049-10-14", 1000)]  // the third cycle starts
    public void Encode_Follows_The_FEBRABAN_Factor_Rule(string dateText, int expectedFactor)
    {
        Assert.Equal(expectedFactor, Boleto.Encode(DateOnly.Parse(dateText, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1997-10-07")]   // factor 0 means "no due date" and is not encodable
    [InlineData("1997-10-06")]
    [InlineData("1900-01-01")]
    public void Encode_Before_The_First_Encodable_Date_Throws(string dateText)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Boleto.Encode(DateOnly.Parse(dateText, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Decode_Resolves_The_Closest_Cycle_Inside_The_Window()
    {
        // Factor 9999: 2025-02-21 in the first cycle or 2049-10-13 in the second.
        Assert.Equal(new DateOnly(2025, 2, 21), Boleto.Decode(9999, s_reference));
        Assert.Equal(new DateOnly(2049, 10, 13), Boleto.Decode(9999, new DateOnly(2049, 10, 13)));

        // Factor 1000: 2000-07-03 in the first cycle or 2025-02-22 in the second.
        Assert.Equal(new DateOnly(2025, 2, 22), Boleto.Decode(1000, s_reference));
        Assert.Equal(new DateOnly(2000, 7, 3), Boleto.Decode(1000, new DateOnly(2000, 7, 3)));

        // Factor 8981: 2022-05-10 (first cycle) beats 2046-12-30 (second cycle) for the reference.
        Assert.Equal(new DateOnly(2022, 5, 10), Boleto.Decode(8981, new DateOnly(2022, 5, 10)));
        Assert.Equal(new DateOnly(2022, 5, 10), Boleto.Decode(8981, s_reference));

        // Factor 1677: 2002-05-11 or 2026-12-31, resolved by the reference date.
        Assert.Equal(new DateOnly(2002, 5, 11), Boleto.Decode(1677, new DateOnly(2002, 5, 11)));
        Assert.Equal(new DateOnly(2026, 12, 31), Boleto.Decode(1677, new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void Decode_At_The_Window_Edges()
    {
        // The window of 2025-06-01 is [2017-03-15, 2040-06-22]: cycle-1 factor 7099 and
        // cycle-2 factor 6599 land exactly on the edges and still resolve.
        Assert.True(Boleto.TryDecode(7099, s_reference, out DateOnly lowEdge));
        Assert.Equal(new DateOnly(2017, 3, 15), lowEdge);
        Assert.True(Boleto.TryDecode(6599, s_reference, out DateOnly highEdge));
        Assert.Equal(new DateOnly(2040, 6, 22), highEdge);
    }

    [Theory]
    [InlineData(999)]    // first-cycle date far outside the window, no second-cycle candidate
    [InlineData(6600)]   // inside the unresolvable gap between the two cycles
    [InlineData(7098)]   // inside the unresolvable gap between the two cycles
    [InlineData(0)]      // factor 0000 means no due date
    [InlineData(-1)]
    [InlineData(10000)]
    public void TryDecode_Returns_False_For_Unresolvable_Factors(int factor)
    {
        Assert.False(Boleto.TryDecode(factor, s_reference, out DateOnly dueDate));
        Assert.Equal(default, dueDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    [InlineData(6600)]
    public void Decode_Throws_For_Unresolvable_Factors(int factor)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Boleto.Decode(factor, s_reference));
        Assert.Contains("factor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_Factor_Zero_Explains_The_No_Due_Date_Meaning()
    {
        FormatException exception = Assert.Throws<FormatException>(() => Boleto.Decode(0, s_reference));
        Assert.Contains("no due date", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Encode_And_Decode_Round_Trip_Inside_The_Window()
    {
        DateOnly[] dates =
        {
            new(1997, 10, 8),
            new(2000, 1, 1),
            new(2010, 6, 15),
            new(2020, 2, 29),
            new(2025, 2, 21),
            new(2025, 2, 22),
            new(2033, 11, 30),
            new(2040, 6, 22),
            new(2049, 10, 13),
            new(2049, 10, 14),
        };
        foreach (DateOnly date in dates)
        {
            int factor = Boleto.Encode(date);
            Assert.InRange(factor, 1, 9999);
            Assert.True(Boleto.TryDecode(factor, date, out DateOnly decoded), $"factor {factor} must resolve against {date}");
            Assert.Equal(date, decoded);
            Assert.Equal(date, Boleto.Decode(factor, date));
        }
    }

    // ---- DueDate property ---------------------------------------------------

    [Fact]
    public void DueDate_Is_Null_When_The_Factor_Is_Zero()
    {
        Assert.Null(Boleto.Create(1, 9, null, 1m, new string('1', 25)).DueDate);
    }

    [Fact]
    public void DueDate_Resolves_Against_Today()
    {
        // The DueDate property resolves against DateTime.Today at read time. Capture one
        // reference date and allow a one-day slack so a midnight rollover (or a machine in a
        // different time zone) cannot make the assertion flaky. Decode with the explicit
        // reference is asserted exactly, with no clock involved.
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        Boleto soon = Boleto.Create(1, 9, today.AddDays(10), 1m, new string('1', 25));
        DateOnly? soonDueDate = soon.DueDate;
        Assert.NotNull(soonDueDate);
        Assert.InRange(soonDueDate.Value.DayNumber, today.AddDays(9).DayNumber, today.AddDays(11).DayNumber);
        Assert.Equal(today.AddDays(10), Boleto.Decode(soon.DueDateFactor, today));

        Boleto recentPast = Boleto.Create(1, 9, today.AddDays(-30), 1m, new string('1', 25));
        DateOnly? pastDueDate = recentPast.DueDate;
        Assert.NotNull(pastDueDate);
        Assert.InRange(pastDueDate.Value.DayNumber, today.AddDays(-31).DayNumber, today.AddDays(-29).DayNumber);
        Assert.Equal(today.AddDays(-30), Boleto.Decode(recentPast.DueDateFactor, today));
    }

    [Fact]
    public void DueDate_Is_Null_When_No_Candidate_Falls_Inside_The_Window()
    {
        // -3001 days is outside the [-3000, +5500] decoding window by a whole day, so a
        // midnight rollover of the local clock cannot change the expected result.
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        Boleto boleto = Boleto.Create(1, 9, today.AddDays(-3001), 1m, new string('1', 25));
        Assert.Null(boleto.DueDate);
    }

    [Fact]
    public void The_Factor_Of_The_Real_Vectors_Resolves_To_Their_Due_Dates()
    {
        // The Caixa vector carries factor 8981; against its own due date it resolves to 2022-05-10.
        Boleto boleto = Boleto.Parse(CaixaBarcode);
        Assert.Equal(8981, boleto.DueDateFactor);
        Assert.True(Boleto.TryDecode(boleto.DueDateFactor, new DateOnly(2022, 5, 10), out DateOnly dueDate));
        Assert.Equal(new DateOnly(2022, 5, 10), dueDate);

        // The Itaú vector carries factor 1677: first-cycle date 2002-05-11, second-cycle 2026-12-31.
        Boleto itau = Boleto.Parse(ItauBarcode);
        Assert.Equal(1677, itau.DueDateFactor);
        Assert.Equal(new DateOnly(2002, 5, 11), Boleto.Decode(1677, new DateOnly(2002, 5, 11)));
        Assert.Equal(new DateOnly(2026, 12, 31), Boleto.Decode(1677, new DateOnly(2026, 12, 31)));
    }

    // ---- Generate -----------------------------------------------------------

    [Fact]
    public void Generate_Produces_Valid_Boletos_That_Round_Trip()
    {
        var banks = new HashSet<int>();
        for (int i = 0; i < 500; i++)
        {
            Boleto boleto = Boleto.Generate(new Random(i));
            Assert.True(Boleto.IsValid(boleto.Barcode), $"generated {boleto.Barcode} must be valid");
            Assert.True(Boleto.IsValid(boleto.LinhaDigitavel), "the generated linha digitável must be valid");
            Assert.True(Oracles.IsValidBoleto(boleto.Barcode), "the oracle must accept the generated boleto");
            Assert.Equal(44, boleto.Barcode.Length);
            Assert.Equal(47, boleto.LinhaDigitavel.Length);
            Assert.Equal(boleto, Boleto.Parse(boleto.Barcode));
            Assert.Equal(boleto, Boleto.Parse(boleto.LinhaDigitavel));
            Assert.Equal(boleto, Boleto.Parse(boleto.ToString()));
            Assert.InRange(boleto.BankCode, 1, 999);
            Assert.Equal(9, boleto.CurrencyCode);
            Assert.InRange(boleto.Amount, 0m, 999_999.99m);
            Assert.Equal(25, boleto.FreeField.Length);
            Assert.NotNull(boleto.DueDate);
            Assert.Equal(boleto.DueDateFactor, Boleto.Encode(boleto.DueDate!.Value));
            banks.Add(boleto.BankCode);
        }
        Assert.True(banks.Count >= 50, $"expected at least 50 distinct banks, got {banks.Count}");
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        Boleto[] Run(Random random) => Enumerable.Range(0, 25).Select(_ => Boleto.Generate(random)).ToArray();

        Assert.Equal(Run(new Random(4711)), Run(new Random(4711)));
        Assert.NotEqual(Run(new Random(1)), Run(new Random(2)));
    }

    [Fact]
    public void Parse_Is_Culture_Invariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Boleto boleto = Boleto.Parse(CaixaLinhaFormatted);
            Assert.Equal(CaixaLinhaFormatted, boleto.ToString());
            Assert.Equal(CaixaLinha, boleto.ToString("N"));
            Assert.Equal(CaixaBarcode, boleto.ToString("B"));
            Assert.Equal(214.03m, boleto.Amount);
            Assert.Equal(CaixaLinhaFormatted, ((IFormattable)boleto).ToString("G", CultureInfo.GetCultureInfo("ar-SA")));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
