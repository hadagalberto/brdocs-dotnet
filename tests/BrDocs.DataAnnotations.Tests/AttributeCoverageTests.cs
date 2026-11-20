using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BrDocs.DataAnnotations;
using Xunit;

namespace BrDocs.DataAnnotations.Tests;

/// <summary>
/// Exhaustive per-attribute coverage dictated by SPEC.md: for every attribute at least fifteen
/// cases covering formatted input, digits-only input, a third accepted form, a bad check digit
/// (or the structural equivalent where the document has none), short and long input, illegal
/// characters, empty and null values, non-string values, the pt-BR resource message, a custom
/// message, a global resource override and the interaction with <see cref="RequiredAttribute"/>.
/// </summary>
[Collection(DataAnnotationsTestGroup.Name)]
public sealed class AttributeCoverageTests
{
    public enum Kind
    {
        Cpf,
        Cnpj,
        CnpjAlpha,
        CpfOrCnpj,
        Cep,
        Pis,
        TituloEleitor,
        Cnh,
        Renavam,
        Placa,
        TelefoneBr,
        TelefoneBrMobile,
        PixKey,
        LinhaDigitavel,
    }

    private sealed record Spec(
        Kind Kind,
        string TextA,
        string TextB,
        string TextC,
        string[] Invalid,
        string MessageFragment,
        string MessageKey);

