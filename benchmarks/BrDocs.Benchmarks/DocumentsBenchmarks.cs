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
    private readonly Pis _pis = Pis.Parse("12070458824");
    private readonly TituloEleitor _titulo = TituloEleitor.Parse("123456782097");
    private readonly Cnh _cnh = Cnh.Parse("02650306461");
    private readonly Renavam _renavam = Renavam.Parse("639884962");
    private readonly Placa _legacyPlaca = Placa.Parse("ABC1234");
    private readonly Placa _mercosulPlaca = Placa.Parse("ABC1D23");
    private readonly TelefoneBr _mobile = TelefoneBr.Parse("11912345678");
    private readonly TelefoneBr _fixedLine = TelefoneBr.Parse("1131234567");

    private readonly string _validCpfFormatted = "111.444.777-35";
    private readonly string _validCpfRaw = "11144477735";
    private readonly string _invalidCpfFormatted = "111.444.777-36";
    private readonly string _validCnpjNumericFormatted = "11.222.333/0001-81";
    private readonly string _validCnpjAlphanumericFormatted = "12.ABC.345/01DE-35";
    private readonly string _invalidCnpjAlphanumericFormatted = "12.ABC.345/01DE-36";
    private readonly string _validCepFormatted = "01310-100";
    private readonly string _validCepRaw = "01310100";
    private readonly string _validPisFormatted = "120.70458.82-4";
    private readonly string _invalidPisFormatted = "120.70458.82-5";
    private readonly string _validTituloFormatted = "1234 5678 20 97";
    private readonly string _validCnhFormatted = "026503064-61";
    private readonly string _invalidCnhFormatted = "026503064-60";
    private readonly string _validRenavamLegacy = "639884962";
    private readonly string _validLegacyPlate = "ABC-1234";
    private readonly string _validMercosulPlateLower = "abc1d23";
    private readonly string _validMobileDisplay = "(11) 91234-5678";
    private readonly string _validMobileE164 = "+5511912345678";
    private readonly string _invalidMobile = "11712345678";
    private readonly string _validFixedDisplay = "(11) 3123-4567";

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

    // ---- PIS ----

    [Benchmark] public bool PisIsValidFormatted() => Pis.IsValid(_validPisFormatted);
    [Benchmark] public bool PisIsValidInvalid() => Pis.IsValid(_invalidPisFormatted);
    [Benchmark] public bool PisTryParseFormatted() => Pis.TryParse(_validPisFormatted, out _);
    [Benchmark] public string PisToStringFormatted() => _pis.ToString();
    [Benchmark] public bool PisTryFormat() => _pis.TryFormat(_buffer, out _, "G", null);
    [Benchmark] public Pis PisGenerate() => Pis.Generate();

    // ---- Título de Eleitor ----

    [Benchmark] public bool TituloEleitorIsValidFormatted() => TituloEleitor.IsValid(_validTituloFormatted);
    [Benchmark] public bool TituloEleitorTryParseFormatted() => TituloEleitor.TryParse(_validTituloFormatted, out _);
    [Benchmark] public string TituloEleitorToStringFormatted() => _titulo.ToString();
    [Benchmark] public int TituloEleitorUfCode() => _titulo.UfCode;
    [Benchmark] public TituloEleitor TituloEleitorGenerate() => TituloEleitor.Generate();

    // ---- CNH ----

    [Benchmark] public bool CnhIsValidFormatted() => Cnh.IsValid(_validCnhFormatted);
    [Benchmark] public bool CnhIsValidInvalid() => Cnh.IsValid(_invalidCnhFormatted);
    [Benchmark] public bool CnhTryParseFormatted() => Cnh.TryParse(_validCnhFormatted, out _);
    [Benchmark] public string CnhToStringFormatted() => _cnh.ToString();
    [Benchmark] public Cnh CnhGenerate() => Cnh.Generate();

    // ---- RENAVAM ----

    [Benchmark] public bool RenavamIsValidLegacy() => Renavam.IsValid(_validRenavamLegacy);
    [Benchmark] public bool RenavamTryParseLegacy() => Renavam.TryParse(_validRenavamLegacy, out _);
    [Benchmark] public string RenavamToString() => _renavam.ToString();
    [Benchmark] public Renavam RenavamGenerate() => Renavam.Generate();

    // ---- Placa ----

    [Benchmark] public bool PlacaIsValidLegacy() => Placa.IsValid(_validLegacyPlate);
    [Benchmark] public bool PlacaIsValidMercosulLower() => Placa.IsValid(_validMercosulPlateLower);
    [Benchmark] public bool PlacaTryParseLegacy() => Placa.TryParse(_validLegacyPlate, out _);
    [Benchmark] public string PlacaLegacyToString() => _legacyPlaca.ToString();
    [Benchmark] public Placa PlacaToMercosul() => _legacyPlaca.ToMercosul();
    [Benchmark] public bool PlacaIsMercosul() => _mercosulPlaca.IsMercosul;
    [Benchmark] public Placa PlacaGenerate() => Placa.Generate();

    // ---- Telefone BR ----

    [Benchmark] public bool TelefoneBrIsValidDisplay() => TelefoneBr.IsValid(_validMobileDisplay);
    [Benchmark] public bool TelefoneBrIsValidFixedDisplay() => TelefoneBr.IsValid(_validFixedDisplay);
    [Benchmark] public bool TelefoneBrIsValidE164() => TelefoneBr.IsValid(_validMobileE164);
    [Benchmark] public bool TelefoneBrIsValidInvalid() => TelefoneBr.IsValid(_invalidMobile);
    [Benchmark] public bool TelefoneBrTryParseE164() => TelefoneBr.TryParse(_validMobileE164, out _);
    [Benchmark] public string TelefoneBrToStringDisplay() => _mobile.ToString();
    [Benchmark] public string TelefoneBrToE164() => _mobile.ToE164();
    [Benchmark] public string TelefoneBrSubscriber() => _fixedLine.Subscriber;
    [Benchmark] public TelefoneBr TelefoneBrGenerate() => TelefoneBr.Generate();
}
