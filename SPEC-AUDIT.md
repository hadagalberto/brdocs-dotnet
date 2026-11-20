# SPEC audit

Full audit of the repository against [SPEC.md](SPEC.md), performed at milestone 7.
Every requirement below is implemented and covered by tests. **No gaps remain.**

## Solution layout

| Requirement | Implementation | Verified |
|---|---|---|
| `BrDocs.sln` | Root solution, 13 projects | `dotnet build BrDocs.sln --configuration Release -warnaserror` → 0 warnings, 0 errors |
| `src/BrDocs` core, no dependencies | Only `Microsoft.SourceLink.GitHub` (`PrivateAssets=all`) | `BrDocs.csproj` |
| `src/BrDocs.DataAnnotations` | 12 validation attributes | `tests/BrDocs.DataAnnotations.Tests` (33 cases/TFM) |
| `src/BrDocs.FluentValidation` | 12 `IRuleBuilder` extensions | `tests/BrDocs.FluentValidation.Tests` (31 cases/TFM) |
| `src/BrDocs.Json` | Converters for all 12 documents + Pix | `tests/BrDocs.Json.Tests` (19 cases/TFM) |
| `tests/BrDocs.Tests`, xUnit + FsCheck | Property-based and vector tests | 1944 cases/TFM |
| `benchmarks/BrDocs.Benchmarks`, BenchmarkDotNet | `DocumentsBenchmarks`, `PixBenchmarks`, `ParseBenchmarks`, all `[MemoryDiagnoser]` | Solution build includes benchmarks |
| `samples/BrDocs.Samples.Api` | Minimal API with attributes, JSON, Pix + QR | Solution build includes sample |
| `.github/workflows/ci.yml` | Ubuntu + Windows × net8.0 + net10.0 | Reviewed line by line (below) |
| `Directory.Build.props` | Nullable, warnings as errors, deterministic, SourceLink, metadata | Clean `-warnaserror` build |
| Docs | `README.md`, `README.pt-BR.md`, `CHANGELOG.md`, `LICENSE`, `CONTRIBUTING.md`, `SPEC-AUDIT.md` | 14 `BrDocs.ReadmeTests` cases/TFM |

## Design rules

| Requirement | Implementation / evidence |
|---|---|
| `ReadOnlySpan<char>` hot paths, no allocation on `IsValid` | All 11 value objects + `PixPayload`/`PixKey` use `stackalloc`; 50+ allocation tests assert `0` bytes with `GC.GetAllocatedBytesForCurrentThread` after 10 000 warm-up and 10 000 measured iterations |
| `[MemoryDiagnoser]` benchmark evidence | `DocumentsBenchmarks`, `PixBenchmarks`, `ParseBenchmarks` — one benchmark per `IsValid`, `TryParse`, `TryFormat`, `Parse` API |
| Every type is `readonly record struct` | Verified for `Cpf`, `Cnpj`, `Cep`, `Pis`, `TituloEleitor`, `Cnh`, `Renavam`, `Placa`, `TelefoneBr`, `Boleto`, `Arrecadacao` |
| `IsValid`, `TryParse`, `Parse`, `ToString()`, `ToString("N")`, `ISpanFormattable`, `ISpanParsable<T>`, `IEquatable<T>` | Present on all 11; `ParsableHelpers.cs` and per-type test files assert the interfaces |
| Input accepts formatted/unformatted; strips `.`, `-`, `/`, spaces; other chars invalid | `DocumentText` normalization; invalid-character theories in every type's tests (plus documented phone `()`/`+` and case folding) |
| `Generate(Random?)` with check digits | Present on all check-digit types, `Cep`, `Placa` and both payment codes; property tests assert `Generate → IsValid` |
| Culture-invariant everywhere | `CultureInvarianceTests` (11 cultures) + per-type culture tests |

## Documents

