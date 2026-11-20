using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BrDocs.DataAnnotations;
using Xunit;

namespace BrDocs.DataAnnotations.Tests;

[Collection(DataAnnotationsTestGroup.Name)]
public sealed class AttributeTests
{
    private const string CpfFormatted = "111.444.777-35";
    private const string CpfDigits = "11144477735";
    private const string CnpjFormatted = "11.222.333/0001-81";
    private const string CnpjDigits = "11222333000181";
    private const string AlphaCnpjFormatted = "12.ABC.345/01DE-35";
    private const string AlphaCnpjDigits = "12ABC34501DE35";
    private const string CepFormatted = "01310-100";
    private const string CepDigits = "01310100";
    private const string PisFormatted = "120.70458.82-4";
    private const string PisDigits = "12070458824";
    private const string TituloFormatted = "1023 8501 06 71";
    private const string TituloDigits = "102385010671";
    private const string CnhFormatted = "026503064-61";
    private const string CnhDigits = "02650306461";
    private const string RenavamDigits = "00639884962";
    private const string RenavamLegacy = "639884962";
    private const string PlacaFormatted = "ABC-1234";
    private const string PlacaChars = "ABC1234";
    private const string PhoneFormatted = "(11) 91234-5678";
    private const string PhoneNational = "11912345678";
    private const string LinhaFormatted = "10492.00650 61000.100042 00997.263900 9 89810000021403";
    private const string LinhaDigits = "10492006506100010004200997263900989810000021403";

    public static TheoryData<Type, string, string, string> Documents => new()
    {
        { typeof(CpfAttribute), CpfFormatted, CpfDigits, "111.111.111-11" },
        { typeof(CnpjAttribute), CnpjFormatted, CnpjDigits, "11.222.333/0001-82" },
        { typeof(CpfOrCnpjAttribute), CpfFormatted, CpfDigits, "111.111.111-11" },
        { typeof(CpfOrCnpjAttribute), CnpjFormatted, CnpjDigits, "11.222.333/0001-82" },
        { typeof(CepAttribute), CepFormatted, CepDigits, "01310-10" },
        { typeof(PisAttribute), PisFormatted, PisDigits, "000.00000.00-0" },
        { typeof(TituloEleitorAttribute), TituloFormatted, TituloDigits, "1023 8501 06 70" },
        { typeof(CnhAttribute), CnhFormatted, CnhDigits, "026503064-62" },
        { typeof(RenavamAttribute), RenavamDigits, RenavamLegacy, "00639884963" },
        { typeof(PlacaAttribute), PlacaFormatted, PlacaChars, "1234ABC" },
        { typeof(TelefoneBrAttribute), PhoneFormatted, PhoneNational, "(00) 91234-5678" },
        { typeof(LinhaDigitavelAttribute), LinhaFormatted, LinhaDigits, LinhaDigits + "0" },
    };

    [Theory]
    [MemberData(nameof(Documents))]
    public void IsValid_Accepts_Known_Valid_Formatted_And_Unformatted_Values(Type attributeType, string formatted, string digits, string invalid)
    {
        ValidationAttribute attribute = (ValidationAttribute)Activator.CreateInstance(attributeType)!;

        Assert.True(attribute.IsValid(formatted), $"{attributeType.Name} rejected formatted '{formatted}'.");
        Assert.True(attribute.IsValid(digits), $"{attributeType.Name} rejected unformatted '{digits}'.");
        Assert.False(attribute.IsValid(invalid), $"{attributeType.Name} accepted invalid '{invalid}'.");
    }

    public static TheoryData<Type> AttributeTypes => new()
    {
        typeof(CpfAttribute),
        typeof(CnpjAttribute),
        typeof(CpfOrCnpjAttribute),
        typeof(CepAttribute),
        typeof(PisAttribute),
        typeof(TituloEleitorAttribute),
        typeof(CnhAttribute),
        typeof(RenavamAttribute),
        typeof(PlacaAttribute),
        typeof(TelefoneBrAttribute),
        typeof(PixKeyAttribute),
        typeof(LinhaDigitavelAttribute),
    };

    [Theory]
    [MemberData(nameof(AttributeTypes))]
    public void IsValid_Handles_Null_Empty_And_Non_String_Values(Type attributeType)
    {
        ValidationAttribute attribute = (ValidationAttribute)Activator.CreateInstance(attributeType)!;

        Assert.True(attribute.IsValid(null));
        Assert.False(attribute.IsValid(string.Empty));
        Assert.False(attribute.IsValid(12345));
        Assert.False(attribute.IsValid(new object()));
    }

