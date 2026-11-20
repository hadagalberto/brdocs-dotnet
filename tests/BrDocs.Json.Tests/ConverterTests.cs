using System.Text.Json;
using System.Text.Json.Serialization;
using BrDocs.Json;
using Xunit;

namespace BrDocs.Json.Tests;

public sealed class ConverterTests
{
    public static IEnumerable<object[]> Documents()
    {
        yield return new object[] { Cpf.Parse("111.444.777-35"), "\"111.444.777-35\"", "\"11144477735\"", "\"111.111.111-11\"" };
        yield return new object[] { Cnpj.Parse("11.222.333/0001-81"), "\"11.222.333/0001-81\"", "\"11222333000181\"", "\"11.222.333/0001-82\"" };
        yield return new object[] { Cnpj.Parse("12.ABC.345/01DE-35"), "\"12.ABC.345/01DE-35\"", "\"12ABC34501DE35\"", "\"12.ABC.345/01DE-36\"" };
        yield return new object[] { Cep.Parse("01310-100"), "\"01310-100\"", "\"01310100\"", "\"01310-10\"" };
        yield return new object[] { Pis.Parse("120.70458.82-4"), "\"120.70458.82-4\"", "\"12070458824\"", "\"000.00000.00-0\"" };
        yield return new object[] { TituloEleitor.Parse("1023 8501 06 71"), "\"1023 8501 06 71\"", "\"102385010671\"", "\"1023 8501 06 70\"" };
        yield return new object[] { Cnh.Parse("026503064-61"), "\"026503064-61\"", "\"02650306461\"", "\"026503064-62\"" };
        yield return new object[] { Renavam.Parse("00639884962"), "\"00639884962\"", "\"00639884962\"", "\"00639884963\"" };
        yield return new object[] { Placa.Parse("ABC-1234"), "\"ABC-1234\"", "\"ABC1234\"", "\"1234ABC\"" };
        yield return new object[] { Placa.Parse("ABC1D23"), "\"ABC1D23\"", "\"ABC1D23\"", "\"ABC1D23X\"" };
        yield return new object[] { TelefoneBr.Parse("(11) 91234-5678"), "\"(11) 91234-5678\"", "\"11912345678\"", "\"(00) 91234-5678\"" };
        yield return new object[] { Boleto.Parse("10492006506100010004200997263900989810000021403"), "\"10492.00650 61000.100042 00997.263900 9 89810000021403\"", "\"10492006506100010004200997263900989810000021403\"", "\"00000000000000000000000000000000000000000000\"" };
        yield return new object[] { Arrecadacao.Parse("838600000050096000190009000801782309000343062712"), "\"83860000005-0 09600019000-9 00080178230-9 00034306271-2\"", "\"838600000050096000190009000801782309000343062712\"", "\"0000000000000000000000000000000000000000000000\"" };
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public void Converter_Writes_The_Requested_Style_And_Reads_Both_Styles(object value, string formattedJson, string digitsJson, string invalidJson)
    {
        JsonSerializerOptions formatted = new JsonSerializerOptions().AddBrDocsConverters(BrDocsJsonOptions.Default);
        JsonSerializerOptions digits = new JsonSerializerOptions().AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);
        Type type = value.GetType();

        Assert.Equal(formattedJson, JsonSerializer.Serialize(value, type, formatted));
        Assert.Equal(digitsJson, JsonSerializer.Serialize(value, type, digits));

        Assert.Equal(value, JsonSerializer.Deserialize(formattedJson, type, formatted));
        Assert.Equal(value, JsonSerializer.Deserialize(digitsJson, type, formatted));
        Assert.Equal(value, JsonSerializer.Deserialize(formattedJson, type, digits));
        Assert.Equal(value, JsonSerializer.Deserialize(digitsJson, type, digits));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(invalidJson, type, formatted));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(invalidJson, type, digits));
    }

    [Fact]
    public void Reading_Also_Accepts_Legacy_Renavam_And_E164_Phone_Input()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();

        Assert.Equal(Renavam.Parse("00639884962"), JsonSerializer.Deserialize<Renavam>("\"639884962\"", options));
        Assert.Equal(TelefoneBr.Parse("11912345678"), JsonSerializer.Deserialize<TelefoneBr>("\"+5511912345678\"", options));
        Assert.Equal(Cep.Parse("01310100"), JsonSerializer.Deserialize<Cep>("\"01310 100\"", options));
    }

    [Fact]
    public void Null_Handling_Follows_Nullable_And_Non_Nullable_Contracts()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();

        Assert.Equal("null", JsonSerializer.Serialize<Cpf?>(null, options));
        Assert.Null(JsonSerializer.Deserialize<Cpf?>("null", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Cpf>("null", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Cpf?>("\"111.111.111-11\"", options));
    }

    [Fact]
    public void Non_String_Tokens_Are_Rejected_With_A_Clear_JsonException()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();

        JsonException number = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Cpf>("123", options));
        Assert.Contains("JSON string", number.Message, StringComparison.Ordinal);
        JsonException invalid = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Cpf>("\"111.111.111-11\"", options));
        Assert.Contains("not a valid CPF", invalid.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBrDocsConverters_Is_Fluent_And_Registers_Every_Converter()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        JsonSerializerOptions result = options.AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);

        Assert.Same(options, result);
        Assert.Equal(12, options.Converters.Count);
        Assert.Contains(options.Converters, converter => converter is CpfJsonConverter { WriteFormatted: false });
        Assert.Contains(options.Converters, converter => converter is BoletoJsonConverter { WriteFormatted: false });
        Assert.Contains(options.Converters, converter => converter is PixPayloadJsonConverter);
    }

    [Fact]
    public void Converters_Are_Usable_Through_JsonConverterAttribute()
    {
        string json = JsonSerializer.Serialize(new DocumentDto { Cpf = Cpf.Parse("111.444.777-35") });

        Assert.Equal("{\"Cpf\":\"111.444.777-35\"}", json);
        DocumentDto? dto = JsonSerializer.Deserialize<DocumentDto>(json);
        Assert.NotNull(dto);
        Assert.Equal(Cpf.Parse("111.444.777-35"), dto.Cpf);
    }

    [Fact]
    public void PixPayload_Converter_RoundTrips_A_Built_BrCode_And_Rejects_Bad_Crc()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();
        PixPayload payload = PixPayload.CreateStatic("11144477735", "Fulano de Tal", "BRASILIA").Build();
        string json = JsonSerializer.Serialize(payload, options);

        Assert.Equal(JsonSerializer.Serialize(payload.ToString()), json);
        Assert.Equal(payload, JsonSerializer.Deserialize<PixPayload>(json, options));

        string tampered = payload.ToString()[..^1] + (payload.ToString()[^1] == '0' ? '1' : '0');
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PixPayload>(JsonSerializer.Serialize(tampered), options));
    }

    private sealed class DocumentDto
    {
        [JsonConverter(typeof(CpfJsonConverter))]
        public Cpf Cpf { get; set; }
    }
}
