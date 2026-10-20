using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Restarts the game through SMAPI once the current process has exited. A detached shell waits on our PID,
    /// finishes any mod updates and uninstalls that were blocked by the running game, then starts SMAPI pointed at the
    /// linked mods folder, so the new mod set loads in a clean process. On Windows the same script can instead just
    /// finish those, and change Steam's launch option for the game (see <see cref="SteamJob"/>), without starting it.
    /// </summary>
    public static class Relauncher
    {
        /// <summary>The variable the restarted game finds its restart ID in (see <see cref="RestartRecord"/>).</summary>
        public const string LaunchIdVariable = "DEWDROP_LAUNCH";

        /// <summary>
        /// What a relaunch starts: SMAPI on Windows and macOS, and on Linux the game's own launcher, which SMAPI's
        /// installer replaces with its own.
        /// </summary>
        public static string LauncherPath(string gamePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(gamePath, "StardewModdingAPI.exe");
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine(gamePath, "StardewModdingAPI")
                : Path.Combine(gamePath, "StardewValley");
        }

        /// <summary>Whether this Windows build of the game is running through Wine or Proton, where the relaunch can't work.</summary>
        public static bool IsWine()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            try
            {
                var ntdll = GetModuleHandle("ntdll.dll");
                return ntdll != IntPtr.Zero && GetProcAddress(ntdll, "wine_get_version") != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        /// <summary>
        /// A launch option change for the script to make once the game has closed: Steam is closed (and opened again
        /// after), and the option is written only if it still reads <see cref="Old"/>, so a change the player made in
        /// Steam meanwhile is never overwritten. The outcome goes to <see cref="ResultFile"/> for the next session.
        /// </summary>
        public record SteamJob(string SteamExe, string ConfigFile, string Old, string New, string ResultFile);

        /// <summary>What the script reports about a <see cref="SteamJob"/>.</summary>
        public record SteamJobResult(bool Ok, string Message, string Value);

        /// <param name="launchId">Handed to the new game as <see cref="LaunchIdVariable"/>, so it can tell it's this restart.</param>
        public static void Schedule(string gamePath, string modsPath, string pendingUpdatesFile, string scriptFolder, ILog log, string? launchId = null)
        {
            Start(gamePath, modsPath, pendingUpdatesFile, scriptFolder, log, launchId, relaunch: true, steamJob: null);
        }

        /// <summary>
        /// Windows only: once the game has closed, finishes waiting mod updates and uninstalls and makes a Steam launch
        /// option change, without starting the game again. Does nothing elsewhere, where open files never block a move
        /// and Steam isn't changed.
        /// </summary>
        public static void ScheduleAfterExit(string gamePath, string modsPath, string pendingUpdatesFile, string scriptFolder, ILog log, SteamJob? steamJob)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Start(gamePath, modsPath, pendingUpdatesFile, scriptFolder, log, launchId: null, relaunch: false, steamJob);
            }
        }

        private static void Start(string gamePath, string modsPath, string pendingUpdatesFile, string scriptFolder, ILog log, string? launchId, bool relaunch, SteamJob? steamJob)
        {
            var pid = Environment.ProcessId;
            ProcessStartInfo info;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Directory.CreateDirectory(scriptFolder);
                var script = Path.Combine(scriptFolder, "relaunch.ps1");
                File.WriteAllText(script, WindowsScript);

                info = new ProcessStartInfo("powershell.exe")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                foreach (var arg in new[]
                {
                    "-NoProfile", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden", "-File", script,
                    "-GamePid", pid.ToString(), "-GamePath", gamePath, "-ModsPath", modsPath, "-PendingFile", pendingUpdatesFile
                })
                {
                    info.ArgumentList.Add(arg);
                }

                // Windows PowerShell rejects an empty value after a parameter name, so the ID only goes in when there is one
                if (!string.IsNullOrEmpty(launchId))
                {
                    info.ArgumentList.Add("-LaunchId");
                    info.ArgumentList.Add(launchId);
                }

                if (!relaunch)
                {
                    info.ArgumentList.Add("-NoRelaunch");
                }

                // Launch options hold quotes, which Windows PowerShell mangles in arguments, so they go in a file
                if (steamJob is not null)
                {
                    var jobFile = Path.Combine(scriptFolder, "steam-job.json");
                    File.WriteAllText(jobFile, System.Text.Json.JsonSerializer.Serialize(steamJob));
                    info.ArgumentList.Add("-SteamJob");
                    info.ArgumentList.Add(jobFile);
                }
            }
            else
            {
                // Unix lets open files be replaced, so updates never wait for a restart there
                var launcher = LauncherPath(gamePath);
                string Quote(string value) => "'" + value.Replace("'", "'\\''") + "'";
                var script = $"while kill -0 {pid} 2>/dev/null; do sleep 0.5; done; cd {Quote(gamePath)} && {LaunchIdVariable}={Quote(launchId ?? "")} SMAPI_MODS_PATH={Quote(modsPath)} nohup {Quote(launcher)} --mods-path {Quote(modsPath)} >/dev/null 2>&1 &";
                info = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
                info.ArgumentList.Add("-c");
                info.ArgumentList.Add(script);
            }

            log.Info(relaunch
                ? $"Scheduling a relaunch through SMAPI with mods from {modsPath}"
                : $"Scheduling {(steamJob is null ? "the waiting mod updates and uninstalls" : "the Steam launch option change")} for when the game closes");
            Process.Start(info);
        }

        // Mirrors PendingUpdates.TrySwap: carry the player's files over, rename the old folder aside, rename the new
        // one in, put the old one back on failure. Folders are moved with [IO.Directory]::Move, a single rename that
        // fails as a whole while a file is in use; Move-Item falls back to moving files one at a time and can leave
        // a mod split between two folders. Paths only arrive as arguments, so the script itself stays plain ASCII.
        internal const string WindowsScript = @"# DewDrop relauncher: waits for the game to close, finishes blocked mod updates and uninstalls, can change Steam's launch
