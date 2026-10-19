namespace DewDrop.Core.Models
{
    /// <summary>A mod as listed by the Nexus Mods search API.</summary>
    public class NexusMod
    {
        public int ModId { get; set; }
        public string Name { get; set; } = "";
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? PictureUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int Endorsements { get; set; }
        public long Downloads { get; set; }
        public string? Version { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool AdultContent { get; set; }

        public string PageUrl => $"https://www.nexusmods.com/stardewvalley/mods/{ModId}";
    }

    public class NexusModFile
    {
        public int FileId { get; set; }
        public string Name { get; set; } = "";
        public string? Version { get; set; }

        /// <summary>MAIN, UPDATE, OPTIONAL, OLD_VERSION, MISCELLANEOUS, ARCHIVED.</summary>
        public string Category { get; set; } = "";
        public bool Primary { get; set; }
        public long SizeInBytes { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }

        public bool IsCurrent => Category is "MAIN" or "OPTIONAL" or "UPDATE" or "MISCELLANEOUS";
    }

    public class NexusSearchPage
    {
        public List<NexusMod> Mods { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public enum NexusSort
    {
        Trending,
        Endorsements,
        Downloads,
        Updated,
        Newest,
        Relevance
    }

    public class NexusAccount
    {
        public string Name { get; set; } = "";
        public bool IsPremium { get; set; }
        public int DailyRemaining { get; set; } = -1;
        public int HourlyRemaining { get; set; } = -1;
    }

    /// <summary>A parsed nxm://stardewvalley/mods/{mod}/files/{file}?key=...&amp;expires=...&amp;user_id=... link.</summary>
    public record NxmLink(int ModId, int FileId, string? Key, string? Expires)
    {
        private static readonly System.Text.RegularExpressions.Regex _pattern = new(
            @"^nxm://stardewvalley/mods/(?<mod>[0-9]+)/files/(?<file>[0-9]+)(\?(?<query>.*))?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex _collectionPattern = new(
            @"^nxm://[a-z0-9]+/collections/",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Whether a link is a Nexus collection download, which DewDrop can't install (Stardrop can).</summary>
        public static bool IsCollectionLink(string link) => _collectionPattern.IsMatch(link.Trim());

        /// <summary>The link, or null if it isn't a Stardew Valley mod file link DewDrop can use (any page can open one, so expect junk).</summary>
        public static NxmLink? Parse(string link)
        {
            var match = _pattern.Match(link.Trim());
            if (!match.Success || !int.TryParse(match.Groups["mod"].Value, out var modId) || !int.TryParse(match.Groups["file"].Value, out var fileId))
            {
                return null;
            }

            string? key = null, expires = null;
            foreach (var pair in match.Groups["query"].Value.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (parts[0] == "key")
                {
                    key = Uri.UnescapeDataString(parts[1]);
                }
                else if (parts[0] == "expires")
                {
                    expires = Uri.UnescapeDataString(parts[1]);
                }
            }

            return new NxmLink(modId, fileId, key, expires);
        }
    }
}
