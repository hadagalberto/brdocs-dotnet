using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BrDocs.Json;
using Xunit;

namespace BrDocs.Json.Tests;

/// <summary>
/// Exhaustive per-converter coverage dictated by SPEC.md: every converter gets at least fifteen
/// cases (formatted read, digits-only read, alternate accepted form, bad check digit or CRC,
/// short/long input, illegal characters, empty string, null and non-string tokens), plus both
/// output styles, round-trips through nullable value types and through a source-generated
/// <see cref="JsonSerializerContext"/>.
/// </summary>
public sealed partial class ConverterCoverageTests
{
    public enum Kind
    {
        Cpf, Cnpj, Cep, Pis, TituloEleitor, Cnh, Renavam, Placa, TelefoneBr,
        Boleto, Arrecadacao, PixPayload,
    }

    private sealed record Spec(
        Kind Kind, Type Type, string FormattedJson, string DigitsJson, string AlternateJson,
        string[] Invalid, bool NullIsValid);

    private static readonly Spec[] s_specs =
    [
        new(Kind.Cpf, typeof(Cpf), "\"111.444.777-35\"", "\"11144477735\"", "\"52998224725\"",
            ["\"11144477736\"", "\"11111111111\"", "\"1234567890\"", "\"111444777355\"",
             "\"111,444,777,35\"", "\"1114447773A\"", "\"....\"", "null", "123", "\"abc\""], false),
        new(Kind.Cnpj, typeof(Cnpj), "\"11.222.333/0001-81\"", "\"11222333000181\"",
            "\"12.ABC.345/01DE-35\"",
            ["\"11222333000182\"", "\"12ABC34501DE36\"", "\"00000000000000\"", "\"12ABC34501DE3\"",
             "\"112223330001810\"", "\"12.AB$.345/01DE-35\"", "\"12.ABC.345/01DE-A5\"", "\"....\"",
             "null", "123"], false),
        new(Kind.Cep, typeof(Cep), "\"01310-100\"", "\"01310100\"", "\"013 10 100\"",
            ["\"01310-10\"", "\"013101000\"", "\"01310-10A\"", "\"01310_100\"",
             "\"CEP 01310-100\"", "\"....\"", "null", "123", "\"\\t01310-100\\n\"", "\"01310:100\""], false),
        new(Kind.Pis, typeof(Pis), "\"120.70458.82-4\"", "\"12070458824\"", "\"120.70458.82 4\"",
            ["\"12070458825\"", "\"00000000000\"", "\"1207045882\"", "\"120704588244\"",
             "\"120,70458,82,4\"", "\"1207045882A\"", "\"....\"", "null", "123", "\"120_70458_82_4\""], false),
        new(Kind.TituloEleitor, typeof(TituloEleitor), "\"1023 8501 06 71\"", "\"102385010671\"",
            "\"1023.8501.06-71\"",
            ["\"123456782098\"", "\"000000000000\"", "\"12345678209\"", "\"1234567820977\"",
             "\"12345678209a\"", "\"1234_5678_20_97\"", "\"....\"", "null", "123",
             "\"titulo 123456782097\""], false),
        new(Kind.Cnh, typeof(Cnh), "\"026503064-61\"", "\"02650306461\"", "\"026 503 064 61\"",
            ["\"026503064-60\"", "\"00000000000\"", "\"0265030646\"", "\"026503064611\"",
             "\"026,503,064,61\"", "\"026503064-6A\"", "\"....\"", "null", "123", "\"CNH 02650306461\""], false),
        new(Kind.Renavam, typeof(Renavam), "\"00639884962\"", "\"00639884962\"", "\"639884962\"",
            ["\"00639884963\"", "\"00000000000\"", "\"63988496\"", "\"6398849622\"",
             "\"00639884,962\"", "\"0063988496A\"", "\"....\"", "null", "123", "\"renavam00639884962\""], false),
        new(Kind.Placa, typeof(Placa), "\"ABC-1234\"", "\"ABC1234\"", "\"ABC1D23\"",
            ["\"1234ABC\"", "\"AB-1234\"", "\"ABC-12345\"", "\"ABC12D3\"", "\"ABC_1234\"",
             "\"ABC123a\"", "\"....\"", "null", "123", "\"(ABC) 1234\""], false),
        new(Kind.TelefoneBr, typeof(TelefoneBr), "\"(11) 91234-5678\"", "\"11912345678\"",
            "\"+5511912345678\"",
            ["\"(00) 91234-5678\"", "\"(11) 91234-567\"", "\"(11) 91234-56789\"",
             "\"(11) 91234-567A\"", "\"+551191234567A\"", "\"(11) 91234_5678\"", "\"....\"",
             "null", "123", "\"1191234567\""], false),
        new(Kind.Boleto, typeof(Boleto),
            "\"10492.00650 61000.100042 00997.263900 9 89810000021403\"",
            "\"10492006506100010004200997263900989810000021403\"",
            "\"10499898100000214032006561000100040099726390\"",
            ["\"10492006506100010004200997263900889810000021403\"",
             "\"1049200650610001000420099726390098981000002140\"",
             "\"104920065061000100042009972639009898100000214030\"",
             "\"1049a0065006100010004200997263900989810000021403\"",
             "\"10499898100000214032006561000100040099726391\"",
             "\"00000000000000000000000000000000000000000000\"", "\"....\"", "null", "123", "\"abc\""], false),
        new(Kind.Arrecadacao, typeof(Arrecadacao),
            "\"83860000005-0 09600019000-9 00080178230-9 00034306271-2\"",
            "\"838600000050096000190009000801782309000343062712\"",
            "\"83860000005096000190000008017823000034306271\"",
            ["\"838600000050096000190009000801782309000343062713\"",
             "\"83860000005009600019000900080178230900034306271\"",
             "\"8386000000500960001900090008017823090003430627123\"",
             "\"838a00000050096000190009000801782309000343062712\"", "\"....\"", "null", "123",
             "\"abc\"", "\"746500000003299902962024004101360008002006441147\"",
             "\"858200000007572503282030560708202107539591904461\""], false),
        new(Kind.PixPayload, typeof(PixPayload),
            "\"00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D\"",
            "\"00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D\"",
            JsonSerializer.Serialize(PixPayload.CreateStatic("11144477735", "Fulano de Tal", "BRASILIA").Build().ToString()),
            ["\"00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3E\"",
             "\"00020126360014BR.GOV.BCB.PIX0114+55119123456785204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63040000\"",
             "\"000201\"", "\"abc\"", "\"....\"", "\"{}\"", "true", "123.45", "[]",
             "\"00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-426655440000\""], true),
    ];

