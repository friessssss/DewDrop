using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>Keeps warnings and errors so a test can check what was reported.</summary>
    internal sealed class TestLog : ILog
    {
        public List<string> Warnings { get; } = new();

        public void Trace(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message) => Warnings.Add(message);

        public void Error(string message) => Warnings.Add(message);
    }

    /// <summary>Makes mod folders with a manifest in a scratch folder that's removed afterwards.</summary>
    internal sealed class ScratchFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dewdrop-tests", Guid.NewGuid().ToString("N"));

        public ScratchFolder()
        {
            Directory.CreateDirectory(Path);
        }

        public string Mod(string relativeFolder, string uniqueId, string version = "1.0.0", string? updateKeys = null)
        {
            var folder = System.IO.Path.Combine(Path, relativeFolder);
            Directory.CreateDirectory(folder);
            var keys = updateKeys is null ? "" : $", \"UpdateKeys\": [{updateKeys}]";
            File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), $"{{ \"Name\": \"{uniqueId} Name\", \"UniqueID\": \"{uniqueId}\", \"Version\": \"{version}\"{keys} }}");
            File.WriteAllText(System.IO.Path.Combine(folder, "config.json"), "{}");
            return folder;
        }

        public void Dispose()
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            // .NET 6's recursive delete reports a junction it removed as access denied, so links go one by one first
            RemoveLinks(new DirectoryInfo(Path));
            Directory.Delete(Path, recursive: true);
        }

        private static void RemoveLinks(DirectoryInfo folder)
        {
            foreach (var child in folder.EnumerateDirectories())
            {
                if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    child.Delete(false);
                }
                else
                {
                    RemoveLinks(child);
                }
            }
        }
    }
}
