using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Tests for the TLV data objects: creation validation, encode/decode round trips, nested
/// templates (26–51, 62 and 80–99) and the structural rejection of malformed streams.
/// </summary>
public sealed class PixTlvTests
{
    [Theory]
    [InlineData("00", "01", "000201")]
    [InlineData("59", "Fulano de Tal", "5913Fulano de Tal")]
    [InlineData("05", "***", "0503***")]
    [InlineData("54", "100.50", "5406100.50")]
    [InlineData("61", "", "6100")]
    public void Leaf_Objects_Encode_With_The_Two_Digit_Length(string id, string value, string expected)
    {
        PixTlv tlv = PixTlv.Create(id, value);
        Assert.Equal(expected, tlv.Encode());
        Assert.Equal(expected, tlv.ToString());
        Assert.Equal(id, tlv.Id);
        Assert.Equal(value, tlv.Value);
        Assert.Null(tlv.Children);
        Assert.False(tlv.IsTemplate);
        Assert.Equal(4 + value.Length, tlv.EncodedLength);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("000")]
    [InlineData("7a")]
    [InlineData("a7")]
    [InlineData("")]
    [InlineData(" 0")]
    public void Create_Rejects_Ids_That_Are_Not_Two_Digits(string id)
    {
        Assert.Throws<ArgumentException>(() => PixTlv.Create(id, "value"));
        Assert.Throws<ArgumentException>(() => PixTlv.CreateTemplate(id, new[] { PixTlv.Create("01", "x") }));
    }

