using BenchmarkDotNet.Attributes;
using BrDocs;

namespace BrDocs.Benchmarks;

/// <summary>
/// Benchmarks for the Pix BR Code: validation, parsing, encoding and building. Run with
/// <c>dotnet run -c Release --project benchmarks/BrDocs.Benchmarks</c>. The [MemoryDiagnoser]
/// results are the evidence that the validation path allocates nothing.
/// </summary>
[MemoryDiagnoser]
public class PixBenchmarks
{
    private const string StaticVector =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    private const string DynamicVector =
        "00020101021226700014br.gov.bcb.pix2548pix.example.com/8b3da2f39a4140d1a91abd93113bd4415204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***630464E4";

    private readonly PixPayload _staticPayload = PixPayload.Parse(StaticVector);
    private readonly PixPayloadBuilder _builder = PixPayload.CreateStatic(
        "123e4567-e12b-12d1-a456-426655440000", "Fulano de Tal", "BRASILIA").WithAmount(100.50m);

    private readonly string _invalidPayload = StaticVector[..^1];

    private readonly char[] _buffer = new char[StaticVector.Length];

    // ---- Validation (allocation-free by design) ----

    [Benchmark] public bool PixPayloadIsValidStatic() => PixPayload.IsValid(StaticVector);
    [Benchmark] public bool PixPayloadIsValidDynamic() => PixPayload.IsValid(DynamicVector);
    [Benchmark] public bool PixPayloadIsValidInvalid() => PixPayload.IsValid(_invalidPayload);
    [Benchmark] public bool PixKeyIsValidEvp() => PixKey.IsValid("123e4567-e12b-12d1-a456-426655440000");
    [Benchmark] public bool PixKeyIsValidFormattedCnpj() => PixKey.IsValid("12.ABC.345/01DE-35");

    // ---- Parse, encode and build (allocate the object tree / the payload) ----

    [Benchmark] public bool PixPayloadTryParse() => PixPayload.TryParse(StaticVector, out _);
    [Benchmark] public string PixPayloadToString() => _staticPayload.ToString();
    [Benchmark] public bool PixPayloadTryEncode() => _staticPayload.TryEncode(_buffer, out _);
    [Benchmark] public PixPayload PixPayloadBuildStatic() => _builder.Build();
}
