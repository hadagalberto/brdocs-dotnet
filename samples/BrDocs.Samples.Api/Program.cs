using System.Globalization;
using BrDocs;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "BrDocs sample API — POST to /validate/{cpf|cnpj|cep} with JSON {\"value\": \"...\"}");

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
    _ => Results.NotFound($"Unknown document type '{document}'."),
});

app.Run();

internal sealed record ValidationRequest(string Value);

internal sealed record CpfResponse(bool Valid, string? Formatted, string? Digits, string? Regions);

internal sealed record CnpjResponse(bool Valid, string? Formatted, string? Root, string? Branch, bool IsAlphanumeric, bool IsHeadOffice);

internal sealed record CepResponse(bool Valid, string? Formatted, string? Uf);
