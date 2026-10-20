using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>What smapi.io said about one mod, for the version that was checked.</summary>
    /// <param name="NexusId">The Nexus mod smapi.io's compatibility list has for it, if any (it lists code mods, not content packs).</param>
    public record UpdateCheckResult(string UniqueId, string CheckedVersion, ModUpdate? Update, int? NexusId = null);

    /// <summary>Asks smapi.io's update API for newer versions of installed mods, the same service SMAPI uses.</summary>
    public class UpdateChecker
    {
        /// <summary>
        /// SMAPI's strict version format (SemanticVersionReader without non-standard extensions). smapi.io answers a
        /// batch holding any other version with an empty list, so one odd manifest would hide every update.
        /// </summary>
        private static readonly Regex _semanticVersion = new(
            @"^(0|[1-9][0-9]{0,8})\.(0|[1-9][0-9]{0,8})(\.(0|[1-9][0-9]{0,8}))?(-(?>[a-z0-9]+[\-\.]?)+)?(\+(?>[a-z0-9]+[\-\.]?)+)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly ILog _log;
        private readonly string _appVersion;

        public UpdateChecker(string appVersion, ILog log)
        {
            _appVersion = appVersion;
            _log = log;
        }

        /// <summary>Whether SMAPI (and smapi.io) accept a version string, e.g. "1.2", "1.2.3-beta.1" but not "v1.2" or "1.2.3.4".</summary>
        public static bool IsValidVersion(string? version)
        {
            var match = version is null ? null : _semanticVersion.Match(version.Trim());
            return match is { Success: true } && !(match.Groups[1].Value == "0" && match.Groups[2].Value == "0" && match.Groups[4].Value is "" or "0");
        }

        /// <summary>
        /// Asks smapi.io about every loose mod and returns its answers by unique ID, without touching the mods, so the
        /// caller can apply them on its own thread to whatever mod list is current by then (see <see cref="Apply"/>).
        /// Throws when smapi.io doesn't give a usable answer, rather than reporting that nothing has updates.
        /// </summary>
        public async Task<Dictionary<string, UpdateCheckResult>> FetchAsync(IReadOnlyList<InstalledMod> mods, string smapiVersion, string gameVersion, string platform, CancellationToken token = default)
        {
            var results = new Dictionary<string, UpdateCheckResult>(StringComparer.OrdinalIgnoreCase);

            // Collection mods are pinned by their collection, so they're never offered updates. Mods without update keys
            // are asked about too, only for the Nexus ID smapi.io knows them by (Browse uses it to mark them installed)
            var candidates = mods
                .Where(m => m.SourceId is null && !string.IsNullOrEmpty(m.Version))
                .GroupBy(m => m.UniqueId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var invalid = candidates.Where(m => !IsValidVersion(m.Version)).ToList();
            foreach (var mod in invalid)
            {
                // SMAPI won't load these either, so they're usually broken leftovers
                _log.Trace($"Not checking {mod.UniqueId} for updates: '{mod.Version}' isn't a version SMAPI understands");
            }
            candidates = candidates.Except(invalid).ToList();
            if (candidates.Count == 0)
            {
                return results;
            }

            var request = new JsonObject
            {
                ["mods"] = new JsonArray(candidates.Select(m => (JsonNode)new JsonObject
                {
                    ["id"] = m.UniqueId,
                    ["installedVersion"] = m.Version.Trim(),
                    ["updateKeys"] = new JsonArray(KeysOf(m).Select(k => (JsonNode)JsonValue.Create(k)!).ToArray())
                }).ToArray()),
                ["apiVersion"] = smapiVersion,
                ["gameVersion"] = gameVersion,
                ["platform"] = platform,
                ["includeExtendedMetadata"] = true
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"DewDrop/{_appVersion}");
            using var response = await http.PostAsJsonAsync("https://smapi.io/api/v3.0/mods", request, token);
            if (!response.IsSuccessStatusCode)
            {
                _log.Warn($"smapi.io update check failed ({(int)response.StatusCode})");
                throw new Exception($"smapi.io didn't answer ({(int)response.StatusCode}). Try again in a moment.");
            }

            var entries = await response.Content.ReadFromJsonAsync<JsonArray>(cancellationToken: token);
            if (entries is null || entries.Count == 0)
            {
                // smapi.io answers every mod it was asked about, so an empty list means it rejected the request
                _log.Warn($"smapi.io returned no results for {candidates.Count} mods");
                throw new Exception("smapi.io didn't return any results, so the update check couldn't finish.");
            }

            results = ReadResults(entries, candidates);
            _log.Info($"Update check: {results.Values.Count(r => r.Update is not null)} of {candidates.Count(m => KeysOf(m).Length > 0)} mods have updates");
            return results;
        }

        /// <summary>
        /// Reads smapi.io's answers for the mods that were asked about. Only mods with update keys of their own are offered
        /// updates, as before: for the rest smapi.io guesses the keys from its compatibility list, and those answers are
        /// only used for the Nexus ID.
        /// </summary>
        internal static Dictionary<string, UpdateCheckResult> ReadResults(JsonArray entries, IReadOnlyList<InstalledMod> asked)
        {
            var results = new Dictionary<string, UpdateCheckResult>(StringComparer.OrdinalIgnoreCase);
            var byId = asked
                .GroupBy(m => m.UniqueId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                var id = entry?["id"]?.GetValue<string>();
                if (id is null || !byId.TryGetValue(id, out var mod))
                {
                    continue;
                }

                ModUpdate? update = null;
                var suggested = entry?["suggestedUpdate"];
                if (KeysOf(mod).Length > 0 && suggested?["version"] is { } version)
                {
                    update = new ModUpdate(version.GetValue<string>(), suggested["url"]?.GetValue<string>());
                }

                results[id] = new UpdateCheckResult(id, mod.Version, update, ReadNexusId(entry?["metadata"]?["nexusID"]));
            }

            return results;
        }

        /// <summary>The Nexus ID in an answer's metadata: a number, though read leniently in case it ever comes as text.</summary>
        private static int? ReadNexusId(JsonNode? node)
        {
            try
            {
                if (node is JsonValue value)
                {
                    if (value.TryGetValue<long>(out var number))
                    {
                        return number is > 0 and <= int.MaxValue ? (int)number : null;
                    }
                    if (value.TryGetValue<string>(out var text) && int.TryParse(text, out var parsed) && parsed > 0)
                    {
                        return parsed;
                    }
                }
            }
            catch (Exception)
            {
                // Only the Installed mark on Browse depends on it
            }

            return null;
        }

        /// <summary>A manifest's update keys; a hand-built one can hold null.</summary>
        private static string[] KeysOf(InstalledMod mod) => mod.Manifest.UpdateKeys ?? Array.Empty<string>();

        /// <summary>Sets each loose mod's update from a check's results, skipping mods whose version changed since that check.</summary>
        public static void Apply(IReadOnlyDictionary<string, UpdateCheckResult> results, IEnumerable<InstalledMod> mods)
        {
            foreach (var mod in mods)
            {
                if (mod.SourceId is null && results.TryGetValue(mod.UniqueId, out var result) && result.CheckedVersion == mod.Version)
                {
                    mod.Update = result.Update;
                }
            }
        }

        /// <summary>
        /// Checks and writes the results straight onto the given mods, returning how many have updates. Prefer
        /// <see cref="FetchAsync"/> plus <see cref="Apply"/> on the main thread, which survives the mod list being rescanned meanwhile.
        /// </summary>
        public async Task<int> CheckAsync(IReadOnlyList<InstalledMod> mods, string smapiVersion, string gameVersion, string platform, CancellationToken token = default)
        {
            var results = await FetchAsync(mods, smapiVersion, gameVersion, platform, token);
            Apply(results, mods);
            return results.Values.Count(r => r.Update is not null);
        }
    }
}
