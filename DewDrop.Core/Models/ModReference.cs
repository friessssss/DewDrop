using System.Text.Json;
using System.Text.Json.Serialization;

namespace DewDrop.Core.Models
{
    /// <summary>
    /// Identifies an installed copy of a mod, matching Stardrop's ModReference so profile files stay compatible.
    /// SourceId is null for loose installs and the collection's source ID for collection installs.
    /// </summary>
    [JsonConverter(typeof(ModReferenceConverter))]
    public record ModReference(string UniqueId, string? SourceId = null)
    {
        [JsonIgnore]
        public bool IsFromCollection => !string.IsNullOrEmpty(SourceId);

        public bool Matches(string uniqueId, string? sourceId)
        {
            return UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(SourceId, sourceId, StringComparison.OrdinalIgnoreCase);
        }

        public virtual bool Equals(ModReference? other)
        {
            return other is not null && Matches(other.UniqueId, other.SourceId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UniqueId.ToLowerInvariant(), SourceId?.ToLowerInvariant() ?? "");
        }

        public override string ToString()
        {
            return IsFromCollection ? $"{UniqueId} ({SourceId})" : UniqueId;
        }
    }

    /// <summary>Reads both a bare unique ID string and the object form; writes the bare string when there's no SourceId, like Stardrop.</summary>
    public class ModReferenceConverter : JsonConverter<ModReference>
    {
        public override ModReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var legacyId = reader.GetString();
                return string.IsNullOrEmpty(legacyId) ? null : new ModReference(legacyId);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Unexpected token {reader.TokenType} when reading a {nameof(ModReference)}");
            }

            string? uniqueId = null;
            string? sourceId = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, nameof(ModReference.UniqueId), StringComparison.OrdinalIgnoreCase))
                {
                    uniqueId = reader.GetString();
                }
                else if (string.Equals(name, nameof(ModReference.SourceId), StringComparison.OrdinalIgnoreCase))
                {
                    sourceId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }

            return string.IsNullOrEmpty(uniqueId) ? null : new ModReference(uniqueId, sourceId);
        }

        public override void Write(Utf8JsonWriter writer, ModReference value, JsonSerializerOptions options)
        {
            if (!value.IsFromCollection)
            {
                writer.WriteStringValue(value.UniqueId);
                return;
            }

            writer.WriteStartObject();
            writer.WriteString(nameof(ModReference.UniqueId), value.UniqueId);
            writer.WriteString(nameof(ModReference.SourceId), value.SourceId);
            writer.WriteEndObject();
        }
    }
}
