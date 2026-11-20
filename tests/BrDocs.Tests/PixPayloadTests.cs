using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Tests for the Pix BR Code payload: the four official BCB vectors reproduced by the builders
/// and parsed back with full validation, the builder options, the name/city normalization and
/// the complete rejection of malformed payloads.
/// </summary>
/// <remarks>
/// The vectors come from the "Manual de Padrões para Iniciação do Pix" (BCB): section 2.6.3
/// (static, CRC 1D3D), 2.7.2 (dynamic, CRC 64E4) and 2.8.5 (compostos, CRC 2875/FB42) — each
/// verified against the manual's own CRC. Mutations repair the CRC through the independent
/// oracle so that each test isolates one semantic rule.
/// </remarks>
public sealed class PixPayloadTests
{
    // Section 2.6.3 — static example: EVP key, open amount, txid "***".
    private const string StaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    // Section 2.7.2 — dynamic example: location URL, point of initiation "12", no amount.
    private const string DynamicVector =
        "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***630464E4";

    // Section 2.8.5.2 — QR code composto: static key + amount + recurrence parameters in 80.
    private const string CompositeStaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865406100.505802BR5913Fulano de Tal6008BRASILIA62070503***80740014br.gov.bcb.pix2552pix.example.com/rec/2353c790eefb11eaadc10242ac12000263042875";

    // Section 2.8.5.3 — QR code composto: dynamic location + recurrence parameters in 80.
    private const string CompositeDynamicVector =
        "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***80740014br.gov.bcb.pix2552pix.example.com/rec/2353c790eefb11eaadc10242ac1200026304FB42";

    private const string EvpKey = "123e4567-e12b-12d1-a456-426655440000";
    private const string RecurrenceLocation = "pix.example.com/rec/2353c790eefb11eaadc10242ac120002";

    // ---- Official vectors: validation and typed extraction -------------------

    [Theory]
    [InlineData(StaticVector)]
    [InlineData(DynamicVector)]
    [InlineData(CompositeStaticVector)]
    [InlineData(CompositeDynamicVector)]
    public void The_Official_Vectors_Are_Valid_And_Round_Trip_Byte_For_Byte(string vector)
    {
        Assert.True(PixPayload.IsValid(vector));
        Assert.True(PixPayload.TryParse(vector, out PixPayload? parsed));
        Assert.NotNull(parsed);
        Assert.Equal(vector, parsed!.ToString());
        Assert.Equal(vector, PixPayload.Parse(vector).ToString());
        Assert.Equal(parsed, PixPayload.Parse(vector));
        Assert.Equal(parsed.GetHashCode(), PixPayload.Parse(vector).GetHashCode());
        Assert.Equal(vector.Length, parsed.EncodedLength);
    }

    [Fact]
    public void The_Static_Vector_Extracts_Every_Typed_Field()
    {
        PixPayload payload = PixPayload.Parse(StaticVector);
        Assert.True(payload.IsStatic);
        Assert.False(payload.IsDynamic);
        Assert.Equal(EvpKey, payload.Key);
        Assert.Equal(PixKeyKind.Evp, PixKey.Detect(payload.Key!));
        Assert.Null(payload.Location);
        Assert.Null(payload.Description);
        Assert.Equal("br.gov.bcb.pix", payload.Gui);
        Assert.Equal("0000", payload.Mcc);
        Assert.Equal("986", payload.Currency);
        Assert.Null(payload.Amount);
        Assert.Equal("BR", payload.Country);
        Assert.Equal("Fulano de Tal", payload.Name);
        Assert.Equal("BRASILIA", payload.City);
        Assert.Equal("***", payload.Txid);
        Assert.Equal("1D3D", payload.Crc);
        Assert.False(payload.IsSingleUse);
        Assert.Null(payload.InitiationMethod);
        Assert.Empty(payload.UnreservedTemplates);
        Assert.Equal(2, payload.MerchantAccountFields.Count);
        Assert.Single(payload.AdditionalDataFields);
    }

    [Fact]
    public void The_Dynamic_Vector_Extracts_Every_Typed_Field()
    {
        PixPayload payload = PixPayload.Parse(DynamicVector);
        Assert.True(payload.IsDynamic);
        Assert.False(payload.IsStatic);
        Assert.Equal("pix.example.com/8b3da2f39a4140d1a91abd93113bd441", payload.Location);
        Assert.Null(payload.Key);
        Assert.True(payload.IsSingleUse);
        Assert.Equal("12", payload.InitiationMethod);
        Assert.Null(payload.Amount);
        Assert.Equal("***", payload.Txid);
        Assert.Equal("64E4", payload.Crc);
    }

