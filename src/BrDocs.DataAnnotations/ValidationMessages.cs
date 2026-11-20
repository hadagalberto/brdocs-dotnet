using System.Globalization;
using System.Resources;

namespace BrDocs.DataAnnotations;

/// <summary>
/// pt-BR error messages used by the BrDocs validation attributes.
/// </summary>
/// <remarks>
/// <para>
/// The strings live in the embedded <c>ValidationMessages.resx</c> resource and are the neutral
/// resource of this assembly (see <see cref="NeutralResourcesLanguageAttribute"/>), so they are
/// returned in pt-BR regardless of the thread's current UI culture.
/// </para>
/// <para>
/// Consumers may override a message in three ways: set the <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessage"/>
/// property on an individual attribute, supply a culture-specific satellite resource named
/// <c>BrDocs.DataAnnotations.ValidationMessages</c>, or replace a string at runtime with
/// <see cref="SetMessage"/>.
/// </para>
/// </remarks>
public static class ValidationMessages
{
    private const string ResourceBaseName = "BrDocs.DataAnnotations.ValidationMessages";

    private static readonly Dictionary<string, string> s_overrides = new(StringComparer.Ordinal);

    /// <summary>The resource manager that backs the pt-BR messages.</summary>
    public static ResourceManager ResourceManager { get; } = new(ResourceBaseName, typeof(ValidationMessages).Assembly);

    /// <summary>CPF message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CpfInvalid => Get(nameof(CpfInvalid));

    /// <summary>CNPJ message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CnpjInvalid => Get(nameof(CnpjInvalid));

    /// <summary>CNPJ (numeric only) message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CnpjAlphanumericNotAllowed => Get(nameof(CnpjAlphanumericNotAllowed));

    /// <summary>CPF-or-CNPJ message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CpfOrCnpjInvalid => Get(nameof(CpfOrCnpjInvalid));

    /// <summary>CEP message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CepInvalid => Get(nameof(CepInvalid));

    /// <summary>PIS message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string PisInvalid => Get(nameof(PisInvalid));

    /// <summary>Título de Eleitor message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string TituloEleitorInvalid => Get(nameof(TituloEleitorInvalid));

    /// <summary>CNH message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string CnhInvalid => Get(nameof(CnhInvalid));

    /// <summary>RENAVAM message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string RenavamInvalid => Get(nameof(RenavamInvalid));

    /// <summary>Placa message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string PlacaInvalid => Get(nameof(PlacaInvalid));

    /// <summary>Telefone BR message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string TelefoneBrInvalid => Get(nameof(TelefoneBrInvalid));

    /// <summary>Telefone BR (mobile only) message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string TelefoneBrMobileOnly => Get(nameof(TelefoneBrMobileOnly));

    /// <summary>Pix key message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string PixKeyInvalid => Get(nameof(PixKeyInvalid));

    /// <summary>Linha digitável message. The <c>{0}</c> placeholder receives the member name.</summary>
    public static string LinhaDigitavelInvalid => Get(nameof(LinhaDigitavelInvalid));

    /// <summary>
    /// Returns the message registered under <paramref name="key"/>: an explicit
    /// <see cref="SetMessage"/> override first, then the resource value, then the key itself.
    /// </summary>
    /// <param name="key">One of the resource keys (a <see langword="nameof"/> value above).</param>
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

    /// <summary>
    /// Replaces a message globally with a custom template. Pass <see langword="null"/> to restore
    /// the embedded pt-BR resource. The <c>{0}</c> placeholder receives the member name.
    /// </summary>
    /// <param name="key">The resource key to override.</param>
    /// <param name="message">The replacement template, or <see langword="null"/> to reset it.</param>
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
