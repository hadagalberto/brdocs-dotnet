# BrDocs

[![CI](https://github.com/hadagalberto/brdocs-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/hadagalberto/brdocs-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BrDocs.svg)](https://www.nuget.org/packages/BrDocs)
[![Licença: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-5127D8.svg)](https://dotnet.microsoft.com/)

Biblioteca .NET de nível produção para documentos brasileiros e códigos de pagamento. Toda API
pública é um value object com `IsValid`, `TryParse`, `Parse`, `ToString()` formatado,
`ToString("N")` com apenas dígitos, suporte a `ISpanFormattable`/`ISpanParsable<T>`. A validação é
baseada em `Span`, não aloca, é invariante de cultura, compatível com AOT e trimming, e o pacote
principal não tem dependências em runtime.

English: [README.md](README.md).

## Instalação

```shell
dotnet add package BrDocs                  # núcleo: documentos, boletos, Pix
dotnet add package BrDocs.DataAnnotations  # [Cpf], [Cnpj], [Cep], ...
dotnet add package BrDocs.FluentValidation # MustBeCpf(), MustBeCnpj(), ...
dotnet add package BrDocs.Json             # conversores System.Text.Json
dotnet add package BrDocs.QrCode           # QR Code ISO/IEC 18004 + SVG do Pix
```

Alvos: `net8.0` e `net10.0`.

## Documentos suportados

| Documento | Tipo | Formato | Dígitos verificadores | `Generate()` |
|---|---|---|---|---|
| CPF | `Cpf` | `529.982.247-25` | mod 11 × 2 | `Generate()` |
| CNPJ numérico | `Cnpj` | `11.222.333/0001-81` | mod 11 × 2 | `Generate()` |
| CNPJ alfanumérico | `Cnpj` | `12.ABC.345/01DE-35` | mod 11 × 2 | `GenerateAlphanumeric()` |
| CEP | `Cep` | `01310-100` | nenhum (estrutural) | `Generate()` |
| PIS/PASEP/NIT | `Pis` | `120.70458.82-4` | mod 11 | `Generate()` |
| Título de Eleitor | `TituloEleitor` | `1234 5678 20 97` | mod 11 × 2 | `Generate()` |
| CNH | `Cnh` | `026503064-61` | mod 11 × 2 | `Generate()` |
| RENAVAM | `Renavam` | `00639884962` | mod 11 | `Generate()` |
| Placa | `Placa` | `ABC-1234` / `ABC1D23` | nenhum (estrutural) | `Generate()`, `GenerateLegacy()` |
| Telefone BR | `TelefoneBr` | `(11) 91234-5678` / `+5511912345678` | nenhum (lista ANATEL) | `Generate()`, `GenerateFixed()` |
| Boleto (FEBRABAN) | `Boleto` | barras 44 dígitos / linha digitável 47 | mod 10 × 3 + mod 11 | `Generate()` |
| Arrecadação / convênio | `Arrecadacao` | barras de 44 dígitos / linha 48 | mod 10 ou 11 (escolhido pelo 3º dígito) para o DV geral e os 4 DVs de campo | `Generate()` |
| Pix BR Code | `PixPayload` | TLV EMV® QRCPS-MPM | CRC16/CCITT-FALSE | builder |

Os parsers ignoram `.`, `-`, `/` e espaços (além de parênteses e `+` inicial em telefones e da
normalização de caixa em letras). Qualquer outro caractere é rejeitado. O valor `default` de cada
value object é inválido, exceto em placas (o padrão zerado é estruturalmente válido).

## Início rápido: documentos

```csharp
using BrDocs;

bool ok = Cpf.IsValid("529.982.247-25");          // true
bool bad = Cpf.IsValid("111.111.111-11");         // false (sequência reservada)
Cpf cpf = Cpf.Parse("52998224725");
Console.WriteLine(cpf);                           // 529.982.247-25
Console.WriteLine(cpf.ToString("N"));             // 52998224725
Console.WriteLine(cpf.FiscalRegion);              // EsRj
Console.WriteLine(cpf.FiscalRegion.Ufs());        // ES, RJ
Console.WriteLine(Cpf.Generate());                // um novo CPF válido
```

`IsValid(ReadOnlySpan<char>)`, `TryParse(ReadOnlySpan<char>, out T)` e `TryFormat(...)` não alocam:
normalizam em buffers `stackalloc`. `Parse` lança `FormatException` com mensagem que inclui o valor
inválido.

### CNPJ, inclusive o formato alfanumérico

Desde a IN RFB nº 2.229/2024 (emitidos a partir de julho de 2026), a base do CNPJ pode conter letras
A–Z maiúsculas. A base tem 8 caracteres de raiz + 4 de filial; os DVs usam o código de cada caractere
menos 48 (`'A'` → 17), com pesos `5,4,3,2,9,8,7,6,5,4,3,2` e
`6,5,4,3,2,9,8,7,6,5,4,3,2`. Entrada minúscula é normalizada e a pontuação é ignorada.

```csharp
using BrDocs;

bool numeric = Cnpj.IsValid("11.222.333/0001-81");     // true
Cnpj alpha = Cnpj.Parse("12.ABC.345/01DE-35");          // vetor oficial da RFB
Console.WriteLine(alpha.IsAlphanumeric);                // True
Console.WriteLine(alpha.Root);                          // 12ABC345
Console.WriteLine(alpha.Branch);                        // 01DE
Console.WriteLine(alpha.IsHeadOffice);                  // False
Console.WriteLine(Cnpj.IsValid("12abc34501de35"));     // true (minúsculas aceitas)
Console.WriteLine(Cnpj.GenerateAlphanumeric().IsAlphanumeric); // True
```

### CEP, PIS, Título de Eleitor, CNH e RENAVAM

```csharp
using BrDocs;

Console.WriteLine(Cep.Parse("01310-100").Uf);                     // SP
Console.WriteLine(Pis.Parse("120.70458.82-4"));                   // 120.70458.82-4
TituloEleitor titulo = TituloEleitor.Parse("1234 5678 20 97");
Console.WriteLine(titulo.Uf);                                     // DF (código de UF 20)
Console.WriteLine(titulo.UfCode);                                 // 20
Console.WriteLine(Cnh.Parse("026503064-61"));                     // 026503064-61
Console.WriteLine(Renavam.Parse("639884962"));                    // 00639884962 (legado com zeros)
```

### Placas e telefones

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
Console.WriteLine(TelefoneBr.IsValid("11712345678"));  // False (prefixo inválido)
```

## Boleto (FEBRABAN)

Analisa um código de barras de 44 dígitos ou uma linha digitável de 47 e converte entre ambos.
Os DVs de campo usam módulo 10; o DV geral usa módulo 11 com `0`, `10` e `11` mapeados para `1`.
Arrecadação/convênio tem sempre código de barras de 44 dígitos (a linha digitável tem 48 e começa
com `8`). O DV geral e os quatro DVs de campo usam módulo 10 quando o terceiro dígito é `6` ou `7`,
e módulo 11 quando é `8` ou `9`; o valor efetivo é exposto quando esse dígito é `6` ou `8`.

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

### A reinicialização do fator de vencimento

O boleto codifica o vencimento em um fator de 4 dígitos contado a partir de 1997-10-07. O fator
`9999` é **2025-02-21**; no dia seguinte o ciclo reinicia em `1000` (**2025-02-22**), portanto um
fator sozinho é ambíguo. `Boleto.Encode(DateOnly)` escreve o fator e
`Boleto.Decode(factor, reference)` escolhe o ciclo mais próximo da data de referência dentro da
janela `[-3000, +5500]` dias. `0000` significa "sem vencimento". `Boleto.DueDate` usa hoje como
referência; `GetDueDate(reference)` resolve contra uma data explícita e
`Generate(seed, reference)`/`Generate(rng, reference)` tornam o vencimento gerado determinístico.

```csharp
using BrDocs;

Console.WriteLine(Boleto.Decode(9999, new DateOnly(2025, 2, 21))); // 2025-02-21
Console.WriteLine(Boleto.Decode(1000, new DateOnly(2025, 2, 22))); // 2025-02-22
Console.WriteLine(Boleto.Decode(1001, new DateOnly(2025, 2, 23))); // 2025-02-23
Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 21)));       // 9999
Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 22)));       // 1000
```

## Pix BR Code

Pix estático leva a chave do recebedor; dinâmico leva a URL da cobrança (e não pode conter valor ou
txid). O campo 26 é um template aninhado da conta do recebedor, o 62 contém dados adicionais e o 63
é o CRC-16/CCITT-FALSE calculado sobre todo o payload, incluindo `6304`.

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
Console.WriteLine(parsed.Crc);                            // quatro dígitos hexadecimais maiúsculos
Console.WriteLine(parsed.IsStatic);                       // True

// O exemplo oficial do BCB também analisa e valida.
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

Chaves Pix são detectadas como CPF, CNPJ (inclusive alfanumérico), e-mail, telefone `+55` ou EVP
(UUID v4):

```csharp
using BrDocs;