    [Fact]
    public void Create_Rejects_Values_Longer_Than_99_Characters()
    {
        string value = new('a', 100);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => PixTlv.Create("59", value));
        Assert.Contains("99", exception.Message);
    }

    [Fact]
    public void Create_Rejects_Template_Ids_As_Leaves_And_Leaf_Ids_As_Templates()
    {
        Assert.Throws<ArgumentException>(() => PixTlv.Create("26", "value"));
        Assert.Throws<ArgumentException>(() => PixTlv.Create("62", "value"));
        Assert.Throws<ArgumentException>(() => PixTlv.Create("80", "value"));
        Assert.Throws<ArgumentException>(() => PixTlv.CreateTemplate("59", new[] { PixTlv.Create("01", "x") }));
        Assert.Throws<ArgumentException>(() => PixTlv.CreateTemplate("53", new[] { PixTlv.Create("01", "x") }));
    }

    [Fact]
    public void Templates_Encode_Their_Children_And_Nest_Exactly_One_Level()
    {
        PixTlv gui = PixTlv.Create("00", "br.gov.bcb.pix");
        PixTlv key = PixTlv.Create("01", "123e4567-e12b-12d1-a456-426655440000");
        PixTlv merchantAccount = PixTlv.CreateTemplate("26", new[] { gui, key });
        Assert.Equal("2658" + gui.Encode() + key.Encode(), merchantAccount.Encode());
        Assert.True(merchantAccount.IsTemplate);
        Assert.Equal(string.Empty, merchantAccount.Value);
        Assert.Equal(2, merchantAccount.Children!.Count);

        Assert.Throws<ArgumentException>(() => PixTlv.CreateTemplate("26", new[] { merchantAccount }));
    }

    [Fact]
    public void CreateTemplate_Rejects_Content_Longer_Than_99_Characters()
    {
        PixTlv big = PixTlv.Create("01", new string('a', 99));
        ArgumentException exception = Assert.Throws<ArgumentException>(() => PixTlv.CreateTemplate("26", new[] { big }));
        Assert.Contains("99", exception.Message);
    }

    [Theory]
    [InlineData("26")]
    [InlineData("27")]
    [InlineData("51")]
    [InlineData("62")]
    [InlineData("80")]
    [InlineData("99")]
    public void The_Documented_Ranges_Are_Templates(string id)
    {
        PixTlv template = PixTlv.CreateTemplate(id, new[] { PixTlv.Create("00", "br.gov.bcb.pix") });
        Assert.True(template.IsTemplate);
        Assert.True(PixTlv.Parse(template.Encode()).IsTemplate);
    }

    [Theory]
    [InlineData("02")]
    [InlineData("25")]
    [InlineData("59")]
    [InlineData("63")]
    [InlineData("64")]
    public void Ids_Outside_The_Documented_Template_Ranges_Are_Leaves(string id)
    {
        PixTlv leaf = PixTlv.Create(id, "value");
        Assert.False(leaf.IsTemplate);
        Assert.False(PixTlv.Parse(leaf.Encode()).IsTemplate);
    }

    [Fact]
    public void Parse_Decodes_One_Leaf_And_Rejects_Trailing_Data()
    {
        PixTlv parsed = PixTlv.Parse("5802BR");
        Assert.Equal(PixTlv.Create("58", "BR"), parsed);

        Assert.False(PixTlv.TryParse("5802BR580", out _));
        Assert.False(PixTlv.TryParse("5802BRx", out _));
        Assert.Throws<FormatException>(() => PixTlv.Parse("5802BRx"));
    }

    [Fact]
    public void Parse_Decodes_A_Template_With_Its_Children()
    {
        PixTlv parsed = PixTlv.Parse("62070503***");
        Assert.True(parsed.IsTemplate);
        Assert.Equal("62", parsed.Id);
        PixTlv txid = Assert.Single(parsed.Children!);
        Assert.Equal("05", txid.Id);
        Assert.Equal("***", txid.Value);
        Assert.Equal("62070503***", parsed.Encode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("58")]
    [InlineData("5802")]
    [InlineData("5805BR")]        // declared length beyond the end
    [InlineData("5a02BR")]
    [InlineData("58x2BR")]
    [InlineData("5802BR ")]       // trailing character
    public void TryParse_Rejects_Malformed_Single_Objects(string text)
    {
        Assert.False(PixTlv.TryParse(text, out PixTlv? result));
        Assert.Null(result);
    }

    [Fact]
    public void ParseSequence_Decodes_Every_Root_Object_Of_The_Spec_Vector()
    {
        // The SPEC/BCB static payload without its CRC.
        IReadOnlyList<PixTlv> fields = PixTlv.ParseSequence(
            "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***");
        string[] ids = fields.Select(field => field.Id).ToArray();
        Assert.Equal(new[] { "00", "26", "52", "53", "58", "59", "60", "62" }, ids);
        Assert.Equal("01", fields[0].Value);
        Assert.Equal(2, fields[1].Children!.Count);
        Assert.Equal("0000", fields[2].Value);
        Assert.Equal("986", fields[3].Value);
        Assert.Equal("BR", fields[4].Value);
        Assert.Equal("Fulano de Tal", fields[5].Value);
        Assert.Equal("BRASILIA", fields[6].Value);
        Assert.Equal("***", fields[7].Children![0].Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0002")]                          // no value for the declared length
    [InlineData("0002010002")]                    // duplicated root ID
    [InlineData("0002012610AAAA")]                // template value that does not decompose
    [InlineData("62042610AAAAAAAAAA")]           // template inside a template (sub ID 26 inside 62)
    [InlineData("0002 01")]                       // space inside a header
    [InlineData("5913Fulano de Tal5913Other")]    // duplicate 59
    public void TryParseSequence_Rejects_Malformed_Streams(string text)
    {
        Assert.False(PixTlv.TryParseSequence(text, out IReadOnlyList<PixTlv>? fields));
        Assert.Null(fields);
        Assert.Throws<FormatException>(() => PixTlv.ParseSequence(text));
    }

    [Fact]
    public void An_Empty_Template_Decodes_With_No_Children()
    {
        // A template content may itself be empty ("6200"); the stream still has to hold objects.
        Assert.True(PixTlv.TryParse("6200", out PixTlv? empty));
        Assert.Empty(empty!.Children!);
        Assert.Equal("6200", empty.Encode());
        Assert.True(PixTlv.TryParseSequence("6200", out IReadOnlyList<PixTlv>? fields));
        Assert.Single(fields!);
        Assert.True(PixTlv.TryParseSequence("62005303986", out _));
    }

    [Fact]
    public void Empty_Template_In_A_Stream_Is_Preserved()
    {
        IReadOnlyList<PixTlv> fields = PixTlv.ParseSequence("62005303986");
        Assert.Equal("62", fields[0].Id);
        Assert.Empty(fields[0].Children!);
        Assert.Equal("53", fields[1].Id);
    }

    [Fact]
    public void Encode_Writes_Into_A_Span_Only_When_It_Fits()
    {
        PixTlv tlv = PixTlv.Create("59", "Fulano de Tal");
        Span<char> enough = stackalloc char[17];
        Span<char> tooSmall = stackalloc char[16];
        Assert.True(tlv.TryEncode(enough, out int written));
        Assert.Equal(17, written);
        Assert.False(tlv.TryEncode(tooSmall, out int rejected));
        Assert.Equal(0, rejected);
    }

    [Fact]
    public void Equality_Covers_Ids_Values_And_Children_In_Order()
    {
        PixTlv a = PixTlv.Create("00", "01");
        PixTlv b = PixTlv.Create("00", "01");
        PixTlv c = PixTlv.Create("00", "02");
        PixTlv template1 = PixTlv.CreateTemplate("26", new[] { a, b });
        PixTlv template2 = PixTlv.CreateTemplate("26", new[] { b, a });
        PixTlv template3 = PixTlv.CreateTemplate("26", new[] { a, b });
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.NotEqual(template1, template2);   // children order matters
        Assert.Equal(template1, template3);
        Assert.NotEqual(a, template1);
        Assert.False(a.Equals(null));
        Assert.True(a.Equals((object?)b));
    }

    [Fact]
    public void Round_Trip_Through_Encode_And_ParseSequence_Preserves_A_Whole_Tree()
    {
        PixTlv payload = PixTlv.CreateTemplate("62", new PixTlv[]
        {
            PixTlv.Create("01", "***"),
            PixTlv.Create("05", "PEDIDO123456"),
            PixTlv.Create("50", "custom-value"),
        });
        string encoded = payload.Encode();
        Assert.True(PixTlv.TryParse(encoded, out PixTlv? decoded));
        Assert.Equal(payload, decoded);
        Assert.True(PixTlv.TryParseSequence(encoded, out IReadOnlyList<PixTlv>? fields));
        Assert.Equal(payload, Assert.Single(fields!));
    }

    [Fact]
    public void Max_Sized_Objects_Round_Trip()
    {
        // A 99-character leaf value and a template with 99 characters of content.
        PixTlv bigLeaf = PixTlv.Create("01", new string('a', 99));
        Assert.Equal("01" + "99" + new string('a', 99), bigLeaf.Encode());
        Assert.Equal(bigLeaf, PixTlv.Parse(bigLeaf.Encode()));

        PixTlv bigChild = PixTlv.Create("50", new string('b', 95));
        PixTlv bigTemplate = PixTlv.CreateTemplate("80", new[] { PixTlv.Create("00", "br.gov.bcb.pix"), bigChild });
        Assert.Equal(99, bigTemplate.EncodedLength - 4);
        Assert.Equal(bigTemplate, PixTlv.Parse(bigTemplate.Encode()));
    }
}
