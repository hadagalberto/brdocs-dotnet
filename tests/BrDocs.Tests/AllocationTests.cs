using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Verifies the SPEC requirement that validation is allocation-free: <c>IsValid</c>,
/// <c>TryParse</c> and <c>TryFormat</c> must not allocate anything on the GC heap.
/// </summary>
/// <remarks>
/// Test parallelization is disabled for the whole assembly (see TestAssemblyInfo.cs) so that
/// <see cref="GC.GetTotalAllocatedBytes"/> measures only this suite's allocations.
/// </remarks>
public sealed class AllocationTests
{
    private const int WarmupIterations = 2_000;
    private const int MeasuredIterations = 5_000;
    private const int MaxWindows = 12;

    /// <summary>
    /// Warms the code up (past tiered-JIT tier-up points), then measures short windows and
    /// returns the smallest delta, stopping early at the first perfectly clean window.
    /// <see cref="GC.GetTotalAllocatedBytes"/> counts allocations from ALL threads, so foreign
    /// threads in the test host can dirty a window; a clean window therefore proves the steady
    /// state allocates nothing, while a genuinely allocating path (any allocation per call is
    /// at least 5_000 × 24 bytes per window) would dirty every single window and surface the
    /// smallest delta. The non-precise counter read is used on purpose: forcing a collection
    /// between windows would itself stimulate finalizer-thread work that pollutes the next
    /// window.
    /// </summary>
    private static long MeasureAllocations(Func<bool> action)
    {
        for (int i = 0; i < WarmupIterations; i++)
        {
            _ = action();
        }
        long minimum = long.MaxValue;
        for (int window = 0; window < MaxWindows; window++)
        {
            long before = GC.GetTotalAllocatedBytes(precise: false);
            for (int i = 0; i < MeasuredIterations; i++)
            {
                _ = action();
            }
            long delta = GC.GetTotalAllocatedBytes(precise: false) - before;
            if (delta == 0)
            {
                return 0;
            }
            minimum = Math.Min(minimum, delta);
        }
        return minimum;
    }

    // ---- CPF ---------------------------------------------------------------

