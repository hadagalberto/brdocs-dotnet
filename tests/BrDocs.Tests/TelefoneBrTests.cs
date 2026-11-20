using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

public sealed class TelefoneBrTests
{
    private const string ValidMobileFormatted = "(11) 91234-5678";
    private const string ValidMobileNational = "11912345678";
    private const string ValidMobileE164 = "+5511912345678";

    [Theory]
    [InlineData("11912345678")]        // local: DDD + 9-digit mobile
    [InlineData("(11) 91234-5678")]   // display form
    [InlineData("+5511912345678")]     // E.164
    [InlineData("+55 11 91234-5678")]
    [InlineData("+55 (11) 91234-5678")]
    [InlineData("5511987654321")]      // bare country code, no +
    [InlineData("55 11 98765-4321")]
    [InlineData("+55/11/98765-4321")]
    [InlineData("1131234567")]         // local: DDD + 8-digit fixed
    [InlineData("(11) 3123-4567")]
    [InlineData("+551131234567")]
    [InlineData("+55 11 3123-4567")]
    [InlineData("551131234567")]       // bare country code + fixed
    [InlineData("5531234567")]         // DDD 55 (RS) fixed: the leading 55 is NOT a country code
    [InlineData("55987654321")]        // DDD 55 (RS) mobile
    [InlineData(" +5511987654321")]    // ignored chars around the + are fine
    [InlineData("1 1 9 1 2 3 4 5 6 7 8")]
    public void IsValid_Returns_True_For_Every_Accepted_Form(string value)
    {
        Assert.True(TelefoneBr.IsValid(value));
    }

    [Theory]
    [InlineData("11 91234 5678")]
    [InlineData("( 11 )91234-5678")]
    [InlineData("  (11)  91234-5678  ")]
    [InlineData("11-91234-5678")]
    [InlineData("11.91234.5678")]
    [InlineData("11/91234/5678")]
    public void IsValid_Ignores_Standard_Punctuation_Anywhere(string value)
    {
        Assert.True(TelefoneBr.IsValid(value));
    }

