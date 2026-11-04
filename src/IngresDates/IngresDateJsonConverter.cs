using System.Text.Json;
using System.Text.Json.Serialization;

namespace IngresDates;
/// <summary>The discriminator preserves empty, date-only, timestamp and interval states.</summary>
public sealed class IngresDateJsonConverter : JsonConverter<IngresDate>
{
    public override IngresDate Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        string kind = root.GetProperty("kind").GetString() ?? throw new JsonException("kind is required");
        if (!Enum.TryParse<IngresDateKind>(kind, true, out var expected))
            throw new JsonException("Unknown Ingres date kind.");
        string? zone = root.TryGetProperty("timeZone", out var zoneElement) ? zoneElement.GetString() : null;
        var value = expected == IngresDateKind.Empty ? IngresDate.Empty : IngresDate.Parse(root.GetProperty("value").GetString()!, zone);
        if (value.Kind != expected)
            throw new JsonException("The value does not match its kind.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, IngresDate value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", value.Kind.ToString().ToLowerInvariant());
        if (!value.IsEmpty)
            writer.WriteString("value", value.ToString());
        if (value.TimeZoneId is not null)
            writer.WriteString("timeZone", value.TimeZoneId);
        writer.WriteEndObject();
    }
}
