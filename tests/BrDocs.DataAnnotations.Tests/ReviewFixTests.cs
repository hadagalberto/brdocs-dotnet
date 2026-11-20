using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BrDocs.DataAnnotations.Tests;

/// <summary>Regression tests for the DataAnnotations defects reported by the independent reviewer.</summary>
[Collection(DataAnnotationsTestGroup.Name)]
public sealed class ReviewFixTests
{
    [Fact]
    public void Cnpj_Attribute_Reports_The_Actual_Reason_For_A_Bad_Numeric_Cnpj()
    {
        // 11.222.333/0001-82 has a wrong second check digit (81 is valid), so the failure is an
        // invalid CNPJ — not a rejected alphanumeric format.
        var attribute = new CnpjAttribute();
        Assert.False(attribute.IsValid("11.222.333/0001-82"));
        string message = attribute.FormatErrorMessage("Documento");
        Assert.Contains("CNPJ válido", message, StringComparison.Ordinal);
        Assert.DoesNotContain("alfanumérico", message, StringComparison.Ordinal);

        // A genuinely alphanumeric key with AllowAlphanumeric = false keeps its own message.
        Assert.False(attribute.IsValid("12.ABC.345/01DE-35"));
        Assert.Contains("alfanumérico", attribute.FormatErrorMessage("Documento"), StringComparison.Ordinal);
    }

    [Fact]
    public void Attributes_Accept_Document_Value_Objects_As_Well_As_Strings()
    {
        var cpf = new CpfAttribute();
        Assert.True(cpf.IsValid(Cpf.Parse("111.444.777-35")));
        Assert.False(cpf.IsValid(default(Cpf)));

        var model = new ValueObjectModel
        {
            Cpf = Cpf.Parse("111.444.777-35"),
            Cnpj = Cnpj.Parse("11.222.333/0001-81"),
            Placa = Placa.Parse("ABC-1234"),
        };
        var results = new List<ValidationResult>();
        Assert.True(
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true),
            string.Join("; ", results.Select(r => r.ErrorMessage)));

        model.Cpf = default;
        results.Clear();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ValueObjectModel.Cpf)));
    }

    private sealed class ValueObjectModel
    {
        [Cpf]
        public Cpf Cpf { get; set; }

        [Cnpj]
        public Cnpj Cnpj { get; set; }

        [Placa]
        public Placa Placa { get; set; }
    }
}