    [Fact]
    public void The_Composite_Vectors_Extract_The_Amount_And_The_Recurrence_Template()
    {
        PixPayload payload = PixPayload.Parse(CompositeStaticVector);
        Assert.Equal(100.50m, payload.Amount);
        Assert.True(payload.IsStatic);
        PixTlv recurrence = Assert.Single(payload.UnreservedTemplates);
        Assert.Equal("80", recurrence.Id);
        Assert.Equal("br.gov.bcb.pix", recurrence.Children![0].Value);
        Assert.Equal(RecurrenceLocation, recurrence.Children[1].Value);

        PixPayload dynamic = PixPayload.Parse(CompositeDynamicVector);
        Assert.True(dynamic.IsDynamic);
        Assert.Null(dynamic.Amount);
        Assert.Equal(RecurrenceLocation, Assert.Single(dynamic.UnreservedTemplates).Children![1].Value);
    }

    [Fact]
    public void The_Payload_Can_Be_Written_Into_A_Span()
    {
        PixPayload payload = PixPayload.Parse(StaticVector);
        Span<char> enough = stackalloc char[StaticVector.Length];
        Span<char> tooSmall = stackalloc char[StaticVector.Length - 1];
        Assert.True(payload.TryEncode(enough, out int written));
        Assert.Equal(StaticVector.Length, written);
        Assert.True(enough[..written].SequenceEqual(StaticVector.AsSpan()));
        Assert.False(payload.TryEncode(tooSmall, out int rejected));
        Assert.Equal(0, rejected);
    }

    // ---- Builders reproduce the official vectors ----------------------------

