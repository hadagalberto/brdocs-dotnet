using System.Globalization;
using System.Resources;

namespace BrDocs.FluentValidation;

/// <summary>
/// pt-BR message templates used by the BrDocs FluentValidation rules. FluentValidation replaces
/// the <c>{PropertyName}</c> placeholder when a failure is formatted.
/// </summary>
/// <remarks>
/// The strings are the neutral resource of this assembly, so they remain pt-BR under any UI
/// culture. Call <c>WithMessage(...)</c> on a rule to override an individual message.
/// </remarks>
public static class ValidationMessages
{
    private const string ResourceBaseName = "BrDocs.FluentValidation.ValidationMessages";

    private static readonly Dictionary<string, string> s_overrides = new(StringComparer.Ordinal);

    /// <summary>The resource manager that backs the pt-BR messages.</summary>
    public static ResourceManager ResourceManager { get; } = new(ResourceBaseName, typeof(ValidationMessages).Assembly);

    /// <summary>CPF message.</summary>
    public static string CpfInvalid => Get(nameof(CpfInvalid));

    /// <summary>CNPJ message.</summary>
    public static string CnpjInvalid => Get(nameof(CnpjInvalid));

    /// <summary>CNPJ (numeric only) message.</summary>
    public static string CnpjAlphanumericNotAllowed => Get(nameof(CnpjAlphanumericNotAllowed));

    /// <summary>CPF-or-CNPJ message.</summary>
    public static string CpfOrCnpjInvalid => Get(nameof(CpfOrCnpjInvalid));

    /// <summary>CEP message.</summary>
    public static string CepInvalid => Get(nameof(CepInvalid));

    /// <summary>PIS message.</summary>
    public static string PisInvalid => Get(nameof(PisInvalid));

    /// <summary>Título de Eleitor message.</summary>
    public static string TituloEleitorInvalid => Get(nameof(TituloEleitorInvalid));

    /// <summary>CNH message.</summary>
    public static string CnhInvalid => Get(nameof(CnhInvalid));

    /// <summary>RENAVAM message.</summary>
    public static string RenavamInvalid => Get(nameof(RenavamInvalid));

    /// <summary>Placa message.</summary>
    public static string PlacaInvalid => Get(nameof(PlacaInvalid));

    /// <summary>Telefone BR message.</summary>
    public static string TelefoneBrInvalid => Get(nameof(TelefoneBrInvalid));

    /// <summary>Telefone BR (mobile only) message.</summary>
    public static string TelefoneBrMobileOnly => Get(nameof(TelefoneBrMobileOnly));

    /// <summary>Pix key message.</summary>
    public static string PixKeyInvalid => Get(nameof(PixKeyInvalid));

    /// <summary>Linha digitável message.</summary>
    public static string LinhaDigitavelInvalid => Get(nameof(LinhaDigitavelInvalid));

    /// <summary>Returns the message template registered under <paramref name="key"/>.</summary>
    /// <param name="key">One of the resource keys.</param>
    /// <returns>The message template.</returns>
    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        lock (s_overrides)
        {
            if (s_overrides.TryGetValue(key, out string? value))
            {
                return value;
            }
        }
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    /// <summary>Replaces a message globally, or restores the embedded pt-BR resource when <paramref name="message"/> is <see langword="null"/>.</summary>
    /// <param name="key">The resource key to override.</param>
    /// <param name="message">The replacement template.</param>
    public static void SetMessage(string key, string? message)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        lock (s_overrides)
        {
            if (message is null)
            {
                s_overrides.Remove(key);
            }
            else
            {
                s_overrides[key] = message;
            }
        }
    }
}
