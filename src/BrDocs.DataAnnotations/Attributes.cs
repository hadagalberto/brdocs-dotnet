using System.Globalization;

namespace BrDocs.DataAnnotations;

/// <summary>Validates that the value is a CPF (Cadastro de Pessoas Físicas).</summary>
/// <remarks>Formatted and digits-only values are accepted; reserved all-same-digit sequences are rejected.</remarks>
public sealed class CpfAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public CpfAttribute() : base(nameof(ValidationMessages.CpfInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Cpf.IsValid(value);
}

/// <summary>Validates that the value is a CNPJ.</summary>
/// <remarks>
/// Numeric CNPJs are always accepted. The alphanumeric format of IN RFB nº 2.229/2024 is accepted
/// only when <see cref="AllowAlphanumeric"/> is <see langword="true"/>.
/// </remarks>
public sealed class CnpjAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public CnpjAttribute() : base(nameof(ValidationMessages.CnpjInvalid))
    {
    }

    /// <summary>
    /// Whether the alphanumeric CNPJ format is accepted. Defaults to <see langword="false"/>
    /// (numeric CNPJs only); set it to <see langword="true"/> to also accept alphanumeric CNPJs.
    /// </summary>
    public bool AllowAlphanumeric { get; set; }

    // Why the last validation failed, used only to pick an accurate error message. Null (no
    // validation yet) preserves the option-specific default; false means an invalid CNPJ,
    // true means a valid alphanumeric CNPJ refused because AllowAlphanumeric is false.
    private bool? _rejectedAlphanumeric;

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value)
    {
        if (!Cnpj.TryParse(value, out Cnpj cnpj))
        {
            // 11.222.333/0001-82 has a wrong DV: this is an invalid CNPJ, not a format refusal.
            _rejectedAlphanumeric = false;
            return false;
        }
        _rejectedAlphanumeric = cnpj.IsAlphanumeric && !AllowAlphanumeric;
        return !_rejectedAlphanumeric.Value;
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        if (ErrorMessage is not null)
        {
            return base.FormatErrorMessage(name);
        }
        string key = AllowAlphanumeric || _rejectedAlphanumeric == false
            ? nameof(ValidationMessages.CnpjInvalid)
            : nameof(ValidationMessages.CnpjAlphanumericNotAllowed);
        return string.Format(CultureInfo.CurrentCulture, ValidationMessages.Get(key), name);
    }
}

/// <summary>Validates that the value is either a CPF or a CNPJ (including alphanumeric CNPJs).</summary>
public sealed class CpfOrCnpjAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public CpfOrCnpjAttribute() : base(nameof(ValidationMessages.CpfOrCnpjInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Cpf.IsValid(value) || Cnpj.IsValid(value);
}

/// <summary>Validates that the value is a structurally valid CEP (8 digits, formatted or not).</summary>
public sealed class CepAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public CepAttribute() : base(nameof(ValidationMessages.CepInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Cep.IsValid(value);
}

/// <summary>Validates that the value is a PIS/PASEP/NIT.</summary>
public sealed class PisAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public PisAttribute() : base(nameof(ValidationMessages.PisInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Pis.IsValid(value);
}

/// <summary>Validates that the value is a Título de Eleitor.</summary>
public sealed class TituloEleitorAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public TituloEleitorAttribute() : base(nameof(ValidationMessages.TituloEleitorInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => TituloEleitor.IsValid(value);
}

/// <summary>Validates that the value is a CNH registration number.</summary>
public sealed class CnhAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public CnhAttribute() : base(nameof(ValidationMessages.CnhInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Cnh.IsValid(value);
}

/// <summary>Validates that the value is a RENAVAM (9-digit legacy or 11-digit canonical form).</summary>
public sealed class RenavamAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public RenavamAttribute() : base(nameof(ValidationMessages.RenavamInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Renavam.IsValid(value);
}

/// <summary>Validates that the value is a vehicle plate in legacy or Mercosul format.</summary>
public sealed class PlacaAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public PlacaAttribute() : base(nameof(ValidationMessages.PlacaInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => Placa.IsValid(value);
}

/// <summary>Validates that the value is a Brazilian geographic phone number.</summary>
public sealed class TelefoneBrAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public TelefoneBrAttribute() : base(nameof(ValidationMessages.TelefoneBrInvalid))
    {
    }

    /// <summary>
    /// When <see langword="true"/>, only 9-digit mobile numbers are accepted. Defaults to
    /// <see langword="false"/>, which also accepts 8-digit fixed lines.
    /// </summary>
    public bool MobileOnly { get; set; }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value)
        => TelefoneBr.TryParse(value, out TelefoneBr phone) && (!MobileOnly || phone.IsMobile);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        if (ErrorMessage is not null)
        {
            return base.FormatErrorMessage(name);
        }
        string key = MobileOnly
            ? nameof(ValidationMessages.TelefoneBrMobileOnly)
            : nameof(ValidationMessages.TelefoneBrInvalid);
        return string.Format(CultureInfo.CurrentCulture, ValidationMessages.Get(key), name);
    }
}

/// <summary>Validates that the value is a canonical Pix key (CPF, CNPJ, e-mail, phone or EVP).</summary>
public sealed class PixKeyAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public PixKeyAttribute() : base(nameof(ValidationMessages.PixKeyInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value) => PixKey.IsValid(value);
}

/// <summary>
/// Validates that the value is a FEBRABAN linha digitável: a 47-digit bank-slip line or a
/// 48-digit arrecadação/convênio line. A 44-digit código de barras is deliberately rejected.
/// </summary>
public sealed class LinhaDigitavelAttribute : BrDocsValidationAttribute
{
    /// <summary>Creates the attribute with the default pt-BR message.</summary>
    public LinhaDigitavelAttribute() : base(nameof(ValidationMessages.LinhaDigitavelInvalid))
    {
    }

    /// <inheritdoc />
    protected override bool IsValidText(ReadOnlySpan<char> value)
    {
        int digits = 0;
        foreach (char c in value)
        {
            if (c is '.' or '-' or '/' or ' ')
            {
                continue;
            }
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
            digits++;
        }
        return digits switch
        {
            47 => Boleto.IsValid(value),
            48 => Arrecadacao.IsValid(value),
            _ => false,
        };
    }
}
