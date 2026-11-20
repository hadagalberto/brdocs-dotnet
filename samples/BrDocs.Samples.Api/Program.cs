using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using BrDocs;
using BrDocs.FluentValidation;
using BrDocs.Json;
using BrDocs.QrCode;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Converters are registered once. Reading always accepts formatted or unformatted input;
// writing uses the presentation format here (use BrDocsJsonOptions.DigitsOnly to change it).
JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    .AddBrDocsConverters(BrDocsJsonOptions.Default);

PixSvgRequestValidator pixValidator = new();

app.MapGet("/", () => "BrDocs sample API — POST /validate/{cpf|cnpj|cep} with {\"value\":\"...\"}, POST /pix for an SVG QR Code, GET /pix/demo for a ready-made one.");

app.MapPost("/validate/cpf", (CpfRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    if (!Cpf.TryParse(request.Value!, out Cpf cpf))
    {
        return Results.Ok(new CpfResponse(false, null, null, null));
    }
    return Results.Ok(new CpfResponse(true, cpf.ToString(), cpf.ToString("N", CultureInfo.InvariantCulture), cpf.FiscalRegion.Ufs()));
});

app.MapPost("/validate/cnpj", (CnpjRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    if (!Cnpj.TryParse(request.Value!, out Cnpj cnpj))
    {
        return Results.Ok(new CnpjResponse(false, null, null, null, false, false));
    }
    return Results.Ok(new CnpjResponse(true, cnpj.ToString(), cnpj.Root, cnpj.Branch, cnpj.IsAlphanumeric, cnpj.IsHeadOffice));
});

app.MapPost("/validate/cep", (CepRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    if (!Cep.TryParse(request.Value!, out Cep cep))
    {
        return Results.Ok(new CepResponse(false, null, null));
    }
    return Results.Ok(new CepResponse(true, cep.ToString(), cep.Uf));
});

app.MapPost("/json/cpf", (CpfRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    Cpf cpf = Cpf.Parse(request.Value!);
    return Results.Json(new CpfDocument(cpf), jsonOptions);
});

app.MapGet("/generate/{document}", (string document) => document switch
{
    "cpf" => Results.Ok(Cpf.Generate().ToString()),
    "cnpj" => Results.Ok(Cnpj.Generate().ToString()),
    "cnpj-alphanumeric" => Results.Ok(Cnpj.GenerateAlphanumeric().ToString()),
    "cep" => Results.Ok(Cep.Generate().ToString()),
    "pix-key-evp" => Results.Ok(Guid.NewGuid().ToString()),
    _ => Results.NotFound($"Unknown document type '{document}'."),
});

