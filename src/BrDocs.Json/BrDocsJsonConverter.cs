using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrDocs.Json;

/// <summary>
/// Shared implementation for converters of BrDocs value objects. Reading accepts formatted or
/// unformatted text; writing uses the presentation format or the significant characters only,
/// according to <see cref="WriteFormatted"/>.
/// </summary>
/// <typeparam name="T">A BrDocs value object implementing <see cref="ISpanParsable{T}"/> and <see cref="ISpanFormattable"/>.</typeparam>
public abstract class BrDocsJsonConverter<T> : JsonConverter<T>
    where T : struct, ISpanParsable<T>, ISpanFormattable
{
    /// <summary>Creates a converter.</summary>
    /// <param name="writeFormatted">Whether <c>Write</c> emits the presentation format.</param>
    protected BrDocsJsonConverter(bool writeFormatted) => WriteFormatted = writeFormatted;

    /// <summary>Whether <c>Write</c> emits the presentation format instead of significant characters only.</summary>
    public bool WriteFormatted { get; }

    /// <summary>The human-readable document name used in error messages (e.g. "CPF").</summary>
    protected abstract string DocumentName { get; }

    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException($"Null is not a valid {DocumentName} value; use a nullable type to represent a missing document.");
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a JSON string for {DocumentName}, but found {reader.TokenType}.");
        }
        string? text = reader.GetString();
        if (text is null || !T.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out T result))
        {
            throw new JsonException($"'{text}' is not a valid {DocumentName}.");
        }
        return result;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        string text = WriteFormatted
            ? value.ToString()!
            : value.ToString("N", CultureInfo.InvariantCulture)!;
        writer.WriteStringValue(text);
    }
}
