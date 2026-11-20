using System.Text.Json;

namespace BrDocs.Json;

/// <summary>Helpers for registering the BrDocs converters on a <see cref="JsonSerializerOptions"/> instance.</summary>
public static class BrDocsJsonExtensions
{
    /// <summary>
    /// Adds converters for every BrDocs value object (and for <see cref="PixPayload"/>) to
    /// <paramref name="options"/>. The returned instance is the same object, for fluent use.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="brDocsOptions">Output style; defaults to <see cref="BrDocsJsonOptions.Default"/>.</param>
    /// <returns><paramref name="options"/>.</returns>
    public static JsonSerializerOptions AddBrDocsConverters(this JsonSerializerOptions options, BrDocsJsonOptions? brDocsOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        bool formatted = (brDocsOptions ?? BrDocsJsonOptions.Default).WriteFormatted;
        options.Converters.Add(new CpfJsonConverter(formatted));
        options.Converters.Add(new CnpjJsonConverter(formatted));
        options.Converters.Add(new CepJsonConverter(formatted));
        options.Converters.Add(new PisJsonConverter(formatted));
        options.Converters.Add(new TituloEleitorJsonConverter(formatted));
        options.Converters.Add(new CnhJsonConverter(formatted));
        options.Converters.Add(new RenavamJsonConverter(formatted));
        options.Converters.Add(new PlacaJsonConverter(formatted));
        options.Converters.Add(new TelefoneBrJsonConverter(formatted));
        options.Converters.Add(new BoletoJsonConverter(formatted));
        options.Converters.Add(new ArrecadacaoJsonConverter(formatted));
        options.Converters.Add(new PixPayloadJsonConverter());
        return options;
    }
}
