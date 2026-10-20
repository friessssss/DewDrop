using System.Text.Json.Nodes;
using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>Reading smapi.io's answers, in the shape it sends with includeExtendedMetadata.</summary>
    public class UpdateCheckerTests
    {
        private static InstalledMod Mod(string id, string version, params string[] keys)
        {
            return new InstalledMod(new Manifest { UniqueID = id, Name = id, Version = version, UpdateKeys = keys }, new DirectoryInfo(Path.Combine(Path.GetTempPath(), id)), null);
        }

        [Fact]
        public void OnlyModsWithTheirOwnUpdateKeysAreOfferedUpdates()
        {
            var keyed = Mod("Pathoschild.ContentPatcher", "2.0.0", "Nexus:1915");
            var keyless = Mod("spacechase0.GenericModConfigMenu", "1.0.0");
            var entries = JsonNode.Parse(@"[
                  { ""id"": ""Pathoschild.ContentPatcher"", ""suggestedUpdate"": { ""version"": ""2.1.0"", ""url"": ""https://www.nexusmods.com/stardewvalley/mods/1915"" }, ""metadata"": { ""id"": [""Pathoschild.ContentPatcher""], ""nexusID"": 1915 }, ""errors"": [] },
                  { ""id"": ""spacechase0.GenericModConfigMenu"", ""suggestedUpdate"": { ""version"": ""1.14.0"", ""url"": ""https://www.nexusmods.com/stardewvalley/mods/5098"" }, ""metadata"": { ""id"": [""spacechase0.GenericModConfigMenu""], ""nexusID"": 5098 }, ""errors"": [] }
                ]")!.AsArray();

            var results = UpdateChecker.ReadResults(entries, new[] { keyed, keyless });

            Assert.Equal("2.1.0", results["Pathoschild.ContentPatcher"].Update?.Version);
            Assert.Equal(1915, results["Pathoschild.ContentPatcher"].NexusId);

            // smapi.io guessed GMCM's keys from its compatibility list; only its Nexus ID is used
            Assert.Null(results["spacechase0.GenericModConfigMenu"].Update);
            Assert.Equal(5098, results["spacechase0.GenericModConfigMenu"].NexusId);
            Assert.Equal("1.0.0", results["spacechase0.GenericModConfigMenu"].CheckedVersion);
        }

        [Fact]
        public void ModsNotOnTheListHaveNoNexusId()
        {
            var pack = Mod("FlashShifter.StardewValleyExpandedCP", "1.15.11", "Nexus:???");
            var entries = JsonNode.Parse(@"[
                  { ""id"": ""flashshifter.stardewvalleyexpandedcp"", ""metadata"": { ""id"": [] }, ""errors"": [""The update key 'Nexus:???' isn't valid.""] },
                  { ""id"": ""Not.Asked"", ""metadata"": { ""nexusID"": 1 } },
                  { ""metadata"": { ""nexusID"": 2 } }
                ]")!.AsArray();

            var results = UpdateChecker.ReadResults(entries, new[] { pack });

            var result = Assert.Single(results.Values);
            Assert.Null(result.NexusId);
            Assert.Null(result.Update);
        }

        [Fact]
        public void AnOddNexusIdIsReadLeniently()
        {
            var a = Mod("A", "1.0.0");
            var b = Mod("B", "1.0.0");
            var c = Mod("C", "1.0.0");
            var entries = JsonNode.Parse(@"[
                  { ""id"": ""A"", ""metadata"": { ""nexusID"": ""321"" } },
                  { ""id"": ""B"", ""metadata"": { ""nexusID"": -4 } },
                  { ""id"": ""C"", ""metadata"": { ""nexusID"": { ""oops"": true } } }
                ]")!.AsArray();

            var results = UpdateChecker.ReadResults(entries, new[] { a, b, c });

            Assert.Equal(321, results["A"].NexusId);
            Assert.Null(results["B"].NexusId);
            Assert.Null(results["C"].NexusId);
        }
    }
}
