namespace DewDrop.Core.Models
{
    /// <summary>A mod folder found on disk.</summary>
    public class InstalledMod
    {
        public Manifest Manifest { get; }
        public DirectoryInfo Folder { get; }

        /// <summary>The collection that owns this copy, or null for a loose install.</summary>
        public string? SourceId { get; }

        public string UniqueId => Manifest.UniqueID;
        public string Name => string.IsNullOrWhiteSpace(Manifest.Name) ? Folder.Name : Manifest.Name;
        public string Author => Manifest.Author ?? "";
        public string Version => Manifest.Version ?? "";
        public string Description => Manifest.Description ?? "";
        public bool IsContentPack => Manifest.ContentPackFor is not null;
        public int? NexusId => Manifest.GetNexusId();

        /// <summary>Filled in by the update checker when a newer version exists.</summary>
        public ModUpdate? Update { get; set; }

        public InstalledMod(Manifest manifest, DirectoryInfo folder, string? sourceId)
        {
            Manifest = manifest;
            Folder = folder;
            SourceId = sourceId;
        }

        /// <summary>Required dependency IDs, including the framework a content pack is for.</summary>
        public IEnumerable<string> RequiredIds()
        {
            if (!string.IsNullOrEmpty(Manifest.ContentPackFor?.UniqueID))
            {
                yield return Manifest.ContentPackFor.UniqueID;
            }

            foreach (var dependency in Manifest.Dependencies ?? Array.Empty<ManifestDependency>())
            {
                if (dependency.IsRequired && !string.IsNullOrEmpty(dependency.UniqueID))
                {
                    yield return dependency.UniqueID;
                }
            }
        }
    }

    public record ModUpdate(string Version, string? Url);
}
