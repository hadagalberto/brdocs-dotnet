using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BrDocs;
using BrDocs.DataAnnotations;
using BrDocs.FluentValidation;
using BrDocs.Json;
using BrDocs.QrCode;
using FluentValidation;
using Xunit;

namespace BrDocsReadmeTests;

/// <summary>
/// Every <c>csharp</c> code block in README.md and README.pt-BR.md is mirrored here. The
/// <see cref="Readme_Code_Blocks_Are_All_Mirrored"/> test guards the count so a new sample cannot
/// be added to the READMEs without a compiling, running counterpart in this project.
/// </summary>
public sealed class ReadmeTests
{
    // README sample: CPF quick start.
    [Fact]
    public void Readme_01_Cpf()
    {
        bool ok = Cpf.IsValid("529.982.247-25");          // true
        bool bad = Cpf.IsValid("111.111.111-11");         // false (reserved sequence)
        Cpf cpf = Cpf.Parse("52998224725");
        Console.WriteLine(cpf);                           // 529.982.247-25
        Console.WriteLine(cpf.ToString("N"));             // 52998224725
        Console.WriteLine(cpf.FiscalRegion);              // EsRj
        Console.WriteLine(cpf.FiscalRegion.Ufs());        // ES, RJ
        Console.WriteLine(Cpf.Generate());                // a new valid CPF

        Assert.True(ok);
        Assert.False(bad);
        Assert.Equal("529.982.247-25", cpf.ToString());
        Assert.Equal("52998224725", cpf.ToString("N"));
        Assert.Equal(CpfFiscalRegion.EsRj, cpf.FiscalRegion);
        Assert.Equal("ES, RJ", cpf.FiscalRegion.Ufs());
        Assert.True(Cpf.IsValid(Cpf.Generate().ToString()));
    }

    // README sample: CNPJ, including the alphanumeric format.
    [Fact]
    public void Readme_02_Cnpj()
    {
        bool numeric = Cnpj.IsValid("11.222.333/0001-81");     // true
        Cnpj alpha = Cnpj.Parse("12.ABC.345/01DE-35");          // official RFB vector
        Console.WriteLine(alpha.IsAlphanumeric);                // True
        Console.WriteLine(alpha.Root);                          // 12ABC345
        Console.WriteLine(alpha.Branch);                        // 01DE
        Console.WriteLine(alpha.IsHeadOffice);                  // False
        Console.WriteLine(Cnpj.IsValid("12abc34501de35"));     // true (lowercase accepted)
        Console.WriteLine(Cnpj.GenerateAlphanumeric().IsAlphanumeric); // True

        Assert.True(numeric);
        Assert.True(alpha.IsAlphanumeric);
        Assert.Equal("12ABC345", alpha.Root);
        Assert.Equal("01DE", alpha.Branch);
        Assert.False(alpha.IsHeadOffice);
        Assert.True(Cnpj.IsValid("12abc34501de35"));
        Assert.True(Cnpj.GenerateAlphanumeric().IsAlphanumeric);
    }

    // README sample: CEP, PIS, Título de Eleitor, CNH and RENAVAM.
    [Fact]
    public void Readme_03_Other_Documents()
    {
        Console.WriteLine(Cep.Parse("01310-100").Uf);                     // SP
        Console.WriteLine(Pis.Parse("120.70458.82-4"));                   // 120.70458.82-4
        TituloEleitor titulo = TituloEleitor.Parse("1234 5678 20 97");
        Console.WriteLine(titulo.Uf);                                     // DF (UF code 20)
        Console.WriteLine(titulo.UfCode);                                 // 20
        Console.WriteLine(Cnh.Parse("026503064-61"));                     // 026503064-61
        Console.WriteLine(Renavam.Parse("639884962"));                    // 00639884962 (legacy is padded)

        Assert.Equal("SP", Cep.Parse("01310-100").Uf);
        Assert.Equal("120.70458.82-4", Pis.Parse("120.70458.82-4").ToString());
        Assert.Equal("DF", titulo.Uf);
        Assert.Equal(20, titulo.UfCode);
        Assert.Equal("026503064-61", Cnh.Parse("026503064-61").ToString());
        Assert.Equal("00639884962", Renavam.Parse("639884962").ToString());
    }

