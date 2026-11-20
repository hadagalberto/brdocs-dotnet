using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrDocs.Json;

/// <summary>Serializes a <see cref="PixPayload"/> as its BR Code text, validating the CRC on read.</summary>
public sealed class PixPayloadJsonConverter : JsonConverter<PixPayload>
{
    /// <summary>Creates a converter.</summary>
    public PixPayloadJsonConverter()
    {
    }

    /// <inheritdoc />
    public override PixPayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a JSON string for a Pix BR Code, but found {reader.TokenType}.");
        }
        string? text = reader.GetString();
        if (!PixPayload.TryParse(text, out PixPayload? payload) || payload is null)
        {
            throw new JsonException($"'{text}' is not a valid Pix BR Code.");
        }
        return payload;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PixPayload value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.ToString());
    }
}