| Requirement | Implementation / evidence |
|---|---|
| CPF mod-11 × 2, rejects all-same, `FiscalRegion` | `Cpf.cs`, `CpfFiscalRegion.cs`; vectors `529.982.247-25`, `111.444.777-35`, `111.111.111-11` (invalid) |
| CNPJ numeric + alphanumeric IN RFB nº 2.229/2024, char value `c-48` weights, lowercase normalization, `IsAlphanumeric`/`Root`/`Branch`/`IsHeadOffice`, reject all-same | `Cnpj.cs`; official vector `12.ABC.345/01DE-35`, numeric `11.222.333/0001-81`, lowercase and invalid vectors |
| CEP 8 digits, `00000-000`, UF by Correios ranges | `Cep.cs`, `CepUfTable.cs`; boundary tests for 00000–00999, 01000–19999, 78899/78900–78999 |
| PIS/PASEP/NIT 11 digits, weights `3,2,9,8,7,6,5,4,3,2` | `Pis.cs`; vectors `120.70458.82-4`, `120.5641.284-7`, all-same rejected |
| Título de Eleitor 12 digits, UF 01–28, SP/MG remainder-zero rule | `TituloEleitor.cs`; vectors `1023 8501 06 71`, `1111 1111 01 16`, invalid UF 00 |
| CNH 11 digits, two DVs with decrement rule | `Cnh.cs`; vectors `026503064-61`, `012345678-07`, `000000001-19` |
| RENAVAM 11 digits, legacy 9 padded, reversed weights | `Renavam.cs`; vectors `639884962` → `00639884962`, `12345678900` |
| Placa legacy + Mercosul, `ToMercosul()` A–J | `Placa.cs`; `ABC-1234` → `ABC1C34`, `ABC1D23`, natively issued Mercosul letters outside A–J preserved |
| Telefone BR E.164/local, ANATEL DDD list, mobile 9 digits starting 9, `(11) 91234-5678`, `ToE164()` | `TelefoneBr.cs`, `AnatelDddTable.cs`; valid/invalid vectors for all accepted forms and DDD 55 disambiguation |

## Payments

| Requirement | Implementation / evidence |
|---|---|
| Boleto parse barcode/linha, convert both ways, field mod-10 DVs, general mod-11 DV | `Boleto.cs`; `BoletoTests` valid/invalid vectors for corrupted field DVs, general DV, length and non-digit input |
| Extract bank, currency, due date, amount | `BoletoTests` asserts `104`, `9`, `214.03m`, factor `8981` and resolved due date |
| Factor rule 1997-10-07, 9999 = 2025-02-21, reset 1000 = 2025-02-22, `[-3000,+5500]` window, `0000` → no date, `Encode`/`Decode` | `Boleto.cs`; boundary theories for 2025-02-21/22/23 and 2049-10-13/14, ambiguous-cycle tests with explicit references, `0000` tests |
| Arrecadação 48-digit linha starting `8`, 4 blocks, mod 10 vs 11 by 3rd digit, segment/effective value, general DV | `Arrecadacao.cs`; reference mod-10 and mod-11 barcodes, invalid DVs, value/segment assertions |
| `Boleto.Create(bank, currency, dueDate, amount, freeField)` | `BoletoTests` round-trip and oracle verification |
| Pix TLV ID/length/value, nested 26/62/80–99 | `PixTlv.cs`; `PixTlvTests` and `PixPropertyTests` |
| Static Pix: GUI 26-00, key 26-01, description 26-02, MCC 52 `0000`, currency 53 `986`, amount 54 `0.00`, country 58 `BR`, name 59 ≤25, city 60 ≤15, txid 62-05 `***`, CRC 63 last | `PixPayloadBuilder.cs`, `PixBrCode.cs`; `PixPayloadTests`, official BCB vector |
| Dynamic Pix: 26-25 URL, initiation 01 = `12` single-use, no amount/txid | `PixPayloadBuilder.SingleUse`; tests assert dynamic rules and exceptions |
| CRC16/CCITT-FALSE over payload including `6304`, uppercase | `Crc16.cs`; vector `…63041D3D` and tampered-CRC rejection |
| Pix key types and validation (CPF, CNPJ incl. alphanumeric, e-mail, `+55`, EVP) | `PixKey.cs`, `PixKeyTests` |
| Merchant/city normalization, max lengths, allowed chars | `PixText.cs`; normalization tests |
| `PixPayload.Parse` typed result, `ToString()` | `PixPayload.cs`; valid/invalid/round-trip tests |
| QR: no core dependency, complete SVG encoder versions 1–40 ECC M byte mode + masking | `src/BrDocs.QrCode`; 243 cases/TFM incl. `OracleVectorTests`, `FullMatrixSweepTests`, `RenderTests`, `PixQrExtensionTests` |