    [Theory]
    [InlineData("11712345678")]       // 9 digits starting with 7: not a mobile (SPEC: mobile starts with 9)
    [InlineData("11812345678")]       // 9 digits starting with 8
    [InlineData("11612345678")]       // 9 digits starting with 6
    [InlineData("11512345678")]       // 9 digits starting with 5
    [InlineData("11312345678")]       // 9 digits starting with 3
    [InlineData("1112345678")]        // 8 digits starting with 1: not a fixed line (2–6)
    [InlineData("1191234567")]        // 8 digits starting with 9: not a fixed line
    [InlineData("1171234567")]        // 8 digits starting with 7
    [InlineData("08001234567")]        // non-geographic service number: DDD 08 does not exist
    [InlineData("005511987654321")]    // the legacy 00 dialing prefix is out of scope
    [InlineData("551198765432")]       // DDD 55 + 9 digits starting with 1: not a mobile
    [InlineData("1191234567890")]      // too long
    [InlineData("55119876543210")]     // too long with the country code
    [InlineData("+11987654321")]       // '+' without the country code
    [InlineData("55+11987654321")]     // '+' after digits
    [InlineData("++5511987654321")]    // double '+'
    [InlineData("+55119876543 21+")]  // '+' after digits at the end
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("....")]
    [InlineData("telefone 11987654321")]
    [InlineData("1198765432a")]
    [InlineData("(11) 91234-567a")]
    [InlineData("１１９８７６５４３２１")] // fullwidth digits
    [InlineData("11987654321\n")]
    [InlineData("\t11987654321")]
    public void IsValid_Returns_False_For_Invalid_Texts(string value)
    {
        Assert.False(TelefoneBr.IsValid(value));
    }

    [Fact]
    public void IsValid_Null_String_Yields_An_Empty_Span_And_Is_Invalid()
    {
        // The implicit string -> ReadOnlySpan<char> conversion maps a null string to an empty
        // span, so validation simply reports the value as invalid instead of throwing.
        Assert.False(TelefoneBr.IsValid((string?)null!));
    }

    // Every one of the 67 ANATEL Códigos Nacionais accepts a mobile number.

    [Theory]
    [InlineData("11912345678")]
    [InlineData("12912345678")]
    [InlineData("13912345678")]
    [InlineData("14912345678")]
    [InlineData("15912345678")]
    [InlineData("16912345678")]
    [InlineData("17912345678")]
    [InlineData("18912345678")]
    [InlineData("19912345678")]
    [InlineData("21912345678")]
    [InlineData("22912345678")]
    [InlineData("24912345678")]
    [InlineData("27912345678")]
    [InlineData("28912345678")]
    [InlineData("31912345678")]
    [InlineData("32912345678")]
    [InlineData("33912345678")]
    [InlineData("34912345678")]
    [InlineData("35912345678")]
    [InlineData("37912345678")]
    [InlineData("38912345678")]
    [InlineData("41912345678")]
    [InlineData("42912345678")]
    [InlineData("43912345678")]
    [InlineData("44912345678")]
    [InlineData("45912345678")]
    [InlineData("46912345678")]
    [InlineData("47912345678")]
    [InlineData("48912345678")]
    [InlineData("49912345678")]
    [InlineData("51912345678")]
    [InlineData("53912345678")]
    [InlineData("54912345678")]
    [InlineData("55912345678")]
    [InlineData("61912345678")]
    [InlineData("62912345678")]
    [InlineData("63912345678")]
    [InlineData("64912345678")]
    [InlineData("65912345678")]
    [InlineData("66912345678")]
    [InlineData("67912345678")]
    [InlineData("68912345678")]
    [InlineData("69912345678")]
    [InlineData("71912345678")]
    [InlineData("73912345678")]
    [InlineData("74912345678")]
    [InlineData("75912345678")]
    [InlineData("77912345678")]
    [InlineData("79912345678")]
    [InlineData("81912345678")]
    [InlineData("82912345678")]
    [InlineData("83912345678")]
    [InlineData("84912345678")]
    [InlineData("85912345678")]
    [InlineData("86912345678")]
    [InlineData("87912345678")]
    [InlineData("88912345678")]
    [InlineData("89912345678")]
    [InlineData("91912345678")]
    [InlineData("92912345678")]
    [InlineData("93912345678")]
    [InlineData("94912345678")]
    [InlineData("95912345678")]
    [InlineData("96912345678")]
    [InlineData("97912345678")]
    [InlineData("98912345678")]
    [InlineData("99912345678")]
    public void IsValid_Accepts_Every_Anatel_Ddd_For_Mobile_Numbers(string value)
    {
        Assert.True(TelefoneBr.IsValid(value));
        Assert.Equal(int.Parse(value[..2], CultureInfo.InvariantCulture), TelefoneBr.Parse(value).Ddd);
    }

    // The 23 two-digit codes that ANATEL never assigned (plus 00–09) are not DDDs.

    [Theory]
    [InlineData("10")]
    [InlineData("20")]
    [InlineData("23")]
    [InlineData("25")]
    [InlineData("26")]
    [InlineData("29")]
    [InlineData("30")]
    [InlineData("36")]
    [InlineData("39")]
    [InlineData("40")]
    [InlineData("50")]
    [InlineData("52")]
    [InlineData("56")]
    [InlineData("57")]
    [InlineData("58")]
    [InlineData("59")]
    [InlineData("60")]
    [InlineData("70")]
    [InlineData("72")]
    [InlineData("76")]
    [InlineData("78")]
    [InlineData("80")]
    [InlineData("90")]
    [InlineData("00")]
    [InlineData("01")]
    [InlineData("05")]
    [InlineData("09")]
    public void IsValid_Rejects_Every_Unassigned_Ddd_Code(string code)
    {
        Assert.False(TelefoneBr.IsValid($"{code}912345678"), $"mobile DDD {code} must be invalid");
        Assert.False(TelefoneBr.IsValid($"{code}31234567"), $"fixed DDD {code} must be invalid");
        Assert.False(TelefoneBr.TryParse($"+55{code}912345678", out _));
    }

    [Fact]
    public void Every_Anatel_Ddd_Accepts_A_Fixed_Number_Too()
    {
        foreach (int ddd in Oracles.ValidDdds)
        {
            string value = $"{ddd}31234567";
            Assert.True(TelefoneBr.IsValid(value), $"fixed {value} must be valid");
            TelefoneBr phone = TelefoneBr.Parse(value);
            Assert.Equal(ddd, phone.Ddd);
            Assert.True(phone.IsFixed);
        }
    }

    [Theory]
    [InlineData("(11) 91234-5678", "(11) 91234-5678", "11912345678", 11, "912345678", true)]
    [InlineData("11912345678", "(11) 91234-5678", "11912345678", 11, "912345678", true)]
    [InlineData("+5511912345678", "(11) 91234-5678", "11912345678", 11, "912345678", true)]
    [InlineData("+55 11 91234-5678", "(11) 91234-5678", "11912345678", 11, "912345678", true)]
    [InlineData("5511987654321", "(11) 98765-4321", "11987654321", 11, "987654321", true)]
    [InlineData("55987654321", "(55) 98765-4321", "55987654321", 55, "987654321", true)]
    [InlineData("1131234567", "(11) 3123-4567", "1131234567", 11, "31234567", false)]
    [InlineData("(11) 3123-4567", "(11) 3123-4567", "1131234567", 11, "31234567", false)]
    [InlineData("+551131234567", "(11) 3123-4567", "1131234567", 11, "31234567", false)]
    [InlineData("5531234567", "(55) 3123-4567", "5531234567", 55, "31234567", false)]
    [InlineData("551131234567", "(11) 3123-4567", "1131234567", 11, "31234567", false)]
    public void Parse_Returns_The_Phone_For_Valid_Input(
        string value, string expectedFormatted, string expectedNational, int expectedDdd, string expectedSubscriber, bool expectedMobile)
    {
        TelefoneBr phone = TelefoneBr.Parse(value);
        Assert.Equal(expectedFormatted, phone.ToString());
        Assert.Equal(expectedNational, phone.ToString("N"));
        Assert.Equal(expectedDdd, phone.Ddd);
        Assert.Equal(expectedSubscriber, phone.Subscriber);
        Assert.Equal(expectedMobile, phone.IsMobile);
        Assert.Equal(!expectedMobile, phone.IsFixed);
    }

    [Fact]
    public void Parse_Produces_Culture_Invariant_Formatted_Output()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
            Assert.Equal(ValidMobileFormatted, phone.ToString());
            Assert.Equal(ValidMobileNational, phone.ToString("N"));
            Assert.Equal(ValidMobileNational, ((IFormattable)phone).ToString("N", CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(ValidMobileFormatted, ((IFormattable)phone).ToString("G", CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Null_Throws_Argument_Null_Exception()
    {
        Assert.Throws<ArgumentNullException>(() => TelefoneBr.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a phone")]
    [InlineData("11712345678")]
    [InlineData("10912345678")]
    [InlineData("55119876543210")]
    [InlineData("+11987654321")]
    public void Parse_Invalid_Throws_Format_Exception(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() => TelefoneBr.Parse(value));
        Assert.Contains("phone", exception.Message, StringComparison.Ordinal);
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Truncates_Long_Values_In_The_Exception_Message()
    {
        string longInvalid = "11912345678" + new string('5', 100);
        FormatException exception = Assert.Throws<FormatException>(() => TelefoneBr.Parse(longInvalid));
        Assert.Contains("...", exception.Message);
        Assert.True(exception.Message.Length < 120, "the message must be truncated");
    }

    [Theory]
    [InlineData("")]
    [InlineData("11712345678")]
    [InlineData("1191234567890")]
    [InlineData("11,91234,5678")]
    [InlineData("10912345678")]
    public void TryParse_Invalid_Outputs_Default(string value)
    {
        Assert.False(TelefoneBr.TryParse(value, out TelefoneBr phone));
        Assert.Equal(default, phone);
        Assert.Equal("(00) 0000-0000", phone.ToString());
        Assert.Equal(0, phone.Ddd);
    }

    [Fact]
    public void TryParse_Valid_Outputs_The_Phone()
    {
        Assert.True(TelefoneBr.TryParse(ValidMobileFormatted, out TelefoneBr phone));
        Assert.Equal(ValidMobileNational, phone.ToString("N"));
        Assert.True(TelefoneBr.TryParse(ValidMobileNational, out TelefoneBr raw));
        Assert.Equal(phone, raw);
        Assert.True(TelefoneBr.TryParse(ValidMobileE164, out TelefoneBr e164));
        Assert.Equal(phone, e164);
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
        TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
        string expected = format is "N" or "n" ? ValidMobileNational : ValidMobileFormatted;
        Assert.Equal(expected, phone.ToString(format, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("GG")]
    [InlineData(" D")]
    [InlineData("0")]
    public void ToString_Unknown_Format_Throws(string format)
    {
        TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
        Assert.Throws<FormatException>(() => phone.ToString(format, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TryFormat_Writes_The_Mobile_Display_Form()
    {
        TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
        Span<char> buffer = stackalloc char[15];
        Assert.True(phone.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(15, written);
        Assert.Equal(ValidMobileFormatted, new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_Fixed_Display_Form()
    {
        TelefoneBr phone = TelefoneBr.Parse("1131234567");
        Span<char> buffer = stackalloc char[14];
        Assert.True(phone.TryFormat(buffer, out int written, "G", null));
        Assert.Equal(14, written);
        Assert.Equal("(11) 3123-4567", new string(buffer));
    }

    [Fact]
    public void TryFormat_Writes_The_National_Digits()
    {
        TelefoneBr mobile = TelefoneBr.Parse(ValidMobileFormatted);
        Span<char> mobileBuffer = stackalloc char[11];
        Assert.True(mobile.TryFormat(mobileBuffer, out int mobileWritten, "N", null));
        Assert.Equal(11, mobileWritten);
        Assert.Equal(ValidMobileNational, new string(mobileBuffer));

        TelefoneBr fixedLine = TelefoneBr.Parse("(11) 3123-4567");
        Span<char> fixedBuffer = stackalloc char[10];
        Assert.True(fixedLine.TryFormat(fixedBuffer, out int fixedWritten, "N", null));
        Assert.Equal(10, fixedWritten);
        Assert.Equal("1131234567", new string(fixedBuffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void TryFormat_Returns_False_When_The_Mobile_Destination_Is_Too_Small(int size)
    {
        TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
        char[] buffer = new char[15];
        buffer.AsSpan().Fill('x');
        Assert.False(phone.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 15), new string(buffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void TryFormat_Returns_False_When_The_Fixed_Destination_Is_Too_Small(int size)
    {
        TelefoneBr phone = TelefoneBr.Parse("1131234567");
        char[] buffer = new char[14];
        buffer.AsSpan().Fill('x');
        Assert.False(phone.TryFormat(buffer.AsSpan(0, size), out int written, "G", null));
        Assert.Equal(0, written);
        Assert.Equal(new string('x', 14), new string(buffer));
    }

    [Fact]
    public void TryFormat_Unknown_Format_Throws()
    {
        TelefoneBr phone = TelefoneBr.Parse(ValidMobileNational);
        Assert.Throws<FormatException>(() => phone.TryFormat(stackalloc char[15], out _, "X", null));
    }

    [Fact]
    public void Span_Parsable_Interface_Round_Trips()
    {
        TelefoneBr expected = TelefoneBr.Parse(ValidMobileNational);
        TelefoneBr parsed = ParsableHelpers.ParseViaSpanParsable<TelefoneBr>(ValidMobileFormatted.AsSpan(), null);
        Assert.Equal(expected, parsed);
        Assert.True(ParsableHelpers.TryParseViaSpanParsable<TelefoneBr>(ValidMobileE164.AsSpan(), null, out TelefoneBr result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_Round_Trips()
    {
        TelefoneBr expected = TelefoneBr.Parse(ValidMobileNational);
        Assert.Equal(expected, ParsableHelpers.ParseViaParsable<TelefoneBr>(ValidMobileFormatted, null));
        Assert.True(ParsableHelpers.TryParseViaParsable<TelefoneBr>(ValidMobileE164, null, out TelefoneBr result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parsable_Interface_TryParse_Null_Returns_False()
    {
        Assert.False(ParsableHelpers.TryParseViaParsable<TelefoneBr>(null, null, out TelefoneBr result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Span_Parsable_Parse_Invalid_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() => ParsableHelpers.ParseViaSpanParsable<TelefoneBr>("11712345678".AsSpan(), null));
    }

    [Fact]
    public void Equality_Ignores_The_Input_Form()
    {
        TelefoneBr a = TelefoneBr.Parse("(11) 91234-5678");
        TelefoneBr b = TelefoneBr.Parse("11912345678");
        TelefoneBr c = TelefoneBr.Parse("+5511912345678");
        TelefoneBr d = TelefoneBr.Parse("5511912345678");
        TelefoneBr e = TelefoneBr.Parse("+55 11 91234-5678");
        TelefoneBr f = TelefoneBr.Parse("(11) 3123-4567");
        Assert.Equal(a, b);
        Assert.Equal(a, c);
        Assert.Equal(a, d);
        Assert.Equal(a, e);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, f);
        Assert.True(((IEquatable<TelefoneBr>)a).Equals(b));
        Assert.False(((IEquatable<TelefoneBr>)a).Equals(f));
    }

    [Theory]
    [InlineData("11912345678", "+5511912345678")]
    [InlineData("(11) 91234-5678", "+5511912345678")]
    [InlineData("1131234567", "+551131234567")]
    [InlineData("(11) 3123-4567", "+551131234567")]
    [InlineData("5531234567", "+555531234567")]
    [InlineData("55987654321", "+5555987654321")]
    [InlineData("5511987654321", "+5511987654321")]
    public void ToE164_Returns_The_International_Form_And_Round_Trips(string value, string expectedE164)
    {
        TelefoneBr phone = TelefoneBr.Parse(value);
        Assert.Equal(expectedE164, phone.ToE164());
        Assert.True(TelefoneBr.TryParse(expectedE164, out TelefoneBr roundTripped));
        Assert.Equal(phone, roundTripped);
        Assert.Equal(phone, TelefoneBr.Parse(phone.ToE164()));
    }

    [Fact]
    public void ToE164_Lengths_Follow_The_Number_Type()
    {
        Assert.Equal(14, TelefoneBr.Parse("11912345678").ToE164().Length);
        Assert.Equal(13, TelefoneBr.Parse("1131234567").ToE164().Length);
    }

    [Fact]
    public void Subscriber_Excludes_The_Ddd()
    {
        Assert.Equal("912345678", TelefoneBr.Parse("(11) 91234-5678").Subscriber);
        Assert.Equal(9, TelefoneBr.Parse("(11) 91234-5678").Subscriber.Length);
        Assert.Equal("31234567", TelefoneBr.Parse("(11) 3123-4567").Subscriber);
        Assert.Equal(8, TelefoneBr.Parse("(11) 3123-4567").Subscriber.Length);
        Assert.Equal("987654321", TelefoneBr.Parse("(55) 98765-4321").Subscriber);
    }

    [Fact]
    public void Round_Trip_Preserves_The_Value()
    {
        TelefoneBr original = TelefoneBr.Parse("(21) 98765-4321");
        Assert.Equal("(21) 98765-4321", original.ToString());
        Assert.Equal(original, TelefoneBr.Parse(original.ToString()));
        Assert.Equal(original.ToString("N"), TelefoneBr.Parse(original.ToString("N")).ToString("N"));
        Assert.Equal(21, original.Ddd);
        Assert.Equal("987654321", original.Subscriber);
    }

    [Fact]
    public void Default_Is_The_All_Zero_Digits_And_Invalid()
    {
        TelefoneBr @default = default;
        Assert.Equal("(00) 0000-0000", @default.ToString());
        Assert.Equal("0000000000", @default.ToString("N"));
        Assert.False(TelefoneBr.IsValid(@default.ToString()));
        Assert.Equal(0, @default.Ddd);
    }

    [Fact]
    public void Generate_Returns_Valid_Mobile_Phones()
    {
        for (int i = 0; i < 1_000; i++)
        {
            TelefoneBr phone = TelefoneBr.Generate();
            string national = phone.ToString("N");
            Assert.Equal(11, national.Length);
            Assert.True(TelefoneBr.IsValid(national), $"generated {national} must be valid");
            Assert.True(Oracles.IsValidTelefoneBr(national), $"generated {national} must satisfy the oracle too");
            Assert.True(phone.IsMobile);
            Assert.Equal('9', national[2]);
            Assert.Contains(phone.Ddd, Oracles.ValidDdds);
            Assert.Equal(phone, TelefoneBr.Parse(national));
            Assert.Equal(phone, TelefoneBr.Parse(phone.ToE164()));
        }
    }

    [Fact]
    public void GenerateFixed_Returns_Valid_Fixed_Phones()
    {
        for (int i = 0; i < 1_000; i++)
        {
            TelefoneBr phone = TelefoneBr.GenerateFixed();
            string national = phone.ToString("N");
            Assert.Equal(10, national.Length);
            Assert.True(TelefoneBr.IsValid(national), $"generated {national} must be valid");
            Assert.True(Oracles.IsValidTelefoneBr(national), $"generated {national} must satisfy the oracle too");
            Assert.True(phone.IsFixed);
            Assert.InRange(national[2], '2', '5');
            Assert.Contains(phone.Ddd, Oracles.ValidDdds);
            Assert.Equal(phone, TelefoneBr.Parse(national));
        }
    }

    [Fact]
    public void Generate_Covers_All_Anatel_Ddds()
    {
        var ddds = new HashSet<int>();
        for (int i = 0; i < 5_000; i++)
        {
            ddds.Add(TelefoneBr.Generate(new Random(i)).Ddd);
        }
        foreach (int ddd in Oracles.ValidDdds)
        {
            Assert.True(ddds.Contains(ddd), $"DDD {ddd} must be generated");
        }
    }

    [Fact]
    public void Generate_Uses_The_Provided_Random_And_Is_Deterministic_Per_Seed()
    {
        var first = new Random(12345);
        var second = new Random(12345);
        var third = new Random(54321);
        TelefoneBr[] firstRun = Enumerable.Range(0, 100).Select(_ => TelefoneBr.Generate(first)).ToArray();
        TelefoneBr[] secondRun = Enumerable.Range(0, 100).Select(_ => TelefoneBr.Generate(second)).ToArray();
        TelefoneBr[] thirdRun = Enumerable.Range(0, 100).Select(_ => TelefoneBr.Generate(third)).ToArray();
        Assert.Equal(firstRun, secondRun);
        Assert.NotEqual(firstRun, thirdRun);
    }

    [Fact]
    public void Generate_Without_A_Random_Uses_The_Shared_Instance()
    {
        TelefoneBr phone = TelefoneBr.Generate();
        Assert.True(TelefoneBr.IsValid(phone.ToString("N")));
        TelefoneBr also = TelefoneBr.Generate(rng: null);
        Assert.True(TelefoneBr.IsValid(also.ToString("N")));
    }
}