    // Vectors are the known-valid / known-invalid values used by the core BrDocs.Tests project,
    // so an integration attribute can never drift away from the algorithm it delegates to.
    private static readonly Spec[] s_specs =
    [
        new(Kind.Cpf, "111.444.777-35", "11144477735", "529.982.247-25",
            ["11144477736", "11144477745", "11111111111", "1234567890", "111444777355",
             "111,444,777,35", "1114447773A", "\t11144477735\n", "....", "91144477735"],
            "CPF válido", nameof(ValidationMessages.CpfInvalid)),

        new(Kind.Cnpj, "11.222.333/0001-81", "11222333000181", "45.723.174/0001-10",
            ["11222333000182", "11222333000191", "00000000000000", "11111111111111", "12ABC34501DE3",
             "112223330001810", "12.AB$.345/01DE-35", "12.ABC.345/01DE-A5", "\t11222333000181\n",
             "CEP 11.222.333/0001-81"],
            "alfanumérico", nameof(ValidationMessages.CnpjAlphanumericNotAllowed)),

        new(Kind.CnpjAlpha, "12.ABC.345/01DE-35", "12ABC34501DE35", "12abc34501de35",
            ["12ABC34501DE36", "12ABC34501DE3", "12ABC34501DE355", "12.AB$.345/01DE-35",
             "12.ABC.345/01DE-A5", "\t12.ABC.345/01DE-35\n", "CEP 12.ABC.345/01DE-35",
             "00.000.000/0000-00", "AAAAAAAAAAAA46", "13ABC34501DE35"],
            "CNPJ válido", nameof(ValidationMessages.CnpjInvalid)),

        new(Kind.CpfOrCnpj, "111.444.777-35", "11144477735", "11.222.333/0001-81",
            ["11144477736", "11222333000182", "11111111111", "00000000000000", "1234567890",
             "111444777355", "111,444,777,35", "12.AB$.345/01DE-35", "....", "\t11144477735\n"],
            "CPF ou CNPJ válido", nameof(ValidationMessages.CpfOrCnpjInvalid)),

        // CEP has no check digit; its "bad check digit" case is a truncated digit group.
        new(Kind.Cep, "01310-100", "01310100", "013 10 100",
            ["01310-10", "013101000", "01310-10A", "9250000A", "01310_100", "0131:0100",
             "CEP 01310-100", "\t01310-100", "01310-100\n", "...."],
            "CEP válido", nameof(ValidationMessages.CepInvalid)),

        new(Kind.Pis, "120.70458.82-4", "12070458824", "120.70458.82 4",
            ["12070458825", "12070458814", "00000000000", "11111111111", "1207045882",
             "120704588244", "120,70458,82,4", "\t12070458824\n", "....", "1207045882A"],
            "PIS/PASEP/NIT válido", nameof(ValidationMessages.PisInvalid)),

        new(Kind.TituloEleitor, "1023 8501 06 71", "102385010671", "1023.8501.06-71",
            ["123456782098", "123456780199", "000000000000", "12345678209", "1234567820977",
             "12345678209a", "1234_5678_20_97", "titulo 123456782097", "\t123456782097\n", "...."],
            "Título de Eleitor válido", nameof(ValidationMessages.TituloEleitorInvalid)),

        new(Kind.Cnh, "026503064-61", "02650306461", "026 503 064 61",
            ["026503064-60", "026503064-51", "00000000000", "11111111111", "0265030646",
             "026503064611", "026,503,064,61", "\t02650306461\n", "....", "CNH 02650306461"],
            "CNH válida", nameof(ValidationMessages.CnhInvalid)),

        new(Kind.Renavam, "00639884962", "639884962", "00.639.884.962",
            ["00639884963", "00639884961", "00000000000", "11111111111", "63988496",
             "6398849622", "00639884,962", "\t00639884962\n", "....", "renavam00639884962"],
            "RENAVAM válido", nameof(ValidationMessages.RenavamInvalid)),

        new(Kind.Placa, "ABC-1234", "ABC1234", "ABC1D23",
            ["1234ABC", "AB-1234", "ABC-12345", "ABC12D3", "ABC_1234", "ABC:1234",
             "(ABC) 1234", "ABC123a", "\tABC-1234\n", "...."],
            "placa veicular válida", nameof(ValidationMessages.PlacaInvalid)),

        new(Kind.TelefoneBr, "(11) 91234-5678", "11912345678", "(11) 3123-4567",
            ["(00) 91234-5678", "(11) 91234-567", "(11) 91234-56789", "(11) 91234-567A",
             "1191234567", "+551191234567", "+551191234567A", "(11) 91234_5678",
             "\t11912345678\n", "...."],
            "telefone brasileiro válido", nameof(ValidationMessages.TelefoneBrInvalid)),

        new(Kind.TelefoneBrMobile, "(11) 91234-5678", "11912345678", "+5511912345678",
            ["(11) 3123-4567", "(00) 91234-5678", "(11) 91234-567", "(11) 91234-56789",
             "(11) 91234-567A", "1191234567", "+551191234567", "+551191234567A",
             "\t11912345678\n", "...."],
            "celular", nameof(ValidationMessages.TelefoneBrMobileOnly)),

        new(Kind.PixKey, "11144477735", "11222333000181", "fulano@example.com",
            ["111.444.777-35", "11144477736", "12abc34501de35", "11222333000182",
             "fulano@example", "@example.com", "+556191234567", "+5520123456789",
             "123e4567-e12b-12d1-a456-42665544000", "not-a-uuid"],
            "chave Pix válida", nameof(ValidationMessages.PixKeyInvalid)),

        new(Kind.LinhaDigitavel,
            "10492.00650 61000.100042 00997.263900 9 89810000021403",
            "10492006506100010004200997263900989810000021403",
            "83860000005-0 09600019000-9 00080178230-9 00034306271-2",
            ["10492006506100010004200997263900889810000021403",
             "1049200650610001000420099726390098981000002140",
             "104920065061000100042009972639009898100000214030",
             "1049a0065006100010004200997263900989810000021403",
             "10499898100000214032006561000100040099726390",
             "00000000000000000000000000000000000000000000000", "abc",
             "\t10492006506100010004200997263900989810000021403\n", "....",
             "838600000050096000190009000801782309000343062713"],
            "linha digitável válida", nameof(ValidationMessages.LinhaDigitavelInvalid)),
    ];

    public static TheoryData<Kind, object?, bool, string> Cases()
    {
        var data = new TheoryData<Kind, object?, bool, string>();
        foreach (Spec spec in s_specs)
        {
            data.Add(spec.Kind, spec.TextA, true, "valid formatted / canonical");
            data.Add(spec.Kind, spec.TextB, true, "valid digits-only");
            data.Add(spec.Kind, spec.TextC, true, "valid alternate form or option value");
            for (int i = 0; i < 9; i++)
            {
                data.Add(spec.Kind, spec.Invalid[i], false, $"invalid {i + 1}: {spec.Invalid[i]}");
            }
            data.Add(spec.Kind, string.Empty, false, "empty string");
            data.Add(spec.Kind, null, true, "null is valid; presence is [Required]'s job");
            data.Add(spec.Kind, 12345, false, "non-string value");
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_Attribute_Has_Fifteen_Cases_With_Real_Expectations(
        Kind kind, object? value, bool expected, string caseName)
    {
        ValidationAttribute attribute = Create(kind);

        Assert.True(attribute.IsValid(value) == expected, $"{kind}: {caseName}");
    }

    public static TheoryData<Kind, string, string> Messages()
    {
        var data = new TheoryData<Kind, string, string>();
        foreach (Spec spec in s_specs)
        {
            data.Add(spec.Kind, spec.MessageFragment, spec.MessageKey);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Messages))]
    public void Every_Attribute_Uses_Its_PtBr_Resource_Message_And_Honours_Overrides(
        Kind kind, string fragment, string messageKey)
    {
        ValidationAttribute attribute = Create(kind);

        string resource = ValidationMessages.Get(messageKey);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, resource, "Documento"),
            attribute.FormatErrorMessage("Documento"));
        Assert.Contains(fragment, attribute.FormatErrorMessage("Documento"), StringComparison.Ordinal);

