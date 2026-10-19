using System.Text.Json;
using System.Text.Json.Serialization;

namespace DewDrop.Core.Models
{
    /// <summary>The parts of a SMAPI manifest.json that DewDrop reads. Based on SMAPI's own Manifest model.</summary>
    public class Manifest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string UniqueID { get; set; } = "";
        public string? EntryDll { get; set; }

        [JsonConverter(typeof(UpdateKeysConverter))]
        public string[] UpdateKeys { get; set; } = Array.Empty<string>();

        public ManifestContentPackFor? ContentPackFor { get; set; }
        public ManifestDependency[]? Dependencies { get; set; }

        private static readonly JsonSerializerOptions _options = new() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, PropertyNameCaseInsensitive = true };

        public static Manifest? Parse(string text)
        {
            try
            {
                return JsonSerializer.Deserialize<Manifest>(text, _options);
            }
            catch (JsonException)
            {
                // Manifests in the wild often hold raw newlines inside strings, which System.Text.Json rejects
                return JsonSerializer.Deserialize<Manifest>(text.Replace("\r", "").Replace("\n", ""), _options);
            }
        }

        /// <summary>The Nexus mod ID from the update keys, if any (e.g. "Nexus:541").</summary>
        public int? GetNexusId()
        {
            foreach (var key in UpdateKeys)
            {
                var parts = key.Split(':', 2);
                if (parts.Length == 2 && parts[0].Trim().Equals("Nexus", StringComparison.OrdinalIgnoreCase))
                {
                    var id = parts[1].Split('@')[0].Trim();
                    if (int.TryParse(id, out var value) && value > 0)
                    {
                        return value;
                    }
                }
            }

            return null;
        }
    }

    public class ManifestContentPackFor
    {
        public string? UniqueID { get; set; }
        public string? MinimumVersion { get; set; }
    }

    public class ManifestDependency
    {
        public string UniqueID { get; set; } = "";
        public string? MinimumVersion { get; set; }

        [JsonConverter(typeof(LenientBoolConverter))]
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>Update keys are an array in the spec, but some mods write a single string, or null.</summary>
    internal class UpdateKeysConverter : JsonConverter<string[]>
    {
        // Without this, "UpdateKeys": null bypasses Read and leaves the property null
        public override bool HandleNull => true;

        public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return Array.Empty<string>();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : new[] { value };
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                reader.Skip();
                return Array.Empty<string>();
            }

            var keys = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String && reader.GetString() is { Length: > 0 } key)
                {
                    keys.Add(key);
                }
                else
                {
                    reader.Skip();
                }
            }

            return keys.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    /// <summary>Reads booleans written as true/false or as "true"/"false" strings.</summary>
    internal class LenientBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => !bool.TryParse(reader.GetString(), out var value) || value,
                _ => true
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
