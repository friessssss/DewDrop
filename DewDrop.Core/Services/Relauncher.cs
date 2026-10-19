using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Restarts the game through SMAPI once the current process has exited. A detached shell waits on our PID,
    /// finishes any mod updates that were blocked by the running game, then starts SMAPI pointed at the linked
    /// mods folder, so the new mod set loads in a clean process.
    /// </summary>
    public static class Relauncher
    {
        public static void Schedule(string gamePath, string modsPath, string pendingUpdatesFile, string scriptFolder, ILog log)
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
            }
            else
            {
                // Unix lets open files be replaced, so updates never wait for a restart there
                var launcher = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? Path.Combine(gamePath, "StardewModdingAPI")
                    : Path.Combine(gamePath, "StardewValley");
                string Quote(string value) => "'" + value.Replace("'", "'\\''") + "'";
                var script = $"while kill -0 {pid} 2>/dev/null; do sleep 0.5; done; cd {Quote(gamePath)} && SMAPI_MODS_PATH={Quote(modsPath)} nohup {Quote(launcher)} --mods-path {Quote(modsPath)} >/dev/null 2>&1 &";
                info = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
                info.ArgumentList.Add("-c");
                info.ArgumentList.Add(script);
            }

            log.Info($"Scheduling a relaunch through SMAPI with mods from {modsPath}");
            Process.Start(info);
        }

        // Mirrors PendingUpdates.TrySwap: carry the player's files over, rename the old folder aside, rename the new
        // one in, put the old one back on failure. Folders are moved with [IO.Directory]::Move, a single rename that
        // fails as a whole while a file is in use; Move-Item falls back to moving files one at a time and can leave
        // a mod split between two folders. Paths only arrive as arguments, so the script itself stays plain ASCII.
        private const string WindowsScript = @"# DewDrop relauncher: waits for the game to close, finishes blocked mod updates, starts SMAPI again.
param([int]$GamePid, [string]$GamePath, [string]$ModsPath, [string]$PendingFile)

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

# Started through .NET because Start-Process reads -WorkingDirectory as a wildcard, so a game path with [ ] fails
$env:SMAPI_MODS_PATH = $ModsPath
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = [System.IO.Path]::Combine($GamePath, 'StardewModdingAPI.exe')
$start.WorkingDirectory = $GamePath
$start.Arguments = '--mods-path ""' + $ModsPath.TrimEnd('\') + '""'
$start.UseShellExecute = $true
try { [void][System.Diagnostics.Process]::Start($start) } catch { Write-Log ('Could not start SMAPI: ' + $_.Exception.Message) }
";
    }
}
