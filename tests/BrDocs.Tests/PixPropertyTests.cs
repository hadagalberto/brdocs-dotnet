using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Property-based tests for the Pix BR Code: the builders match independent oracles, the CRC
/// matches a table-driven oracle, TLV decoding matches an independent parser, and every payload
/// that parses round-trips byte for byte.
/// </summary>
public sealed class PixPropertyTests
{
    // Alphabets for the generated inputs.
    private const string UpperAlphanumeric = Oracles.UpperAlphanumericAlphabet;
    private const string Digits = Oracles.DigitsAlphabet;
    private const string Atext = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._%+-!#$&'*/=?^`{|}~";
    private const string TlvGarbage = "0123456789br.gov.cbpix.*-.@";

    // ---- Keys ----------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void PixKey_Matches_The_Oracle_For_Arbitrary_Cpf_Like_Strings(int seed)
    {
        // 11-digit strings are detected as CPFs (shape first), so the oracle only needs the
        // CPF/CNPJ arms for these.
        string value = Oracles.RandomString(seed, 11, Digits);
        Assert.Equal(Oracles.IsValidPixKeyOracle(value), PixKey.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void PixKey_Matches_The_Oracle_For_Arbitrary_Cnpj_Like_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 14, UpperAlphanumeric);
        Assert.Equal(Oracles.IsValidPixKeyOracle(value), PixKey.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void PixKey_Matches_The_Oracle_For_Arbitrary_Email_Like_Strings(int seed)
    {
        uint state = (uint)seed | 1u;
        int localLength = 1 + (int)(state % 20u);
        string local = Oracles.RandomString((int)state, localLength, Atext);
        string domain = Oracles.RandomString((int)(state >> 8) | 1, 1 + (int)(state % 12u), "examplecombr.-");
        string value = local + "@" + domain;
        Assert.Equal(Oracles.IsValidPixKeyOracle(value), PixKey.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void PixKey_Matches_The_Oracle_For_Arbitrary_Uuid_Like_Strings(int seed)
    {
        string hex = Oracles.RandomString(seed, 32, "0123456789abcdefABCDEF");
        string value = hex[..8] + "-" + hex[8..12] + "-" + hex[12..16] + "-" + hex[16..20] + "-" + hex[20..];
        Assert.Equal(Oracles.IsValidPixKeyOracle(value), PixKey.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void PixKey_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        // No leading '+': the phone arm needs a real ANATEL DDD, covered separately below.
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 60, TlvGarbage);
        Assert.Equal(Oracles.IsValidPixKeyOracle(value), PixKey.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Generated_Phones_Are_Valid_Keys_And_Normalize_To_Themselves(int seed)
    {
        string e164 = TelefoneBr.Generate(new Random(seed)).ToE164();
        Assert.Equal(PixKeyKind.Phone, PixKey.Detect(e164));
        Assert.True(PixKey.IsPhone(e164));
        Assert.Equal(e164, PixKey.Normalize(e164));
    }

    [Property(MaxTest = 1_000)]
    public void Generated_Documents_Are_Valid_Keys(int seed)
    {
        Random random = new(seed);
        string cpf = Cpf.Generate(random).ToString("N");
        string cnpj = Cnpj.Generate(random).ToString("N");
        string alphanumeric = Cnpj.GenerateAlphanumeric(random).ToString("N");
        Assert.True(PixKey.IsValid(cpf));
        Assert.True(PixKey.IsValid(cnpj));
        Assert.True(PixKey.IsValid(alphanumeric));
        Assert.Equal(PixKeyKind.Cpf, PixKey.Detect(cpf));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect(cnpj));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect(alphanumeric));
    }

    [Property(MaxTest = 1_000)]
    public void PixKey_Normalization_Round_Trips_Canonical_Keys(int seed)
    {
        // Whatever the library accepts as canonical normalizes to itself.
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 60, TlvGarbage);
        if (PixKey.IsValid(value))
        {
            Assert.Equal(value, PixKey.Normalize(value));
        }
        else
        {
            Assert.Throws<FormatException>(() => PixKey.Normalize(value));
        }
    }

    // ---- Name and city normalization ------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Normalized_Names_Only_Hold_Allowed_Characters(int seed)
    {
        // Random text with mappable accents (precomposed and combining) always builds and the
        // encoded payload only ever carries ASCII letters, digits and spaces in field 59.
        string raw = Oracles.RandomString(seed, 1 + Math.Abs(seed) % 25,
            "abcXYZ 0123456789áàâãäéêíóôõöúçüÁÀÉÊÍÓÔÕÚÇñÑÿÝ");
        PixPayload payload = PixPayload.CreateStatic("123e4567-e12b-12d1-a456-426655440000", raw, "BRASILIA").Build();
        Assert.InRange(payload.Name.Length, 1, 25);
        Assert.All(payload.Name, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c == ' ', payload.Name));
        Assert.True(PixPayload.IsValid(payload.ToString()));
        Assert.Equal(payload.Name, PixPayload.Parse(payload.ToString()).Name);
    }

    // ---- TLV -----------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Tlv_Decoding_Matches_The_Oracle_For_Arbitrary_Streams(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 80, TlvGarbage);
        bool libraryParsed = PixTlv.TryParseSequence(value, out IReadOnlyList<PixTlv>? fields);
        Assert.Equal(Oracles.TryParseTlvStreamOracle(value) is not null, libraryParsed);
        if (libraryParsed)
        {
            Assert.True(Oracles.TlvTreesEqual(Oracles.TryParseTlvStreamOracle(value), fields));
        }
    }

    [Property(MaxTest = 500)]
    public void Tlv_Encoding_Round_Trips_Through_The_Oracle(int seed)
    {
        // A random tree of leaves under a template (26/62/80-99), built with the library,
        // re-parses to an equal tree and agrees with the oracle parse.
        Random random = new(seed);
        int childCount = random.Next(1, 4);
        List<PixTlv> children = new();
        for (int i = 0; i < childCount; i++)
        {
            string id = i == 0 ? "00" : "0" + (i + 2);
            children.Add(PixTlv.Create(id, Oracles.RandomString(random.Next(), random.Next(1, 30), TlvGarbage)));
        }
        PixTlv template = PixTlv.CreateTemplate(random.Next(3) == 0 ? "26" : "80", children);
        string encoded = template.Encode();
        Assert.Equal(template, PixTlv.Parse(encoded));
        Assert.True(Oracles.TlvTreesEqual(Oracles.TryParseTlvStreamOracle(encoded), new[] { template }));
    }

    // ---- Payload building, CRC and round trips ----------------------------------

    [Property(MaxTest = 1_000)]
    public void Static_Builders_Match_The_Independent_Oracle(int seed)
    {
        Random random = new(seed);
        string key = Cpf.Generate(random).ToString("N");
        string name = Oracles.RandomString(random.Next(), random.Next(1, 25), "abcXYZ 0123456789");
        string city = Oracles.RandomString(random.Next(), random.Next(1, 15), "abcXYZ 0123456789");
        string txid = Oracles.RandomString(random.Next(), random.Next(1, 25), Oracles.LowerAlphanumericAlphabet);
        string mcc = Oracles.RandomString(random.Next(), 4, Digits);
        decimal amount = random.Next(0, 100_000) / 100m;
        string? description = random.Next(3) == 0
            ? Oracles.RandomString(random.Next(), random.Next(1, 20), TlvGarbage)
            : null;

        string library = PixPayload.CreateStatic(key, name, city)
            .WithTxid(txid)
            .WithMcc(mcc)
            .WithAmount(amount)
            .WithDescription(description)
            .Build()
            .ToString();

        string oracle = Oracles.BuildStaticPayloadOracle(
            key, name, city, txid, mcc, amount.ToString("0.00", CultureInfo.InvariantCulture), description, null, null);
        Assert.Equal(oracle, library);

        // The CRC is the oracle's, the payload parses and round-trips, and the typed fields match.
        PixPayload parsed = PixPayload.Parse(library);
        Assert.Equal(library, parsed.ToString());
        Assert.Equal(key, parsed.Key);
        Assert.Equal(name, parsed.Name);
        Assert.Equal(city, parsed.City);
        Assert.Equal(txid, parsed.Txid);
        Assert.Equal(mcc, parsed.Mcc);
        Assert.Equal(amount, parsed.Amount);
        Assert.Equal(description, parsed.Description);
    }

    [Property(MaxTest = 1_000)]
    public void Dynamic_Builders_Match_The_Independent_Oracle(int seed)
    {
        Random random = new(seed);
        string location = Oracles.RandomString(random.Next(), random.Next(1, 60), "example.com/cobv0123456789abcdef.-_~");
        string name = Oracles.RandomString(random.Next(), random.Next(1, 25), "abcXYZ 0123456789");
        string city = Oracles.RandomString(random.Next(), random.Next(1, 15), "abcXYZ 0123456789");
        bool singleUse = random.Next(2) == 0;
        string bill = Oracles.RandomString(random.Next(), random.Next(1, 25), Oracles.LowerAlphanumericAlphabet);

        string library = PixPayload.CreateDynamic(location, name, city)
            .SingleUse(singleUse)
            .WithAdditionalDataField("01", bill)
            .Build()
            .ToString();
        string oracle = Oracles.BuildDynamicPayloadOracle(location, name, city, singleUse, new[] { ("01", bill) });
        Assert.Equal(oracle, library);

        PixPayload parsed = PixPayload.Parse(library);
        Assert.Equal(library, parsed.ToString());
        Assert.Equal(location, parsed.Location);
        Assert.True(parsed.IsDynamic);
        Assert.Equal(singleUse, parsed.IsSingleUse);
    }

    [Property(MaxTest = 1_000)]
    public void The_Crc_Matches_The_Oracle_Over_Arbitrary_Text(int seed)
    {
        // Through the payload builder: the last four characters of every built payload must be
        // the oracle's CRC-16/CCITT-FALSE over the rest of the payload.
        string key = Cnpj.Generate(new Random(seed)).ToString("N");
        string name = Oracles.RandomString(seed, 1 + Math.Abs(seed) % 20, "abcXYZ 0123456789");
        string payload = PixPayload.CreateStatic(key, name, "SAO PAULO").Build().ToString();
        Assert.Equal(Oracles.Crc16Oracle(payload[..^4]), payload[^4..]);
        Assert.True(PixPayload.IsValid(payload));
    }

    [Property(MaxTest = 2_000)]
    public void Every_Payload_That_Parses_Round_Trips_Byte_For_Byte(int seed)
    {
        // Start from the official vectors, apply up to three mutations and repair the CRC: when
        // the result still parses, it must re-encode to exactly the same text (the library's
        // documented guarantee), and re-parse to an equal payload.
        string[] vectors =
        {
            "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D",
            "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***630464E4",
        };
        string payload = vectors[Math.Abs(seed) % vectors.Length];
        for (int mutation = 0; mutation < 1 + Math.Abs(seed) % 3; mutation++)
        {
            payload = Oracles.Mutate(seed * 31 + mutation, payload);
        }
        payload = Oracles.FixCrc(payload);
        if (PixPayload.TryParse(payload, out PixPayload? parsed) && parsed is not null)
        {
            Assert.Equal(payload, parsed.ToString());
            Assert.Equal(payload, PixPayload.Parse(payload).ToString());
            Assert.Equal(parsed, PixPayload.Parse(payload));
        }
        else
        {
            Assert.False(PixPayload.IsValid(payload));
        }
    }

    [Property(MaxTest = 2_000)]
    public void A_Mutated_Payload_That_Still_Parses_Matches_The_Oracle_Crc_And_Round_Trips(int seed)
    {
        // One arbitrary mutation of an official vector: CRC-16/CCITT-FALSE detects every
        // single-character change, so a surviving parse implies the oracle CRC agrees with the
        // carried one — and the library guarantees the payload re-encodes to the same text.
        string[] vectors =
        {
            "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D",
            "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***630464E4",
        };
        string payload = vectors[Math.Abs(seed) % vectors.Length];
        string mutated = Oracles.Mutate(seed, payload);
        if (PixPayload.TryParse(mutated, out PixPayload? parsed) && parsed is not null)
        {
            Assert.Equal(mutated, Oracles.FixCrc(mutated));
            Assert.Equal(mutated, parsed.ToString());
            Assert.True(PixPayload.IsValid(mutated));
        }
        else
        {
            Assert.False(PixPayload.IsValid(mutated));
        }
    }
}