    // README sample: plates and phones.
    [Fact]
    public void Readme_04_Placa_And_Telefone()
    {
        Placa legacy = Placa.Parse("ABC-1234");
        Placa mercosul = legacy.ToMercosul();
        Console.WriteLine(mercosul);                           // ABC1C34
        Console.WriteLine(mercosul.IsMercosul);                // True

        TelefoneBr phone = TelefoneBr.Parse("(11) 91234-5678");
        Console.WriteLine(phone.Ddd);                          // 11
        Console.WriteLine(phone.IsMobile);                     // True
        Console.WriteLine(phone.ToE164());                     // +5511912345678
        Console.WriteLine(TelefoneBr.IsValid("11712345678"));  // False (invalid subscriber prefix)

        Assert.Equal("ABC1C34", mercosul.ToString());
        Assert.True(mercosul.IsMercosul);
        Assert.Equal(11, phone.Ddd);
        Assert.True(phone.IsMobile);
        Assert.Equal("+5511912345678", phone.ToE164());
        Assert.False(TelefoneBr.IsValid("11712345678"));
    }

    // README sample: boleto and arrecadação.
    [Fact]
    public void Readme_05_Boleto_And_Arrecadacao()
    {
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

        Assert.Equal(104, boleto.BankCode);
        Assert.Equal(9, boleto.CurrencyCode);
        Assert.Equal(214.03m, boleto.Amount);
        Assert.Equal("10499898100000214032006561000100040099726390", boleto.Barcode);
        Assert.Equal(boleto, Boleto.Parse(boleto.Barcode));
        Assert.True(Boleto.IsValid(created.LinhaDigitavel));
        Assert.Equal(3, conta.Segment);
        Assert.Equal(509.60m, conta.Value);
        Assert.True(conta.IsEffectiveValue);
    }

