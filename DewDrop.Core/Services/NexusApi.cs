using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Nexus Mods client. Browsing and file lists use the public v2 GraphQL API; account validation and download
    /// links use v1 and need the user's API key.
    /// </summary>
    public class NexusApi : IDisposable
    {
        public const int StardewGameId = 1303;
        public const string ApiKeyPageUrl = "https://www.nexusmods.com/users/myaccount?tab=api%20access";

        private static readonly Uri _v1 = new("https://api.nexusmods.com/v1/");
        private static readonly Uri _graphQL = new("https://api.nexusmods.com/v2/graphql");
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;
        private readonly ILog _log;
        private string? _apiKey;

        public NexusAccount? Account { get; private set; }
        public bool IsLoggedIn => Account is not null && _apiKey is not null;

        public NexusApi(string appVersion, ILog log)
        {
            _log = log;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Add("Application-Name", "DewDrop");
            _http.DefaultRequestHeaders.Add("Application-Version", appVersion);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd($"DewDrop/{appVersion}");
        }

        // ---------- account ----------

        /// <summary>Validates a key; on success it becomes this client's key.</summary>
        public async Task<NexusAccount?> LoginAsync(string apiKey, CancellationToken token = default)
        {
            apiKey = apiKey.Trim();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_v1, "users/validate.json"));
            request.Headers.Add("apikey", apiKey);
            using var response = await _http.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                if (status is 401 or 403)
                {
                    _log.Warn($"Nexus rejected the API key ({status})");
                    return null;
                }

                // Busy or rate limited says nothing about the key, so it isn't reported as rejected
                _log.Warn($"Nexus couldn't check the API key ({status})");
                throw new NexusApiException(status == 429
                    ? "Nexus Mods is limiting requests right now. Try again later."
                    : $"Nexus Mods didn't answer ({status}). Try again in a moment.");
            }

            var body = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: token);
            var account = new NexusAccount
            {
                Name = body?["name"]?.GetValue<string>() ?? "",
                IsPremium = body?["is_premium"]?.GetValue<bool>() ?? false
            };
            ReadRateLimits(response.Headers, account);

            _apiKey = apiKey;
            Account = account;
            return account;
        }

        public void Logout()
        {
            _apiKey = null;
            Account = null;
        }

        // ---------- browse ----------

        private const string ModFields = "modId name summary author pictureUrl thumbnailUrl endorsements downloads version updatedAt adultContent";

        public async Task<NexusSearchPage> SearchAsync(string? text, NexusSort sort, int offset, int count, bool includeAdult, CancellationToken token = default)
        {
            var filter = new JsonObject
            {
                ["gameDomainName"] = new JsonArray(new JsonObject { ["value"] = "stardewvalley" })
            };
            if (!includeAdult)
            {
                filter["adultContent"] = new JsonArray(new JsonObject { ["value"] = false });
            }
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Substring match on the name; Nexus refuses that below two characters (a single CJK character, say),
                // so those match whole words instead
                var trimmed = text.Trim();
                var isShort = new StringInfo(trimmed).LengthInTextElements < 2;
                filter[isShort ? "nameStemmed" : "name"] = new JsonArray(new JsonObject { ["value"] = trimmed, ["op"] = isShort ? "MATCHES" : "WILDCARD" });
            }
            if (sort == NexusSort.Trending)
            {
                // GraphQL has no trending sort, so trending is "most endorsed among mods from the last 30 days"
                var since = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString();
                filter["createdAt"] = new JsonArray(new JsonObject { ["value"] = since, ["op"] = "GT" });
            }

            // Nexus's relevance score is the same for every substring match and its order changes between requests,
            // so paging by it repeats some mods and skips others. Most endorsed first puts the well-known match on
            // top and pages reliably.
            var sortKey = sort switch
            {
                NexusSort.Downloads => "downloads",
                NexusSort.Updated => "updatedAt",
                NexusSort.Newest => "createdAt",
                _ => "endorsements"
            };

            // Ties (plenty of mods have no endorsements) are broken by age so pages never overlap
            var sortOrder = new JsonArray(new JsonObject { [sortKey] = new JsonObject { ["direction"] = "DESC" } });
            if (sortKey != "createdAt")
            {
                sortOrder.Add(new JsonObject { ["createdAt"] = new JsonObject { ["direction"] = "DESC" } });
            }

            var data = await GraphQLAsync(
                $"query Search($filter: ModsFilter, $sort: [ModsSort!], $count: Int, $offset: Int) {{ mods(filter: $filter, sort: $sort, count: $count, offset: $offset) {{ totalCount nodes {{ {ModFields} }} }} }}",
                new JsonObject
                {
                    ["filter"] = filter,
                    ["sort"] = sortOrder,
                    ["count"] = count,
                    ["offset"] = offset
                },
                token);

            var mods = data?["mods"];
            return new NexusSearchPage
            {
                TotalCount = mods?["totalCount"]?.GetValue<int>() ?? 0,
                Mods = mods?["nodes"]?.Deserialize<List<NexusMod>>(_json) ?? new List<NexusMod>()
            };
        }

        public async Task<NexusMod?> GetModAsync(int modId, CancellationToken token = default)
        {
            var data = await GraphQLAsync(
                $"query Mod($ids: [CompositeDomainWithIdInput!]!) {{ legacyModsByDomain(ids: $ids) {{ nodes {{ {ModFields} description }} }} }}",
                new JsonObject { ["ids"] = new JsonArray(new JsonObject { ["gameDomain"] = "stardewvalley", ["modId"] = modId }) },
                token);

            return data?["legacyModsByDomain"]?["nodes"]?.AsArray().FirstOrDefault()?.Deserialize<NexusMod>(_json);
        }

        public async Task<List<NexusModFile>> GetFilesAsync(int modId, CancellationToken token = default)
        {
            var data = await GraphQLAsync(
                "query Files($modId: ID!, $gameId: ID!) { modFiles(modId: $modId, gameId: $gameId) { fileId name version category primary sizeInBytes date description } }",
                new JsonObject { ["modId"] = modId, ["gameId"] = StardewGameId },
                token);

            var files = new List<NexusModFile>();
            foreach (var node in data?["modFiles"]?.AsArray() ?? new JsonArray())
            {
                if (node is null)
                {
                    continue;
                }

                files.Add(new NexusModFile
                {
                    FileId = node["fileId"]?.GetValue<int>() ?? 0,
                    Name = node["name"]?.GetValue<string>() ?? "",
                    Version = node["version"]?.GetValue<string>(),
                    Category = node["category"]?.GetValue<string>() ?? "",
                    Primary = (node["primary"]?.GetValue<int>() ?? 0) != 0,
                    SizeInBytes = long.TryParse(node["sizeInBytes"]?.ToString(), out var size) ? size : 0,
                    Date = DateTimeOffset.FromUnixTimeSeconds(node["date"]?.GetValue<long>() ?? 0).UtcDateTime,
                    Description = node["description"]?.GetValue<string>()
                });
            }

            // Newest first within each category, main files on top
            return files
                .OrderBy(f => f.Category switch { "MAIN" => 0, "UPDATE" => 1, "OPTIONAL" => 2, "MISCELLANEOUS" => 3, _ => 4 })
                .ThenByDescending(f => f.Date)
                .ToList();
        }

        // ---------- downloads ----------

        /// <summary>
        /// Gets a CDN link for a file. Premium accounts can ask for any file directly; free accounts must pass the
        /// key/expires pair from an nxm:// link that the user started on the website.
        /// </summary>
        public async Task<string?> GetDownloadLinkAsync(int modId, int fileId, NxmLink? nxm = null, CancellationToken token = default)
        {
            if (_apiKey is null)
            {
                throw new InvalidOperationException("Log in to Nexus Mods first.");
            }

            var url = $"games/stardewvalley/mods/{modId}/files/{fileId}/download_link.json";
            if (nxm?.Key is not null && nxm.Expires is not null)
            {
                url += $"?key={Uri.EscapeDataString(nxm.Key)}&expires={Uri.EscapeDataString(nxm.Expires)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_v1, url));
            request.Headers.Add("apikey", _apiKey);
            using var response = await _http.SendAsync(request, token);
            if (Account is not null)
            {
                ReadRateLimits(response.Headers, Account);
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync(token);
                var status = (int)response.StatusCode;
                _log.Warn($"Nexus refused a download link for {modId}/{fileId} ({status}): {message}");

                // Statuses as documented for download_link.json; an nxm key is only good for a while and one account
                throw status switch
                {
                    403 when nxm?.Key is null => new NexusPremiumRequiredException(),
                    410 => new NexusApiException("This download link has expired. Click 'Slow download' on the mod's Nexus page again.", canRetry: false),
                    400 or 403 => new NexusApiException("Nexus didn't accept this download link for your account. Start the download again from the mod's Nexus page.", canRetry: false),
                    404 => new NexusApiException("Nexus Mods no longer has this file.", canRetry: false),
                    401 => new NexusApiException("Nexus didn't accept your API key. Log in again on the Account tab.", canRetry: false),
                    429 => new NexusApiException("You've reached Nexus Mods' download limit for now. Try again later."),
                    _ => new NexusApiException($"Nexus Mods didn't answer ({status}). Try again in a moment.")
                };
            }

            var links = await response.Content.ReadFromJsonAsync<List<DownloadLink>>(cancellationToken: token);
            return links?.FirstOrDefault()?.Uri;
        }

        /// <summary>How long a download may go without receiving anything before it counts as stalled.</summary>
        private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Streams a file to disk, reporting progress from 0 to 1 when the size is known. Fails with a
        /// <see cref="NexusApiException"/> when no data arrives for a minute: once the headers are in, the client's
        /// own timeout no longer applies, and a silently dropped connection would otherwise wait forever.
        /// </summary>
        public async Task DownloadAsync(string url, string destination, IProgress<double>? progress, CancellationToken token = default)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;
            var temp = destination + ".part";
            try
            {
                using var stall = CancellationTokenSource.CreateLinkedTokenSource(token);
                await using (var input = await response.Content.ReadAsStreamAsync(token))
                await using (var output = File.Create(temp))
                {
                    var buffer = new byte[81920];
                    long read = 0;
                    while (true)
                    {
                        // Restarted for every read, so only a gap with no data at all trips it
                        stall.CancelAfter(StallTimeout);
                        int count;
                        try
                        {
                            count = await input.ReadAsync(buffer, stall.Token);
                        }
                        catch (OperationCanceledException) when (!token.IsCancellationRequested)
                        {
                            throw new NexusApiException("The download stopped receiving data. Try again.");
                        }

                        if (count == 0)
                        {
                            break;
                        }

                        await output.WriteAsync(buffer.AsMemory(0, count), token);
                        read += count;
                        if (total > 0)
                        {
                            progress?.Report((double)read / total);
                        }
                    }
                }

                File.Move(temp, destination, overwrite: true);
            }
            catch
            {
                try
                {
                    File.Delete(temp);
                }
                catch
                {
                    // Removed with the rest of the downloads folder at the next start
                }
                throw;
            }

            progress?.Report(1);
        }

        public async Task<byte[]?> GetBytesAsync(string url, CancellationToken token = default)
        {
            try
            {
                return await _http.GetByteArrayAsync(url, token);
            }
            catch (Exception ex)
            {
                _log.Trace($"Couldn't fetch {url}: {ex.Message}");
                return null;
            }
        }

        // ---------- plumbing ----------

        private async Task<JsonNode?> GraphQLAsync(string query, JsonObject variables, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _graphQL)
            {
                Content = JsonContent.Create(new JsonObject { ["query"] = query, ["variables"] = variables })
            };
            if (_apiKey is not null)
            {
                request.Headers.Add("apikey", _apiKey);
            }

            using var response = await _http.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
            {
                throw new NexusApiException($"Nexus Mods didn't answer ({(int)response.StatusCode}). Try again in a moment.");
            }

            var body = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: token);
            var data = body?["data"];
            if (body?["errors"] is JsonArray errors && errors.Count > 0)
            {
                var message = errors[0]?["message"]?.ToString()?.Split('\n')[0] ?? "unknown error";
                var code = errors[0]?["extensions"]?["code"]?.ToString();
                _log.Warn($"Nexus GraphQL error ({code ?? "no code"}): {message}");

                // Partial data is still usable; only a reply with nothing in it is a failure
                if (data is null || (data is JsonObject fields && fields.All(f => f.Value is null)))
                {
                    // A request Nexus rejects as invalid fails the same way every time, so asking again won't help
                    if (code is "WILDCARD_ERROR" or "BAD_USER_INPUT" or "GRAPHQL_VALIDATION_FAILED" or "GRAPHQL_PARSE_FAILED")
                    {
                        throw new NexusApiException($"Nexus Mods can't answer that: {message}", code, canRetry: false);
                    }

                    throw new NexusApiException("Nexus Mods couldn't answer that request. Try again in a moment.", code);
                }
            }

            return data;
        }

        private static void ReadRateLimits(HttpResponseHeaders headers, NexusAccount account)
        {
            if (headers.TryGetValues("x-rl-daily-remaining", out var daily) && int.TryParse(daily.FirstOrDefault(), out var d))
            {
                account.DailyRemaining = d;
            }
            if (headers.TryGetValues("x-rl-hourly-remaining", out var hourly) && int.TryParse(hourly.FirstOrDefault(), out var h))
            {
                account.HourlyRemaining = h;
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        private class DownloadLink
        {
            [JsonPropertyName("URI")]
            public string? Uri { get; set; }

            [JsonPropertyName("short_name")]
            public string? ShortName { get; set; }
        }
    }

    /// <summary>Raised when Nexus Mods can't answer a request, with a message fit to show the player.</summary>
    public class NexusApiException : Exception
    {
        /// <summary>The GraphQL error code, when Nexus gave one (e.g. WILDCARD_ERROR).</summary>
        public string? Code { get; }

        /// <summary>False when the same request would fail again, so offering "Try again" would only repeat the error.</summary>
        public bool CanRetry { get; }

        public NexusApiException(string message, string? code = null, bool canRetry = true) : base(message)
        {
            Code = code;
            CanRetry = canRetry;
        }
    }

    /// <summary>Raised when a free account asks for a download link without an nxm key.</summary>
    public class NexusPremiumRequiredException : NexusApiException
    {
        public NexusPremiumRequiredException() : base("Direct downloads need Nexus Premium. Use 'Get on Nexus' instead.", canRetry: false)
        {
        }
    }
}
