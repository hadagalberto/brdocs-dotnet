using FluentValidation;

namespace BrDocs.FluentValidation;

/// <summary>
/// FluentValidation rules for Brazilian documents and payment codes. Every rule delegates to the
/// corresponding <c>BrDocs</c> value object, accepts both formatted and unformatted input and
/// treats <see langword="null"/> as valid (presence is the concern of <c>NotNull</c>/<c>NotEmpty</c>).
/// </summary>
public static class BrDocsRuleExtensions
{
    /// <summary>Requires a valid CPF.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeCpf<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Cpf.IsValid(value))
            .WithMessage(ValidationMessages.CpfInvalid);
    }

    /// <summary>Requires a valid CNPJ.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="allowAlphanumeric">
    /// Whether the alphanumeric format of IN RFB nº 2.229/2024 is accepted. Defaults to
    /// <see langword="false"/>.
    /// </param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeCnpj<T>(this IRuleBuilder<T, string?> builder, bool allowAlphanumeric = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        string message = allowAlphanumeric
            ? ValidationMessages.CnpjInvalid
            : ValidationMessages.CnpjAlphanumericNotAllowed;
        return builder
            .Must((_, value) => value is null
                || (Cnpj.TryParse(value, out Cnpj cnpj) && (allowAlphanumeric || !cnpj.IsAlphanumeric)))
            .WithMessage(message);
    }

    /// <summary>Requires a valid CPF or CNPJ (alphanumeric CNPJs included).</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeCpfOrCnpj<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Cpf.IsValid(value) || Cnpj.IsValid(value))
            .WithMessage(ValidationMessages.CpfOrCnpjInvalid);
    }

    /// <summary>Requires a structurally valid CEP.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeCep<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Cep.IsValid(value))
            .WithMessage(ValidationMessages.CepInvalid);
    }

    /// <summary>Requires a valid PIS/PASEP/NIT.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBePis<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Pis.IsValid(value))
            .WithMessage(ValidationMessages.PisInvalid);
    }

    /// <summary>Requires a valid Título de Eleitor.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeTituloEleitor<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || TituloEleitor.IsValid(value))
            .WithMessage(ValidationMessages.TituloEleitorInvalid);
    }

    /// <summary>Requires a valid CNH registration number.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeCnh<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Cnh.IsValid(value))
            .WithMessage(ValidationMessages.CnhInvalid);
    }

    /// <summary>Requires a valid RENAVAM (9- or 11-digit form).</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeRenavam<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Renavam.IsValid(value))
            .WithMessage(ValidationMessages.RenavamInvalid);
    }

    /// <summary>Requires a valid vehicle plate (legacy or Mercosul).</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBePlaca<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || Placa.IsValid(value))
            .WithMessage(ValidationMessages.PlacaInvalid);
    }

    /// <summary>Requires a valid Brazilian phone number.</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="mobileOnly">
    /// When <see langword="true"/>, only 9-digit mobile numbers are accepted. Defaults to
    /// <see langword="false"/>.
    /// </param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeTelefoneBr<T>(this IRuleBuilder<T, string?> builder, bool mobileOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        string message = mobileOnly
            ? ValidationMessages.TelefoneBrMobileOnly
            : ValidationMessages.TelefoneBrInvalid;
        return builder
            .Must((_, value) => value is null
                || (TelefoneBr.TryParse(value, out TelefoneBr phone) && (!mobileOnly || phone.IsMobile)))
            .WithMessage(message);
    }

    /// <summary>Requires a canonical Pix key (CPF, CNPJ, e-mail, +55 phone or UUID).</summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBePixKey<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || PixKey.IsValid(value))
            .WithMessage(ValidationMessages.PixKeyInvalid);
    }

    /// <summary>
    /// Requires a FEBRABAN linha digitável: a 47-digit bank-slip line or a 48-digit
    /// arrecadação/convênio line. A 44-digit código de barras is rejected.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The configured rule.</returns>
    public static IRuleBuilderOptions<T, string?> MustBeLinhaDigitavel<T>(this IRuleBuilder<T, string?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .Must((_, value) => value is null || IsLinhaDigitavel(value))
            .WithMessage(ValidationMessages.LinhaDigitavelInvalid);
    }

    private static bool IsLinhaDigitavel(string value)
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
