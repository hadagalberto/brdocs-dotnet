using BenchmarkDotNet.Attributes;
using BrDocs;

namespace BrDocs.Benchmarks;

/// <summary>
/// [MemoryDiagnoser] benchmarks for every public <c>Parse</c> entry point of the core library.
/// <para>
/// <see cref="DocumentsBenchmarks"/> and <see cref="PixBenchmarks"/> cover every <c>IsValid</c>
/// and <c>TryParse</c> overload (the allocation-free paths). This class completes the SPEC
/// requirement with the allocating, throwing <c>Parse(string)</c> paths and the TLV parsers,
/// so the cost of each parse API is measured and visible in the same report.
/// </para>
/// Run with <c>dotnet run -c Release --project benchmarks/BrDocs.Benchmarks</c>.
/// </summary>
[MemoryDiagnoser]
public class ParseBenchmarks
{
    private const string PixStaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    private const string BoletoBarcode = "10499898100000214032006561000100040099726390";
    private const string ArrecadacaoBarcode = "83860000005096000190000008017823000034306271";

    // ---- Value objects ------------------------------------------------------

    [Benchmark] public Cpf CpfParse() => Cpf.Parse("111.444.777-35");
    [Benchmark] public Cnpj CnpjParseNumeric() => Cnpj.Parse("11.222.333/0001-81");
    [Benchmark] public Cnpj CnpjParseAlphanumeric() => Cnpj.Parse("12.ABC.345/01DE-35");
    [Benchmark] public Cep CepParse() => Cep.Parse("01310-100");
    [Benchmark] public Pis PisParse() => Pis.Parse("120.70458.82-4");
    [Benchmark] public TituloEleitor TituloEleitorParse() => TituloEleitor.Parse("1234 5678 20 97");
    [Benchmark] public Cnh CnhParse() => Cnh.Parse("026503064-61");
    [Benchmark] public Renavam RenavamParse() => Renavam.Parse("639884962");
    [Benchmark] public Placa PlacaParseLegacy() => Placa.Parse("ABC-1234");
    [Benchmark] public Placa PlacaParseMercosul() => Placa.Parse("ABC1D23");
    [Benchmark] public TelefoneBr TelefoneBrParseE164() => TelefoneBr.Parse("+5511912345678");
    [Benchmark] public Boleto BoletoParseBarcode() => Boleto.Parse(BoletoBarcode);
    [Benchmark] public Arrecadacao ArrecadacaoParseBarcode() => Arrecadacao.Parse(ArrecadacaoBarcode);

    // ---- Pix ----------------------------------------------------------------

    [Benchmark] public PixPayload PixPayloadParse() => PixPayload.Parse(PixStaticVector);
    [Benchmark] public PixTlv PixTlvParse() => PixTlv.Parse("000201");
    [Benchmark] public IReadOnlyList<PixTlv> PixTlvParseSequence() => PixTlv.ParseSequence(PixStaticVector);
    [Benchmark] public bool PixTlvTryParseSequence() => PixTlv.TryParseSequence(PixStaticVector, out _);
}
