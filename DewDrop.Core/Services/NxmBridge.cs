using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Free Nexus accounts can only download through the website's "Mod manager download" button, which opens an
    /// nxm:// link. DewDrop registers a tiny script as the nxm handler; the script drops each link into a queue
    /// folder that the running game polls.
    /// </summary>
    public class NxmBridge
    {
        private readonly DewDropPaths _paths;
        private readonly ILog _log;

        public NxmBridge(DewDropPaths paths, ILog log)
        {
            _paths = paths;
            _log = log;
        }

        /// <summary>Takes every queued link, oldest first, removing them from the queue.</summary>
        public List<string> Drain()
        {
            var links = new List<string>();
            if (!Directory.Exists(_paths.NxmQueueFolder))
            {
                return links;
            }

            foreach (var file in new DirectoryInfo(_paths.NxmQueueFolder).GetFiles("*.nxm").OrderBy(f => f.CreationTimeUtc))
            {
                try
                {
                    var link = File.ReadAllText(file.FullName).Trim();
                    file.Delete();
                    if (link.Length > 0)
                    {
                        links.Add(link);
                    }
                }
                catch (IOException)
                {
                    // Still being written by the handler; pick it up next poll
                }
            }

            return links;
        }

        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private const string UrlAssociationPath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\nxm";

        /// <summary>
        /// The command Windows runs for nxm:// links, if any. An app picked in Windows' Default apps (the UserChoice)
        /// wins over the plain nxm class that <see cref="Register"/> writes, so that one is reported when set.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public string? CurrentHandler()
        {
            var progId = UserChoiceProgId();
            if (progId is not null)
            {
                using var chosen = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
                return chosen?.GetValue(null) as string ?? progId;
            }

            return ClassCommand();
        }

        /// <summary>The command under the plain nxm class, which Windows uses when no app was picked in Default apps.</summary>
        [SupportedOSPlatform("windows")]
        private static string? ClassCommand()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\nxm\shell\open\command");
            return key?.GetValue(null) as string;
        }

        /// <summary>
        /// The app chosen for nxm:// in Windows' Default apps, when one other than the plain nxm class is set.
        /// Links then go there whatever <see cref="Register"/> writes; only the player can change it, in
        /// Settings &gt; Apps &gt; Default apps (Windows protects that choice from other programs).
        /// </summary>
        [SupportedOSPlatform("windows")]
        public string? UserChoiceProgId()
        {
            // Newer Windows 11 builds keep the choice in UserChoiceLatest, older ones in UserChoice
            using var latest = Registry.CurrentUser.OpenSubKey(UrlAssociationPath + @"\UserChoiceLatest\ProgId");
            using var choice = Registry.CurrentUser.OpenSubKey(UrlAssociationPath + @"\UserChoice");
            var progId = latest?.GetValue("ProgId") as string ?? choice?.GetValue("ProgId") as string;
            return string.IsNullOrWhiteSpace(progId) || progId.Equals("nxm", StringComparison.OrdinalIgnoreCase) ? null : progId;
        }

        [SupportedOSPlatform("windows")]
        public bool IsRegistered(string scriptPath)
        {
            return CurrentHandler()?.Contains(scriptPath, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>Points nxm:// at the handler script, under the current user only.</summary>
        [SupportedOSPlatform("windows")]
        public void Register(string scriptPath)
        {
            WriteHandlerScript(scriptPath);

            using var root = Registry.CurrentUser.CreateSubKey(@"Software\Classes\nxm");
            root.SetValue(null, "URL:NXM Protocol");
            root.SetValue("URL Protocol", "");
            using var command = root.CreateSubKey(@"shell\open\command");
            command.SetValue(null, $"powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\" \"%1\"");
            _log.Info("Registered DewDrop as the nxm:// handler");
        }

        /// <summary>Rewrites the handler script if DewDrop is registered for nxm:// links, so a script from an older version is replaced.</summary>
        [SupportedOSPlatform("windows")]
        public void RefreshHandlerScript(string scriptPath)
        {
            try
            {
                // Checked against the nxm class rather than the Default apps choice, which the player can change back
                if (ClassCommand()?.Contains(scriptPath, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    WriteHandlerScript(scriptPath);
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't update the nxm:// handler script: {ex.Message}");
            }
        }

        private void WriteHandlerScript(string scriptPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);

            // With a BOM, Windows PowerShell reads the file as UTF-8 rather than the ANSI code page, which would garble a
            // queue path with accented or CJK characters (from the Windows user name)
            File.WriteAllText(scriptPath, HandlerScript(scriptPath, _paths.NxmQueueFolder), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string HandlerScript(string scriptPath, string queueFolder)
        {
            // Beside the script, the queue is found from the script's own location, so no path is written into it
            var queue = string.Equals(Path.GetDirectoryName(Path.GetFullPath(scriptPath)), Path.GetDirectoryName(Path.GetFullPath(queueFolder)), StringComparison.OrdinalIgnoreCase)
                ? $"Join-Path $PSScriptRoot '{Path.GetFileName(queueFolder).Replace("'", "''")}'"
                : $"'{queueFolder.Replace("'", "''")}'";
            return $@"# DewDrop nxm:// handler: queues the link for the running game to pick up.
param([string]$link)
$queue = {queue}
[void][System.IO.Directory]::CreateDirectory($queue)
$name = [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff') + '-' + [Guid]::NewGuid().ToString('N') + '.nxm'
$temp = Join-Path $queue ($name + '.tmp')
[System.IO.File]::WriteAllText($temp, $link)
[System.IO.File]::Move($temp, (Join-Path $queue $name))
";
        }
    }
}
