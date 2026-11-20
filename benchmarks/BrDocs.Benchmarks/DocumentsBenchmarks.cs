using System.Globalization;
using BenchmarkDotNet.Attributes;
using BrDocs;

namespace BrDocs.Benchmarks;

/// <summary>
/// Benchmarks for the BrDocs value objects. Run with
/// <c>dotnet run -c Release --project benchmarks/BrDocs.Benchmarks</c>.
/// The [MemoryDiagnoser] results are the evidence for the SPEC requirement that validation
/// paths (IsValid/TryParse/TryFormat) do not allocate.
/// </summary>
[MemoryDiagnoser]
public class DocumentsBenchmarks
{
    // Instance fields on purpose: benchmark methods must read instance state.
    private readonly Cpf _cpf = Cpf.Parse("11144477735");
    private readonly Cnpj _numericCnpj = Cnpj.Parse("11222333000181");
    private readonly Cnpj _alphanumericCnpj = Cnpj.Parse("12ABC34501DE35");
    private readonly Cep _cep = Cep.Parse("01310100");

    private readonly string _validCpfFormatted = "111.444.777-35";
    private readonly string _validCpfRaw = "11144477735";
    private readonly string _invalidCpfFormatted = "111.444.777-36";
    private readonly string _validCnpjNumericFormatted = "11.222.333/0001-81";
    private readonly string _validCnpjAlphanumericFormatted = "12.ABC.345/01DE-35";
    private readonly string _invalidCnpjAlphanumericFormatted = "12.ABC.345/01DE-36";
    private readonly string _validCepFormatted = "01310-100";
    private readonly string _validCepRaw = "01310100";

    private readonly char[] _buffer = new char[18];

    // ---- CPF ----

    [Benchmark] public bool CpfIsValidFormatted() => Cpf.IsValid(_validCpfFormatted);
    [Benchmark] public bool CpfIsValidRaw() => Cpf.IsValid(_validCpfRaw);
    [Benchmark] public bool CpfIsValidInvalid() => Cpf.IsValid(_invalidCpfFormatted);
    [Benchmark] public bool CpfTryParseFormatted() => Cpf.TryParse(_validCpfFormatted, out _);
    [Benchmark] public string CpfToStringFormatted() => _cpf.ToString();
    [Benchmark] public string CpfToStringDigits() => _cpf.ToString("N", CultureInfo.InvariantCulture);
    [Benchmark] public bool CpfTryFormat() => _cpf.TryFormat(_buffer, out _, "G", null);
    [Benchmark] public Cpf CpfGenerate() => Cpf.Generate();

    // ---- CNPJ ----

    [Benchmark] public bool CnpjIsValidNumericFormatted() => Cnpj.IsValid(_validCnpjNumericFormatted);
    [Benchmark] public bool CnpjIsValidAlphanumericFormatted() => Cnpj.IsValid(_validCnpjAlphanumericFormatted);
    [Benchmark] public bool CnpjIsValidAlphanumericInvalid() => Cnpj.IsValid(_invalidCnpjAlphanumericFormatted);
    [Benchmark] public bool CnpjTryParseAlphanumeric() => Cnpj.TryParse(_validCnpjAlphanumericFormatted, out _);
    [Benchmark] public string CnpjRoot() => _alphanumericCnpj.Root;
    [Benchmark] public bool CnpjIsHeadOfficeNumeric() => _numericCnpj.IsHeadOffice;
    [Benchmark] public bool CnpjIsAlphanumeric() => _alphanumericCnpj.IsAlphanumeric;
    [Benchmark] public Cnpj CnpjGenerateNumeric() => Cnpj.Generate();
    [Benchmark] public Cnpj CnpjGenerateAlphanumeric() => Cnpj.GenerateAlphanumeric();

    // ---- CEP ----

    [Benchmark] public bool CepIsValidFormatted() => Cep.IsValid(_validCepFormatted);
    [Benchmark] public bool CepIsValidRaw() => Cep.IsValid(_validCepRaw);
    [Benchmark] public bool CepTryParseFormatted() => Cep.TryParse(_validCepFormatted, out _);
    [Benchmark] public string? CepUf() => _cep.Uf;
    [Benchmark] public Cep CepGenerate() => Cep.Generate();
}
