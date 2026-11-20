# BrDocs

[![CI](https://github.com/hadagalberto/brdocs-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/hadagalberto/brdocs-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BrDocs.svg)](https://www.nuget.org/packages/BrDocs)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-5127D8.svg)](https://dotnet.microsoft.com/)

Production-grade .NET library for Brazilian documents and payment codes. Every public API is a
value object with `IsValid`, `TryParse`, `Parse`, formatted `ToString()`, digits-only `ToString("N")`,
`ISpanFormattable` and `ISpanParsable<T>` support. Validation is span-based and allocation-free,
culture-invariant, AOT- and trimming-compatible, and the core package has zero runtime dependencies.

Português: [README.pt-BR.md](README.pt-BR.md).

## Install

```shell
dotnet add package BrDocs                  # core: documents, boletos, Pix
dotnet add package BrDocs.DataAnnotations  # [Cpf], [Cnpj], [Cep], ...
dotnet add package BrDocs.FluentValidation # MustBeCpf(), MustBeCnpj(), ...
dotnet add package BrDocs.Json             # System.Text.Json converters
dotnet add package BrDocs.QrCode           # ISO/IEC 18004 QR Code + Pix SVG
```

Targets `net8.0` and `net10.0`.

## Supported documents

| Document | Type | Format | Check digits | `Generate()` |
|---|---|---|---|---|
| CPF | `Cpf` | `529.982.247-25` | mod 11 × 2 | `Generate()` |
| CNPJ (numeric) | `Cnpj` | `11.222.333/0001-81` | mod 11 × 2 | `Generate()` |
| CNPJ (alphanumeric) | `Cnpj` | `12.ABC.345/01DE-35` | mod 11 × 2 | `GenerateAlphanumeric()` |
| CEP | `Cep` | `01310-100` | none (structural) | `Generate()` |
| PIS/PASEP/NIT | `Pis` | `120.70458.82-4` | mod 11 | `Generate()` |
| Título de Eleitor | `TituloEleitor` | `1234 5678 20 97` | mod 11 × 2 | `Generate()` |
| CNH | `Cnh` | `026503064-61` | mod 11 × 2 | `Generate()` |
| RENAVAM | `Renavam` | `00639884962` | mod 11 | `Generate()` |
| Placa | `Placa` | `ABC-1234` / `ABC1D23` | none (structural) | `Generate()`, `GenerateLegacy()` |
| Telefone BR | `TelefoneBr` | `(11) 91234-5678` / `+5511912345678` | none (DDD list) | `Generate()`, `GenerateFixed()` |
| Boleto (FEBRABAN) | `Boleto` | 44-digit barcode / 47-digit linha digitável | mod 10 × 3 + mod 11 | `Generate()` |
| Arrecadação / convênio | `Arrecadacao` | 44-digit barcode / 48-digit linha | mod 10 or 11 (chosen by the 3rd digit) for the general DV and all 4 field DVs | `Generate()` |
| Pix BR Code | `PixPayload` | EMV® QRCPS-MPM TLV string | CRC16/CCITT-FALSE | builder |

All parsers ignore `.`, `-`, `/` and spaces (plus parentheses and a leading `+` for phones, and
case normalization for letters). Any other character is rejected. The `default` value of every
value object is invalid unless noted (a plate's all-zero default is structurally valid).

## Quick start: documents

```csharp
using BrDocs;

bool ok = Cpf.IsValid("529.982.247-25");          // true
bool bad = Cpf.IsValid("111.111.111-11");         // false (reserved sequence)
Cpf cpf = Cpf.Parse("52998224725");
Console.WriteLine(cpf);                           // 529.982.247-25
Console.WriteLine(cpf.ToString("N"));             // 52998224725
Console.WriteLine(cpf.FiscalRegion);              // EsRj
Console.WriteLine(cpf.FiscalRegion.Ufs());        // ES, RJ
Console.WriteLine(Cpf.Generate());                // a new valid CPF
```

`IsValid(ReadOnlySpan<char>)`, `TryParse(ReadOnlySpan<char>, out T)` and `TryFormat(...)` allocate
nothing: they normalize into `stackalloc` buffers. `Parse` throws `FormatException` with a message
that includes the offending value.

### CNPJ, including the alphanumeric format

Since IN RFB nº 2.229/2024 (issued from July 2026) the CNPJ base may contain uppercase letters A–Z.
The base is 8 root characters + 4 branch characters and the check digits use each character's code
point minus 48 (`'A'` → 17), with weights `5,4,3,2,9,8,7,6,5,4,3,2` and
`6,5,4,3,2,9,8,7,6,5,4,3,2`. Lowercase input is normalized and punctuation is ignored.

```csharp
using BrDocs;

bool numeric = Cnpj.IsValid("11.222.333/0001-81");     // true
Cnpj alpha = Cnpj.Parse("12.ABC.345/01DE-35");          // official RFB vector
Console.WriteLine(alpha.IsAlphanumeric);                // True
Console.WriteLine(alpha.Root);                          // 12ABC345
Console.WriteLine(alpha.Branch);                        // 01DE
Console.WriteLine(alpha.IsHeadOffice);                  // False
Console.WriteLine(Cnpj.IsValid("12abc34501de35"));     // true (lowercase accepted)
Console.WriteLine(Cnpj.GenerateAlphanumeric().IsAlphanumeric); // True
```

### CEP, PIS, Título de Eleitor, CNH and RENAVAM

```csharp
using BrDocs;

Console.WriteLine(Cep.Parse("01310-100").Uf);                     // SP
Console.WriteLine(Pis.Parse("120.70458.82-4"));                   // 120.70458.82-4
TituloEleitor titulo = TituloEleitor.Parse("1234 5678 20 97");
Console.WriteLine(titulo.Uf);                                     // DF (UF code 20)
Console.WriteLine(titulo.UfCode);                                 // 20
Console.WriteLine(Cnh.Parse("026503064-61"));                     // 026503064-61
Console.WriteLine(Renavam.Parse("639884962"));                    // 00639884962 (legacy is padded)
```

### Plates and phones

```csharp
using BrDocs;

Placa legacy = Placa.Parse("ABC-1234");
Placa mercosul = legacy.ToMercosul();
Console.WriteLine(mercosul);                           // ABC1C34
Console.WriteLine(mercosul.IsMercosul);                // True

TelefoneBr phone = TelefoneBr.Parse("(11) 91234-5678");
Console.WriteLine(phone.Ddd);                          // 11
Console.WriteLine(phone.IsMobile);                     // True
Console.WriteLine(phone.ToE164());                     // +5511912345678
Console.WriteLine(TelefoneBr.IsValid("11712345678"));  // False (invalid subscriber prefix)
```

## Boleto (FEBRABAN)

Parse either a 44-digit código de barras or a 47-digit linha digitável and convert both ways.
Field DVs use mod 10; the general DV uses mod 11 with `0`, `10` and `11` mapped to `1`.
Arrecadação/convênio codes always have a 44-digit código de barras (the 48-digit linha starts
with `8`). The general DV and all four field DVs use mod 10 when the third digit is `6` or `7`,
and mod 11 when it is `8` or `9`; the effective value is exposed when that digit is `6` or `8`.

```csharp
using BrDocs;

Boleto boleto = Boleto.Parse("10492.00650 61000.100042 00997.263900 9 89810000021403");
Console.WriteLine(boleto.BankCode);        // 104 (Itaú)
Console.WriteLine(boleto.CurrencyCode);    // 9 (real)
Console.WriteLine(boleto.Amount);          // 214.03
Console.WriteLine(boleto.Barcode);         // 10499898100000214032006561000100040099726390
Console.WriteLine(Boleto.Parse(boleto.Barcode) == boleto); // True

Boleto created = Boleto.Create(104, 9, new DateOnly(2026, 6, 15), 123.45m, new string('1', 25));
Console.WriteLine(Boleto.IsValid(created.LinhaDigitavel)); // True

Arrecadacao conta = Arrecadacao.Parse("83860000005-0 09600019000-9 00080178230-9 00034306271-2");
Console.WriteLine(conta.Segment);          // 3
Console.WriteLine(conta.Value);            // 509.60
Console.WriteLine(conta.IsEffectiveValue); // True
```

### The due-date factor reset

Boletos encode the due date as a 4-digit factor counted from 1997-10-07. Factor `9999` is
**2025-02-21**; the next day the cycle restarts at `1000` (**2025-02-22**), so a factor alone is
ambiguous. `Boleto.Encode(DateOnly)` writes the factor and `Boleto.Decode(factor, reference)` picks
the cycle whose date is nearest the reference within the `[-3000, +5500]` day window. `0000` means
"no due date". `Boleto.DueDate` uses today as the reference; `GetDueDate(reference)` resolves
against an explicit date, and `Generate(seed, reference)`/`Generate(rng, reference)` make
generated due dates deterministic.

```csharp
using BrDocs;

Console.WriteLine(Boleto.Decode(9999, new DateOnly(2025, 2, 21))); // 2025-02-21
Console.WriteLine(Boleto.Decode(1000, new DateOnly(2025, 2, 22))); // 2025-02-22
Console.WriteLine(Boleto.Decode(1001, new DateOnly(2025, 2, 23))); // 2025-02-23
Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 21)));       // 9999
Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 22)));       // 1000
```

## Pix BR Code

Static codes carry the receiver's key; dynamic codes carry a charge URL (and must not carry an
amount or txid). Field 26 holds a nested merchant-account template, 62 the additional data, 63 the
CRC-16/CCITT-FALSE computed over the whole payload including `6304`.

```csharp
using BrDocs;

PixPayload payload = PixPayload
    .CreateStatic("123e4567-e12b-12d1-a456-426655440000", "Fulano de Tal", "BRASILIA")
    .WithAmount(100.50m)
    .WithTxid("PEDIDO123")
    .Build();

string brCode = payload.ToString();
Console.WriteLine(PixPayload.IsValid(brCode));            // True
PixPayload parsed = PixPayload.Parse(brCode);
Console.WriteLine(parsed.Key);                            // 123e4567-e12b-12d1-a456-426655440000
Console.WriteLine(parsed.Amount);                         // 100.50
Console.WriteLine(parsed.Crc);                            // four uppercase hex digits
Console.WriteLine(parsed.IsStatic);                       // True

// The official BCB example also parses and validates.
PixPayload official = PixPayload.Parse(
    "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D");
Console.WriteLine(official.Crc);                          // 1D3D
```

```csharp
using BrDocs;

PixPayload dynamic = PixPayload
    .CreateDynamic("pix.example.com/8b3da2f39a4140d1a91abd93113bd441", "Fulano de Tal", "BRASILIA")
    .SingleUse()
    .Build();
Console.WriteLine(dynamic.IsDynamic);   // True
Console.WriteLine(dynamic.IsSingleUse); // True
Console.WriteLine(PixPayload.IsValid(dynamic.ToString())); // True
```

Pix keys are detected as CPF, CNPJ (including alphanumeric), e-mail, `+55` phone or EVP (UUID v4):

```csharp
using BrDocs;

Console.WriteLine(PixKey.Detect("123e4567-e12b-12d1-a456-426655440000")); // Evp
Console.WriteLine(PixKey.Detect("12ABC34501DE35"));                      // Cnpj
Console.WriteLine(PixKey.IsValid("+5511912345678"));                     // True
Console.WriteLine(PixKey.IsValid("garbage key"));                        // False
```

A bare 11-digit string that is not a valid CPF is ambiguous with a local mobile number and is
rejected by `Normalize`/`CreateStatic` (just as `Detect` rejects it); make the phone intent
explicit with `+55`, local formatting or a country-code prefix.

### QR Code rendering

The core package has no QR dependency. `BrDocs.QrCode` is a complete ISO/IEC 18004 encoder
(byte/alphanumeric modes, versions 1–40, ECC L/M/Q/H, all eight masks, SVG and PNG output).

```csharp
using BrDocs;
using BrDocs.QrCode;

PixPayload pix = PixPayload.Parse(
    "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D");
string svg = pix.ToQrSvg();                 // standalone SVG document
QrCode qr = QrCode.Encode("https://example.com");
byte[] png = qr.ToPng();                    // PNG bytes
Console.WriteLine(svg.StartsWith("<svg", StringComparison.Ordinal)); // True
Console.WriteLine(png.Length > 0);          // True
```

## Validation integrations

### DataAnnotations

Attributes ship with pt-BR messages and overridable resources. All of them accept `null` as valid
and empty text as invalid; `[Cnpj(AllowAlphanumeric = true)]` opts into the new format and
`[TelefoneBr(MobileOnly = true)]` rejects fixed lines.

```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BrDocs.DataAnnotations;

public sealed class Customer
{
    [Cpf]
    public string? Cpf { get; set; }

    [Cnpj(AllowAlphanumeric = true)]
    public string? Cnpj { get; set; }

    [TelefoneBr(MobileOnly = true)]
    public string? Phone { get; set; }
}

var customer = new Customer
{
    Cpf = "111.444.777-35",
    Cnpj = "12.ABC.345/01DE-35",
    Phone = "(11) 91234-5678",
};
var results = new List<ValidationResult>();
bool valid = Validator.TryValidateObject(customer, new ValidationContext(customer), results, true);
Console.WriteLine(valid); // True
```

Available attributes: `[Cpf]`, `[Cnpj]`, `[CpfOrCnpj]`, `[Cep]`, `[Pis]`, `[TituloEleitor]`,
`[Cnh]`, `[Renavam]`, `[Placa]`, `[TelefoneBr]`, `[PixKey]` and `[LinhaDigitavel]`.

### FluentValidation

```csharp
using BrDocs.FluentValidation;
using FluentValidation;

public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Cpf).MustBeCpf();
        RuleFor(x => x.Cnpj).MustBeCnpj(allowAlphanumeric: true);
        RuleFor(x => x.Phone).MustBeTelefoneBr(mobileOnly: true);
    }
}

bool valid = new CustomerValidator().Validate(customer).IsValid; // customer from above
Console.WriteLine(valid); // True
```

Other extensions: `MustBeCpfOrCnpj`, `MustBeCep`, `MustBePis`, `MustBeTituloEleitor`, `MustBeCnh`,
`MustBeRenavam`, `MustBePlaca`, `MustBePixKey` and `MustBeLinhaDigitavel`.

### System.Text.Json

Converters normalize/format on serialization and accept both styles when deserializing. Reading a
bad value throws `JsonException`. Source-generated contexts work with `[JsonConverter(typeof(...))]`.

```csharp
using System.Text.Json;
using BrDocs;
using BrDocs.Json;

JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();
Cpf cpf = Cpf.Parse("111.444.777-35");
string json = JsonSerializer.Serialize(cpf, options);                  // "111.444.777-35"
Cpf back = JsonSerializer.Deserialize<Cpf>(json, options)!;            // same value
JsonSerializerOptions digits = new JsonSerializerOptions()
    .AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);
string digitsJson = JsonSerializer.Serialize(cpf, digits);             // "11144477735"
Console.WriteLine(back == cpf); // True
```

Converters exist for every value object, for `Boleto`, `Arrecadacao` and for `PixPayload`.

## Performance

`benchmarks/BrDocs.Benchmarks` contains BenchmarkDotNet classes with `[MemoryDiagnoser]` for every
`IsValid`, `TryParse`, `TryFormat` and `Parse` API. Verified with unit tests using
`GC.GetAllocatedBytesForCurrentThread`, `IsValid`/`TryParse`/`TryFormat` report `0 B` allocated.

```shell
dotnet run -c Release --project benchmarks/BrDocs.Benchmarks
```

## Sample API

`samples/BrDocs.Samples.Api` is a minimal API using DataAnnotations, FluentValidation, JSON
converters and Pix QR generation. Run it with
`dotnet run --project samples/BrDocs.Samples.Api`.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Changelog: [CHANGELOG.md](CHANGELOG.md).
Spec: [SPEC.md](SPEC.md). Audit: [SPEC-AUDIT.md](SPEC-AUDIT.md).

## License

MIT — Copyright (c) Hadagalberto Junior. See [LICENSE](LICENSE).
