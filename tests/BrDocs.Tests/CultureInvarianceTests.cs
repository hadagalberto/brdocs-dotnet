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

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Pis_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Pis pis = Pis.Parse(" 120 70458 82 4 ");
            Assert.Equal("120.70458.82-4", pis.ToString());
            Assert.Equal("12070458824", pis.ToString("N"));
            Assert.Equal("120.70458.82-4", ((IFormattable)pis).ToString("G", other));
            Assert.Equal("12070458824", ((IFormattable)pis).ToString("N", other));
            Assert.Equal(pis, ParsableHelpers.ParseViaSpanParsable<Pis>("12070458824".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void TituloEleitor_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            TituloEleitor titulo = TituloEleitor.Parse("1234.5678.20.97");
            Assert.Equal("1234 5678 20 97", titulo.ToString());
            Assert.Equal("123456782097", titulo.ToString("N"));
            Assert.Equal("1234 5678 20 97", ((IFormattable)titulo).ToString("G", other));
            Assert.Equal("123456782097", ((IFormattable)titulo).ToString("N", other));
            Assert.Equal(titulo, ParsableHelpers.ParseViaSpanParsable<TituloEleitor>("123456782097".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Cnh_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Cnh cnh = Cnh.Parse(" 026 503 064 61 ");
            Assert.Equal("026503064-61", cnh.ToString());
            Assert.Equal("02650306461", cnh.ToString("N"));
            Assert.Equal("026503064-61", ((IFormattable)cnh).ToString("G", other));
            Assert.Equal("02650306461", ((IFormattable)cnh).ToString("N", other));
            Assert.Equal(cnh, ParsableHelpers.ParseViaSpanParsable<Cnh>("02650306461".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Renavam_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Renavam renavam = Renavam.Parse(" 639884962 ");
            Assert.Equal("00639884962", renavam.ToString());
            Assert.Equal("00639884962", renavam.ToString("N"));
            Assert.Equal("00639884962", ((IFormattable)renavam).ToString("G", other));
            Assert.Equal("00639884962", ((IFormattable)renavam).ToString("N", other));
            Assert.Equal(renavam, ParsableHelpers.ParseViaSpanParsable<Renavam>("639884962".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void Placa_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            Placa legacy = Placa.Parse(" abc-1234 ");
            Assert.Equal("ABC-1234", legacy.ToString());
            Assert.Equal("ABC1234", legacy.ToString("N"));
            Assert.Equal("ABC-1234", ((IFormattable)legacy).ToString("G", other));
            Assert.Equal("ABC1234", ((IFormattable)legacy).ToString("N", other));
            Assert.Equal(legacy, ParsableHelpers.ParseViaSpanParsable<Placa>("ABC1234".AsSpan(), other));

            Placa mercosul = Placa.Parse("abc1d23");
            Assert.Equal("ABC1D23", mercosul.ToString());
            Assert.Equal(mercosul, ParsableHelpers.ParseViaSpanParsable<Placa>("ABC1D23".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ExoticCultures))]
    public void TelefoneBr_Parsing_And_Formatting_Are_Culture_Invariant(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo other = CultureInfo.GetCultureInfo("fr-FR");

            TelefoneBr phone = TelefoneBr.Parse(" +55 (11) 91234-5678 ");
            Assert.Equal("(11) 91234-5678", phone.ToString());
            Assert.Equal("11912345678", phone.ToString("N"));
            Assert.Equal("(11) 91234-5678", ((IFormattable)phone).ToString("G", other));
            Assert.Equal("11912345678", ((IFormattable)phone).ToString("N", other));
            Assert.Equal("+5511912345678", phone.ToE164());
            Assert.Equal(phone, ParsableHelpers.ParseViaSpanParsable<TelefoneBr>("11912345678".AsSpan(), other));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