    public static TheoryData<Kind, string, bool, string> Cases()
    {
        var data = new TheoryData<Kind, string, bool, string>();
        foreach (Spec spec in s_specs)
        {
            data.Add(spec.Kind, spec.FormattedJson, true, "formatted read");
            data.Add(spec.Kind, spec.DigitsJson, true, "digits-only read");
            data.Add(spec.Kind, spec.AlternateJson, true, "alternate accepted form");
            for (int i = 0; i < 9; i++)
            {
                data.Add(spec.Kind, spec.Invalid[i], false, $"invalid {i + 1}: {spec.Invalid[i]}");
            }
            data.Add(spec.Kind, "\"\"", false, "empty JSON string");
            data.Add(spec.Kind, "null", spec.NullIsValid, "null token");
            data.Add(spec.Kind, "123", false, "number token");
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_Converter_Has_Fifteen_Cases_And_Writes_Both_Styles(Kind kind, string json, bool valid, string caseName)
    {
        Spec spec = s_specs.Single(item => item.Kind == kind);
        JsonSerializerOptions formatted = new JsonSerializerOptions().AddBrDocsConverters(BrDocsJsonOptions.Default);
        JsonSerializerOptions digits = new JsonSerializerOptions().AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);

        if (!valid)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, spec.Type, formatted));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, spec.Type, digits));
            return;
        }

        object? value = JsonSerializer.Deserialize(json, spec.Type, formatted);
        Assert.True(spec.NullIsValid && json == "null" ? value is null : value is not null,
            $"{kind}: {caseName}");
        if (value is null)
        {
            return; // PixPayload is a reference type: its null token is covered separately.
        }
        // Expected output comes from the value object's own formatting contract, so an alternate
        // accepted input (another CPF, a Mercosul plate, an alphanumeric CNPJ...) is checked
        // against its canonical representation rather than against the primary vector.
        string canonicalFormatted = value is ISpanFormattable span
            ? span.ToString(null, CultureInfo.InvariantCulture)!
            : value.ToString()!;
        string canonicalDigits = value is ISpanFormattable spanDigits
            ? spanDigits.ToString("N", CultureInfo.InvariantCulture)!
            : value.ToString()!;
        string writtenFormatted = JsonSerializer.Serialize(value, spec.Type, formatted);
        string writtenDigits = JsonSerializer.Serialize(value, spec.Type, digits);
        Assert.Equal(JsonSerializer.Serialize(canonicalFormatted), writtenFormatted);
        Assert.Equal(JsonSerializer.Serialize(canonicalDigits), writtenDigits);
        if (json == spec.FormattedJson || json == spec.DigitsJson)
        {
            Assert.Equal(spec.FormattedJson, writtenFormatted);
            Assert.Equal(spec.DigitsJson, writtenDigits);
        }
        Assert.Equal(value, JsonSerializer.Deserialize(json, spec.Type, digits));
    }

    [Fact]
    public void Every_Spec_Is_Well_Formed()
    {
        Assert.Equal(12, s_specs.Length);
        foreach (Spec spec in s_specs)
        {
            Assert.Equal(10, spec.Invalid.Length);
            Assert.Equal(10, spec.Invalid.Distinct(StringComparer.Ordinal).Count());
            Assert.DoesNotContain(spec.FormattedJson, spec.Invalid, StringComparer.Ordinal);
            Assert.DoesNotContain(spec.DigitsJson, spec.Invalid, StringComparer.Ordinal);
        }
    }

    public static TheoryData<Kind, string, string> ValueTypeSpecs()
    {
        var data = new TheoryData<Kind, string, string>();
        foreach (Spec spec in s_specs.Where(item => !item.NullIsValid))
        {
            data.Add(spec.Kind, spec.FormattedJson, spec.DigitsJson);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ValueTypeSpecs))]
    public void Nullable_Value_Types_RoundTrip_And_Handle_Null(Kind kind, string formattedJson, string digitsJson)
    {
        Type type = s_specs.Single(item => item.Kind == kind).Type;
        Type nullableType = typeof(Nullable<>).MakeGenericType(type);
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();

        Assert.Null(JsonSerializer.Deserialize("null", nullableType, options));
        Assert.Equal("null", JsonSerializer.Serialize((object?)null, nullableType, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("null", type, options));

        object? fromFormatted = JsonSerializer.Deserialize(formattedJson, nullableType, options);
        object? fromDigits = JsonSerializer.Deserialize(digitsJson, nullableType, options);
        Assert.NotNull(fromFormatted);
        Assert.NotNull(fromDigits);
        Assert.Equal(fromFormatted, fromDigits);
        Assert.Equal(formattedJson, JsonSerializer.Serialize(fromFormatted, nullableType, options));
        Assert.Equal(digitsJson, JsonSerializer.Serialize(fromFormatted, nullableType,
            new JsonSerializerOptions().AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly)));
    }

    [Fact]
    public void Nullable_Reference_Payload_RoundTrips_And_Handles_Null()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();

        Assert.Null(JsonSerializer.Deserialize<PixPayload?>("null", options));
        Assert.Equal("null", JsonSerializer.Serialize<PixPayload?>(null, options));
        PixPayload? payload = JsonSerializer.Deserialize<PixPayload?>(s_specs[11].FormattedJson, options);
        Assert.NotNull(payload);
        Assert.Equal(s_specs[11].FormattedJson, JsonSerializer.Serialize(payload, options));
    }

    public static TheoryData<Kind, string, string> SourceGeneratedSpecs()
    {
        var data = new TheoryData<Kind, string, string>();
        foreach (Spec spec in s_specs)
        {
            data.Add(spec.Kind, spec.FormattedJson, spec.DigitsJson);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(SourceGeneratedSpecs))]
    public void Source_Generated_Context_RoundTrips_Every_Converter(Kind kind, string formattedJson, string digitsJson)
    {
        Type type = s_specs.Single(item => item.Kind == kind).Type;
        JsonSerializerContext context = BrDocsJsonContext.Default;

        object? value = JsonSerializer.Deserialize(formattedJson, type, context);
        Assert.NotNull(value);
        Assert.Equal(formattedJson, JsonSerializer.Serialize(value, type, context));

        object? fromDigits = JsonSerializer.Deserialize(digitsJson, type, context);
        Assert.Equal(value, fromDigits);

        if (!s_specs.Single(item => item.Kind == kind).NullIsValid)
        {
            Type nullableType = typeof(Nullable<>).MakeGenericType(type);
            Assert.Null(JsonSerializer.Deserialize("null", nullableType, context));
            object? nullableValue = JsonSerializer.Deserialize(formattedJson, nullableType, context);
            Assert.NotNull(nullableValue);
            Assert.Equal(formattedJson, JsonSerializer.Serialize(nullableValue, nullableType, context));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("null", type, context));
        }
    }

    [Fact]
    public void Source_Generated_Context_Uses_The_Converters_For_Non_String_Tokens_Too()
    {
        JsonSerializerContext context = BrDocsJsonContext.Default;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("123", typeof(Cpf), context));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("\"111.111.111-11\"", typeof(Cpf), context));
    }

    /// <summary>
    /// Source-generated context: the converters are declared explicitly because they are supplied
    /// by a library rather than discovered from <c>[JsonConverter]</c> on the value objects.
    /// </summary>
    [JsonSourceGenerationOptions(Converters = new[]
    {
        typeof(CpfJsonConverter), typeof(CnpjJsonConverter), typeof(CepJsonConverter),
        typeof(PisJsonConverter), typeof(TituloEleitorJsonConverter), typeof(CnhJsonConverter),
        typeof(RenavamJsonConverter), typeof(PlacaJsonConverter), typeof(TelefoneBrJsonConverter),
        typeof(BoletoJsonConverter), typeof(ArrecadacaoJsonConverter), typeof(PixPayloadJsonConverter),
    })]
    [JsonSerializable(typeof(Cpf))]
    [JsonSerializable(typeof(Cpf?))]
    [JsonSerializable(typeof(Cnpj))]
    [JsonSerializable(typeof(Cnpj?))]
    [JsonSerializable(typeof(Cep))]
    [JsonSerializable(typeof(Cep?))]
    [JsonSerializable(typeof(Pis))]
    [JsonSerializable(typeof(Pis?))]
    [JsonSerializable(typeof(TituloEleitor))]
    [JsonSerializable(typeof(TituloEleitor?))]
    [JsonSerializable(typeof(Cnh))]
    [JsonSerializable(typeof(Cnh?))]
    [JsonSerializable(typeof(Renavam))]
    [JsonSerializable(typeof(Renavam?))]
    [JsonSerializable(typeof(Placa))]
    [JsonSerializable(typeof(Placa?))]
    [JsonSerializable(typeof(TelefoneBr))]
    [JsonSerializable(typeof(TelefoneBr?))]
    [JsonSerializable(typeof(Boleto))]
    [JsonSerializable(typeof(Boleto?))]
    [JsonSerializable(typeof(Arrecadacao))]
    [JsonSerializable(typeof(Arrecadacao?))]
    [JsonSerializable(typeof(PixPayload))]
    internal sealed partial class BrDocsJsonContext : JsonSerializerContext
    {
    }
}