Console.WriteLine(PixKey.Detect("123e4567-e12b-12d1-a456-426655440000")); // Evp
Console.WriteLine(PixKey.Detect("12ABC34501DE35"));                      // Cnpj
Console.WriteLine(PixKey.IsValid("+5511912345678"));                     // True
Console.WriteLine(PixKey.IsValid("garbage key"));                        // False
```

Uma string de 11 dígitos que não é um CPF válido é ambígua com um celular local e é rejeitada por
`Normalize`/`CreateStatic` (como `Detect` já faz); deixe a intenção de telefone explícita com
`+55`, formatação local ou prefixo de país.

### Geração de QR Code

O pacote principal não tem dependência de QR. `BrDocs.QrCode` é um codificador ISO/IEC 18004
completo (modos byte e alfanumérico, versões 1–40, ECC L/M/Q/H, oito máscaras, saída SVG e PNG).

```csharp
using BrDocs;
using BrDocs.QrCode;

PixPayload pix = PixPayload.Parse(
    "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D");
string svg = pix.ToQrSvg();                 // documento SVG completo
QrCode qr = QrCode.Encode("https://example.com");
byte[] png = qr.ToPng();                    // bytes PNG
Console.WriteLine(svg.StartsWith("<svg", StringComparison.Ordinal)); // True
Console.WriteLine(png.Length > 0);          // True
```

## Integrações

### DataAnnotations

Atributos com mensagens em pt-BR e recursos sobrescrevíveis. Todos aceitam `null` como válido e
texto vazio como inválido; `[Cnpj(AllowAlphanumeric = true)]` habilita o novo formato e
`[TelefoneBr(MobileOnly = true)]` rejeita fixos.

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

Atributos disponíveis: `[Cpf]`, `[Cnpj]`, `[CpfOrCnpj]`, `[Cep]`, `[Pis]`, `[TituloEleitor]`,
`[Cnh]`, `[Renavam]`, `[Placa]`, `[TelefoneBr]`, `[PixKey]` e `[LinhaDigitavel]`.

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

bool valid = new CustomerValidator().Validate(customer).IsValid; // customer acima
Console.WriteLine(valid); // True
```

