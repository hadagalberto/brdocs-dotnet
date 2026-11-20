using System.Globalization;
using System.Text;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Regression tests for the defects reported by the independent reviewer. Every expected value
/// below is derived by hand from the cited official rule (not from the library under test).
/// </summary>
public sealed class ReviewFixTests
{
    private const string EvpKey = "123e4567-e12b-12d1-a456-426655440000";
    private const string StaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    // ---- Finding 1: ambiguous 11-digit input must not become a phone key ------------------

    [Fact]
    public void Ambiguous_Eleven_Digit_Input_Is_Rejected_By_Normalize_And_The_Builder()
    {
        // Hand-derived: 11987654022 has 11 digits. Its CPF check digits fail (DV1 = 2 matches,
        // DV2 computes to 1 but the text carries 2), and it is not a canonical "+55" phone, so
        // PixKey.Detect returns null. Normalize must agree instead of guessing a mobile key.
        const string ambiguous = "11987654022";
        Assert.Null(PixKey.Detect(ambiguous));

        Span<char> buffer = stackalloc char[PixKey.MaxLength];
        Assert.False(PixKey.TryNormalize(ambiguous, buffer, out int written));
        Assert.Equal(0, written);
        Assert.Throws<FormatException>(() => PixKey.Normalize(ambiguous));
        Assert.Throws<ArgumentException>(() => PixPayload.CreateStatic(ambiguous, "Fulano de Tal", "BRASILIA"));
    }

    [Fact]
    public void An_Invalid_Formatted_Cpf_Does_Not_Fall_Back_To_A_Phone_Key()
    {
        // 111.444.777-34 strips to 11 digits: the CPF DV is wrong and 11 + 944477734 would be a
        // plausible mobile. CPF-shaped input must never be reinterpreted as a phone.
        const string ambiguous = "111.444.777-34";
        Assert.Null(PixKey.Detect(ambiguous));
        Assert.Throws<FormatException>(() => PixKey.Normalize(ambiguous));
    }

    [Fact]
    public void Whitespace_Padding_Does_Not_Hide_The_Ambiguous_Shape()
    {
        // Trailing whitespace is insignificant, so "11987654022 " is still the same ambiguous
        // CPF-shaped 11-digit input and must not be recast as a phone key.
        const string padded = " 11987654022 ";
        Assert.Throws<FormatException>(() => PixKey.Normalize(padded));
    }

    [Fact]
    public void Explicit_Phone_Forms_Are_Still_Normalized()
    {
        // Parentheses, spaces or a 13-digit country-code prefix make the key type explicit;
        // only the bare 11-digit CPF shape is ambiguous.
        Assert.Equal("+5511912345678", PixKey.Normalize("(11) 91234-5678"));
        Assert.Equal("+5511912345678", PixKey.Normalize("11 91234 5678"));
        Assert.Equal("+5561912345678", PixKey.Normalize("5561912345678"));
    }

    // ---- Finding 2: description/name/city normalization and CRC over the exact bytes -------

    [Fact]
    public void Description_Accents_Are_Stripped_And_The_Crc_Covers_The_Emitted_Bytes()
    {
        // BCB manual: text fields use the EMV common character set, so "Café" is emitted as
        // "Cafe". The TLV length (04) and CRC must then describe those exact ASCII bytes.
        string brCode = PixPayload.CreateStatic(EvpKey, "Fulano de Tal", "BRASILIA")
            .WithDescription("Café")
            .Build()
            .ToString();

        Assert.Contains("0204Cafe", brCode, StringComparison.Ordinal);
        Assert.Equal("Cafe", PixPayload.Parse(brCode).Description);
        Assert.Equal(IndependentCrc16Hex(Encoding.UTF8.GetBytes(brCode[..^4])), brCode[^4..]);
        Assert.True(PixPayload.IsValid(brCode));
    }

    [Fact]
    public void Name_And_City_Accents_Produce_Ascii_Lengths_And_Crc()
    {
        // "Café da Silva" -> 13 ASCII chars ("Cafe da Silva"); "Goiânia" -> 7 ("Goiania").
        string brCode = PixPayload.CreateStatic(EvpKey, "Café da Silva", "Goiânia").Build().ToString();
        Assert.Contains("5913Cafe da Silva", brCode, StringComparison.Ordinal);
        Assert.Contains("6007Goiania", brCode, StringComparison.Ordinal);
        Assert.Equal("Cafe da Silva", PixPayload.Parse(brCode).Name);
        Assert.Equal("Goiania", PixPayload.Parse(brCode).City);
        Assert.Equal(IndependentCrc16Hex(Encoding.UTF8.GetBytes(brCode[..^4])), brCode[^4..]);
    }

    [Fact]
    public void The_Independent_Crc_Oracle_Reproduces_The_Bcb_Manual_Vector()
    {
        // Sanity check of the oracle itself against the checksum printed in the BCB manual.
        Assert.Equal("1D3D", IndependentCrc16Hex(Encoding.UTF8.GetBytes(StaticVector[..^4])));
    }

