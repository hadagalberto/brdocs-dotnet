using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace BrDocs.FluentValidation.Tests;

[Collection(FluentValidationTestGroup.Name)]
public sealed class RuleTests
{
    private const string CpfFormatted = "111.444.777-35";
    private const string CpfDigits = "11144477735";
    private const string CnpjFormatted = "11.222.333/0001-81";
    private const string CnpjDigits = "11222333000181";
    private const string AlphaCnpj = "12.ABC.345/01DE-35";
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

    public static TheoryData<RuleKind, string, string, string, string> Rules => new()
    {
        { RuleKind.Cpf, CpfFormatted, CpfDigits, "111.111.111-11", "CPF válido" },
        { RuleKind.Cnpj, CnpjFormatted, CnpjDigits, "11.222.333/0001-82", "CNPJ numérico válido" },
        { RuleKind.CpfOrCnpj, CpfFormatted, CpfDigits, "111.111.111-11", "CPF ou CNPJ válido" },
        { RuleKind.CpfOrCnpj, CnpjFormatted, CnpjDigits, "11.222.333/0001-82", "CPF ou CNPJ válido" },
        { RuleKind.Cep, CepFormatted, CepDigits, "01310-10", "CEP válido" },
        { RuleKind.Pis, PisFormatted, PisDigits, "000.00000.00-0", "PIS/PASEP/NIT válido" },
        { RuleKind.TituloEleitor, TituloFormatted, TituloDigits, "1023 8501 06 70", "Título de Eleitor válido" },
        { RuleKind.Cnh, CnhFormatted, CnhDigits, "026503064-62", "CNH válida" },
        { RuleKind.Renavam, RenavamDigits, RenavamLegacy, "00639884963", "RENAVAM válido" },
        { RuleKind.Placa, PlacaFormatted, PlacaChars, "1234ABC", "placa veicular válida" },
        { RuleKind.TelefoneBr, PhoneFormatted, PhoneNational, "(00) 91234-5678", "telefone brasileiro válido" },
        { RuleKind.PixKey, CpfDigits, "fulano@example.com", CpfFormatted, "chave Pix válida" },
        { RuleKind.LinhaDigitavel, LinhaFormatted, LinhaDigits, LinhaDigits + "0", "linha digitável válida" },
    };

    [Theory]
    [MemberData(nameof(Rules))]
    public void Rule_Accepts_Known_Valid_Values_And_Rejects_Known_Invalid_Ones(
        RuleKind kind, string formatted, string digits, string invalid, string messageFragment)
    {
        Assert.True(Validate(formatted, kind).IsValid, $"{kind} rejected '{formatted}'.");
        Assert.True(Validate(digits, kind).IsValid, $"{kind} rejected '{digits}'.");

        ValidationResult result = Validate(invalid, kind);
        Assert.False(result.IsValid, $"{kind} accepted '{invalid}'.");
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Contains(messageFragment, failure.ErrorMessage, StringComparison.Ordinal);
    }

    public static TheoryData<RuleKind> AllRules => new()
    {
        RuleKind.Cpf, RuleKind.Cnpj, RuleKind.CpfOrCnpj, RuleKind.Cep, RuleKind.Pis,
        RuleKind.TituloEleitor, RuleKind.Cnh, RuleKind.Renavam, RuleKind.Placa,
        RuleKind.TelefoneBr, RuleKind.PixKey, RuleKind.LinhaDigitavel,
    };

    [Theory]
    [MemberData(nameof(AllRules))]
    public void Rule_Treats_Null_As_Valid_And_Empty_As_Invalid(RuleKind kind)
    {
        Assert.True(Validate(null, kind).IsValid);
        Assert.False(Validate(string.Empty, kind).IsValid);
    }

    [Fact]
    public void Cnpj_Option_Controls_Alphanumeric_Acceptance_And_Message()
    {
        Assert.False(Validate(AlphaCnpj, RuleKind.Cnpj).IsValid);
        Assert.Contains("alfanumérico", Validate(AlphaCnpj, RuleKind.Cnpj).Errors[0].ErrorMessage, StringComparison.Ordinal);

        Assert.True(Validate(AlphaCnpj, RuleKind.CnpjAlpha).IsValid);
        Assert.True(Validate("12abc34501de35", RuleKind.CnpjAlpha).IsValid);
        Assert.True(Validate(CnpjDigits, RuleKind.CnpjAlpha).IsValid);
        Assert.False(Validate("12.ABC.345/01DE-36", RuleKind.CnpjAlpha).IsValid);
    }