    // README sample: the due-date factor reset.
    [Fact]
    public void Readme_06_Factor_Reset()
    {
        Console.WriteLine(Boleto.Decode(9999, new DateOnly(2025, 2, 21))); // 2025-02-21
        Console.WriteLine(Boleto.Decode(1000, new DateOnly(2025, 2, 22))); // 2025-02-22
        Console.WriteLine(Boleto.Decode(1001, new DateOnly(2025, 2, 23))); // 2025-02-23
        Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 21)));       // 9999
        Console.WriteLine(Boleto.Encode(new DateOnly(2025, 2, 22)));       // 1000

        Assert.Equal(new DateOnly(2025, 2, 21), Boleto.Decode(9999, new DateOnly(2025, 2, 21)));
        Assert.Equal(new DateOnly(2025, 2, 22), Boleto.Decode(1000, new DateOnly(2025, 2, 22)));
        Assert.Equal(new DateOnly(2025, 2, 23), Boleto.Decode(1001, new DateOnly(2025, 2, 23)));
        Assert.Equal(9999, Boleto.Encode(new DateOnly(2025, 2, 21)));
        Assert.Equal(1000, Boleto.Encode(new DateOnly(2025, 2, 22)));
    }

    // README sample: static Pix BR Code.
    [Fact]
    public void Readme_07_Pix_Static()
    {
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

        Assert.True(PixPayload.IsValid(brCode));
        Assert.Equal("123e4567-e12b-12d1-a456-426655440000", parsed.Key);
        Assert.Equal(100.50m, parsed.Amount);
        Assert.Equal(4, parsed.Crc.Length);
        Assert.True(parsed.IsStatic);
        Assert.Equal("1D3D", official.Crc);
    }

    // README sample: dynamic Pix BR Code.
    [Fact]
    public void Readme_08_Pix_Dynamic()
    {
        PixPayload dynamic = PixPayload
            .CreateDynamic("pix.example.com/8b3da2f39a4140d1a91abd93113bd441", "Fulano de Tal", "BRASILIA")
            .SingleUse()
            .Build();
        Console.WriteLine(dynamic.IsDynamic);   // True
        Console.WriteLine(dynamic.IsSingleUse); // True
        Console.WriteLine(PixPayload.IsValid(dynamic.ToString())); // True

        Assert.True(dynamic.IsDynamic);
        Assert.True(dynamic.IsSingleUse);
        Assert.True(PixPayload.IsValid(dynamic.ToString()));
    }

    // README sample: Pix key detection.
    [Fact]
    public void Readme_09_Pix_Keys()
    {
        Console.WriteLine(PixKey.Detect("123e4567-e12b-12d1-a456-426655440000")); // Evp
        Console.WriteLine(PixKey.Detect("12ABC34501DE35"));                      // Cnpj
        Console.WriteLine(PixKey.IsValid("+5511912345678"));                     // True
        Console.WriteLine(PixKey.IsValid("garbage key"));                        // False

        Assert.Equal(PixKeyKind.Evp, PixKey.Detect("123e4567-e12b-12d1-a456-426655440000"));
        Assert.Equal(PixKeyKind.Cnpj, PixKey.Detect("12ABC34501DE35"));
        Assert.True(PixKey.IsValid("+5511912345678"));
        Assert.False(PixKey.IsValid("garbage key"));
    }

    // README sample: QR Code rendering.
    [Fact]
    public void Readme_10_Qr_Code()
    {
        PixPayload pix = PixPayload.Parse(
            "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D");
        string svg = pix.ToQrSvg();                 // standalone SVG document
        QrCode qr = QrCode.Encode("https://example.com");
        byte[] png = qr.ToPng();                    // PNG bytes
        Console.WriteLine(svg.StartsWith("<svg", StringComparison.Ordinal)); // True
        Console.WriteLine(png.Length > 0);          // True

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
        Assert.True(png.Length > 0);
    }

    // README sample: DataAnnotations.
    [Fact]
    public void Readme_11_DataAnnotations()
    {
        var customer = new Customer
        {
            Cpf = "111.444.777-35",
            Cnpj = "12.ABC.345/01DE-35",
            Phone = "(11) 91234-5678",
        };
        var results = new List<ValidationResult>();
        bool valid = Validator.TryValidateObject(customer, new ValidationContext(customer), results, true);
        Console.WriteLine(valid); // True

        Assert.True(valid);
        Assert.Empty(results);
    }

    // README sample: FluentValidation.
    [Fact]
    public void Readme_12_FluentValidation()
    {
        var customer = new Customer
        {
            Cpf = "111.444.777-35",
            Cnpj = "12.ABC.345/01DE-35",
            Phone = "(11) 91234-5678",
        };

        bool valid = new CustomerValidator().Validate(customer).IsValid; // customer from above
        Console.WriteLine(valid); // True

        Assert.True(valid);
    }

    // README sample: System.Text.Json.
    [Fact]
    public void Readme_13_Json()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddBrDocsConverters();
        Cpf cpf = Cpf.Parse("111.444.777-35");
        string json = JsonSerializer.Serialize(cpf, options);                  // "111.444.777-35"
        Cpf back = JsonSerializer.Deserialize<Cpf>(json, options)!;            // same value
        JsonSerializerOptions digits = new JsonSerializerOptions()
            .AddBrDocsConverters(BrDocsJsonOptions.DigitsOnly);
        string digitsJson = JsonSerializer.Serialize(cpf, digits);             // "11144477735"
        Console.WriteLine(back == cpf); // True

        Assert.Equal("\"111.444.777-35\"", json);
        Assert.Equal(cpf, back);
        Assert.Equal("\"11144477735\"", digitsJson);
    }

    /// <summary>
    /// Both READMEs are written in lockstep: if a code sample is added, a mirrored test must be
    /// added here as well. This test keeps the two languages from drifting apart.
    /// </summary>
    [Fact]
    public void Readme_Code_Blocks_Are_All_Mirrored()
    {
        const int mirroredSamples = 13;

        foreach (string fileName in new[] { "README.md", "README.pt-BR.md" })
        {
            string text = File.ReadAllText(FindRepositoryFile(fileName));
            int csharpBlocks = text.Split("```csharp", StringSplitOptions.None).Length - 1;
            Assert.Equal(mirroredSamples, csharpBlocks);
        }
    }

    private static string FindRepositoryFile(string fileName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find '{fileName}' above the test output directory.", fileName);
    }
}

/// <summary>The model used by the DataAnnotations and FluentValidation README samples.</summary>
public sealed class Customer
{
    [Cpf]
    public string? Cpf { get; set; }

    [Cnpj(AllowAlphanumeric = true)]
    public string? Cnpj { get; set; }

    [TelefoneBr(MobileOnly = true)]
    public string? Phone { get; set; }
}

/// <summary>The validator used by the FluentValidation README sample.</summary>
public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Cpf).MustBeCpf();
        RuleFor(x => x.Cnpj).MustBeCnpj(allowAlphanumeric: true);
        RuleFor(x => x.Phone).MustBeTelefoneBr(mobileOnly: true);
    }
}