    [Fact]
    public void Cpf_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cpf.IsValid("111.444.777-35")));
    }

    [Fact]
    public void Cpf_IsValid_Raw_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cpf.IsValid("11144477735")));
    }

    [Fact]
    public void Cpf_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cpf.IsValid("111.444.777-36")));
    }

    [Fact]
    public void Cpf_IsValid_AllSame_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cpf.IsValid("111.111.111-11")));
    }

    [Fact]
    public void Cpf_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cpf.TryParse("529.982.247-25", out _)));
    }

    [Fact]
    public void Cpf_TryFormat_Does_Not_Allocate()
    {
        Cpf cpf = Cpf.Parse("11144477735");
        char[] buffer = new char[14];
        Assert.Equal(0, MeasureAllocations(() => cpf.TryFormat(buffer, out _, "G", null)));
    }

    // ---- CNPJ --------------------------------------------------------------

    [Fact]
    public void Cnpj_IsValid_Numeric_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnpj.IsValid("11.222.333/0001-81")));
    }

    [Fact]
    public void Cnpj_IsValid_Alphanumeric_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnpj.IsValid("12.ABC.345/01DE-35")));
    }

    [Fact]
    public void Cnpj_IsValid_Lowercase_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnpj.IsValid("12abc34501de35")));
    }

    [Fact]
    public void Cnpj_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnpj.IsValid("12.ABC.345/01DE-36")));
    }

    [Fact]
    public void Cnpj_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnpj.TryParse("12.ABC.345/01DE-35", out _)));
    }

    [Fact]
    public void Cnpj_TryFormat_Does_Not_Allocate()
    {
        Cnpj cnpj = Cnpj.Parse("12ABC34501DE35");
        char[] buffer = new char[18];
        Assert.Equal(0, MeasureAllocations(() => cnpj.TryFormat(buffer, out _, "G", null)));
    }

    // ---- CEP ---------------------------------------------------------------

    [Fact]
    public void Cep_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cep.IsValid("01310-100")));
    }

    [Fact]
    public void Cep_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cep.IsValid("01310-10a")));
    }

    [Fact]
    public void Cep_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cep.TryParse("01310-100", out _)));
    }

    [Fact]
    public void Cep_TryFormat_Does_Not_Allocate()
    {
        Cep cep = Cep.Parse("01310100");
        char[] buffer = new char[9];
        Assert.Equal(0, MeasureAllocations(() => cep.TryFormat(buffer, out _, "G", null)));
    }

    [Fact]
    public void Cep_Uf_Does_Not_Allocate()
    {
        Cep cep = Cep.Parse("01310100");
        Assert.Equal(0, MeasureAllocations(() => cep.Uf is not null));
    }

    // ---- PIS ---------------------------------------------------------------

    [Fact]
    public void Pis_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Pis.IsValid("120.70458.82-4")));
    }

    [Fact]
    public void Pis_IsValid_Raw_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Pis.IsValid("12070458824")));
    }

    [Fact]
    public void Pis_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Pis.IsValid("120.70458.82-5")));
    }

    [Fact]
    public void Pis_IsValid_AllSame_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Pis.IsValid("111.11111.11-1")));
    }

    [Fact]
    public void Pis_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Pis.TryParse("120.70458.82-4", out _)));
    }

    [Fact]
    public void Pis_TryFormat_Does_Not_Allocate()
    {
        Pis pis = Pis.Parse("12070458824");
        char[] buffer = new char[14];
        Assert.Equal(0, MeasureAllocations(() => pis.TryFormat(buffer, out _, "G", null)));
    }

    // ---- Título de Eleitor ------------------------------------------------

    [Fact]
    public void TituloEleitor_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TituloEleitor.IsValid("1023 8501 06 71")));
    }

    [Fact]
    public void TituloEleitor_IsValid_Raw_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TituloEleitor.IsValid("102385010671")));
    }

    [Fact]
    public void TituloEleitor_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TituloEleitor.IsValid("1234 5678 00 91")));
    }

    [Fact]
    public void TituloEleitor_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TituloEleitor.TryParse("1234 5678 20 97", out _)));
    }

    [Fact]
    public void TituloEleitor_TryFormat_Does_Not_Allocate()
    {
        TituloEleitor titulo = TituloEleitor.Parse("123456782097");
        char[] buffer = new char[15];
        Assert.Equal(0, MeasureAllocations(() => titulo.TryFormat(buffer, out _, "G", null)));
    }

    [Fact]
    public void TituloEleitor_UfCode_Does_Not_Allocate()
    {
        TituloEleitor titulo = TituloEleitor.Parse("123456782097");
        Assert.Equal(0, MeasureAllocations(() => titulo.UfCode is >= 1 and <= 28));
    }

    // ---- CNH ---------------------------------------------------------------

    [Fact]
    public void Cnh_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnh.IsValid("026503064-61")));
    }

    [Fact]
    public void Cnh_IsValid_Raw_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnh.IsValid("02650306461")));
    }

    [Fact]
    public void Cnh_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnh.IsValid("026503064-60")));
    }

    [Fact]
    public void Cnh_IsValid_AllSame_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnh.IsValid("111111111-11")));
    }

    [Fact]
    public void Cnh_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Cnh.TryParse("026503064-61", out _)));
    }

    [Fact]
    public void Cnh_TryFormat_Does_Not_Allocate()
    {
        Cnh cnh = Cnh.Parse("02650306461");
        char[] buffer = new char[12];
        Assert.Equal(0, MeasureAllocations(() => cnh.TryFormat(buffer, out _, "G", null)));
    }

    // ---- RENAVAM -----------------------------------------------------------

    [Fact]
    public void Renavam_IsValid_Formatted_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Renavam.IsValid("0063988.4962")));
    }

    [Fact]
    public void Renavam_IsValid_Legacy_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Renavam.IsValid("639884962")));
    }

    [Fact]
    public void Renavam_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Renavam.IsValid("12345678901")));
    }

    [Fact]
    public void Renavam_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Renavam.TryParse("639884962", out _)));
    }

    [Fact]
    public void Renavam_TryFormat_Does_Not_Allocate()
    {
        Renavam renavam = Renavam.Parse("639884962");
        char[] buffer = new char[11];
        Assert.Equal(0, MeasureAllocations(() => renavam.TryFormat(buffer, out _, "G", null)));
    }

    // ---- Placa -------------------------------------------------------------

    [Fact]
    public void Placa_IsValid_Legacy_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Placa.IsValid("ABC-1234")));
    }

    [Fact]
    public void Placa_IsValid_Mercosul_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Placa.IsValid("abc1d23")));
    }

    [Fact]
    public void Placa_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Placa.IsValid("ABC12D3")));
    }

    [Fact]
    public void Placa_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Placa.TryParse("ABC-1234", out _)));
    }

    [Fact]
    public void Placa_TryFormat_Does_Not_Allocate()
    {
        Placa placa = Placa.Parse("ABC1234");
        char[] buffer = new char[8];
        Assert.Equal(0, MeasureAllocations(() => placa.TryFormat(buffer, out _, "G", null)));
    }

    [Fact]
    public void Placa_ToMercosul_Does_Not_Allocate()
    {
        Placa legacy = Placa.Parse("ABC1234");
        Placa mercosul = Placa.Parse("ABC1D23");
        Assert.Equal(0, MeasureAllocations(() => legacy.ToMercosul().IsMercosul));
        Assert.Equal(0, MeasureAllocations(() => mercosul.ToMercosul() == mercosul));
    }

    [Fact]
    public void Placa_IsMercosul_Does_Not_Allocate()
    {
        Placa mercosul = Placa.Parse("ABC1D23");
        Assert.Equal(0, MeasureAllocations(() => mercosul.IsMercosul));
    }

    // ---- Telefone BR --------------------------------------------------------

    [Fact]
    public void TelefoneBr_IsValid_Display_Form_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.IsValid("(11) 91234-5678")));
    }

    [Fact]
    public void TelefoneBr_IsValid_E164_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.IsValid("+5511912345678")));
    }

    [Fact]
    public void TelefoneBr_IsValid_Bare_Country_Code_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.IsValid("5511987654321")));
    }

    [Fact]
    public void TelefoneBr_IsValid_Fixed_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.IsValid("(11) 3123-4567")));
    }

    [Fact]
    public void TelefoneBr_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.IsValid("11712345678")));
    }

    [Fact]
    public void TelefoneBr_TryParse_E164_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => TelefoneBr.TryParse("+55 11 91234-5678", out _)));
    }

    [Fact]
    public void TelefoneBr_TryFormat_Does_Not_Allocate()
    {
        TelefoneBr phone = TelefoneBr.Parse("11912345678");
        char[] buffer = new char[15];
        Assert.Equal(0, MeasureAllocations(() => phone.TryFormat(buffer, out _, "G", null)));
    }

    [Fact]
    public void TelefoneBr_Ddd_And_IsMobile_Do_Not_Allocate()
    {
        TelefoneBr phone = TelefoneBr.Parse("11912345678");
        Assert.Equal(0, MeasureAllocations(() => phone.Ddd == 11));
        Assert.Equal(0, MeasureAllocations(() => phone.IsMobile));
    }

    // ---- Boleto -------------------------------------------------------------

    [Fact]
    public void Boleto_IsValid_Barcode_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Boleto.IsValid("10499898100000214032006561000100040099726390")));
    }

    [Fact]
    public void Boleto_IsValid_Formatted_Linha_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Boleto.IsValid("10492.00650 61000.100042 00997.263900 9 89810000021403")));
    }

    [Fact]
    public void Boleto_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Boleto.IsValid("10498898100000214032006561000100040099726390")));
    }

    [Fact]
    public void Boleto_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Boleto.TryParse("10492.00650 61000.100042 00997.263900 9 89810000021403", out _)));
    }

    [Fact]
    public void Boleto_TryFormat_Does_Not_Allocate()
    {
        Boleto boleto = Boleto.Parse("10499898100000214032006561000100040099726390");
        char[] buffer = new char[54];
        Assert.Equal(0, MeasureAllocations(() => boleto.TryFormat(buffer, out _, "G", null)));
        Assert.Equal(0, MeasureAllocations(() => boleto.TryFormat(buffer, out _, "N", null)));
        Assert.Equal(0, MeasureAllocations(() => boleto.TryFormat(buffer, out _, "B", null)));
    }

    [Fact]
    public void Boleto_Properties_Do_Not_Allocate()
    {
        Boleto boleto = Boleto.Parse("10499898100000214032006561000100040099726390");
        Assert.Equal(0, MeasureAllocations(() => boleto.BankCode == 104));
        Assert.Equal(0, MeasureAllocations(() => boleto.CurrencyCode == 9));
        Assert.Equal(0, MeasureAllocations(() => boleto.DueDateFactor == 8981));
        Assert.Equal(0, MeasureAllocations(() => boleto.Amount == 214.03m));
        Assert.Equal(0, MeasureAllocations(() => boleto.DueDate is not null));
    }

    [Fact]
    public void Boleto_Fator_Encode_And_Decode_Do_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Boleto.Encode(new DateOnly(2030, 1, 1)) == 1000 + 1774));
        Assert.Equal(0, MeasureAllocations(() => Boleto.TryDecode(1000, new DateOnly(2026, 1, 1), out _)));
    }

    // ---- Arrecadação ---------------------------------------------------------

    [Fact]
    public void Arrecadacao_IsValid_Barcode_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Arrecadacao.IsValid("83860000005096000190000008017823000034306271")));
    }

    [Fact]
    public void Arrecadacao_IsValid_Formatted_Linha_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Arrecadacao.IsValid("83860000005-0 09600019000-9 00080178230-9 00034306271-2")));
    }

    [Fact]
    public void Arrecadacao_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Arrecadacao.IsValid("83870000005096000190000008017823000034306271")));
    }

    [Fact]
    public void Arrecadacao_TryParse_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => Arrecadacao.TryParse("83860000005-0 09600019000-9 00080178230-9 00034306271-2", out _)));
    }

    [Fact]
    public void Arrecadacao_TryFormat_Does_Not_Allocate()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse("83860000005096000190000008017823000034306271");
        char[] buffer = new char[55];
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.TryFormat(buffer, out _, "G", null)));
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.TryFormat(buffer, out _, "N", null)));
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.TryFormat(buffer, out _, "B", null)));
    }

    [Fact]
    public void Arrecadacao_Properties_Do_Not_Allocate()
    {
        Arrecadacao arrecadacao = Arrecadacao.Parse("83860000005096000190000008017823000034306271");
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.Segment == 3));
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.IsEffectiveValue));
        Assert.Equal(0, MeasureAllocations(() => arrecadacao.Value == 509.60m));
    }

    // ---- Pix ------------------------------------------------------------------

    // The BCB static vector (Manual de Padrões para Iniciação do Pix, section 2.6.3).
    private const string PixStaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    // The BCB dynamic vector (section 2.7.2).
    private const string PixDynamicVector =
        "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***630464E4";

    [Fact]
    public void PixPayload_IsValid_Static_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => PixPayload.IsValid(PixStaticVector)));
    }

    [Fact]
    public void PixPayload_IsValid_Dynamic_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => PixPayload.IsValid(PixDynamicVector)));
    }

    [Fact]
    public void PixPayload_IsValid_Composite_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => PixPayload.IsValid(
            "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865406100.505802BR5913Fulano de Tal6008BRASILIA62070503***80740014br.gov.bcb.pix2552pix.example.com/rec/2353c790eefb11eaadc10242ac12000263042875")));
    }

    [Fact]
    public void PixPayload_IsValid_Invalid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => PixPayload.IsValid(PixStaticVector.AsSpan()[..^1])));
    }

    [Fact]
    public void PixKey_IsValid_Does_Not_Allocate()
    {
        Assert.Equal(0, MeasureAllocations(() => PixKey.IsValid("123e4567-e12b-12d1-a456-426655440000")));
        Assert.Equal(0, MeasureAllocations(() => PixKey.IsValid("111.444.777-35")));
        Assert.Equal(0, MeasureAllocations(() => PixKey.IsValid("fulano_da_silva.recebedor@example.com")));
        Assert.Equal(0, MeasureAllocations(() => PixKey.IsValid("+5511912345678")));
        Assert.Equal(0, MeasureAllocations(() => PixKey.IsValid("garbage key")));
    }

    [Fact]
    public void PixKey_TryNormalize_Canonical_Does_Not_Allocate()
    {
        char[] buffer = new char[PixKey.MaxLength];
        Assert.Equal(0, MeasureAllocations(() => PixKey.TryNormalize("123E4567-E12B-12D1-A456-426655440000", buffer, out _)));
    }

    [Fact]
    public void PixTlv_TryParseSequence_Invalid_Does_Not_Allocate()
    {
        // The all-allocating object tree is only materialized when the stream is valid.
        Assert.Equal(0, MeasureAllocations(() => PixTlv.TryParseSequence("not a tlv stream", out _)));
    }
}