    [Fact]
    public void TelefoneBr_Option_Controls_Mobile_Only_And_Message()
    {
        Assert.True(Validate("(11) 3123-4567", RuleKind.TelefoneBr).IsValid);
        Assert.False(Validate("(11) 3123-4567", RuleKind.TelefoneBrMobile).IsValid);
        Assert.Contains("celular", Validate("(11) 3123-4567", RuleKind.TelefoneBrMobile).Errors[0].ErrorMessage, StringComparison.Ordinal);
        Assert.True(Validate(PhoneFormatted, RuleKind.TelefoneBrMobile).IsValid);
        Assert.True(Validate("+5511912345678", RuleKind.TelefoneBrMobile).IsValid);
    }

    [Fact]
    public void LinhaDigitavel_Rule_Rejects_Barcodes_And_Accepts_Arrecadacao()
    {
        Assert.False(Validate("10499898100000214032006561000100040099726390", RuleKind.LinhaDigitavel).IsValid);
        Assert.True(Validate("838600000050096000190009000801782309000343062712", RuleKind.LinhaDigitavel).IsValid);
    }

    [Fact]
    public void Messages_Remain_PtBR_Under_An_English_UiCulture()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            ValidationResult result = Validate("111.111.111-11", RuleKind.Cpf);
            Assert.Contains("deve conter um CPF válido", result.Errors[0].ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void Messages_Can_Be_Overridden_Globally()
    {
        const string key = nameof(ValidationMessages.CpfInvalid);
        try
        {
            ValidationMessages.SetMessage(key, "Regra personalizada para {PropertyName}.");
            ValidationResult result = Validate("111.111.111-11", RuleKind.Cpf);
            Assert.Equal("Regra personalizada para Value.", result.Errors[0].ErrorMessage);
        }
        finally
        {
            ValidationMessages.SetMessage(key, null);
        }
        Assert.Contains("CPF válido", Validate("111.111.111-11", RuleKind.Cpf).Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Rules_Throw_On_A_Null_Builder()
    {
        Assert.Throws<ArgumentNullException>(() => BrDocsRuleExtensions.MustBeCpf<Model>(null!));
        Assert.Throws<ArgumentNullException>(() => BrDocsRuleExtensions.MustBeCnpj<Model>(null!));
        Assert.Throws<ArgumentNullException>(() => BrDocsRuleExtensions.MustBeTelefoneBr<Model>(null!));
        Assert.Throws<ArgumentNullException>(() => BrDocsRuleExtensions.MustBeLinhaDigitavel<Model>(null!));
    }

    private static ValidationResult Validate(string? value, RuleKind kind)
        => new RuleValidator(kind).Validate(new Model { Value = value });

    public enum RuleKind
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

    private sealed class Model
    {
        public string? Value { get; set; }
    }

    private sealed class RuleValidator : AbstractValidator<Model>
    {
        public RuleValidator(RuleKind kind)
        {
            RuleFor(model => model.Value).RuleForKind(kind);
        }
    }
}

internal static class RuleKindExtensions
{
    internal static IRuleBuilderOptions<T, string?> RuleForKind<T>(this IRuleBuilder<T, string?> builder, RuleTests.RuleKind kind)
        => kind switch
        {
            RuleTests.RuleKind.Cpf => builder.MustBeCpf(),
            RuleTests.RuleKind.Cnpj => builder.MustBeCnpj(),
            RuleTests.RuleKind.CnpjAlpha => builder.MustBeCnpj(allowAlphanumeric: true),
            RuleTests.RuleKind.CpfOrCnpj => builder.MustBeCpfOrCnpj(),
            RuleTests.RuleKind.Cep => builder.MustBeCep(),
            RuleTests.RuleKind.Pis => builder.MustBePis(),
            RuleTests.RuleKind.TituloEleitor => builder.MustBeTituloEleitor(),
            RuleTests.RuleKind.Cnh => builder.MustBeCnh(),
            RuleTests.RuleKind.Renavam => builder.MustBeRenavam(),
            RuleTests.RuleKind.Placa => builder.MustBePlaca(),
            RuleTests.RuleKind.TelefoneBr => builder.MustBeTelefoneBr(),
            RuleTests.RuleKind.TelefoneBrMobile => builder.MustBeTelefoneBr(mobileOnly: true),
            RuleTests.RuleKind.PixKey => builder.MustBePixKey(),
            RuleTests.RuleKind.LinhaDigitavel => builder.MustBeLinhaDigitavel(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
}