Outras extensões: `MustBeCpfOrCnpj`, `MustBeCep`, `MustBePis`, `MustBeTituloEleitor`, `MustBeCnh`,
`MustBeRenavam`, `MustBePlaca`, `MustBePixKey` e `MustBeLinhaDigitavel`.

### System.Text.Json

Conversores formatam na serialização e aceitam os dois estilos na desserialização. Valores inválidos
geram `JsonException`. Contextos source-generated funcionam com `[JsonConverter(typeof(...))]`.

```csharp
using System.Text.Json;
using BrDocs;
using BrDocs.Json;

JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();
Cpf cpf = Cpf.Parse("111.444.777-35");
string json = JsonSerializer.Serialize(cpf, options);                  // "111.444.777-35"
Cpf back = JsonSerializer.Deserialize<Cpf>(json, options)!;            // mesmo valor
JsonSerializerOptions digits = new JsonSerializerOptions()
    .AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);
string digitsJson = JsonSerializer.Serialize(cpf, digits);             // "11144477735"
Console.WriteLine(back == cpf); // True
```

Existem conversores para todos os value objects, `Boleto`, `Arrecadacao` e `PixPayload`.

## Performance

`benchmarks/BrDocs.Benchmarks` contém classes BenchmarkDotNet com `[MemoryDiagnoser]` para todas
as APIs `IsValid`, `TryParse`, `TryFormat` e `Parse`. Testes unitários com
`GC.GetAllocatedBytesForCurrentThread` confirmam `0 B` alocados em `IsValid`/`TryParse`/`TryFormat`.

```shell
dotnet run -c Release --project benchmarks/BrDocs.Benchmarks
```

## API de exemplo

`samples/BrDocs.Samples.Api` é uma minimal API que usa DataAnnotations, FluentValidation,
conversores JSON e geração de QR do Pix. Execute com
`dotnet run --project samples/BrDocs.Samples.Api`.

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md). Changelog: [CHANGELOG.md](CHANGELOG.md).
Especificação: [SPEC.md](SPEC.md). Auditoria: [SPEC-AUDIT.md](SPEC-AUDIT.md).

## Licença

MIT — Copyright (c) Hadagalberto Junior. Veja [LICENSE](LICENSE).