    [Fact]
    public void PixKeyAttribute_Accepts_Canonical_Keys_And_Rejects_Formatted_Or_Invalid_Ones()
    {
        var attribute = new PixKeyAttribute();

        Assert.True(attribute.IsValid(CpfDigits));
        Assert.True(attribute.IsValid("11222333000181"));
        Assert.True(attribute.IsValid("12ABC34501DE35"));
        Assert.True(attribute.IsValid("fulano@example.com"));
        Assert.True(attribute.IsValid("+5561912345678"));
        Assert.True(attribute.IsValid("123e4567-e12b-12d1-a456-426655440000"));
        Assert.False(attribute.IsValid(CpfFormatted));
        Assert.False(attribute.IsValid("fulano@example"));
        Assert.False(attribute.IsValid("+556191234567"));
        Assert.False(attribute.IsValid("not-a-uuid"));
        Assert.True(attribute.IsValid(null));
    }

    [Fact]
    public void CnpjAttribute_AllowAlphanumeric_Controls_The_New_Format()
    {
        var strict = new CnpjAttribute();
        var permissive = new CnpjAttribute { AllowAlphanumeric = true };

        Assert.True(strict.IsValid(CnpjFormatted));
        Assert.True(permissive.IsValid(CnpjFormatted));
        Assert.False(strict.IsValid(AlphaCnpjFormatted), "Alphanumeric CNPJ must be rejected by default.");
        Assert.False(strict.IsValid(AlphaCnpjDigits));
        Assert.True(permissive.IsValid(AlphaCnpjFormatted));
        Assert.True(permissive.IsValid(AlphaCnpjDigits), "Lowercase/uppercase normalization and unformatted input must work.");
        Assert.False(permissive.IsValid("12.ABC.345/01DE-36"));
    }

    [Fact]
    public void TelefoneBrAttribute_MobileOnly_Rejects_Fixed_Lines()
    {
        var any = new TelefoneBrAttribute();
        var mobileOnly = new TelefoneBrAttribute { MobileOnly = true };

        Assert.True(any.IsValid("(11) 3123-4567"));
        Assert.False(mobileOnly.IsValid("(11) 3123-4567"));
        Assert.True(mobileOnly.IsValid(PhoneFormatted));
        Assert.True(mobileOnly.IsValid("+5511912345678"));
    }

    [Theory]
    [InlineData("838600000050096000190009000801782309000343062712")]
    [InlineData("83860000005-0 09600019000-9 00080178230-9 00034306271-2")]
    [InlineData("858200000007572503282030560708202107539591904460")]
    public void LinhaDigitavelAttribute_Accepts_Arrecadacao_And_Rejects_Barcodes(string arrecadacao)
    {
        var attribute = new LinhaDigitavelAttribute();
        const string barcode = "10499898100000214032006561000100040099726390";

        Assert.True(attribute.IsValid(arrecadacao));
        Assert.False(attribute.IsValid(barcode), "A 44-digit barcode is not a linha digitável.");
    }

    [Fact]
    public void Messages_Are_PtBR_From_Resources_Even_Under_An_English_UiCulture()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var results = new List<ValidationResult>();
            var model = new CpfModel { Document = "111.111.111-11" };

            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));
            ValidationResult result = Assert.Single(results);
            Assert.Contains("CPF válido", result.ErrorMessage, StringComparison.Ordinal);
            Assert.Contains("deve conter", result.ErrorMessage, StringComparison.Ordinal);
            Assert.Contains(nameof(CpfModel.Document), result.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void Messages_Can_Be_Overridden_Globally_And_Per_Attribute()
    {
        const string key = nameof(ValidationMessages.CpfInvalid);
        try
        {
            ValidationMessages.SetMessage(key, "CPF personalizado para {0}.");
            var results = new List<ValidationResult>();
            var model = new CpfModel { Document = "111.111.111-11" };
            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));
            Assert.Equal("CPF personalizado para Document.", Assert.Single(results).ErrorMessage);

            var attribute = new CpfAttribute { ErrorMessage = "Mensagem local" };
            Assert.Equal("Mensagem local", attribute.FormatErrorMessage("Document"));
        }
        finally
        {
            ValidationMessages.SetMessage(key, null);
        }
        Assert.Contains("CPF válido", new CpfAttribute().FormatErrorMessage("Document"), StringComparison.Ordinal);
    }

    [Fact]
    public void Option_Specific_Messages_Are_Used()
    {
        Assert.Contains("alfanumérico", new CnpjAttribute().FormatErrorMessage("Document"), StringComparison.Ordinal);
        Assert.Contains("CNPJ válido", new CnpjAttribute { AllowAlphanumeric = true }.FormatErrorMessage("Document"), StringComparison.Ordinal);
        Assert.Contains("celular", new TelefoneBrAttribute { MobileOnly = true }.FormatErrorMessage("Document"), StringComparison.Ordinal);
        Assert.Contains("brasileiro", new TelefoneBrAttribute().FormatErrorMessage("Document"), StringComparison.Ordinal);
    }

    private sealed class CpfModel
    {
        [Cpf]
        public string? Document { get; set; }
    }
}
