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
}
