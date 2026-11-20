# BrDocs — SPEC

Production-grade .NET library for Brazilian documents and payment codes. Open source (MIT).
Target: `net8.0` and `net10.0`. AOT- and trimming-compatible. Zero runtime dependencies in the core package.

This is NOT a toy. Every public API must be complete, documented (XML docs), and covered by real tests
that assert real behaviour. No stubs, no `TODO`, no `NotImplementedException`, no skipped tests,
no placeholder tests (`Assert.True(true)`), no "simplified" implementations.

## Solution layout

```
BrDocs.sln
src/BrDocs/                      core library (no deps)
src/BrDocs.DataAnnotations/      [Cpf], [Cnpj], [Cep], ... validation attributes
src/BrDocs.FluentValidation/     .MustBeCpf() etc. extensions (depends on FluentValidation)
src/BrDocs.Json/                 System.Text.Json converters (normalize/format on (de)serialize)
tests/BrDocs.Tests/              xUnit, property-based tests with FsCheck where useful
benchmarks/BrDocs.Benchmarks/    BenchmarkDotNet
samples/BrDocs.Samples.Api/      minimal API sample using attributes + Pix generation
.github/workflows/ci.yml         build + test on ubuntu & windows, net8 + net10, pack artifacts
Directory.Build.props            nullable enable, warnings as errors, deterministic, SourceLink, package metadata
README.md (English) + README.pt-BR.md, CHANGELOG.md, LICENSE (MIT, "Hadagalberto Junior"), CONTRIBUTING.md
```

## Design rules

- Hot paths use `ReadOnlySpan<char>`; no allocations on validation (`IsValid`). Verify with a benchmark using `[MemoryDiagnoser]`.
- Every document type is a `readonly record struct` value object with:
  `static bool IsValid(ReadOnlySpan<char>)`, `static bool TryParse(ReadOnlySpan<char>, out T)`, `static T Parse(string)`
  (throws `FormatException` with a clear message), `ToString()` (formatted), `ToString("N")` (digits only),
  implements `ISpanFormattable`, `ISpanParsable<T>`, `IEquatable<T>`.
- Input accepts formatted or unformatted text; strips the standard punctuation only (`.`, `-`, `/`, spaces). Any other char → invalid.
- `Generate(Random? rng = null)` for every document type that has a check digit (for tests / fake data).
- Culture-invariant everywhere.

## Documents (all with check-digit algorithms and tests with known valid AND invalid vectors)

1. **CPF** — 11 digits, mod-11 two DVs. Reject all-same-digit sequences (`111.111.111-11`).
   Expose `FiscalRegion` (9th digit → UF list, per Receita Federal table).
2. **CNPJ — including the new ALPHANUMERIC CNPJ** (Receita Federal IN RFB nº 2.229/2024, issued from July 2026).
   Format: 12 alphanumeric chars (root 8 + order 4, uppercase A–Z or 0–9) + 2 numeric DVs.
   Algorithm (verified): each char value = `(int)c - 48` (so '0'..'9' → 0..9, 'A' → 17, 'Z' → 42);
   DV1 weights `5,4,3,2,9,8,7,6,5,4,3,2`, DV2 weights `6,5,4,3,2,9,8,7,6,5,4,3,2`; `r = sum % 11`; `dv = r < 2 ? 0 : 11 - r`.
   Official test vector: `12.ABC.345/01DE-35` is valid. Numeric vector: `11.222.333/0001-81` is valid.
   Lowercase input is normalized to uppercase. Expose `IsAlphanumeric`, `Root` (8 chars), `Branch` (4 chars), `IsHeadOffice` (branch == "0001").
   Reject all-zero and all-same numeric sequences.
3. **CEP** — 8 digits, format `00000-000`, expose UF by range (Correios ranges table).
4. **PIS/PASEP/NIT** — 11 digits, weights `3,2,9,8,7,6,5,4,3,2`.
5. **Título de Eleitor** — 12 digits, two DVs, UF code (digits 9–10) 01..28, special DV rule for SP (01) and MG (02) when remainder 0 → DV 1.
6. **CNH** — 11 digits, two DVs (with the standard decrement rule).
7. **RENAVAM** — 11 digits (pad legacy 9-digit with leading zeros), weights `3,2,9,8,7,6,5,4,3,2` over the reversed first 10.
8. **Placa veicular** — legacy `AAA-9999` and Mercosul `AAA9A99`; `ToMercosul()` conversion (5th char digit → letter A–J). No check digit (no Generate requirement beyond random valid format).
9. **Telefone BR** — parse E.164 / local formats; DDD validated against the ANATEL DDD list; mobile = 9 digits starting with 9. Format `(11) 91234-5678`, `ToE164()` → `+5511912345678`.

## Payments