    [Fact]
    public void CreateStatic_Reproduces_The_Official_Static_Vector()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").Build().ToString();
        Assert.Equal(StaticVector, brCode);
    }

    [Fact]
    public void CreateStatic_Normalizes_The_Key_And_Accepts_Forms()
    {
        // The BCB canonical key form goes in even when the input is formatted.
        Assert.Equal(
            StaticVector,
            PixPayload.CreateStatic("123E4567-E12B-12D1-A456-426655440000", "Fulano de Tal", "BRASILIA").Build().ToString());
        Assert.Equal(
            StaticVector,
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "Brasília").WithCity("Brasília", uppercase: true).Build().ToString());
        Assert.Equal(
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "SAO PAULO").Build().ToString(),
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "São Paulo").Build().ToString());
    }

    [Fact]
    public void CreateDynamic_Reproduces_The_Official_Dynamic_Vector()
    {
        string brCode = PixPayload.CreateDynamic(
            "pix.example.com/8b3da2f39a4140d1a91abd93113bd441", "Fulano de Tal", "BRASILIA").SingleUse().Build().ToString();
        Assert.Equal(DynamicVector, brCode);
    }

    [Fact]
    public void CreateDynamic_Strips_The_Protocol_Prefix_When_Given()
    {
        string brCode = PixPayload.CreateDynamic(
            "https://pix.example.com/8b3da2f39a4140d1a91abd93113bd441", "Fulano de Tal", "BRASILIA").SingleUse().Build().ToString();
        Assert.Equal(DynamicVector, brCode);
    }

    [Fact]
    public void Builders_Reproduce_Both_Composite_Official_Vectors()
    {
        PixTlv recurrence = PixTlv.CreateTemplate("80", new[]
        {
            PixTlv.Create("00", "br.gov.bcb.pix"),
            PixTlv.Create("25", RecurrenceLocation),
        });
        string composite = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithAmount(100.50m)
            .WithUnreservedTemplate(recurrence)
            .Build().ToString();
        Assert.Equal(CompositeStaticVector, composite);

        string compositeDynamic = PixPayload.CreateDynamic(
            "pix.example.com/8b3da2f39a4140d1a91abd93113bd441", "Fulano de Tal", "BRASILIA")
            .SingleUse()
            .WithUnreservedTemplate(recurrence)
            .Build().ToString();
        Assert.Equal(CompositeDynamicVector, compositeDynamic);
    }

    // ---- Builder options ------------------------------------------------------

    [Fact]
    public void Amounts_Are_Encoded_Invariantly_With_Two_Decimal_Places()
    {
        Assert.Equal("1.50", ExtractField(PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAmount(1.5m), "54")[4..]);
        Assert.Equal("0.00", ExtractField(PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAmount(0m), "54")[4..]);
        Assert.Equal("1234567890.12", ExtractField(
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAmount(1234567890.12m), "54")[4..]);
    }

    [Fact]
    public void Amount_Errors_Are_Rejected()
    {
        PixPayloadBuilder builder = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA");
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithAmount(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithAmount(10_000_000_000m));
        Assert.Throws<ArgumentException>(() => builder.WithAmount(100.005m));
        Assert.Throws<InvalidOperationException>(() =>
            PixPayload.CreateDynamic("pix.example.com/abc", "Fulano de Tal", "BRASILIA").WithAmount(10m));
    }

    [Fact]
    public void Txid_Is_Encoded_And_Parsed_Back()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithTxid("PEDIDO123456").Build().ToString();
        Assert.Contains("0512PEDIDO123456", brCode);
        Assert.Equal("PEDIDO123456", PixPayload.Parse(brCode).Txid);
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithTxid("PEDIDO-1"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithTxid(new string('A', 26)));
        Assert.Throws<InvalidOperationException>(() =>
            PixPayload.CreateDynamic("pix.example.com/abc", "Fulano de Tal", "BRASILIA").WithTxid("PEDIDO"));
    }

    [Fact]
    public void Descriptions_Are_Encoded_Within_The_72_Character_Limit()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithDescription("Pagamento do pedido 123").Build().ToString();
        PixPayload parsed = PixPayload.Parse(brCode);
        Assert.Equal("Pagamento do pedido 123", parsed.Description);

        PixPayloadBuilder cleared = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithDescription("x");
        Assert.Equal(StaticVector, cleared.WithDescription(null).Build().ToString());
        Assert.Throws<ArgumentException>(() =>
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithDescription(new string('d', 73)));
        Assert.Throws<InvalidOperationException>(() =>
            PixPayload.CreateDynamic("pix.example.com/abc", "Fulano de Tal", "BRASILIA").WithDescription("descrição"));
    }

    [Fact]
    public void The_Merchant_Category_Code_Can_Be_Overridden()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithMcc("5812").Build().ToString();
        Assert.Contains("52045812", brCode);
        Assert.Equal("5812", PixPayload.Parse(brCode).Mcc);
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithMcc("581"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithMcc("581a"));
    }

    [Fact]
    public void The_Withdrawal_Facilitator_Marks_A_Pix_Saque_Code()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithWithdrawalFacilitator("12345678").Build().ToString();
        Assert.Contains("030812345678", brCode);
        Assert.True(PixPayload.IsValid(brCode));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithWithdrawalFacilitator("1234567"));
        Assert.Throws<InvalidOperationException>(() =>
            PixPayload.CreateDynamic("pix.example.com/abc", "Fulano de Tal", "BRASILIA").WithWithdrawalFacilitator("12345678"));
    }

    [Fact]
    public void Additional_Data_Sub_Fields_Are_Encoded_Beside_The_Txid()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithAdditionalDataField("01", "BILL-1")
            .WithAdditionalDataField("50", "PSP1")
            .Build().ToString();
        PixPayload parsed = PixPayload.Parse(brCode);
        Assert.Equal(3, parsed.AdditionalDataFields.Count);
        Assert.Equal(("01", "BILL-1"), (parsed.AdditionalDataFields[1].Id, parsed.AdditionalDataFields[1].Value));
        Assert.Equal(("50", "PSP1"), (parsed.AdditionalDataFields[2].Id, parsed.AdditionalDataFields[2].Value));
        Assert.Equal("***", parsed.Txid);

        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAdditionalDataField("05", "no"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAdditionalDataField("25", "no"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAdditionalDataField("00", "no"));
        Assert.Throws<ArgumentException>(() =>
            PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithAdditionalDataField("01", new string('v', 26)));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithAdditionalDataField("01", "a").WithAdditionalDataField("01", "b"));
    }

    [Fact]
    public void Unreserved_Templates_Require_A_Valid_Id_And_The_Pix_Gui()
    {
        PixTlv recurrence = PixTlv.CreateTemplate("81", new[]
        {
            PixTlv.Create("00", "br.gov.bcb.pix"),
            PixTlv.Create("25", RecurrenceLocation),
        });
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithUnreservedTemplate(recurrence).Build().ToString();
        Assert.True(PixPayload.IsValid(brCode));
        Assert.Equal("81", Assert.Single(PixPayload.Parse(brCode).UnreservedTemplates).Id);

        PixTlv notATemplate = PixTlv.Create("81", "value");
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithUnreservedTemplate(notATemplate));
        PixTlv wrongGui = PixTlv.CreateTemplate("80", new[] { PixTlv.Create("00", "outro.arranjo") });
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA").WithUnreservedTemplate(wrongGui));
        PixTlv repeated = PixTlv.CreateTemplate("81", new[] { PixTlv.Create("00", "br.gov.bcb.pix") });
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithUnreservedTemplate(recurrence).WithUnreservedTemplate(repeated));
    }

    [Fact]
    public void A_Too_Long_Description_That_Overflows_The_Template_Fails_The_Build()
    {
        Assert.Throws<InvalidOperationException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithDescription(new string('d', 72))
            .Build());
    }

    // ---- Name and city normalization ------------------------------------------

    [Fact]
    public void Names_And_Cities_Are_Normalized_With_Stripped_Diacritics()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano da Silva", "S\u0303ao Paulo")
            .WithName("José de Ribamar")
            .WithCity("Fortaleza", uppercase: true)
            .Build()
            .ToString();
        PixPayload parsed = PixPayload.Parse(brCode);
        Assert.Equal("Jose de Ribamar", parsed.Name);
        Assert.Equal("FORTALEZA", parsed.City);
        Assert.Equal(PixPayload.CreateStatic(EvpKey, "Jose de Ribamar", "Fortaleza").Build().ToString(), brCode);
    }

    [Theory]
    [InlineData("")]                           // empty
    [InlineData("   ")]                        // spaces only
    [InlineData("Foo Bar!")]                   // punctuation outside the allowed set
    [InlineData("Ægir Industries")]            // Æ has no single-letter ASCII base
    [InlineData("Foo\u0000Bar")]               // control character
    public void Invalid_Names_Are_Rejected(string name)
    {
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, name, "BRASILIA"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", name));
    }

    [Fact]
    public void Accented_Names_And_Cities_Are_Normalized_Not_Rejected()
    {
        string brCode = PixPayload.CreateStatic(EvpKey, "José de Ó, Êle", "Goiânia")
            .WithName("ÁBkçdFUÎP", uppercase: true)
            .Build()
            .ToString();
        PixPayload parsed = PixPayload.Parse(brCode);
        Assert.Equal("ABKCDFUIP", parsed.Name);
        Assert.Equal("Goiania", parsed.City);
    }

    [Fact]
    public void Names_And_Cities_Enforce_Their_Maximum_Lengths()
    {
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, new string('F', 26), "BRASILIA"));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "F" + new string('C', 15)));
        PixPayload maxed = PixPayload.CreateStatic(EvpKey, new string('F', 25), new string('C', 15)).Build();
        Assert.Equal(25, maxed.Name.Length);
        Assert.Equal(15, maxed.City.Length);
        Assert.True(PixPayload.IsValid(maxed.ToString()));
    }

    // ---- Parse argument handling ----------------------------------------------

    [Fact]
    public void Parse_Throws_For_Null_And_TryParse_Handles_Null()
    {
        Assert.Throws<ArgumentNullException>(() => PixPayload.Parse(null!));
        Assert.False(PixPayload.TryParse((string?)null, out PixPayload? result));
        Assert.Null(result);
        Assert.False(PixPayload.IsValid((string?)null!));
    }

    [Fact]
    public void Parse_Throws_With_A_Clear_Message_For_The_Empty_Payload()
    {
        FormatException exception = Assert.Throws<FormatException>(() => PixPayload.Parse(string.Empty));
        Assert.Contains("empty", exception.Message);
    }

    // ---- Malformed payloads -----------------------------------------------------

    /// <summary>
    /// A corpus of malformed payloads, each crafted from an official vector by a single
    /// structural or semantic violation (CRC repaired through the oracle), with the fragment
    /// the FormatException message must carry. Substitutions inside a template replace the whole
    /// enclosing field, rebuilt with the oracle TLV helper, so that every row stays well-formed
    /// except for the rule under test.
    /// </summary>
    public static TheoryData<string, string> MalformedPayloads => new()
    {
        // Structural failures.
        { StaticVector.Replace("63041D3D", "63041D3E", StringComparison.Ordinal), "the CRC-16/CCITT-FALSE (field 63) does not match" },
        { StaticVector.Replace("63041D3D", "63041d3d", StringComparison.Ordinal), "four uppercase hexadecimal digits" },
        { StaticVector[..^8], "field 63) is missing" },
        { StaticVector.Replace("63041D3D", "63041D3D5303986", StringComparison.Ordinal), "last data object" },
        { StaticVector[..^8] + Oracles.Tlv("63", "1D3D1"), "four uppercase hexadecimal digits" },
        { StaticVector[..^8] + "12", "middle of a TLV header" },
        { StaticVector[..^2], "declared value is longer than the payload" },
        { " " + StaticVector, "ID is not exactly two digits" },
        { Replace(StaticVector, "000201", "000x01"), "length is not exactly two digits" },

        // Payload format indicator and point of initiation.
        { Replace(StaticVector, "000201", "000202"), "payload format indicator (field 00) must be '01'" },
        { Remove(StaticVector, "000201"), "first data object" },
        { Append(StaticVector, Oracles.Tlv("01", "13")), "'11' or '12'" },
        { Append(StaticVector, Oracles.Tlv("65", "AAAA")), "reserved for future use (65-79)" },

        // Merchant account information.
        { Remove(StaticVector, StaticMai), "no merchant account information template" },
        { Replace(StaticVector, StaticMai, Mai(KeySub)), "no GUI sub-field 00" },
        { Replace(StaticVector, StaticMai, Mai(Oracles.Tlv("00", "br.gov.visa.pix") + KeySub)), "carries the 'br.gov.bcb.pix' GUI" },
        { Replace(StaticVector, StaticMai, Mai(Oracles.Tlv("00", "br.gov.bcb.pix." + new string('a', 18)) + KeySub)), "32-character limit" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + Oracles.Tlv("01", "1234567890"))), "the Pix key (sub-field 01)" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + KeySub + Oracles.Tlv("25", "pix.example.com/8b3da2f39a4140d1a91abd93113bd441"))), "cannot carry both" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv)), "must carry either" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + Oracles.Tlv("02", ""))), "1..72 characters" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + Oracles.Tlv("02", new string('d', 73)))), "1..72 characters" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + KeySub + Oracles.Tlv("03", "12345"))), "exactly 8 digits (the ISPB)" },
        { Replace(StaticVector, StaticMai, Mai(GuiTlv + KeySub + Oracles.Tlv("01", "abc"))), "appears twice" },

        // Mandatory EMV fields and their domains.
        { Remove(StaticVector, "52040000"), "merchant category code (field 52) is missing" },
        { Replace(StaticVector, "52040000", Oracles.Tlv("52", "000a")), "exactly 4 digits" },
        { Remove(StaticVector, "5303986"), "transaction currency (field 53) is missing" },
        { Replace(StaticVector, "5303986", Oracles.Tlv("53", "985")), "must be '986'" },
        { Replace(CompositeStaticVector, "5406100.50", Oracles.Tlv("54", "100,50")), "transaction amount (field 54)" },
        { Replace(CompositeStaticVector, "5406100.50", Oracles.Tlv("54", "100.505")), "transaction amount (field 54)" },
        { Remove(StaticVector, "5802BR"), "country code (field 58) is missing" },
        { Replace(StaticVector, "5802BR", Oracles.Tlv("58", "US")), "must be 'BR'" },
        { Remove(StaticVector, "5913Fulano de Tal"), "merchant name (field 59) is missing" },
        { Replace(StaticVector, "5913Fulano de Tal", Oracles.Tlv("59", "")), "merchant name (field 59) is missing" },
        { Replace(StaticVector, "Fulano de Tal", "Fulano dé Tal"), "1..25 letters, digits and spaces" },
        { Replace(StaticVector, "5913Fulano de Tal", Oracles.Tlv("59", new string('F', 26))), "1..25 letters, digits and spaces" },
        { Remove(StaticVector, "6008BRASILIA"), "merchant city (field 60) is missing" },
        { Replace(StaticVector, "6008BRASILIA", Oracles.Tlv("60", new string('C', 16))), "1..15 letters, digits and spaces" },

        // Additional data template.
        { Remove(StaticVector, "62070503***"), "Reference Label" },
        { Replace(StaticVector, "62070503***", Oracles.Tlv("62", Oracles.Tlv("05", "PEDIDO-1"))), "txid (sub-field 05)" },
        { Replace(StaticVector, "62070503***", Oracles.Tlv("62", Oracles.Tlv("00", "***"))), "01-09 and 50-99 only" },
        { Replace(StaticVector, "62070503***", Oracles.Tlv("62", Oracles.Tlv("12", "***"))), "01-09 and 50-99 only" },
        { Replace(StaticVector, "62070503***", Oracles.Tlv("62", Oracles.Tlv("05", new string('T', 26)))), "25-character limit" },
        { Replace(StaticVector, "62070503***", Oracles.Tlv("62", "0503***x")), "not fully consumed" },

        // Unreserved templates and duplicates.
        { Append(StaticVector, Oracles.Tlv("80", "AAAA")), "unreserved template (80-99)" },
        { Append(StaticVector, "5303986"), "appears twice" },

        // Location rules for dynamic payloads.
        { Replace(DynamicVector, DynamicMai, Mai(GuiTlv + Oracles.Tlv("25", "https://pix.example.com/8b3da2f39a4140d1a91abd93113bd441"))), "protocol prefix" },
        { Replace(DynamicVector, DynamicMai, Mai(GuiTlv + Oracles.Tlv("25", "pix example com/8b3da2f39a4140d1a91abd93113bd441"))), "1..77 RFC 3986 characters" },
    };

    private const string KeySub = "0136123e4567-e12b-12d1-a456-426655440000";

    private static string GuiTlv => Oracles.Tlv("00", "br.gov.bcb.pix");

    /// <summary>The merchant account template of the static vector, as a whole field.</summary>
    private static string StaticMai => Oracles.Tlv("26", GuiTlv + KeySub);

    /// <summary>The merchant account template of the dynamic vector, as a whole field.</summary>
    private static string DynamicMai => Oracles.Tlv("26", GuiTlv + Oracles.Tlv("25", "pix.example.com/8b3da2f39a4140d1a91abd93113bd441"));

    private static string Mai(string content) => Oracles.Tlv("26", content);

    [Theory]
    [MemberData(nameof(MalformedPayloads))]
    public void Malformed_Payloads_Fail_With_The_Expected_Message(string payload, string fragment)
    {
        Assert.False(PixPayload.IsValid(payload));
        Assert.False(PixPayload.TryParse(payload, out PixPayload? result));
        Assert.Null(result);
        FormatException exception = Assert.Throws<FormatException>(() => PixPayload.Parse(payload));
        Assert.Contains(fragment, exception.Message);
    }

    // ---- Tolerance: fields outside the Pix arrangement ----------------------

    [Fact]
    public void The_Gui_Is_Matched_Case_Insensitively_And_Preserved_On_Round_Trip()
    {
        string payload = Replace(StaticVector, "0014br.gov.bcb.pix", "0014BR.GOV.BCB.PIX");
        Assert.True(PixPayload.IsValid(payload));
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.Equal("BR.GOV.BCB.PIX", parsed.Gui);
        Assert.True(parsed.IsStatic);
        Assert.Equal(payload, parsed.ToString());
    }

    [Fact]
    public void Foreign_Merchant_Account_Templates_And_Other_Mpm_Fields_Are_Preserved()
    {
        // A Visa merchant account template (root ID 02, out of the Pix range 26-51), the tip
        // indicator (55), the postal code (61) and the language template (64) are all valid
        // QRCPS-MPM objects outside the Pix arrangement: preserved verbatim.
        string payload = Append(Append(Append(Append(StaticVector,
            "0206A00000"), "550201"), "610513101"), "6404ptBR");
        Assert.True(PixPayload.IsValid(payload));
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.Equal(payload, parsed.ToString());
        Assert.Contains(parsed.Fields, field => field.Id == "02" && field.Value == "A00000");
        Assert.Contains(parsed.Fields, field => field.Id == "55" && field.Value == "01");
        Assert.Contains(parsed.Fields, field => field.Id == "61" && field.Value == "13101");
        Assert.Contains(parsed.Fields, field => field.Id == "64" && field.Value == "ptBR");
    }

    [Fact]
    public void Amounts_May_Be_Integers_Or_Carry_One_Decimal_When_Parsed()
    {
        // The builder always emits two decimal places; parse accepts the integer and one-digit
        // decimal forms allowed by the 13-character amount format.
        string integer = Replace(CompositeStaticVector, "5406100.50", "5403100");
        PixPayload parsed = PixPayload.Parse(integer);
        Assert.Equal(100m, parsed.Amount);
        Assert.Equal(integer, parsed.ToString());

        string oneDigit = Replace(CompositeStaticVector, "5406100.50", "5405100.5");
        Assert.Equal(100.5m, PixPayload.Parse(oneDigit).Amount);
    }

    [Fact]
    public void A_Dynamic_Payload_May_Carry_Ignored_Amount_And_Txid()
    {
        // Section 2.7: "Se preenchidos, seu conteúdo deve ser ignorado" — parsing tolerates
        // an amount and a real txid in a dynamic code; the builder refuses to emit them.
        string payload = Replace(
            Append(DynamicVector, Oracles.Tlv("54", "100.50")),
            "62070503***",
            Oracles.Tlv("62", "0503***" + Oracles.Tlv("05", "PEDIDO123")));
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.True(parsed.IsDynamic);
        Assert.Equal(100.50m, parsed.Amount);
        Assert.Equal("PEDIDO123", parsed.Txid);
        Assert.Equal(payload, parsed.ToString());
    }

    [Fact]
    public void A_Reusable_Point_Of_Initiation_Is_Accepted()
    {
        string payload = Replace(StaticVector, "00020126", "00020101021126");
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.False(parsed.IsSingleUse);
        Assert.Equal("11", parsed.InitiationMethod);
        Assert.Equal(payload, parsed.ToString());
    }

    [Fact]
    public void Valid_Additional_Data_Sub_Fields_Are_Preserved()
    {
        string payload = Replace(StaticVector, "62070503***", Oracles.Tlv("62", "0503***" + Oracles.Tlv("04", "PEDIDO")));
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.Equal(2, parsed.AdditionalDataFields.Count);
        Assert.Equal("PEDIDO", parsed.AdditionalDataFields[1].Value);
        Assert.Equal(payload, parsed.ToString());
    }

    [Fact]
    public void An_Empty_Unreserved_Template_Is_Tolerated()
    {
        string payload = Append(StaticVector, "8000");
        Assert.True(PixPayload.IsValid(payload));
        Assert.Equal("80", Assert.Single(PixPayload.Parse(payload).UnreservedTemplates).Id);
        Assert.Equal(payload, PixPayload.Parse(payload).ToString());
    }

    [Fact]
    public void The_Txid_Properties_Default_To_Three_Stars_Only_When_Absent()
    {
        // A dynamic payload may omit the whole 62 template: the txid then reads as "***"
        // (the payer application generates it, EMV QRCPS-MPM 4.8.1.2).
        string payload = Remove(DynamicVector, "62070503***");
        Assert.True(PixPayload.IsValid(payload));
        PixPayload parsed = PixPayload.Parse(payload);
        Assert.Equal("***", parsed.Txid);
        Assert.Empty(parsed.AdditionalDataFields);
        Assert.Equal(payload, parsed.ToString());
    }

    // ---- Helpers ---------------------------------------------------------------

    private static string Replace(string payload, string old, string @new) => Oracles.FixCrc(payload.Replace(old, @new, StringComparison.Ordinal));

    private static string Remove(string payload, string old) => Oracles.FixCrc(payload.Replace(old, string.Empty, StringComparison.Ordinal));

    /// <summary>Inserts a data object before the CRC of <paramref name="payload"/> and repairs the checksum.</summary>
    private static string Append(string payload, string tlv) => Oracles.FixCrc(payload[..^8] + tlv + payload[^8..]);

    private static string ExtractField(PixPayloadBuilder builder, string id)
    {
        PixPayload payload = builder.Build();
        PixTlv field = Assert.Single(payload.Fields.Where(field => field.Id == id));
        return field.Encode();
    }
}