    [Fact]
    public void A_Payload_Carrying_A_Non_Ascii_Description_Is_Rejected()
    {
        // A scanned payload with a multi-byte character would break the single-byte length/CRC
        // contract, so it must be rejected rather than silently mis-encoded by a QR writer.
        string originalMai = Oracles.Tlv("26", Oracles.Tlv("00", "br.gov.bcb.pix") + Oracles.Tlv("01", EvpKey));
        string accentedMai = Oracles.Tlv("26", Oracles.Tlv("00", "br.gov.bcb.pix") + Oracles.Tlv("01", EvpKey) + Oracles.Tlv("02", "Café"));
        string payload = Oracles.FixCrc(StaticVector.Replace(originalMai, accentedMai, StringComparison.Ordinal));

        Assert.False(PixPayload.IsValid(payload));
        Assert.Throws<FormatException>(() => PixPayload.Parse(payload));
    }

    // ---- Finding 3: spaces-only merchant name / city --------------------------------------

    [Fact]
    public void A_Name_Or_City_Of_Only_Spaces_Is_Rejected_While_Parsing_Too()
    {
        // Hand-derived from the allowed character set: a text field needs at least one
        // letter or digit; three spaces alone carry no merchant identity.
        string noName = Oracles.FixCrc(StaticVector.Replace("5913Fulano de Tal", Oracles.Tlv("59", "   "), StringComparison.Ordinal));
        string noCity = Oracles.FixCrc(StaticVector.Replace("6008BRASILIA", Oracles.Tlv("60", "   "), StringComparison.Ordinal));

        Assert.False(PixPayload.IsValid(noName));
        Assert.False(PixPayload.IsValid(noCity));
        Assert.Throws<FormatException>(() => PixPayload.Parse(noName));
        Assert.Throws<FormatException>(() => PixPayload.Parse(noCity));
    }

    // ---- Finding 6: the all-zero default plate is structurally valid -----------------------

    [Fact]
    public void The_Documented_All_Zero_Default_Plate_Round_Trips()
    {
        // SPEC.md and the README both state that the default plate's all-zero value is
        // structurally valid (plates have no check digit or reserved-sequence rule).
        Placa @default = default;
        Assert.Equal("000-0000", @default.ToString());
        Assert.True(Placa.IsValid(@default.ToString()));
        Assert.True(Placa.IsValid("0000000"));
        Assert.True(Placa.TryParse(@default.ToString(), out Placa parsed));
        Assert.Equal(@default, parsed);
        Assert.True(parsed.IsLegacy);
    }

    // ---- Finding 9: deterministic boleto due dates ----------------------------------------

    [Fact]
    public void DueDate_And_Generate_Resolve_Against_An_Explicit_Reference_Date()
    {
        // FEBRABAN vector: factor 8981 (the Caixa barcode) is 2022-05-10.
        Boleto caixa = Boleto.Parse("10499898100000214032006561000100040099726390");
        Assert.Equal(new DateOnly(2022, 5, 10), caixa.GetDueDate(new DateOnly(2022, 5, 10)));

        // Generate draws days with Random.Next(0, 91) from the supplied reference. Reproduce
        // those two draws with the BCL Random (independent of BrDocs) to get the exact date.
        DateOnly reference = new(2030, 1, 1);
        var draws = new Random(7);
        int expectedBank = draws.Next(1, 1000);
        int expectedDays = draws.Next(0, 91);
        Boleto generated = Boleto.Generate(new Random(7), reference);

        Assert.Equal(expectedBank, generated.BankCode);
        Assert.Equal(reference.AddDays(expectedDays), generated.GetDueDate(reference));
        Assert.Equal(generated.DueDateFactor, Boleto.Encode(reference.AddDays(expectedDays)));
        Assert.Null(Boleto.Create(1, 9, null, 1m, new string('1', 25)).GetDueDate(reference));
    }

    // ---- Helpers ---------------------------------------------------------------------------

    /// <summary>
    /// CRC-16/CCITT-FALSE computed with a 256-entry lookup table over the exact bytes that a QR
    /// writer emits (poly 0x1021, init 0xFFFF, no reflection, no final XOR). Deliberately a
    /// different implementation shape from the library's bit-by-bit char loop.
    /// </summary>
    private static string IndependentCrc16Hex(byte[] bytes)
    {
        ushort[] table = new ushort[256];
        for (int value = 0; value < 256; value++)
        {
            ushort entry = (ushort)(value << 8);
            for (int bit = 0; bit < 8; bit++)
            {
                entry = (entry & 0x8000) != 0 ? (ushort)((entry << 1) ^ 0x1021) : (ushort)(entry << 1);
            }
            table[value] = entry;
        }

        ushort crc = 0xFFFF;
        foreach (byte b in bytes)
        {
            crc = (ushort)((crc << 8) ^ table[(crc >> 8) ^ b]);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