### Boleto (FEBRABAN)
- Parse both **linha digitável** (47 digits, bank slips) and **código de barras** (44 digits); convert both ways.
- Validate the 3 field DVs (mod 10) and the general DV (mod 11, positions per FEBRABAN; result 0, 10, 11 → 1).
- Extract bank code, currency code, **due date** and **amount**.
- **Due-date factor rule (verified):** base date 1997-10-07; factor 9999 = 2025-02-21; on 2025-02-22 the factor restarts at 1000.
  Decoding a factor is ambiguous across cycles → resolve with a reference date (default `DateOnly.FromDateTime(DateTime.Today)`),
  choosing the cycle whose date is closest to the reference within the window [-3000, +5500] days. Factor `0000` → no due date.
  Expose `Encode(DateOnly)` and `Decode(int factor, DateOnly reference)`. Unit tests for boundaries: 2025-02-21 ↔ 9999, 2025-02-22 ↔ 1000, 2025-02-23 ↔ 1001.
- **Arrecadação / convênio** (linha digitável 48 digits, starts with `8`): parse 4 blocks of 11+DV, DV via mod 10 or mod 11 depending on the 3rd digit (6/7 → mod 10, 8/9 → mod 11), extract segment, value (when 3rd digit 6 or 8 = effective value), and validate general DV.
- `Boleto.Create(bank, currency, dueDate, amount, freeField25)` builds a valid barcode + linha digitável.

### Pix BR Code (EMV® QRCPS-MPM, Manual de Padrões para Iniciação do Pix — BCB)
- TLV (ID 2 digits, length 2 digits, value) encoder/decoder supporting nested templates (26, 62, and 80–99).
- **Static** Pix: GUI `br.gov.bcb.pix` (ID 26 sub 00), key (26 sub 01), optional description (26 sub 02), MCC (52, default `0000`),
  currency 53 = `986`, optional amount 54 (format `0.00`, invariant), country 58 = `BR`, merchant name 59 (max 25), city 60 (max 15),
  62 sub 05 txid (`***` default, 1–25 chars `[A-Za-z0-9]`), CRC 63 computed last.
- **Dynamic** Pix: 26 sub 25 = payload URL (no scheme), point-of-initiation 01 = `12` when single-use.
- **CRC16/CCITT-FALSE** (poly 0x1021, init 0xFFFF, no reflect, no xorout), uppercase hex, computed over the whole payload including `6304`.
  Verified test vector (from BCB manual):
  `00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D`
- Pix key type detection & validation: CPF, CNPJ (incl. alphanumeric), e-mail, phone (`+55...`), EVP (UUID v4).
- Normalise merchant name/city: strip diacritics, uppercase optional, enforce max lengths, reject chars outside the allowed set.
- `PixPayload.Parse(string)` → validates CRC, required fields, returns typed object; `ToString()` → BR Code.
- QR code rendering: core has **no QR dependency**. Provide `BrDocs.Pix.QrCode` only if implemented fully (QR generation with byte mode,
  error correction M, SVG output) — otherwise leave QR rendering out entirely and document how to use QRCoder.
  Prefer: implement a complete, tested SVG QR encoder (versions 1–40, ECC M, byte mode, masking with penalty rules) in a separate package `BrDocs.QrCode`.

## Integrations

- `BrDocs.DataAnnotations`: `[Cpf]`, `[Cnpj(AllowAlphanumeric = true)]`, `[CpfOrCnpj]`, `[Cep]`, `[Pis]`, `[TituloEleitor]`, `[Cnh]`, `[Renavam]`, `[Placa]`, `[TelefoneBr(MobileOnly = false)]`, `[PixKey]`, `[LinhaDigitavel]`. Error messages in pt-BR with overridable resources.
- `BrDocs.FluentValidation`: equivalents as `IRuleBuilder` extensions.
- `BrDocs.Json`: `JsonConverter<T>` for each value object with options (write formatted vs digits only). Source-gen friendly.

## Quality bar

- Tests: ≥ 400 meaningful test cases total; every algorithm has known-valid and known-invalid vectors, round-trip tests
  (Generate → IsValid, Parse → ToString → Parse), and property-based tests.
- `dotnet build -warnaserror` clean, `dotnet test` green on net8.0 and net10.0.
- Benchmarks project builds (no need to run in CI).
- README: badges, install, quick-start for every feature, table of supported documents, alphanumeric CNPJ explanation,
  boleto factor-reset explanation, Pix example. Every code sample in README must compile (mirror them in a test).
- Package metadata ready for NuGet (PackageId `BrDocs`, `BrDocs.DataAnnotations`, ... ; icon not required) — but DO NOT publish.

## Hard constraints

- Never run `git push`, `gh`, `dotnet nuget push`, or anything that publishes. Commit locally only, with clear conventional-commit messages, after each milestone.
- Stay inside this repository directory.
- When unsure about a domain rule not given here, implement the documented official rule and add a comment citing the source; do not invent rules.