## Integrations

| Requirement | Implementation / evidence |
|---|---|
| `[Cpf]`, `[Cnpj(AllowAlphanumeric)]`, `[CpfOrCnpj]`, `[Cep]`, `[Pis]`, `[TituloEleitor]`, `[Cnh]`, `[Renavam]`, `[Placa]`, `[TelefoneBr(MobileOnly)]`, `[PixKey]`, `[LinhaDigitavel]` | `Attributes.cs`; pt-BR messages in `ValidationMessages.resx`, overridable resources, culture tests |
| FluentValidation equivalents | `BrDocsRuleExtensions.cs`; all rules covered in `RuleTests` |
| `JsonConverter<T>` per value object, formatted vs digits-only, source-gen friendly | `Converters.cs`, `BrDocsJsonOptions`, `AddBrDocsConverters`; all 12 types + Pix covered |

## Quality bar

| Requirement | Result |
|---|---|
| ≥400 meaningful tests | **2284 cases per target framework** (4568 executed across net8.0 + net10.0): 1944 core + 243 QR + 33 DataAnnotations + 31 FluentValidation + 19 JSON + 14 README samples. Unknown-valid and known-invalid vectors, round trips and FsCheck properties included |
| `dotnet build -warnaserror` clean | 0 warnings, 0 errors |
| `dotnet test` green on both TFMs | 12/12 test assemblies pass on net8.0 and net10.0 |
| Benchmarks project builds | Yes, net10.0 executable |
| README: badges, install, quick-start for every feature, document table, alphanumeric CNPJ, factor reset, Pix, all samples compiling | `README.md` + `README.pt-BR.md`; `tests/BrDocs.ReadmeTests` compiles/runs all 13 samples per README and asserts the block count |
| NuGet metadata, no publishing | `PackageId`/metadata in `Directory.Build.props` + each library csproj; no push/publish command was executed |

## Hard constraints and CI checklist

- No `git push`, `gh`, `dotnet nuget push` or other publishing command used. All changes stay in this repository.
- Domain rules cite official sources in XML comments/code (RFB, FEBRABAN, BCB, ANATEL, DENATRAN, TSE, ISO).
- `grep -R 'TODO|FIXME|NotImplementedException|NotImplemented|Skip|Assert.True(true)'` finds no stubs, skipped tests or placeholders. The only `Skip` occurrence is LINQ `.Skip(1)` in a PNG-format test; the only `placeholder` occurrences are XML docs describing `{0}` message format placeholders.
- `.gitattributes`: `* text=auto eol=lf`; byte-exact fixtures (`*.bin`, `*.dat`) marked `-text` and binary artifacts marked `binary`. `.editorconfig` pins `end_of_line = lf`.
- No test locates npm/dotnet/cargo through a runtime binary path. Tests that need built code use project references; the workflow builds with `dotnet build` before `dotnet test --no-build`.
- No test depends on ICU labels, `TextDecoder`, culture, timezone, path separators or line endings without normalization: culture tests set and restore cultures explicitly, and the two date-sensitive Boleto tests use an explicit reference date with a one-day midnight tolerance. The README-sample test finds files by walking up from `AppContext.BaseDirectory` with `Path.Combine`.
- Workflow reviewed line by line: checkout, setup-dotnet (8.0.x + 10.0.x), restore, build, test, five pack commands and artifact upload all use cross-platform commands and forward-slash paths; `CI=true` makes builds deterministic; the pack list includes `BrDocs.QrCode`.
- Flaky allocation tests fixed with `GC.GetAllocatedBytesForCurrentThread`, 10 000 warm-up iterations, 10 000 measured iterations per window and 16 windows. Full suite run **10 consecutive times** after a fresh `-warnaserror` build: **10/10 runs, 0 failures**.
