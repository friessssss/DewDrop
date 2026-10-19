using System.Text.Json;
using System.Text.Json.Serialization;

namespace DewDrop.Core.Models
{
    public record ModNote(string UniqueId, string Note);

    /// <summary>A mod profile, stored in the same JSON shape as Stardrop's so both tools can share profiles.</summary>
    public class Profile
    {
        public string Name { get; set; } = "Unknown";
        public bool IsProtected { get; set; }

        /// <summary>Set when Stardrop generated this profile from a Nexus collection; such profiles are read-only.</summary>
        public string? SourceId { get; set; }

        public List<ModReference> EnabledModIds { get; set; } = new();

        /// <summary>Kept as raw JSON so DewDrop round-trips Stardrop's per-profile mod configs untouched.</summary>
        public Dictionary<string, JsonElement> PreservedModConfigs { get; set; } = new();

        public List<ModNote> Notes { get; set; } = new();

        [JsonIgnore]
        public bool IsFromCollection => !string.IsNullOrEmpty(SourceId);

        [JsonIgnore]
        public bool IsReadOnly => IsFromCollection;

        public Profile()
        {
        }

        public Profile(string name, bool isProtected = false)
        {
            Name = name;
            IsProtected = isProtected;
        }

        public bool IsEnabled(InstalledMod mod)
        {
            // A hand-edited file can hold null entries; ProfileStore drops them on load, but not every Profile comes from there
            return EnabledModIds.Any(r => r is not null && r.Matches(mod.UniqueId, mod.SourceId));
        }

        public void SetEnabled(InstalledMod mod, bool enabled)
        {
            EnabledModIds.RemoveAll(r => r is null || r.Matches(mod.UniqueId, mod.SourceId));
            if (enabled)
            {
                EnabledModIds.Add(new ModReference(mod.UniqueId, mod.SourceId));
            }
        }

        public Profile Clone(string name)
        {
            return new Profile(name)
            {
                EnabledModIds = new List<ModReference>(EnabledModIds),
                PreservedModConfigs = new Dictionary<string, JsonElement>(PreservedModConfigs),
                Notes = new List<ModNote>(Notes)
            };
        }
    }
}
