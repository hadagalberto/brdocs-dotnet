# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Package metadata currently uses
`0.1.0`; no package has been published.

## [Unreleased]

### Added

- **Core value objects** (`BrDocs`): `Cpf` (with `FiscalRegion`), `Cnpj` (numeric and the new
  alphanumeric format of IN RFB nº 2.229/2024, with `Root`, `Branch`, `IsAlphanumeric`,
  `IsHeadOffice`), `Cep` (Correios UF ranges), `Pis`, `TituloEleitor` (UF codes and the SP/MG
  remainder-zero rule), `Cnh` (decrement rule), `Renavam` (legacy 9-digit padding), `Placa`
  (legacy and Mercosul, `ToMercosul()`) and `TelefoneBr` (ANATEL DDD list, display and E.164).
- Every value object exposes `IsValid(ReadOnlySpan<char>)`, `TryParse(...)`, `Parse(string)`,
  formatted and digits-only `ToString()`, `TryFormat`, `ISpanFormattable`, `ISpanParsable<T>` and
  `Generate(Random?)` where a check digit exists. All parsing/formatting is culture-invariant and
  validation paths allocate nothing.
- **Payments** (`BrDocs`): FEBRABAN `Boleto` (44-digit barcode, 47-digit linha digitável, field and
  general check digits, bank/currency/amount/due-date extraction, `Boleto.Create`, due-date factor
  encoding/decoding with the 2025-02-22 cycle reset) and `Arrecadacao` (48-digit linha, per-block
  mod 10/mod 11 DVs, segment and effective value).
- **Pix BR Code** (`BrDocs`): nested-TLV encoder/decoder, static and dynamic builders, MCC/currency/
  country defaults, merchant field normalization, txid and amount rules, CRC16/CCITT-FALSE,
  `PixPayload.Parse` with full validation, and Pix key detection (CPF, CNPJ including alphanumeric,
  e-mail, phone, EVP).
- **QR Code** (`BrDocs.QrCode`): complete ISO/IEC 18004 encoder — byte and alphanumeric modes,
  versions 1–40, ECC L/M/Q/H, Reed–Solomon, interleaving, all eight masks with ISO penalty rules,
  format/version BCH codes, SVG and PNG output, plus `PixPayload.ToQrSvg()`.
- **Integrations**: `BrDocs.DataAnnotations` (12 attributes with pt-BR messages and overridable
  resources), `BrDocs.FluentValidation` (12 `IRuleBuilder` extensions), `BrDocs.Json` (converters for
  every document, formatted or digits-only output, source-gen friendly).
- **Documentation**: `README.md`, `README.pt-BR.md`, `CONTRIBUTING.md`, `SPEC-AUDIT.md` and a
  `BrDocs.ReadmeTests` project that compiles and runs every README sample.
- **Benchmarks** (`BrDocs.Benchmarks`): `[MemoryDiagnoser]` BenchmarkDotNet classes covering every
  `IsValid`, `TryParse`, `TryFormat` and `Parse` API.
- CI workflow building and testing on Ubuntu and Windows for net8.0 and net10.0 with warnings as
  errors, plus NuGet package artifacts.

### Fixed

- Allocation tests now use `GC.GetAllocatedBytesForCurrentThread` with thousands of warm-up and
  measured iterations, removing a cold-run flake caused by tiered compilation/foreign-thread
  allocations.
- Date-dependent Boleto tests normalize the local-midnight boundary with an explicit reference
  date and a one-day tolerance.

### Changed

- Repository line endings normalized to LF via `.gitattributes`; byte-exact artifacts are excluded
  from normalization. `.editorconfig` pins the formatter to LF.
