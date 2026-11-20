using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BrDocs.QrCode;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using QrCodeType = BrDocs.QrCode.QrCode;

namespace BrDocs.Samples.Api.Tests;

/// <summary>
/// Hosts the real sample API in-process with <see cref="WebApplicationFactory{TEntryPoint}"/> and
/// exercises its endpoints over HTTP. The SVG returned by <c>POST /pix</c> is parsed back into a
/// module matrix and compared with <see cref="QrCodeType.Encode"/> of the BR Code the API itself
/// produced, so the test fails if the endpoint renders another payload or a broken symbol.
/// </summary>
public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidKey = "11144477735";
    private const string ValidName = "Fulano de Tal";
    private const string ValidCity = "BRASILIA";
    private const int DefaultQuietZone = 4;

    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Root_Describes_The_Available_Endpoints()
    {
        HttpClient client = _factory.CreateClient();

        string body = await client.GetStringAsync("/");

        Assert.Contains("POST /validate/{cpf|cnpj|cep}", body, StringComparison.Ordinal);
        Assert.Contains("POST /pix", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("111.444.777-35", true)]
    [InlineData("11144477735", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("11144477736", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public async Task Validate_Cpf_Uses_The_DataAnnotations_Attributes(string value, bool valid)
    {
        HttpClient client = _factory.CreateClient();
        using HttpContent content = JsonContent.Create(new { value });

        HttpResponseMessage response = await client.PostAsync("/validate/cpf", content);

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (valid)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(json.RootElement.GetProperty("valid").GetBoolean());
            Assert.Equal("111.444.777-35", json.RootElement.GetProperty("formatted").GetString());
            Assert.Equal("11144477735", json.RootElement.GetProperty("digits").GetString());
        }
        else
        {
            // Minimal APIs enforce DataAnnotations before invoking the handler: the response is
            // a 400 with the pt-BR attribute message, not a 200 with valid=false.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string[] errors = json.RootElement.GetProperty("errors").EnumerateArray()
                .Select(item => item.GetString()!).ToArray();
            Assert.NotEmpty(errors);
            if (value.Length > 0)
            {
                Assert.Contains(errors, message => message.Contains("CPF", StringComparison.Ordinal));
            }
        }
    }

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("12.ABC.345/01DE-35", true)]
    [InlineData("11.222.333/0001-82", false)]
    public async Task Validate_Cnpj_Accepts_The_Alphanumeric_Format(string value, bool valid)
    {
        HttpClient client = _factory.CreateClient();
        using HttpContent content = JsonContent.Create(new { value });

        HttpResponseMessage response = await client.PostAsync("/validate/cnpj", content);

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (valid)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(json.RootElement.GetProperty("valid").GetBoolean());
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(json.RootElement.GetProperty("errors").GetArrayLength() > 0);
        }
    }

    [Fact]
    public async Task Pix_Static_Endpoint_Produces_A_BrCode_That_Parses()
    {
        HttpClient client = _factory.CreateClient();
        using HttpContent content = JsonContent.Create(new
        {
            key = ValidKey,
            name = ValidName,
            city = ValidCity,
            txid = "PEDIDO123",
            amount = 123.45m,
        });

        HttpResponseMessage response = await client.PostAsync("/pix/static", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string brCode = json.RootElement.GetProperty("brCode").GetString()!;
        PixPayload payload = PixPayload.Parse(brCode);
        Assert.Equal(ValidKey, payload.Key);
        Assert.Equal(123.45m, payload.Amount);
        Assert.Equal("PEDIDO123", payload.Txid);
    }

    [Fact]
    public async Task Pix_Endpoint_Returns_An_Svg_Qr_Whose_Matrix_Matches_QrCode_Encode()
    {
        HttpClient client = _factory.CreateClient();
        object request = new { key = ValidKey, name = ValidName, city = ValidCity, txid = "PEDIDO123" };

        // Ask the API itself for the canonical BR Code so the comparison uses the exact payload
        // the /pix endpoint renders, not a locally reconstructed one.
        using (HttpContent staticContent = JsonContent.Create(request))
        using (HttpResponseMessage staticResponse = await client.PostAsync("/pix/static", staticContent))
        {
            Assert.Equal(HttpStatusCode.OK, staticResponse.StatusCode);
            using JsonDocument json = JsonDocument.Parse(await staticResponse.Content.ReadAsStringAsync());
            string brCode = json.RootElement.GetProperty("brCode").GetString()!;
            Assert.True(PixPayload.TryParse(brCode, out PixPayload? payload));
            Assert.NotNull(payload);

            using HttpContent svgContent = JsonContent.Create(request);
            HttpResponseMessage svgResponse = await client.PostAsync("/pix", svgContent);

            Assert.Equal(HttpStatusCode.OK, svgResponse.StatusCode);
            Assert.Equal("image/svg+xml", svgResponse.Content.Headers.ContentType!.MediaType);
            string svg = await svgResponse.Content.ReadAsStringAsync();

            QrCodeType expected = QrCodeType.Encode(brCode);
            Assert.Equal(payload.ToQrSvg(), svg);
            AssertSvgMatchesQrCode(svg, expected);
        }
    }

    [Fact]
    public async Task Pix_Demo_Endpoint_Returns_A_Decodable_Svg()
    {
        HttpClient client = _factory.CreateClient();

        string svg = await client.GetStringAsync("/pix/demo");

        const string demoKey = "123e4567-e12b-12d1-a456-426655440000";
        PixPayload payload = PixPayload.CreateStatic(demoKey, ValidName, ValidCity).Build();
        AssertSvgMatchesQrCode(svg, QrCodeType.Encode(payload.ToString()));
    }

    [Theory]
    [InlineData("111.444.777-35", "Fulano de Tal", "BRASILIA", null)]      // key must be canonical
    [InlineData("11144477736", "Fulano de Tal", "BRASILIA", null)]         // wrong check digit
    [InlineData("11144477735", "", "BRASILIA", null)]                      // name required
    [InlineData("11144477735", "Fulano de Tal", "", null)]                 // city required
    [InlineData("11144477735", "Fulano de Tal", "BRASILIA", "bad txid!")]  // txid charset
    public async Task Pix_Endpoint_Rejects_Invalid_Payloads(string key, string name, string city, string? txid)
    {
        HttpClient client = _factory.CreateClient();
        using HttpContent content = JsonContent.Create(new { key, name, city, txid });

        HttpResponseMessage response = await client.PostAsync("/pix", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }

    /// <summary>Reconstructs the symbol from the SVG path and compares it, module by module, with the encoder output.</summary>
    private static void AssertSvgMatchesQrCode(string svg, QrCodeType expected)
    {
        int side = expected.Size + (2 * DefaultQuietZone);
        Assert.Contains($"width=\"{side.ToString(CultureInfo.InvariantCulture)}px\"", svg, StringComparison.Ordinal);
        Assert.Contains($"viewBox=\"0 0 {side.ToString(CultureInfo.InvariantCulture)} {side.ToString(CultureInfo.InvariantCulture)}\"", svg, StringComparison.Ordinal);

        var actual = new bool[side, side];
        const string modulePattern = @"M(\d+) (\d+)h1v1h-1z";
        foreach (Match match in Regex.Matches(svg, modulePattern, RegexOptions.CultureInvariant))
        {
            int x = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            int y = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            Assert.InRange(x, 0, side - 1);
            Assert.InRange(y, 0, side - 1);
            Assert.False(actual[y, x], $"Duplicate dark module at ({x}, {y}).");
            actual[y, x] = true;
        }

        bool[,] matrix = expected.Matrix;
        for (int row = 0; row < expected.Size; row++)
        {
            for (int col = 0; col < expected.Size; col++)
            {
                Assert.Equal(matrix[row, col], actual[row + DefaultQuietZone, col + DefaultQuietZone]);
            }
        }

        // No dark modules may appear inside the four-module quiet zone.
        for (int i = 0; i < side; i++)
        {
            for (int quiet = 0; quiet < DefaultQuietZone; quiet++)
            {
                Assert.False(actual[i, quiet]);
                Assert.False(actual[quiet, i]);
                Assert.False(actual[i, side - 1 - quiet]);
                Assert.False(actual[side - 1 - quiet, i]);
            }
        }
    }
}