        attribute.ErrorMessage = "Mensagem local para {0}";
        Assert.Equal("Mensagem local para Documento", attribute.FormatErrorMessage("Documento"));

        try
        {
            ValidationMessages.SetMessage(messageKey, "Global {0}");
            Assert.Equal("Global Campo", Create(kind).FormatErrorMessage("Campo"));
        }
        finally
        {
            ValidationMessages.SetMessage(messageKey, null);
        }
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, resource, "Documento"),
            Create(kind).FormatErrorMessage("Documento"));
    }

    public static TheoryData<Kind, string> ValidValues()
    {
        var data = new TheoryData<Kind, string>();
        foreach (Spec spec in s_specs)
        {
            data.Add(spec.Kind, spec.TextA);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void Every_Attribute_Interacts_With_Required_In_The_Standard_Way(Kind kind, string valid)
    {
        ValidationAttribute[] attributes = [new RequiredAttribute(), Create(kind)];
        var context = new ValidationContext(new object());
        var results = new List<ValidationResult>();

        // Null is accepted by the BrDocs attribute but rejected by [Required].
        Assert.False(Validator.TryValidateValue(null, context, results, attributes));
        Assert.Single(results);
        Assert.True(Create(kind).IsValid(null), "the BrDocs attribute alone must accept null");

        // Empty fails [Required] and the BrDocs attribute even without [Required].
        results.Clear();
        Assert.False(Validator.TryValidateValue(string.Empty, context, results, attributes));
        Assert.Single(results);
        results.Clear();
        Assert.False(Validator.TryValidateValue(string.Empty, context, results, [Create(kind)]));
        Assert.Single(results);

        // A valid value satisfies both attributes.
        results.Clear();
        Assert.True(Validator.TryValidateValue(valid, context, results, attributes));
        Assert.Empty(results);
    }

    [Fact]
    public void Every_Spec_Provides_Enough_Distinct_Invalid_Vectors()
    {
        foreach (Spec spec in s_specs)
        {
            Assert.Equal(10, spec.Invalid.Length);
            Assert.Equal(10, spec.Invalid.Distinct(StringComparer.Ordinal).Count());
            Assert.DoesNotContain(spec.TextA, spec.Invalid, StringComparer.Ordinal);
            Assert.DoesNotContain(spec.TextB, spec.Invalid, StringComparer.Ordinal);
            Assert.DoesNotContain(spec.TextC, spec.Invalid, StringComparer.Ordinal);
        }
    }

    private static ValidationAttribute Create(Kind kind) => kind switch
    {
        Kind.Cpf => new CpfAttribute(),
        Kind.Cnpj => new CnpjAttribute(),
        Kind.CnpjAlpha => new CnpjAttribute { AllowAlphanumeric = true },
        Kind.CpfOrCnpj => new CpfOrCnpjAttribute(),
        Kind.Cep => new CepAttribute(),
        Kind.Pis => new PisAttribute(),
        Kind.TituloEleitor => new TituloEleitorAttribute(),
        Kind.Cnh => new CnhAttribute(),
        Kind.Renavam => new RenavamAttribute(),
        Kind.Placa => new PlacaAttribute(),
        Kind.TelefoneBr => new TelefoneBrAttribute(),
        Kind.TelefoneBrMobile => new TelefoneBrAttribute { MobileOnly = true },
        Kind.PixKey => new PixKeyAttribute(),
        Kind.LinhaDigitavel => new LinhaDigitavelAttribute(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