# option for the game, then starts SMAPI again (unless -NoRelaunch).
param([int]$GamePid, [string]$GamePath, [string]$ModsPath, [string]$PendingFile, [string]$LaunchId, [switch]$NoRelaunch, [string]$SteamJob)

$logFile = Join-Path $PSScriptRoot 'relaunch.log'
function Write-Log([string]$Message) {
    try { [System.IO.File]::AppendAllText($logFile, (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $Message + [Environment]::NewLine) } catch { }
}

# Same rules as PendingUpdates.CarryOverPlayerFiles: every config.json, plus files under data\ and top-level .json
# files the new version doesn't ship. Files it adds go in $Added, so a failed swap can take them back out.
function Copy-PlayerFiles([string]$From, [string]$To, $Added) {
    $root = [System.IO.Path]::GetFullPath($From).TrimEnd('\') + '\'
    foreach ($file in [System.IO.Directory]::GetFiles($From, '*', [System.IO.SearchOption]::AllDirectories)) {
        $relative = $file.Substring($root.Length)
        $name = [System.IO.Path]::GetFileName($relative)
        $isConfig = $name -eq 'config.json'
        $isData = $relative.StartsWith('data\', [StringComparison]::OrdinalIgnoreCase) -or (-not $relative.Contains('\') -and $name.EndsWith('.json', [StringComparison]::OrdinalIgnoreCase))
        $dest = [System.IO.Path]::Combine($To, $relative)
        $exists = [System.IO.File]::Exists($dest)
        if ($isConfig -or ($isData -and -not $exists)) {
            [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($dest))
            [System.IO.File]::Copy($file, $dest, $true)
            if (-not $exists) { $Added.Add($dest) }
        }
    }
}

# Same checks as PendingUpdates.TryRemove: the copy may only go to the name DewDrop reserved for it ('<32 hex>-removed'
# in the staging folder beside it), and the folder must still hold that mod. Returns 'ok', 'drop' (nothing to remove) or
# 'keep' when it can't tell: ConvertFrom-Json rejects the comments and trailing commas SMAPI allows, so DewDrop's own
# reader decides at the next start.
function Test-Removal($Swap) {
    $target = [System.IO.Path]::GetFullPath([string]$Swap.Target).TrimEnd('\')
    $staged = [System.IO.Path]::GetFullPath([string]$Swap.Staged).TrimEnd('\')
    $root = [System.IO.Path]::GetDirectoryName($staged)
    if ([System.IO.Path]::GetFileName($staged) -notmatch '^[0-9a-f]{32}-removed$' -or [System.IO.Path]::GetFileName($root) -ne '.dewdrop-staging' -or [System.IO.Path]::GetDirectoryName($root) -ne [System.IO.Path]::GetDirectoryName($target)) {
        Write-Log ('Did not uninstall ' + $Swap.Target + ': its record does not look like one DewDrop made')
        return 'drop'
    }
    $manifest = [System.IO.Path]::Combine($target, 'manifest.json')
    if (-not [System.IO.File]::Exists($manifest)) { Write-Log ('Did not uninstall ' + $Swap.Target + ': it is gone'); return 'drop' }
    # Only the top-level UniqueID counts; a content pack names its framework's UniqueID too
    $id = $null
    try { $id = [string](Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop).UniqueID } catch { }
    if ($id -and $id -eq [string]$Swap.UniqueId) { return 'ok' }
    Write-Log ('Left ' + $Swap.Target + ' for DewDrop to check at the next start')
    return 'keep'
}

# Links in the profile's folder that lead to an uninstalled mod would lead nowhere once it's gone
function Remove-LinksTo([string]$Target) {
    if (-not $ModsPath -or -not [System.IO.Directory]::Exists($ModsPath)) { return }
    $want = [System.IO.Path]::GetFullPath($Target).TrimEnd('\')
    foreach ($dir in [System.IO.Directory]::GetDirectories($ModsPath)) {
        try {
            if (-not ([System.IO.File]::GetAttributes($dir) -band [System.IO.FileAttributes]::ReparsePoint)) { continue }
            $to = [string]@((Get-Item -LiteralPath $dir -Force).Target)[0]
            if ($to.StartsWith('\\?\') -or $to.StartsWith('\??\')) { $to = $to.Substring(4) }
            if ($to -and [System.IO.Path]::GetFullPath($to).TrimEnd('\') -eq $want) { [System.IO.Directory]::Delete($dir, $false) }
        } catch { Write-Log ('Could not check the link ' + $dir + ': ' + $_.Exception.Message) }
    }
}

try { [System.IO.File]::WriteAllText($logFile, '') } catch { }
Wait-Process -Id $GamePid -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if ($PendingFile -and (Test-Path -LiteralPath $PendingFile)) {
    $left = @()
    # Assigned before the loop: in Windows PowerShell 5.1 a JSON array piped straight into foreach arrives as one item
    $swaps = $null
    $readOk = $true
    try { $swaps = Get-Content -LiteralPath $PendingFile -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop }
    catch { $readOk = $false; Write-Log ('Could not read ' + $PendingFile + ': ' + $_.Exception.Message) }
    foreach ($swap in $swaps) {
        if ($null -eq $swap) { continue }

        # An uninstall has nothing staged, so it's handled before the check for a staged copy below
        if ($swap.Remove) {
            try {
                $check = Test-Removal $swap
                if ($check -eq 'drop') { continue }
                if ($check -ne 'ok') { $left += $swap; continue }
                [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($swap.Staged))
                [System.IO.Directory]::Move($swap.Target, $swap.Staged)
                Write-Log ('Uninstalled ' + $swap.UniqueId)
                Remove-LinksTo $swap.Target
                try { [System.IO.Directory]::Delete($swap.Staged, $true) } catch { Write-Log ('Left the uninstalled copy at ' + $swap.Staged) }
            }
            catch {
                Write-Log ('Could not uninstall ' + $swap.UniqueId + ' yet: ' + $_.Exception.Message)
                $left += $swap
            }
            continue
        }

        $old = $null
        $carried = New-Object 'System.Collections.Generic.List[string]'
        try {
            if (-not [System.IO.Directory]::Exists($swap.Staged)) { continue }
            if ([System.IO.Directory]::Exists($swap.Target)) {
                Copy-PlayerFiles $swap.Target $swap.Staged $carried
                # A leftover from an earlier run would block the rename, so clear it or pick a fresh name
                $old = $swap.Staged + '-old'
                if ([System.IO.Directory]::Exists($old)) { try { [System.IO.Directory]::Delete($old, $true) } catch { } }
                if ([System.IO.Directory]::Exists($old)) { $old = $swap.Staged + '-old-' + [Guid]::NewGuid().ToString('N') }
                [System.IO.Directory]::Move($swap.Target, $old)
            }
            [System.IO.Directory]::Move($swap.Staged, $swap.Target)
            Write-Log ('Updated ' + $swap.UniqueId)
            if ($old) { try { [System.IO.Directory]::Delete($old, $true) } catch { Write-Log ('Left the old copy at ' + $old) } }
        }
        catch {
            Write-Log ('Could not update ' + $swap.UniqueId + ' yet: ' + $_.Exception.Message)
            if ($old -and [System.IO.Directory]::Exists($old) -and -not [System.IO.Directory]::Exists($swap.Target)) {
                try { [System.IO.Directory]::Move($old, $swap.Target) } catch { Write-Log ('Could not put back ' + $swap.Target + ', it is at ' + $old) }
            }
            foreach ($file in $carried) { try { [System.IO.File]::Delete($file) } catch { } }
            $left += $swap
        }
    }
    # A list that couldn't be read is left for DewDrop to try at the next start
    if (-not $readOk) { }
    elseif ($left.Count -gt 0) { ConvertTo-Json -InputObject @($left) | Set-Content -LiteralPath $PendingFile -Encoding UTF8 }
    else { Remove-Item -LiteralPath $PendingFile -Force -ErrorAction SilentlyContinue }
}

# Steam's launch option for the game. Steam writes localconfig.vdf over when it closes, so it's closed first and
# opened again after. Same parsing as the installer's Find-SteamAppBlock.
function Find-AppBlock([string]$Text) {
    $names = New-Object System.Collections.Generic.List[string]
    $opens = New-Object System.Collections.Generic.List[int]
    $pendingKey = $null
    $result = @{ AppsOpen = -1; AppsDepth = -1; AppOpen = -1; AppDepth = -1; AppClosed = $false; Options = $null }
    foreach ($t in [regex]::Matches($Text, '""((?:[^""\\]|\\.)*)""|\{|\}')) {
        $v = $t.Value
        if ($v -eq '{') {
            $name = ''
            if ($null -ne $pendingKey) { $name = $pendingKey.Groups[1].Value.ToLowerInvariant() }
            $names.Add($name)
            $opens.Add($t.Index)
            $pendingKey = $null
            $n = $names.Count
            if ($n -ge 4 -and $names[$n - 1] -eq 'apps' -and $names[$n - 2] -eq 'steam' -and $names[$n - 3] -eq 'valve' -and $names[$n - 4] -eq 'software') {
                $result.AppsOpen = $t.Index; $result.AppsDepth = $n
            } elseif ($n -ge 5 -and $result.AppOpen -lt 0 -and $names[$n - 1] -eq '413150' -and $names[$n - 2] -eq 'apps' -and $names[$n - 3] -eq 'steam' -and $names[$n - 4] -eq 'valve' -and $names[$n - 5] -eq 'software') {
                $result.AppOpen = $t.Index; $result.AppDepth = $n
            }
        } elseif ($v -eq '}') {
            if ($names.Count -gt 0) {
                if ($opens[$opens.Count - 1] -eq $result.AppOpen) { $result.AppClosed = $true }
                $names.RemoveAt($names.Count - 1)
                $opens.RemoveAt($opens.Count - 1)
            }
            $pendingKey = $null
        } elseif ($null -eq $pendingKey) {
            $pendingKey = $t
        } else {
            if ($result.AppOpen -ge 0 -and -not $result.AppClosed -and $names.Count -eq $result.AppDepth -and $pendingKey.Groups[1].Value -ieq 'LaunchOptions') {
                $result.Options = $t
            }
            $pendingKey = $null
        }
    }
    return $result
}
# Steam itself, by its path, so no other program that happens to be called steam is closed
function Get-SteamProcesses([string]$Exe) {
    return @(Get-Process -Name 'steam' -ErrorAction SilentlyContinue | Where-Object { -not $_.Path -or [string]::Equals($_.Path, $Exe, [StringComparison]::OrdinalIgnoreCase) })
}
function ConvertFrom-VdfString([string]$Value) { return ($Value -replace '\\""', '""') -replace '\\\\', '\' }
function ConvertTo-VdfString([string]$Value) { return ($Value -replace '\\', '\\') -replace '""', '\""' }

if ($SteamJob -and (Test-Path -LiteralPath $SteamJob)) {
    $job = $null
    $ok = $false
    $message = ''
    $closedSteam = $false
    try {
        $job = Get-Content -LiteralPath $SteamJob -Raw -Encoding UTF8 | ConvertFrom-Json
        if ((Get-SteamProcesses $job.SteamExe).Count -gt 0) {
            # Steam finishes with the game (cloud saves, playtime) for a moment after it closes
            Start-Sleep -Seconds 5
            Start-Process -FilePath $job.SteamExe -ArgumentList '-shutdown' | Out-Null
            $closedSteam = $true
            $deadline = (Get-Date).AddSeconds(90)
            while ((Get-SteamProcesses $job.SteamExe).Count -gt 0) {
                if ((Get-Date) -gt $deadline) { throw ""Steam didn't close, so its launch option wasn't changed."" }
                Start-Sleep -Milliseconds 500
            }
            Start-Sleep -Seconds 1
        }

        $text = [System.IO.File]::ReadAllText($job.ConfigFile, [System.Text.Encoding]::UTF8)
        $b = Find-AppBlock $text
        if ($b.AppsOpen -lt 0) { throw ""Steam's settings file has an unexpected layout, so DewDrop left it alone."" }
        $current = ''
        if ($b.Options) { $current = ConvertFrom-VdfString $b.Options.Groups[1].Value }
        if ($current -cne [string]$job.Old) { throw ""Stardew Valley's launch options changed in Steam while the game was running, so DewDrop left them alone."" }

        $escaped = ConvertTo-VdfString ([string]$job.New)
        if ($b.Options) {
            $g = $b.Options.Groups[1]
            $new = $text.Substring(0, $g.Index) + $escaped + $text.Substring($g.Index + $g.Length)
        } elseif ($b.AppOpen -ge 0) {
            $indent = ""`n"" + (""`t"" * $b.AppDepth)
            $new = $text.Substring(0, $b.AppOpen + 1) + $indent + '""LaunchOptions""' + ""`t`t"" + '""' + $escaped + '""' + $text.Substring($b.AppOpen + 1)
        } else {
            $indent = ""`n"" + (""`t"" * $b.AppsDepth)
            $insert = $indent + '""413150""' + $indent + '{' + $indent + ""`t"" + '""LaunchOptions""' + ""`t`t"" + '""' + $escaped + '""' + $indent + '}'
            $new = $text.Substring(0, $b.AppsOpen + 1) + $insert + $text.Substring($b.AppsOpen + 1)
        }
        $check = Find-AppBlock $new
        if (-not $check.Options -or (ConvertFrom-VdfString $check.Options.Groups[1].Value) -cne [string]$job.New) { throw ""The change to Steam's settings didn't check out, so DewDrop left them alone."" }
        [System.IO.File]::Copy($job.ConfigFile, $job.ConfigFile + '.dewdrop-backup', $true)
        [System.IO.File]::WriteAllText($job.ConfigFile, $new, (New-Object System.Text.UTF8Encoding $false))
        $ok = $true
        Write-Log ('Set the Steam launch option to: ' + $job.New)
    }
    catch {
        $message = $_.Exception.Message
        Write-Log ('Could not change the Steam launch option: ' + $message)
    }
    finally {
        if ($closedSteam -and $job) {
            # This script inherited the game's environment; Steam would hand it to every game it starts after
            foreach ($name in @('SMAPI_MODS_PATH', 'DEWDROP_LAUNCH', 'SteamAppId', 'SteamGameId', 'SteamOverlayGameId', 'SteamClientLaunch', 'SteamEnv', 'SteamPath', 'ENABLE_VK_LAYER_VALVE_steam_overlay_1')) {
                Remove-Item -LiteralPath ('Env:' + $name) -ErrorAction SilentlyContinue
            }
            try { Start-Process -FilePath $job.SteamExe -ArgumentList '-silent' | Out-Null } catch { Write-Log ('Could not start Steam again: ' + $_.Exception.Message) }
        }
        if ($job -and $job.ResultFile) {
            try { ConvertTo-Json -InputObject @{ Ok = $ok; Message = $message; Value = [string]$job.New } | Set-Content -LiteralPath $job.ResultFile -Encoding UTF8 } catch { }
        }
        Remove-Item -LiteralPath $SteamJob -Force -ErrorAction SilentlyContinue
    }
}

if ($NoRelaunch) { exit }

# Started through .NET because Start-Process reads -WorkingDirectory as a wildcard, so a game path with [ ] fails.
# --mods-path is enough on Windows; SMAPI_MODS_PATH would follow the game into anything it starts (Steam, above)
Remove-Item -LiteralPath 'Env:SMAPI_MODS_PATH' -ErrorAction SilentlyContinue
if ($LaunchId) { $env:DEWDROP_LAUNCH = $LaunchId }
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = [System.IO.Path]::Combine($GamePath, 'StardewModdingAPI.exe')
$start.WorkingDirectory = $GamePath
$start.Arguments = '--mods-path ""' + $ModsPath.TrimEnd('\') + '""'
$start.UseShellExecute = $true
try { [void][System.Diagnostics.Process]::Start($start) } catch { Write-Log ('Could not start SMAPI: ' + $_.Exception.Message) }
";
    }
}
