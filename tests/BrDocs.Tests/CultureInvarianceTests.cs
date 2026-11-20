using System.Globalization;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Guards the SPEC requirement "culture-invariant everywhere": parsing, formatting and the
/// IFormattable surface must behave identically under any current culture.
/// </summary>
public sealed class CultureInvarianceTests
{
    public static TheoryData<string> ExoticCultures => new()
    {
        "pt-BR",
        "de-DE",
        "ar-SA",
        "th-TH",
        "tr-TR",
        "en-US",
    };

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Cpf_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Cpf cpf = Cpf.Parse("529.982.247-25");
            Assert.Equal("529.982.247-25", cpf.ToString());
            Assert.Equal("52998224725", cpf.ToString("N"));
            Assert.Equal("529.982.247-25", ((IFormattable)cpf).ToString("G", other));
            Assert.Equal("52998224725", ((IFormattable)cpf).ToString("N", other));
            Assert.Equal(cpf, ParsableHelpers.ParseViaSpanParsable<Cpf>("52998224725".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Cnpj_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Cnpj cnpj = Cnpj.Parse("12.abc.345/01de-35");
            Assert.Equal("12.ABC.345/01DE-35", cnpj.ToString());
            Assert.Equal("12ABC34501DE35", cnpj.ToString("N"));
            Assert.Equal("12.ABC.345/01DE-35", ((IFormattable)cnpj).ToString("G", other));
            Assert.Equal("12ABC34501DE35", ((IFormattable)cnpj).ToString("N", other));
            Assert.Equal(cnpj, ParsableHelpers.ParseViaSpanParsable<Cnpj>("12ABC34501DE35".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Cep_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Cep cep = Cep.Parse(" 01310 100 ");
            Assert.Equal("01310-100", cep.ToString());
            Assert.Equal("01310100", cep.ToString("N"));
            Assert.Equal("01310-100", ((IFormattable)cep).ToString("G", other));
            Assert.Equal("01310100", ((IFormattable)cep).ToString("N", other));
            Assert.Equal(cep, ParsableHelpers.ParseViaSpanParsable<Cep>("01310100".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
