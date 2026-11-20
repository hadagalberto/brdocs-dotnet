namespace BrDocs.Json;

/// <summary>
/// Options shared by the BrDocs System.Text.Json converters.
/// </summary>
public sealed class BrDocsJsonOptions
{
    /// <summary>
    /// When <see langword="true"/> (the default), values are written with their standard
    /// presentation punctuation; when <see langword="false"/>, only the significant
    /// characters (digits, or digits and letters for alphanumeric CNPJ/plates) are written.
    /// Reading always accepts both forms.
    /// </summary>
    public bool WriteFormatted { get; init; } = true;

    /// <summary>Default options: formatted output.</summary>
    public static BrDocsJsonOptions Default { get; } = new();

    /// <summary>Options for digits-only (unformatted) output.</summary>
    public static BrDocsJsonOptions DigitsOnly { get; } = new() { WriteFormatted = false };
}
