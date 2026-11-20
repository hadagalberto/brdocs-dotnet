using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Tests for the Pix keys (DICT keys): detection and validation of the five canonical forms and
/// normalization of formatted user input — "Manual de Padrões para Iniciação do Pix", BCB,
/// section 2.5.1.
/// </summary>
public sealed class PixKeyTests
{
    // Known-valid vectors of every key type.
    private const string ValidCpf = "11144477735";
    private const string ValidNumericCnpj = "11222333000181";
    private const string ValidAlphanumericCnpj = "12ABC34501DE35";
    private const string ValidEmail = "fulano_da_silva.recebedor@example.com";
    private const string ValidPhone = "+5561912345678";
    private const string ValidEvp = "123e4567-e12b-12d1-a456-426655440000";

    [Theory]
    [InlineData(ValidCpf, (int)PixKeyKind.Cpf)]
    [InlineData("52998224725", (int)PixKeyKind.Cpf)]
    [InlineData(ValidNumericCnpj, (int)PixKeyKind.Cnpj)]
    [InlineData(ValidAlphanumericCnpj, (int)PixKeyKind.Cnpj)]
    [InlineData(ValidEmail, (int)PixKeyKind.Email)]
    [InlineData("a.b@c.com.br", (int)PixKeyKind.Email)]
    [InlineData(ValidPhone, (int)PixKeyKind.Phone)]
    [InlineData("+5511912345678", (int)PixKeyKind.Phone)]
    [InlineData(ValidEvp, (int)PixKeyKind.Evp)]
    [InlineData("123E4567-E12B-12D1-A456-426655440000", (int)PixKeyKind.Evp)]
    public void Detect_Returns_The_Kind_Of_Every_Valid_Key(string key, int expectedKind)
    {
        Assert.Equal((PixKeyKind)expectedKind, PixKey.Detect(key));
        Assert.True(PixKey.TryDetect(key, out PixKeyKind kind));
        Assert.Equal((PixKeyKind)expectedKind, kind);
        Assert.True(PixKey.IsValid(key));
    }

