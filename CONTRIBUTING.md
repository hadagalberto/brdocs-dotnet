# Contributing

Thanks for helping improve BrDocs. This project targets production quality: every public API must
be complete, documented with XML docs, and covered by tests that assert real behaviour.

## Ground rules

- Read [SPEC.md](SPEC.md) first — it is the source of truth for scope and algorithms.
- No stubs, no `TODO`, no `NotImplementedException`, no skipped tests, no placeholder assertions.
- Domain rules must come from an official source (Receita Federal, FEBRABAN, BCB, ANATEL, DENATRAN,
  TSE, ISO). When you add a rule, cite the document in a code comment and add a known-valid vector.
- All formatting and parsing is culture-invariant. Hot paths use `ReadOnlySpan<char>` and must not
  allocate; verify with `GC.GetAllocatedBytesForCurrentThread` and a `[MemoryDiagnoser]` benchmark.
- Keep the core package dependency-free. New dependencies belong in a separate package.

## Development setup

```shell
dotnet --info                 # SDK 10.0.x (see global.json); net8.0 targeting pack also needed
dotnet restore
dotnet build --configuration Release -warnaserror
dotnet test --configuration Release
```

Run the benchmarks locally (they are not part of CI):

```shell
dotnet run -c Release --project benchmarks/BrDocs.Benchmarks
```

The sample API is runnable with:

```shell
dotnet run --project samples/BrDocs.Samples.Api
```

## Adding a document type

1. Add a `readonly record struct` in `src/BrDocs` implementing `IEquatable<T>`,
   `ISpanFormattable` and `ISpanParsable<T>`, with `IsValid`, `TryParse`, `Parse(string)`,
   `ToString()`, `ToString("N")` and `Generate(Random?)` when there is a check digit.
2. Add XML docs, including the algorithm source and accepted input forms.
3. Add known-valid and known-invalid vectors, round-trip tests (`Generate → IsValid`,
   `Parse → ToString → Parse`) and property-based tests in `tests/BrDocs.Tests`.
4. Add allocation tests in `AllocationTests.cs` and `IsValid`/`Parse` benchmarks in
   `benchmarks/BrDocs.Benchmarks`.
5. Add the new attribute and FluentValidation/JSON converters when applicable, and mirror any new
   README sample in `tests/BrDocs.ReadmeTests`.

## Tests

- Every algorithm needs at least one known-valid and one known-invalid vector from an external
  reference — not a value produced by the implementation under test.
- Prefer independent oracles (`Oracles.cs`) for property-based tests so a shared bug cannot hide.
- Allocation tests must warm up the call on the measuring thread, measure many iterations with
  `GC.GetAllocatedBytesForCurrentThread`, and never rely on tiered-compilation state.
- Tests must be deterministic across operating systems, time zones, ICU versions, path separators
  and line endings. Normalize any clock- or culture-dependent input explicitly.

## Style

- Follow `.editorconfig` (LF line endings, UTF-8, four spaces, file-scoped namespaces).
- `TreatWarningsAsErrors` is on for the whole solution; analyzers run at `latest-recommended`.
- Use conventional commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`, `perf:`.

## Pull requests

- Keep commits atomic and locally committed; never push package releases or NuGet artifacts.
- Update `README.md`, `README.pt-BR.md`, `CHANGELOG.md` and `SPEC-AUDIT.md` when behaviour changes.
- Confirm a clean `dotnet build -warnaserror` and green `dotnet test` on net8.0 and net10.0 before
  opening a PR. CI runs on Ubuntu and Windows.

## License

By contributing you agree that your contributions are licensed under the MIT License,
Copyright (c) Hadagalberto Junior.
