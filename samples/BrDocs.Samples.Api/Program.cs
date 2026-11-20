using System.Globalization;
using BrDocs;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "BrDocs sample API — POST to /validate/{cpf|cnpj|cep} with JSON {\"value\": \"...\"}, or /pix/{static|dynamic|parse} for Pix BR Codes");

app.MapPost("/validate/cpf", (ValidationRequest request) =>
{
    if (!Cpf.TryParse(request.Value, out var cpf))
    {
        return Results.Ok(new CpfResponse(false, null, null, null));
    }
    return Results.Ok(new CpfResponse(true, cpf.ToString(), cpf.ToString("N", CultureInfo.InvariantCulture), cpf.FiscalRegion.Ufs()));
});

app.MapPost("/validate/cnpj", (ValidationRequest request) =>
{
    if (!Cnpj.TryParse(request.Value, out var cnpj))
    {
        return Results.Ok(new CnpjResponse(false, null, null, null, false, false));
    }
    return Results.Ok(new CnpjResponse(true, cnpj.ToString(), cnpj.Root, cnpj.Branch, cnpj.IsAlphanumeric, cnpj.IsHeadOffice));
});

app.MapPost("/validate/cep", (ValidationRequest request) =>
{
    if (!Cep.TryParse(request.Value, out var cep))
    {
        return Results.Ok(new CepResponse(false, null, null));
    }
    return Results.Ok(new CepResponse(true, cep.ToString(), cep.Uf));
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
    try
    {
        PixPayloadBuilder builder = PixPayload.CreateStatic(request.Key, request.Name, request.City);
        if (request.Txid is { } txid)
        {
            builder = builder.WithTxid(txid);
        }
        if (request.Amount is { } amount)
        {
            builder = builder.WithAmount(amount);
        }
        string brCode = builder.Build().ToString();
        return Results.Ok(new PixResponse(true, brCode, "Render the BR Code with any QR library, e.g. QRCoder's CreateQrCode(payload, ECCLevel.M)."));
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
    {
        return Results.BadRequest(new PixResponse(false, null, exception.Message));
    }
});

app.MapPost("/pix/dynamic", (PixDynamicRequest request) =>
{
    try
    {
        string brCode = PixPayload.CreateDynamic(request.Location, request.Name, request.City)
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

app.Run();

internal sealed record ValidationRequest(string Value);

internal sealed record CpfResponse(bool Valid, string? Formatted, string? Digits, string? Regions);

internal sealed record CnpjResponse(bool Valid, string? Formatted, string? Root, string? Branch, bool IsAlphanumeric, bool IsHeadOffice);

internal sealed record CepResponse(bool Valid, string? Formatted, string? Uf);

internal sealed record PixStaticRequest(string Key, string Name, string City, string? Txid, decimal? Amount);

internal sealed record PixDynamicRequest(string Location, string Name, string City, bool SingleUse);

internal sealed record PixParseRequest(string Payload);

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