    [Fact]
    public void Per_Kind_Checks_Agree_With_Detection()
    {
        string cpf = "11144477735";
        string cnpj = "12ABC34501DE35";
        string email = "fulano@example.com";
        string phone = "+5511912345678";
        string evp = "123e4567-e12b-12d1-a456-426655440000";
        Assert.True(PixKey.IsCpf(cpf) && !PixKey.IsCnpj(cpf));
        Assert.True(PixKey.IsCnpj(cnpj) && !PixKey.IsCpf(cnpj));
        Assert.True(PixKey.IsEmail(email) && !PixKey.IsEmail("fulano"));
        Assert.True(PixKey.IsPhone(phone) && !PixKey.IsPhone("5511912345678"));
        Assert.True(PixKey.IsEvp(evp) && !PixKey.IsEvp("123e4567-e12b-12d1-a456-42665544"));
        Assert.Equal(PixKeyKind.Cpf, PixKey.Detect(cpf));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect(cnpj));
        Assert.Equal(PixKeyKind.Email, PixKey.Detect(email));
        Assert.Equal(PixKeyKind.Phone, PixKey.Detect(phone));
        Assert.Equal(PixKeyKind.Evp, PixKey.Detect(evp));
    }

    [Theory]
    [InlineData("")]                                                // empty
    [InlineData("11144477736")]                                      // CPF with a wrong DV
    [InlineData("11111111111")]                                      // all-same digits
    [InlineData("111.444.777-35")]                                   // formatted CPF: not canonical
    [InlineData("11222333000182")]                                   // numeric CNPJ with a wrong DV
    [InlineData("00.000.000/0000-00")]                               // formatted all-zero CNPJ
    [InlineData("12abc34501de35")]                                   // lowercase CNPJ: not canonical
    [InlineData("11222333000181 ")]                                  // trailing space
    [InlineData(" 11222333000181")]                                  // leading space
    [InlineData("A00038166000105")]                                  // 15 characters
    [InlineData("emailsemprofundidade")]                            // no '@'
    [InlineData("@example.com")]                                     // empty local part
    [InlineData("fulano@example")]                                  // no dot in the domain
    [InlineData("fulano@example.c")]                                // 1-character TLD
    [InlineData("fulano..da@example.com")]                          // consecutive dots
    [InlineData("fulano.@example.com")]                             // trailing dot in the local part
    [InlineData(".fulano@example.com")]                             // leading dot in the local part
    [InlineData("fulano@-example.com")]                             // hyphen-terminated label
    [InlineData("fulano@example..com")]                             // empty domain label
    [InlineData("fulano da silva@example.com")]                     // space in the local part
    [InlineData("+551131234567")]                                    // fixed line: keys are mobiles only
    [InlineData("+556191234567")]                                    // 8-digit subscriber starting with 9
    [InlineData("+5561312345678")]                                   // mobile does not start with 9
    [InlineData("+55619123456789")]                                  // 10-digit subscriber
    [InlineData("+5461912345678")]                                   // not +55
    [InlineData("5561912345678")]                                    // missing the '+'
    [InlineData("+5520123456789")]                                   // DDD 20 does not exist
    [InlineData("123e4567-e12b-12d1-a456-42665544000")]              // 35 characters
    [InlineData("123e4567e12b-12d1-a456-426655440000")]              // hyphen at the wrong position
    [InlineData("123e4567-e12b-12d1-a456-42665544000g")]             // non-hex character
    [InlineData("hello")]
    [InlineData("+")]
    [InlineData("@")]
    public void IsValid_Returns_False_For_Invalid_Or_Non_Canonical_Keys(string key)
    {
        Assert.False(PixKey.IsValid(key));
        Assert.Null(PixKey.Detect(key));
        Assert.False(PixKey.TryDetect(key, out _));
    }

    [Fact]
    public void Canonical_Keys_Are_Shorter_Than_The_77_Character_Dict_Limit()
    {
        // The longest possible key is a 77-character e-mail.
        string email = new string('a', 64) + "@" + new string('b', 8) + ".com";
        Assert.Equal(77, email.Length);
        Assert.True(PixKey.IsEmail(email));
        string tooLong = new string('a', 65) + "@example.com";
        Assert.False(PixKey.IsEmail(tooLong));
    }

    [Fact]
    public void Evp_Does_Not_Require_Rfc4122_Version_Nibbles()
    {
        // The BCB manual's own example key (and the SPEC vector) does not carry the RFC 4122 v4
        // version/variant bits, so only the canonical 8-4-4-4-12 form is enforced.
        Assert.True(PixKey.IsEvp(ValidEvp));
        Assert.Equal(PixKeyKind.Evp, PixKey.Detect(ValidEvp));
    }

    [Theory]
    [InlineData("111.444.777-35", ValidCpf)]                          // formatted CPF
    [InlineData("111 444 777 35", ValidCpf)]
    [InlineData("11.222.333/0001-81", ValidNumericCnpj)]              // formatted numeric CNPJ
    [InlineData("12.ABC.345/01DE-35", ValidAlphanumericCnpj)]          // formatted alphanumeric CNPJ
    [InlineData("12.abc.345/01de-35", ValidAlphanumericCnpj)]           // lowercase alphanumeric CNPJ
    [InlineData("12ABC34501DE35", ValidAlphanumericCnpj)]
    [InlineData("(11) 91234-5678", "+5511912345678")]                 // display form
    [InlineData("11 91234 5678", "+5511912345678")]                   // local form
    [InlineData("11912345678", "+5511912345678")]
    [InlineData("+55 (61) 91234-5678", ValidPhone)]
    [InlineData("5561912345678", ValidPhone)]                        // bare country code
    [InlineData("55 61 91234 5678", ValidPhone)]
    [InlineData("+5561912345678", ValidPhone)]                        // already canonical
    [InlineData("123E4567-E12B-12D1-A456-426655440000", ValidEvp)]     // uppercased UUID is lowered
    [InlineData(ValidEmail, ValidEmail)]                              // e-mails are used as registered
    [InlineData("a.b@c.com.br", "a.b@c.com.br")]
    public void TryNormalize_Canonicalizes_Formatted_Input(string input, string expected)
    {
        Span<char> buffer = stackalloc char[PixKey.MaxLength];
        Assert.True(PixKey.TryNormalize(input, buffer, out int written));
        Assert.Equal(expected, new string(buffer[..written]));
        Assert.Equal(expected, PixKey.Normalize(input));
        Assert.True(PixKey.IsValid(new string(buffer[..written])));
    }

    [Fact]
    public void An_Eleven_Digit_String_Is_Normalized_As_A_Cpf_When_It_Is_One()
    {
        // "11144477735" is a valid CPF and also happens to look like a local mobile number
        // (DDD 11 + 944477735); the documented CPF form wins.
        Assert.Equal(PixKeyKind.Cpf, PixKey.Detect(ValidCpf));
        Assert.Equal(ValidCpf, PixKey.Normalize(ValidCpf));
    }

    [Fact]
    public void An_Eleven_Digit_String_That_Is_Not_A_Cpf_Falls_Back_To_A_Phone()
    {
        // "11987654325" is not a valid CPF, but parses as the local mobile 11 98765-4325.
        Assert.False(Cpf.IsValid("11987654325"));
        Assert.Equal("+5511987654325", PixKey.Normalize("11987654325"));
    }

    [Theory]
    [InlineData("(11) 3123-4567")]     // fixed line
    [InlineData("1131234567")]
    [InlineData("1191234567")]         // 8-digit subscriber starting with 9
    [InlineData("hello")]
    [InlineData("11144477736")]        // invalid CPF, invalid phone (DDD 11 + 3-digit-prefixed number)
    [InlineData("111.444.777-36")]     // invalid formatted CPF
    [InlineData("12.ABC.345/01DE-36")] // invalid alphanumeric CNPJ
    [InlineData("foo bar@example.com")]
    public void TryNormalize_Returns_False_For_Input_That_Is_No_Key(string input)
    {
        Span<char> buffer = stackalloc char[PixKey.MaxLength];
        Assert.False(PixKey.TryNormalize(input, buffer, out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryNormalize_Returns_False_When_The_Destination_Is_Too_Small()
    {
        Span<char> tiny = stackalloc char[3];
        Assert.False(PixKey.TryNormalize(ValidPhone, tiny, out _));
        Span<char> cpfSized = stackalloc char[11];
        Assert.False(PixKey.TryNormalize(ValidEmail, cpfSized, out _));
    }

    [Fact]
    public void Normalize_Throws_With_A_Clear_Message_For_Invalid_Input()
    {
        FormatException exception = Assert.Throws<FormatException>(() => PixKey.Normalize("not a key"));
        Assert.Contains("not a key", exception.Message);
        Assert.Contains("CPF", exception.Message);
    }

    [Fact]
    public void Normalize_Throws_For_Null()
    {
        Assert.Throws<ArgumentNullException>(() => PixKey.Normalize(null!));
    }

    [Fact]
    public void Detect_Returns_Null_For_The_Empty_Span()
    {
        Assert.Null(PixKey.Detect(string.Empty));
        Assert.False(PixKey.IsValid(string.Empty.AsSpan()));
    }

    [Fact]
    public void Keys_Generated_From_The_Document_Types_Are_Detected_As_Keys()
    {
        string cpf = Cpf.Generate().ToString("N");
        string cnpj = Cnpj.Generate().ToString("N");
        string alphanumeric = Cnpj.GenerateAlphanumeric().ToString("N");
        string phone = TelefoneBr.Generate().ToE164();
        Assert.Equal(PixKeyKind.Cpf, PixKey.Detect(cpf));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect(cnpj));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect(alphanumeric));
        Assert.Equal(PixKeyKind.Phone, PixKey.Detect(phone));
        Assert.True(PixKey.IsValid(cpf));
        Assert.True(PixKey.IsValid(cnpj));
        Assert.True(PixKey.IsValid(alphanumeric));
        Assert.True(PixKey.IsValid(phone));
    }

    [Theory]
    [InlineData("11144477735")]
    [InlineData("11222333000181")]
    [InlineData("12ABC34501DE35")]
    [InlineData("fulano@example.com")]
    [InlineData("+5511912345678")]
    [InlineData("123e4567-e12b-12d1-a456-426655440000")]
    public void Oracles_And_The_Library_Agree_On_Valid_Keys(string key)
    {
        Assert.Equal(Oracles.IsValidPixKeyOracle(key), PixKey.IsValid(key));
    }
}
