using System.Diagnostics;
using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Framework
{
    internal enum DownloadState
    {
        Queued,
        Downloading,
        Installing,
        Done,
        Failed
    }

    internal class DownloadJob
    {
        public int ModId { get; init; }
        public int FileId { get; init; }
        public string Title { get; set; } = "";
        public double Progress { get; set; }
        public DownloadState State { get; set; } = DownloadState.Queued;
        public string Message { get; set; } = "";
        public DateTime Started { get; } = DateTime.Now;

        /// <summary>False when the failure would happen again on a retry (the file is gone, the link was refused).</summary>
        public bool CanRetry { get; set; } = true;

        /// <summary>Stops the network side of the job; an install that already started finishes.</summary>
        internal CancellationTokenSource Cancellation { get; init; } = new();

        public bool IsActive => State is DownloadState.Queued or DownloadState.Downloading or DownloadState.Installing;

        /// <summary>Whether the job can still be stopped: installing can't be interrupted safely.</summary>
        public bool CanCancel => State is DownloadState.Queued or DownloadState.Downloading;
    }

    /// <summary>Downloads Nexus files and installs them, one at a time so archives never race each other.</summary>
    internal class DownloadManager
    {
        private readonly NexusSession _nexus;
        private readonly ModManager _mods;
        private readonly ModConfig _config;
        private readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>Cancelled when the game restarts, which stops every job's network work at once.</summary>
        private readonly CancellationTokenSource _shutdown = new();

        public List<DownloadJob> Jobs { get; } = new();

        public DownloadManager(NexusSession nexus, ModManager mods, ModConfig config)
        {
            _nexus = nexus;
            _mods = mods;
            _config = config;
        }

        public int ActiveCount => Jobs.Count(j => j.IsActive);

        public bool IsDownloading(int modId) => Jobs.Any(j => j.ModId == modId && j.IsActive);

        /// <summary>Premium path: ask Nexus for a link to any file directly.</summary>
        public void Start(int modId, int fileId, string title) => Enqueue(modId, fileId, title, null);

        /// <summary>Free path: a link the user started from the website's "Mod manager download" button.</summary>
        public void Start(NxmLink link) => Enqueue(link.ModId, link.FileId, $"Nexus mod {link.ModId}", link);

        /// <summary>Opens the file on the website, where free accounts click "Mod manager download".</summary>
        public static void OpenOnNexus(int modId, int? fileId = null)
        {
            var url = fileId is null
                ? $"https://www.nexusmods.com/stardewvalley/mods/{modId}?tab=files"
                : $"https://www.nexusmods.com/stardewvalley/mods/{modId}?tab=files&file_id={fileId}&nmm=1";
            OpenUrl(url);
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't open {url}: {ex.Message}");
            }
        }

        public void ClearFinished() => Jobs.RemoveAll(j => !j.IsActive);

        /// <summary>Stops a queued or downloading job; it ends up failed, with "Cancelled" as its message.</summary>
        public void Cancel(DownloadJob job)
        {
            if (job.CanCancel)
            {
                job.Cancellation.Cancel();
            }
        }

        /// <summary>Stops every download, e.g. right before the game quits to restart. Installs already running finish.</summary>
        public void CancelAll()
        {
            _shutdown.Cancel();
        }

        private void Enqueue(int modId, int fileId, string title, NxmLink? nxm)
        {
            if (!_nexus.IsLoggedIn)
            {
                ModEntry.Toast("Log in to Nexus Mods first (Account tab).", error: true);
                return;
            }

            if (Jobs.Any(j => j.ModId == modId && j.FileId == fileId && j.IsActive))
            {
                return;
            }

            var job = new DownloadJob { ModId = modId, FileId = fileId, Title = title, Cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token) };
            Jobs.Insert(0, job);
            ModEntry.Toast($"Downloading {title}...");

            var downloads = _mods.Paths.DownloadsFolder;
            var token = job.Cancellation.Token;

            Task.Run(async () =>
            {
                var entered = false;
                string? archivePath = null;
                try
                {
                    // Waiting with the token lets a queued job be cancelled without waiting for the one ahead of it
                    await _gate.WaitAsync(token);
                    entered = true;

                    // Free-account links don't carry a name; look up the file for a friendly title
                    if (nxm is not null)
                    {
                        try
                        {
                            var mod = await _nexus.Api.GetModAsync(modId, token);
                            var files = await _nexus.Api.GetFilesAsync(modId, token);
                            var file = files.FirstOrDefault(f => f.FileId == fileId);
                            var name = mod?.Name ?? title;
                            MainThread.Post(() => job.Title = file?.Version is null ? name : $"{name} {file.Version}");
                        }
                        catch (Exception ex) when (!token.IsCancellationRequested)
                        {
                            // Only the friendly name is lost; the download itself can still go ahead
                            ModEntry.Log.Trace($"Couldn't look up the name of {modId}/{fileId}: {ex.Message}");
                        }
                    }

                    MainThread.Post(() =>
                    {
                        if (job.State == DownloadState.Queued)
                        {
                            job.State = DownloadState.Downloading;
                        }
                    });
                    var url = await _nexus.Api.GetDownloadLinkAsync(modId, fileId, nxm, token)
                        ?? throw new Exception("Nexus didn't return a download link.");

                    var fileName = DewDropPaths.SafeSegment(Path.GetFileName(new Uri(url).LocalPath));
                    archivePath = Path.Combine(downloads, $"{modId}-{fileId}-{fileName}");
                    var progress = new Progress<double>(p => MainThread.Post(() => job.Progress = p));
                    await _nexus.Api.DownloadAsync(url, archivePath, progress, token);

                    // One step on the main thread: a job that wasn't cancelled turns Installing right there (so a restart
                    // asked for on the same tick waits for it), and the installed list is read only now, not when the
                    // job was queued, since an earlier job may have installed the same mod meanwhile
                    var (installFolder, installed) = await MainThread.Invoke(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        job.State = DownloadState.Installing;
                        return (_mods.InstallFolder, _mods.Installed.ToList());
                    });
                    var result = _mods.Installer.Install(archivePath, installFolder, installed);

                    MainThread.Post(() => OnInstalled(job, result));
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    ModEntry.Log.Trace($"Download of {modId}/{fileId} was cancelled");
                    MainThread.Post(() =>
                    {
                        job.State = DownloadState.Failed;
                        job.Message = "Cancelled.";
                    });
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Download of {modId}/{fileId} failed: {ex}");
                    MainThread.Post(() =>
                    {
                        job.State = DownloadState.Failed;
                        job.Message = ex is TaskCanceledException ? "Nexus Mods took too long to answer. Try again." : ex.Message;
                        job.CanRetry = ex is not NexusApiException { CanRetry: false };
                        ModEntry.Toast($"{job.Title}: {job.Message}", error: true);
                    });
                }
                finally
                {
                    // Installed or not, the archive is done with (DownloadAsync already removes its .part on failure)
                    if (archivePath is not null)
                    {
                        TryDelete(archivePath);
                        TryDelete(archivePath + ".part");
                    }

                    if (entered)
                    {
                        _gate.Release();
                    }
                }
            });
        }

        private void OnInstalled(DownloadJob job, InstallResult result)
        {
            _mods.MarkChanged(result.InstalledIds.Concat(result.UpdatedIds).Concat(result.PendingIds));
            _mods.Reload();

            // Not into a collection profile, nor one whose file couldn't be read (that copy is only a stand-in)
            if (_config.EnableModsOnInstall && _mods.CanEditActiveProfile)
            {
                foreach (var id in result.InstalledIds.Concat(result.UpdatedIds).Concat(result.PendingIds))
                {
                    var mod = _mods.Installed.FirstOrDefault(m => m.SourceId is null && m.UniqueId.Equals(id, StringComparison.OrdinalIgnoreCase));
                    if (mod is not null && !_mods.IsEnabled(mod))
                    {
                        _mods.SetEnabled(mod, true);
                    }
                }
            }

            var count = result.InstalledIds.Count + result.UpdatedIds.Count + result.PendingIds.Count;
            if (count == 0)
            {
                job.State = DownloadState.Failed;
                job.Message = string.Join(" ", result.Errors);
                ModEntry.Toast($"{job.Title}: {job.Message}", error: true);
                return;
            }

            job.State = DownloadState.Done;
            job.Progress = 1;
            job.Message = result.PendingIds.Count > 0 && result.InstalledIds.Count + result.UpdatedIds.Count == 0
                ? "Update ready, finishes when the game restarts"
                : result.UpdatedIds.Count > 0 && result.InstalledIds.Count == 0
                    ? "Updated"
                    : $"Installed {count} mod{(count == 1 ? "" : "s")}";
            if (result.Errors.Count > 0)
            {
                job.Message += $" ({result.Errors.Count} skipped)";
            }

            ModEntry.Toast($"{job.Title}: {job.Message}. Restart to load it.");
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // A leftover archive in the downloads folder is harmless
            }
        }
    }
}