app.MapPost("/pix/static", (PixStaticRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    try
    {
        PixPayloadBuilder builder = PixPayload.CreateStatic(request.Key!, request.Name!, request.City!);
        if (request.Txid is { } txid)
        {
            builder = builder.WithTxid(txid);
        }
        if (request.Amount is { } amount)
        {
            builder = builder.WithAmount(amount);
        }
        string brCode = builder.Build().ToString();
        return Results.Ok(new PixResponse(true, brCode, "POST /pix to render this BR Code as an SVG QR Code."));
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
    {
        return Results.BadRequest(new PixResponse(false, null, exception.Message));
    }
});

app.MapPost("/pix/dynamic", (PixDynamicRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    try
    {
        string brCode = PixPayload.CreateDynamic(request.Location!, request.Name!, request.City!)
            .SingleUse(request.SingleUse)
            .Build()
            .ToString();
        return Results.Ok(new PixResponse(true, brCode, "The amount and the charge details come from the location URL."));
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
    {
        return Results.BadRequest(new PixResponse(false, null, exception.Message));
    }
});

app.MapPost("/pix/parse", (PixParseRequest request) =>
{
    if (Validate(request) is { } error)
    {
        return error;
    }
    if (!PixPayload.TryParse(request.Payload, out PixPayload? payload) || payload is null)
    {
        return Results.Ok(new PixParsedResponse(false, null, null, null, null, null, null, null, null));
    }
    return Results.Ok(new PixParsedResponse(
        true,
        payload.Key,
        payload.Location,
        payload.Amount,
        payload.Name,
        payload.City,
        payload.Txid,
        payload.IsSingleUse,
        payload.ToString()));
});

// Renders the Pix BR Code as a standalone SVG QR Code (ISO/IEC 18004, byte mode, ECC M).
app.MapPost("/pix", (PixSvgRequest request) =>
{
    FluentValidation.Results.ValidationResult validation = pixValidator.Validate(request);
    if (!validation.IsValid)
    {
        return Results.BadRequest(new ValidationErrorResponse(
            validation.Errors.Select(e => e.ErrorMessage).ToArray()));
    }
    try
    {
        PixPayloadBuilder builder = PixPayload.CreateStatic(request.Key!, request.Name!, request.City!);
        if (request.Txid is { } txid)
        {
            builder = builder.WithTxid(txid);
        }
        if (request.Amount is { } amount)
        {
            builder = builder.WithAmount(amount);
        }
        string svg = builder.Build().ToQrSvg();
        return Results.Content(svg, "image/svg+xml");
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
    {
        return Results.BadRequest(new ValidationErrorResponse(new[] { exception.Message }));
    }
});

app.MapGet("/pix/demo", () =>
{
    PixPayload payload = PixPayload.CreateStatic("123e4567-e12b-12d1-a456-426655440000", "Fulano de Tal", "BRASILIA")
        .Build();
    return Results.Content(payload.ToQrSvg(), "image/svg+xml");
});

app.Run();

// Uses System.ComponentModel.DataAnnotations with the BrDocs pt-BR attributes. Null is accepted
// by the BrDocs attributes (presence is handled here by [Required]).
static IResult? Validate(object model)
{
    var results = new List<ValidationResult>();
    if (Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
    {
        return null;
    }
    return Results.BadRequest(new ValidationErrorResponse(
        results.Select(result => result.ErrorMessage ?? "Erro de validação.").ToArray()));
}

internal sealed record CpfRequest([property: Required, BrDocs.DataAnnotations.Cpf] string? Value);

internal sealed record CnpjRequest([property: Required, BrDocs.DataAnnotations.Cnpj(AllowAlphanumeric = true)] string? Value);

internal sealed record CepRequest([property: Required, BrDocs.DataAnnotations.Cep] string? Value);

internal sealed record PixStaticRequest(
    [property: Required, BrDocs.DataAnnotations.PixKey] string? Key,
    [property: Required, StringLength(25)] string? Name,
    [property: Required, StringLength(15)] string? City,
    string? Txid,
    decimal? Amount);

internal sealed record PixDynamicRequest(
    [property: Required] string? Location,
    [property: Required, StringLength(25)] string? Name,
    [property: Required, StringLength(15)] string? City,
    bool SingleUse);

internal sealed record PixParseRequest([property: Required] string? Payload);

internal sealed record PixSvgRequest(string? Key, string? Name, string? City, string? Txid, decimal? Amount);

internal sealed record CpfDocument(Cpf Cpf);

internal sealed record CpfResponse(bool Valid, string? Formatted, string? Digits, string? Regions);

internal sealed record CnpjResponse(bool Valid, string? Formatted, string? Root, string? Branch, bool IsAlphanumeric, bool IsHeadOffice);

internal sealed record CepResponse(bool Valid, string? Formatted, string? Uf);

internal sealed record PixResponse(bool Valid, string? BrCode, string? Hint);

internal sealed record PixParsedResponse(
    bool Valid,
    string? Key,
    string? Location,
    decimal? Amount,
    string? Name,
    string? City,
    string? Txid,
    bool? IsSingleUse,
    string? BrCode);

internal sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);

internal sealed class PixSvgRequestValidator : AbstractValidator<PixSvgRequest>
{
    public PixSvgRequestValidator()
    {
        RuleFor(request => request.Key).NotEmpty().MustBePixKey();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(25);
        RuleFor(request => request.City).NotEmpty().MaximumLength(15);
        RuleFor(request => request.Txid).Matches("^[A-Za-z0-9]{1,25}$").When(request => request.Txid is not null);
    }
}
