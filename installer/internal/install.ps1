<#
.SYNOPSIS
    Installs DewDrop into Stardew Valley, installing or updating SMAPI first if needed.

.DESCRIPTION
    The beginner-friendly way to install DewDrop on Windows. It:
      1. finds your Stardew Valley folder (Steam, GOG or Xbox app), or lets you pick it;
      2. checks SMAPI, and if it's missing or out of date, downloads the official installer from
         github.com/Pathoschild/SMAPI and runs it for you;
      3. copies DewDrop into your Mods folder, keeping your DewDrop settings when updating;
      4. on Steam, offers to set the launch option that starts the game with SMAPI, and (once DewDrop has run) to
         start it in your DewDrop profile;
      5. offers to start the game.

    It never deletes your mods, saves or settings: an old DewDrop folder is replaced only after the new one is in
    place, and Steam's config file is backed up before it's changed. Everything printed is also saved to a log file in %TEMP%.

    This file must stay plain ASCII: Windows PowerShell 5.1 reads scripts without a BOM as ANSI.

    Exit codes: 0 when everything worked, 10 when the installer stopped with a message. "Install on Windows.bat"
    treats any other code as PowerShell failing to run the script at all.

.PARAMETER GamePath
    The Stardew Valley folder (the one containing "Stardew Valley.dll"). Found automatically if omitted.

.PARAMETER ModSource
    A folder containing DewDrop's manifest.json, or a DewDrop release .zip. Defaults to the copy bundled with
    the installer, or the latest GitHub release when run on its own.

.PARAMETER Yes
    Accept the recommended answer to every question (for unattended installs).

.PARAMETER DryRun
    Show what would happen without changing anything.

.PARAMETER StartInProfile
    Steam: start the game in the DewDrop profile (adds --mods-path to the launch option) without asking.

.PARAMETER NoStartInProfile
    Steam: don't start the game in the DewDrop profile; removes --mods-path from the launch option.

.EXAMPLE
    .\install.ps1
    .\install.ps1 -GamePath "D:\Games\Stardew Valley" -Yes
    .\install.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$GamePath,
    [string]$ModSource,
    [switch]$Yes,
    [switch]$DryRun,
    [switch]$SkipSmapi,
    [switch]$NoSteamLaunchOptions,
    [switch]$StartInProfile,
    [switch]$NoStartInProfile,
    [switch]$NoShortcut,
    [switch]$NoLaunch,
    # Set by "Install on Windows.bat" so the window stays open at the end.
    [switch]$FromLauncher,
    # Set when the installer restarted itself as administrator.
    [switch]$Elevated
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Invoke-WebRequest is far slower with the progress bar in PowerShell 5.1

try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch { }

$DewDropId = 'friessssss.DewDrop'
$DewDropRepo = 'friessssss/DewDrop'
$SmapiRepo = 'Pathoschild/SMAPI'
$MinSmapiVersion = [version]'4.0.0'
$MinGameVersion = [version]'1.6.0'
$SteamAppId = '413150'
$UserAgent = 'DewDrop-Installer'
$LogPath = Join-Path $env:TEMP 'DewDrop-installer.log'
$ScriptParams = $PSBoundParameters
$Work = Join-Path $env:TEMP ('dewdrop-installer-' + [guid]::NewGuid().ToString('N').Substring(0, 8))

# --------------------------------------------------------------------------------------------------------------
# Output

function Write-Banner {
    Write-Host ''
    Write-Host '   .    ' -ForegroundColor Cyan -NoNewline; Write-Host ''
    Write-Host '  / \   ' -ForegroundColor Cyan -NoNewline; Write-Host ' DewDrop installer' -ForegroundColor White
    Write-Host ' (   )  ' -ForegroundColor Cyan -NoNewline; Write-Host ' The mod manager that lives inside Stardew Valley' -ForegroundColor Gray
    Write-Host '  `-`   ' -ForegroundColor Cyan -NoNewline; Write-Host ''
    Write-Host ''
    if ($DryRun) {
        Write-Host '  DRY RUN: nothing will be changed. Lines starting with (dry run) show what would happen.' -ForegroundColor Magenta
        Write-Host ''
    }
}

function Write-Step([string]$Text) { Write-Host ''; Write-Host "==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text) { Write-Host "    [ok] $Text" -ForegroundColor Green }
function Write-Note([string]$Text) { Write-Host "         $Text" -ForegroundColor Gray }
function Write-Warn2([string]$Text) { Write-Host "    [!]  $Text" -ForegroundColor Yellow }
function Write-Bad([string]$Text) { Write-Host "    [x]  $Text" -ForegroundColor Red }
function Write-Dry([string]$Text) { Write-Host "    (dry run) would $Text" -ForegroundColor Magenta }

class InstallerStop : System.Exception {
    InstallerStop([string]$message) : base($message) { }
}

function Stop-Installer([string]$Message) { throw [InstallerStop]::new($Message) }

# --------------------------------------------------------------------------------------------------------------
# Questions

function Read-YesNo([string]$Question, [bool]$Default = $true) {
    $hint = if ($Default) { '[Y/n]' } else { '[y/N]' }
    if ($Yes) {
        $answer = if ($Default) { 'yes' } else { 'no' }
        Write-Host "    ? $Question $hint " -ForegroundColor White -NoNewline
        Write-Host "$answer (automatic)" -ForegroundColor DarkGray
        return $Default
    }
    while ($true) {
        Write-Host "    ? $Question $hint " -ForegroundColor White -NoNewline
        $reply = Read-Host
        if ($null -eq $reply) { return $Default }
        $reply = $reply.Trim().ToLowerInvariant()
        if ($reply -eq '') { return $Default }
        if ($reply -in @('y', 'yes')) { return $true }
        if ($reply -in @('n', 'no')) { return $false }
        Write-Host '      Please type Y or N, then press Enter.' -ForegroundColor Gray
    }
}

function Read-Choice([string]$Question, [string[]]$Options) {
    for ($i = 0; $i -lt $Options.Count; $i++) {
        Write-Host ("      [{0}] {1}" -f ($i + 1), $Options[$i])
    }
    if ($Yes) {
        Write-Host "    ? $Question 1 (automatic)" -ForegroundColor DarkGray
        return 0
    }
    while ($true) {
        Write-Host "    ? $Question " -ForegroundColor White -NoNewline
        $reply = Read-Host
        $n = 0
        if ([int]::TryParse("$reply".Trim(), [ref]$n) -and $n -ge 1 -and $n -le $Options.Count) { return $n - 1 }
        Write-Host "      Type a number from 1 to $($Options.Count), then press Enter." -ForegroundColor Gray
    }
}

# --------------------------------------------------------------------------------------------------------------
# Small helpers

function Get-VersionFromText([string]$Text) {
    if ($Text -match '(\d+)\.(\d+)(?:\.(\d+))?') {
        $patch = 0
        if ($Matches[3]) { $patch = [int]$Matches[3] }
        return [version]::new([int]$Matches[1], [int]$Matches[2], $patch)
    }
    return $null
}

function Get-FileVersion([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $info = (Get-Item -LiteralPath $Path).VersionInfo
    $v = Get-VersionFromText $info.ProductVersion
    if ($null -eq $v) { $v = Get-VersionFromText $info.FileVersion }
    return $v
}

function Get-RegistryValue([string]$Hive, [string]$Key, [string]$Name) {
    foreach ($view in @([Microsoft.Win32.RegistryView]::Registry64, [Microsoft.Win32.RegistryView]::Registry32)) {
        try {
            $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($Hive, $view)
            $sub = $base.OpenSubKey($Key)
            if ($sub) {
                $value = $sub.GetValue($Name)
                $sub.Close()
                if ($value) { return [string]$value }
            }
        } catch { }
    }
    return $null
}

function Invoke-Download([string]$Url, [string]$OutFile) {
    $dir = Split-Path -Parent $OutFile
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Invoke-WebRequest -Uri $Url -OutFile $OutFile -UseBasicParsing -Headers @{ 'User-Agent' = $UserAgent }
}

function Get-LatestRelease([string]$Repo) {
    $url = "https://api.github.com/repos/$Repo/releases/latest"
    return Invoke-RestMethod -Uri $url -UseBasicParsing -Headers @{ 'User-Agent' = $UserAgent; 'Accept' = 'application/vnd.github+json' }
}

function Test-GameFolder([string]$Path) {
    if (-not $Path) { return $false }
    return Test-Path -LiteralPath (Join-Path $Path 'Stardew Valley.dll')
}

function Get-GameStore([string]$Path) {
    if ($Path -match '\\steamapps\\') { return 'Steam' }
    if ($Path -match 'ModifiableWindowsApps|XboxGames|WindowsApps') { return 'Xbox' }
    if ($Path -match 'GOG|Galaxy') { return 'GOG' }
    return 'Other'
}

# SMAPI accepts comments and trailing commas in manifest.json; Windows PowerShell's ConvertFrom-Json doesn't, so
# strip them (outside strings) first.
function ConvertFrom-JsonLoose([string]$Text) {
    $sb = New-Object System.Text.StringBuilder
    $inString = $false
    for ($i = 0; $i -lt $Text.Length; $i++) {
        $c = $Text[$i]
        if ($inString) {
            [void]$sb.Append($c)
            if ($c -eq [char]'\' -and $i + 1 -lt $Text.Length) { [void]$sb.Append($Text[$i + 1]); $i++ }
            elseif ($c -eq [char]'"') { $inString = $false }
            continue
        }
        if ($c -eq [char]'"') { $inString = $true; [void]$sb.Append($c); continue }
        if ($c -eq [char]'/' -and $i + 1 -lt $Text.Length) {
            if ($Text[$i + 1] -eq [char]'/') { while ($i -lt $Text.Length -and $Text[$i] -ne [char]"`n") { $i++ }; continue }
            if ($Text[$i + 1] -eq [char]'*') { $end = $Text.IndexOf('*/', $i + 2); if ($end -lt 0) { break }; $i = $end + 1; continue }
        }
        [void]$sb.Append($c)
    }
    return [regex]::Replace($sb.ToString(), ',(\s*[}\]])', '$1') | ConvertFrom-Json
}

# Reads a manifest's own (top-level) UniqueID and Version. A manifest that can't be read counts as "not DewDrop".
function Read-ManifestId([string]$ManifestPath) {
    try {
        $json = ConvertFrom-JsonLoose (Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8)
        return [pscustomobject]@{ Id = [string]$json.UniqueID; Version = [string]$json.Version; Name = [string]$json.Name }
    } catch {
        return [pscustomobject]@{ Id = ''; Version = ''; Name = '' }
    }
}

function Test-Elevated {
    try {
        $id = [Security.Principal.WindowsIdentity]::GetCurrent()
        return ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } catch { return $false }
}

# Starts a program or URL as the signed-in user. From an elevated installer, going through Explorer keeps Steam and
# the game from running as administrator.
function Start-AsUser([string]$Target, [string]$WorkingDirectory) {
    if (Test-Elevated) {
        Start-Process -FilePath 'explorer.exe' -ArgumentList ('"' + $Target + '"')
    } elseif ($WorkingDirectory) {
        Start-Process -FilePath $Target -WorkingDirectory $WorkingDirectory
    } else {
        Start-Process $Target
    }
}

function Test-SamePath([string]$A, [string]$B) {
    try { return [IO.Path]::GetFullPath($A).TrimEnd('\') -ieq [IO.Path]::GetFullPath($B).TrimEnd('\') } catch { return $false }
}

function Test-Writable([string]$Folder) {
    if (-not (Test-Path -LiteralPath $Folder)) { return $true }
    try {
        $probe = Join-Path $Folder ('.dewdrop-write-test-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        [IO.File]::WriteAllText($probe, 'ok')
        Remove-Item -LiteralPath $probe -Force
        return $true
    } catch {
        return $false
    }
}

# --------------------------------------------------------------------------------------------------------------
# Steam

function Get-SteamRoot {
    $path = Get-RegistryValue 'CurrentUser' 'Software\Valve\Steam' 'SteamPath'
    if (-not $path) { $path = Get-RegistryValue 'LocalMachine' 'SOFTWARE\Valve\Steam' 'InstallPath' }
    if (-not $path) { $path = Get-RegistryValue 'LocalMachine' 'SOFTWARE\WOW6432Node\Valve\Steam' 'InstallPath' }
    if ($path) {
        $path = $path.Replace('/', '\')
        if (Test-Path -LiteralPath $path) { return (Get-Item -LiteralPath $path).FullName }
    }
    return $null
}

function Get-SteamLibraries([string]$SteamRoot) {
    $libs = New-Object System.Collections.Generic.List[string]
    if (-not $SteamRoot) { return $libs }
    $libs.Add($SteamRoot)
    foreach ($vdf in @((Join-Path $SteamRoot 'steamapps\libraryfolders.vdf'), (Join-Path $SteamRoot 'config\libraryfolders.vdf'))) {
        if (-not (Test-Path -LiteralPath $vdf)) { continue }
        $text = Get-Content -LiteralPath $vdf -Raw -Encoding UTF8
        foreach ($m in [regex]::Matches($text, '"path"\s+"((?:[^"\\]|\\.)*)"')) {
            $p = $m.Groups[1].Value -replace '\\\\', '\'
            if (-not $libs.Contains($p)) { $libs.Add($p) }
        }
    }
    return $libs
}

# Tokenizes Steam's KeyValues text format: quoted strings and braces, with their positions in the text.
function Get-VdfTokens([string]$Text) {
    return [regex]::Matches($Text, '"((?:[^"\\]|\\.)*)"|\{|\}')
}

# Finds the Stardew Valley block inside UserLocalConfigStore > Software > Valve > Steam > apps. Returns the brace
# positions of the apps and app blocks (-1 when missing), their depths, and the LaunchOptions value match (or $null).
function Find-SteamAppBlock([string]$Text) {
    $tokens = Get-VdfTokens $Text
    $names = New-Object System.Collections.Generic.List[string]
    $opens = New-Object System.Collections.Generic.List[int]
    $pendingKey = $null
    $result = @{ AppsOpen = -1; AppsDepth = -1; AppOpen = -1; AppDepth = -1; AppClosed = $false; Options = $null }
    foreach ($t in $tokens) {
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
            } elseif ($n -ge 5 -and $result.AppOpen -lt 0 -and $names[$n - 1] -eq $SteamAppId -and $names[$n - 2] -eq 'apps' -and $names[$n - 3] -eq 'steam' -and $names[$n - 4] -eq 'valve' -and $names[$n - 5] -eq 'software') {
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

function ConvertFrom-VdfString([string]$Value) { return ($Value -replace '\\"', '"') -replace '\\\\', '\' }
function ConvertTo-VdfString([string]$Value) { return ($Value -replace '\\', '\\') -replace '"', '\"' }

function Get-SteamUserConfigs([string]$SteamRoot) {
    $files = @()
    $userdata = Join-Path $SteamRoot 'userdata'
    if (Test-Path -LiteralPath $userdata) {
        $files = @(Get-ChildItem -LiteralPath $userdata -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName 'config\localconfig.vdf' } |
            Where-Object { Test-Path -LiteralPath $_ })
    }
    return $files
}

# The signed-in Steam account's ID (the folder name under userdata), from the running Steam or, when Steam is
# closed, from the account marked MostRecent in loginusers.vdf.
function Get-SteamActiveAccountId([string]$SteamRoot) {
    $active = Get-RegistryValue 'CurrentUser' 'Software\Valve\Steam\ActiveProcess' 'ActiveUser'
    if ($active -and $active -ne '0') { return $active }
    $login = Join-Path $SteamRoot 'config\loginusers.vdf'
    if (Test-Path -LiteralPath $login) {
        $text = Get-Content -LiteralPath $login -Raw -Encoding UTF8
        $m = [regex]::Match($text, '"(\d{17})"\s*\{[^{}]*?"MostRecent"\s*"1"', 'IgnoreCase')
        if ($m.Success) { return ([int64]$m.Groups[1].Value - 76561197960265728).ToString() }
    }
    return $null
}

# The program a launch option starts: the first quoted path, or the first word.
function Get-LaunchOptionExe([string]$Option) {
    $o = $Option.Trim()
    if ($o.StartsWith('"')) {
        $end = $o.IndexOf('"', 1)
        if ($end -gt 0) { return [pscustomobject]@{ Exe = $o.Substring(1, $end - 1); Rest = $o.Substring($end + 1) } }
    }
    $space = $o.IndexOf(' ')
    if ($space -gt 0) { return [pscustomobject]@{ Exe = $o.Substring(0, $space); Rest = $o.Substring($space) } }
    return [pscustomobject]@{ Exe = $o; Rest = '' }
}

# --------------------------------------------------------------------------------------------------------------
# Steps

# Makes sure the installer can change files in the game folder. A game under Program Files usually needs
# administrator rights; offers to restart the installer that way.
function Assert-Writable([string]$Game, [string[]]$Folders) {
    $blocked = @($Folders | Where-Object { -not (Test-Writable $_) })
    if ($blocked.Count -eq 0) { return }
    Write-Warn2 "Windows won't let me change files in $($blocked[0])."
    if (Test-Elevated) {
        Stop-Installer 'That happens even as administrator. Check that the folder isn''t read-only and that your antivirus isn''t blocking it.'
    }
    Write-Note 'That usually means the game is in a protected folder, such as Program Files.'
    if (-not $Yes -and (Read-YesNo 'Restart the installer as administrator? (Windows will ask you to allow it)' $true)) {
        $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $PSCommandPath + '"'), '-GamePath', ('"' + $Game + '"'), '-FromLauncher', '-Elevated')
        if ($ModSource) { $argList += @('-ModSource', ('"' + $ModSource + '"')) }
        foreach ($name in @('DryRun', 'SkipSmapi', 'NoSteamLaunchOptions', 'StartInProfile', 'NoStartInProfile', 'NoShortcut', 'NoLaunch')) {
            if ($ScriptParams.ContainsKey($name) -and $ScriptParams[$name]) { $argList += "-$name" }
        }
        try {
            Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argList | Out-Null
            $script:HandedOff = $true
            Stop-Installer 'The installer continues in the new administrator window. You can close this one.'
        } catch [InstallerStop] {
            throw
        } catch {
            Write-Warn2 'Windows didn''t start it as administrator.'
        }
    }
    Stop-Installer 'Right-click "Install on Windows.bat", choose "Run as administrator", then try again.'
}

function Find-Game {
    Write-Step 'Looking for Stardew Valley'
    if ($GamePath) {
        $full = [IO.Path]::GetFullPath($GamePath.Trim().Trim('"')).TrimEnd('\')
        if (Test-GameFolder $full) { Write-Ok "Using $full"; return $full }
        Stop-Installer "There's no Stardew Valley.dll in '$GamePath'. Point -GamePath at the folder that contains it."
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    $steamRoot = Get-SteamRoot
    $add = {
        param($p)
        if (-not $p) { return }
        try { $p = [IO.Path]::GetFullPath($p.Trim().Trim('"').TrimEnd('\')) } catch { return }
        if ((Test-GameFolder $p) -and -not ($candidates | Where-Object { $_ -ieq $p })) { $candidates.Add($p) }
    }
    foreach ($lib in (Get-SteamLibraries $steamRoot)) { & $add (Join-Path $lib 'steamapps\common\Stardew Valley') }
    & $add (Get-RegistryValue 'LocalMachine' "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App $SteamAppId" 'InstallLocation')
    & $add (Get-RegistryValue 'LocalMachine' 'SOFTWARE\WOW6432Node\GOG.com\Games\1453375253' 'PATH')
    & $add (Get-RegistryValue 'LocalMachine' 'SOFTWARE\GOG.com\Games\1453375253' 'PATH')
    foreach ($pf in @(${env:ProgramFiles(x86)}, $env:ProgramFiles, 'C:\Program Files (x86)', 'C:\Program Files')) {
        if (-not $pf) { continue }
        foreach ($rel in @('Steam\steamapps\common\Stardew Valley', 'GalaxyClient\Games\Stardew Valley', 'GOG Galaxy\Games\Stardew Valley', 'GOG Games\Stardew Valley')) {
            & $add (Join-Path $pf $rel)
        }
    }
    foreach ($drive in ([IO.DriveInfo]::GetDrives() | Where-Object { $_.DriveType -eq 'Fixed' -and $_.IsReady })) {
        & $add (Join-Path $drive.RootDirectory.FullName 'Program Files\ModifiableWindowsApps\Stardew Valley')
        & $add (Join-Path $drive.RootDirectory.FullName 'XboxGames\Stardew Valley\Content')
    }
    $targets = Join-Path $env:USERPROFILE 'stardewvalley.targets'
    if (Test-Path -LiteralPath $targets) {
        $t = Get-Content -LiteralPath $targets -Raw
        if ($t -match '<GamePath>\s*([^<]+?)\s*</GamePath>') { & $add $Matches[1] }
    }

    if ($candidates.Count -eq 1) {
        Write-Ok "Found it: $($candidates[0]) ($(Get-GameStore $candidates[0]))"
        return $candidates[0]
    }
    if ($candidates.Count -gt 1) {
        Write-Note 'Found more than one copy of the game:'
        $labels = @($candidates | ForEach-Object { "$_  ($(Get-GameStore $_))" })
        $i = Read-Choice 'Which one do you play?' $labels
        Write-Ok "Using $($candidates[$i])"
        return $candidates[$i]
    }

    Write-Warn2 "I couldn't find Stardew Valley automatically."
    if ($Yes) { Stop-Installer 'Run the installer again with -GamePath "<your game folder>".' }
    Write-Note 'Please show me your game folder: the one that contains "Stardew Valley.exe".'
    Write-Note 'Tip: in Steam, right-click Stardew Valley > Manage > Browse local files.'
    while ($true) {
        $picked = $null
        try {
            Add-Type -AssemblyName System.Windows.Forms
            $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
            $dialog.Description = 'Select your Stardew Valley folder (the one containing Stardew Valley.exe)'
            $dialog.ShowNewFolderButton = $false
            if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { $picked = $dialog.SelectedPath }
        } catch { }
        if (-not $picked) {
            Write-Host '    ? Paste the folder path here (or press Enter to quit): ' -ForegroundColor White -NoNewline
            $picked = Read-Host
            if (-not $picked) { Stop-Installer 'No game folder was chosen.' }
        }
        $picked = $picked.Trim().Trim('"')
        foreach ($p in @($picked, (Join-Path $picked 'Content'), (Join-Path $picked 'Stardew Valley'))) {
            if (Test-GameFolder $p) { $full = [IO.Path]::GetFullPath($p).TrimEnd('\'); Write-Ok "Using $full"; return $full }
        }
        Write-Bad "That folder doesn't contain Stardew Valley.dll. Try again."
    }
}

function Test-GameVersion([string]$Game) {
    $v = Get-FileVersion (Join-Path $Game 'Stardew Valley.dll')
    if ($null -eq $v) { Write-Warn2 "Couldn't read the game's version; carrying on."; return }
    if ($v -lt $MinGameVersion) {
        Stop-Installer "This is Stardew Valley $v. DewDrop and SMAPI 4 need Stardew Valley 1.6 or later; update the game first."
    }
    Write-Ok "Stardew Valley $v"
    $appData = Join-Path $env:APPDATA 'StardewValley'
    if (-not (Test-Path -LiteralPath $appData)) {
        Write-Warn2 "It looks like Stardew Valley has never been started on this PC."
        Write-Note 'SMAPI recommends starting the game once without mods first, so it can finish its first-time setup.'
        if (-not (Read-YesNo 'Continue anyway?' $true)) { Stop-Installer 'Start Stardew Valley once, close it at the title screen, then run this installer again.' }
    }
}

function Wait-GameClosed {
    $names = @('Stardew Valley', 'StardewModdingAPI')
    $running = @(Get-Process -Name $names -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) { return }
    Write-Warn2 'Stardew Valley is running. Please close the game (and the SMAPI console window) first.'
    $deadline = (Get-Date).AddMinutes(5)
    while (@(Get-Process -Name $names -ErrorAction SilentlyContinue).Count -gt 0) {
        if ($Yes) {
            if ((Get-Date) -gt $deadline) { Stop-Installer 'The game is still running.' }
            Start-Sleep -Seconds 2
            continue
        }
        Write-Host '    ? Press Enter once the game is closed (or type Q to quit): ' -ForegroundColor White -NoNewline
        $reply = Read-Host
        if ("$reply".Trim().ToLowerInvariant() -eq 'q') { Stop-Installer 'Cancelled while the game was running.' }
    }
    Write-Ok 'The game is closed.'
}

function Install-Smapi([string]$Game) {
    Write-Step 'Checking SMAPI (the mod loader DewDrop runs on)'
    $installed = Get-FileVersion (Join-Path $Game 'StardewModdingAPI.dll')
    if ($installed -and -not (Test-Path -LiteralPath (Join-Path $Game 'StardewModdingAPI.exe'))) { $installed = $null }

    $latest = $null
    $asset = $null
    try {
        $release = Get-LatestRelease $SmapiRepo
        $latest = Get-VersionFromText $release.tag_name
        $asset = $release.assets | Where-Object { $_.name -match '^SMAPI-[\d.]+-installer\.zip$' } | Select-Object -First 1
    } catch {
        Write-Warn2 "Couldn't check GitHub for the latest SMAPI ($($_.Exception.Message))."
    }

    if ($installed) {
        Write-Ok "SMAPI $installed is installed."
        if ($installed -lt $MinSmapiVersion) {
            Write-Warn2 "DewDrop needs SMAPI $MinSmapiVersion or later, so SMAPI has to be updated."
            if ($SkipSmapi) { Stop-Installer "SMAPI $installed is too old for DewDrop and -SkipSmapi was given. Update SMAPI from https://smapi.io." }
        } elseif ($latest -and $installed -lt $latest) {
            Write-Note "SMAPI $latest is available."
            if ($SkipSmapi -or -not (Read-YesNo "Update SMAPI to $($latest)? (recommended)" $true)) { return $installed }
        } else {
            return $installed
        }
    } else {
        Write-Note "SMAPI isn't installed yet. I'll download the official SMAPI installer from github.com/$SmapiRepo"
        Write-Note 'and run it for you.'
        if ($SkipSmapi) { Stop-Installer 'SMAPI is missing and -SkipSmapi was given.' }
        $question = if ($latest) { "Install SMAPI $latest now?" } else { 'Install SMAPI now?' }
        if (-not (Read-YesNo $question $true)) {
            Stop-Installer 'DewDrop needs SMAPI. Install it from https://smapi.io, then run this installer again.'
        }
    }

    if (-not $asset) {
        Stop-Installer "Couldn't download SMAPI automatically. Get it from https://smapi.io (run 'install on Windows.bat' in its zip), then run this installer again."
    }
    if ($DryRun) {
        Write-Dry "download $($asset.name) and run: SMAPI.Installer.exe --install --game-path `"$Game`" --no-prompt"
        return $latest
    }
    Assert-Writable $Game @($Game, (Join-Path $Game 'smapi-internal'), (Join-Path $Game 'Mods'))

    $zip = Join-Path $Work $asset.name
    Write-Note "Downloading $($asset.name) ($([math]::Round($asset.size / 1MB, 1)) MB)..."
    Invoke-Download $asset.browser_download_url $zip
    $extract = Join-Path $Work 'smapi'
    Expand-Archive -LiteralPath $zip -DestinationPath $extract -Force
    $exe = Get-ChildItem -LiteralPath $extract -Recurse -Filter 'SMAPI.Installer.exe' |
        Where-Object { $_.FullName -match '\\internal\\windows\\' } | Select-Object -First 1
    if (-not $exe) { Stop-Installer "The SMAPI download didn't contain internal\windows\SMAPI.Installer.exe. Your antivirus may have removed it." }
    Get-ChildItem -LiteralPath $extract -Recurse -File | Unblock-File -ErrorAction SilentlyContinue

    Write-Note 'Running the SMAPI installer (it opens in its own window and closes when it finishes)...'
    $installerRoot = $exe.Directory.Parent.Parent.FullName
    # SMAPI's installer clears its console, so it can't share or pipe this one. Its own window stays open on errors.
    $smapiArgs = @('--install', '--game-path', ('"' + $Game + '"'), '--no-prompt')
    $proc = Start-Process -FilePath $exe.FullName -ArgumentList $smapiArgs -WorkingDirectory $installerRoot -PassThru
    # It takes seconds. If it can't replace an old file it retries forever, so don't wait forever.
    if (-not $proc.WaitForExit(180000)) {
        try { $proc.Kill() } catch { }
        Stop-Installer "The SMAPI installer didn't finish within 3 minutes, so I stopped it. It probably couldn't replace SMAPI's old files: try running this installer as administrator, or install SMAPI yourself from https://smapi.io."
    }
    if ($proc.ExitCode -ne 0) { Write-Warn2 "The SMAPI installer exited with code $($proc.ExitCode)." }

    $now = Get-FileVersion (Join-Path $Game 'StardewModdingAPI.dll')
    if (-not $now -or ($latest -and $now -lt $latest) -or -not (Test-Path -LiteralPath (Join-Path $Game 'StardewModdingAPI.exe'))) {
        Stop-Installer 'The SMAPI installer finished, but SMAPI still looks missing or out of date. Try running this installer as administrator, or install SMAPI yourself from https://smapi.io.'
    }
    Write-Ok "SMAPI $now is installed."
    return $now
}

function Get-DewDropSource {
    Write-Step 'Getting DewDrop ready'
    $candidates = @()
    if ($ModSource) {
        if (-not (Test-Path -LiteralPath $ModSource)) { Stop-Installer "'$ModSource' doesn't exist." }
        $candidates += $ModSource
    }
    # The installer zip bundles the mod as internal\DewDrop.zip (a zip, so SMAPI can't load it by accident if the
    # installer gets unzipped into the Mods folder). A plain folder next to the script works too, for development.
    $candidates += (Join-Path $PSScriptRoot 'DewDrop.zip')
    $candidates += (Join-Path $PSScriptRoot 'DewDrop')

    foreach ($c in $candidates) {
        if (-not (Test-Path -LiteralPath $c)) { continue }
        $folder = $c
        if ((Get-Item -LiteralPath $c) -is [IO.FileInfo]) {
            $folder = Join-Path $Work 'dewdrop-zip'
            Expand-Archive -LiteralPath $c -DestinationPath $folder -Force
        }
        $manifest = Get-ChildItem -LiteralPath $folder -Recurse -Filter 'manifest.json' -ErrorAction SilentlyContinue |
            Where-Object { (Read-ManifestId $_.FullName).Id -ieq $DewDropId } | Select-Object -First 1
        if ($manifest) {
            $info = Read-ManifestId $manifest.FullName
            Write-Ok "DewDrop $($info.Version) (from $($manifest.Directory.FullName))"
            return [pscustomobject]@{ Folder = $manifest.Directory.FullName; Version = $info.Version }
        }
        if ($ModSource -and $c -eq $ModSource) { Stop-Installer "'$ModSource' doesn't contain DewDrop's manifest.json." }
    }

    Write-Note "No bundled copy of DewDrop next to the installer, so I'll download the latest release from github.com/$DewDropRepo"
    try {
        $release = Get-LatestRelease $DewDropRepo
    } catch {
        Stop-Installer "Couldn't reach GitHub to download DewDrop ($($_.Exception.Message)). Download it from https://github.com/$DewDropRepo/releases and try again."
    }
    $asset = $release.assets | Where-Object { $_.name -match '^DewDrop[- ][\d.]+\.zip$' } | Select-Object -First 1
    if (-not $asset) { Stop-Installer "The latest DewDrop release has no mod zip. Download it from https://github.com/$DewDropRepo/releases." }
    if ($DryRun) {
        Write-Dry "download $($asset.name)"
        return [pscustomobject]@{ Folder = $null; Version = (Get-VersionFromText $release.tag_name) }
    }
    $zip = Join-Path $Work $asset.name
    Invoke-Download $asset.browser_download_url $zip
    $folder = Join-Path $Work 'dewdrop-release'
    Expand-Archive -LiteralPath $zip -DestinationPath $folder -Force
    $manifest = Get-ChildItem -LiteralPath $folder -Recurse -Filter 'manifest.json' |
        Where-Object { (Read-ManifestId $_.FullName).Id -ieq $DewDropId } | Select-Object -First 1
    if (-not $manifest) { Stop-Installer "The downloaded DewDrop zip doesn't contain DewDrop." }
    $info = Read-ManifestId $manifest.FullName
    Write-Ok "Downloaded DewDrop $($info.Version)"
    return [pscustomobject]@{ Folder = $manifest.Directory.FullName; Version = $info.Version }
}

function Find-InstalledDewDrop([string]$Mods) {
    $found = @()
    if (-not (Test-Path -LiteralPath $Mods)) { return $found }
    # SMAPI ignores folders whose names start with a dot, so this does too. Three levels covers grouping folders.
    $queue = New-Object System.Collections.Generic.Queue[object]
    $queue.Enqueue(@{ Path = $Mods; Depth = 0 })
    while ($queue.Count -gt 0) {
        $item = $queue.Dequeue()
        foreach ($dir in (Get-ChildItem -LiteralPath $item.Path -Directory -ErrorAction SilentlyContinue)) {
            if ($dir.Name.StartsWith('.')) { continue }
            if ($dir.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            $manifest = Join-Path $dir.FullName 'manifest.json'
            if (Test-Path -LiteralPath $manifest) {
                $info = Read-ManifestId $manifest
                if ($info.Id -ieq $DewDropId) { $found += [pscustomobject]@{ Folder = $dir.FullName; Version = $info.Version } }
            } elseif ($item.Depth -lt 3) {
                $queue.Enqueue(@{ Path = $dir.FullName; Depth = $item.Depth + 1 })
            }
        }
    }
    return $found
}

function Install-DewDrop([string]$Game, $Source) {
    Write-Step 'Installing DewDrop into your Mods folder'
    $mods = Join-Path $Game 'Mods'
    if (-not (Test-Path -LiteralPath $mods)) {
        if ($DryRun) { Write-Dry "create $mods" } else { New-Item -ItemType Directory -Path $mods -Force | Out-Null }
    }

    $existing = @(Find-InstalledDewDrop $mods | Where-Object { $_.Folder -ine $Source.Folder })
    $target = Join-Path $mods 'DewDrop'
    if ($existing.Count -ge 1) {
        $keep = $existing | Where-Object { $_.Folder -ieq $target } | Select-Object -First 1
        if (-not $keep) { $keep = $existing[0] }
        $target = $keep.Folder
        Write-Note "DewDrop $($keep.Version) is already installed in $target; it'll be updated in place."
        $extras = @($existing | Where-Object { $_.Folder -ne $keep.Folder })
        if ($extras.Count -gt 0) {
            Write-Warn2 'There are other copies of DewDrop in your Mods folder. SMAPI refuses to load a mod twice:'
            $extras | ForEach-Object { Write-Note "  $($_.Folder)" }
            if (Read-YesNo 'Turn the extra copies off? (they get a "." in front of their folder name, so SMAPI skips them)' $true) {
                foreach ($e in $extras) {
                    $parent = Split-Path -Parent $e.Folder
                    $name = '.' + (Split-Path -Leaf $e.Folder) + '-duplicate'
                    $n = 2
                    while (Test-Path -LiteralPath (Join-Path $parent $name)) { $name = '.' + (Split-Path -Leaf $e.Folder) + "-duplicate-$n"; $n++ }
                    if ($DryRun) { Write-Dry "rename $($e.Folder) to $name" } else { Rename-Item -LiteralPath $e.Folder -NewName $name; Write-Ok "Turned off $($e.Folder)" }
                }
            }
        }
    } elseif (Test-Path -LiteralPath $target) {
        Stop-Installer "There's already a folder at $target that isn't DewDrop. Rename or move it, then run the installer again."
    }

    if ($Source.Folder -and ($Source.Folder.TrimEnd('\') -ieq $target.TrimEnd('\'))) {
        Write-Ok "DewDrop $($Source.Version) is already in $target"
        return $target
    }
    if ($existing.Count -ge 1) {
        $have = Get-VersionFromText $keep.Version
        $new = Get-VersionFromText $Source.Version
        if ($have -and $new -and $have -gt $new) {
            Write-Warn2 "The DewDrop you have ($($keep.Version)) is newer than the one in this installer ($($Source.Version))."
            if (-not (Read-YesNo "Replace it with the older $($Source.Version)?" $false)) {
                Write-Ok "Keeping DewDrop $($keep.Version)"
                $Source.Version = $keep.Version
                return $target
            }
        }
    }
    if ($DryRun) {
        Write-Dry "copy DewDrop $($Source.Version) into $target (keeping config.json)"
        return $target
    }
    Assert-Writable $Game @((Split-Path -Parent $target))

    $parent = Split-Path -Parent $target
    $token = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $staging = Join-Path $parent ".dewdrop-installer-$token"
    $old = Join-Path $parent ".dewdrop-old-$token"
    Copy-Item -LiteralPath $Source.Folder -Destination $staging -Recurse -Force
    Get-ChildItem -LiteralPath $staging -Recurse -File | Unblock-File -ErrorAction SilentlyContinue

    if (Test-Path -LiteralPath $target) {
        # Keep the player's settings.
        $config = Join-Path $target 'config.json'
        if (Test-Path -LiteralPath $config) { Copy-Item -LiteralPath $config -Destination (Join-Path $staging 'config.json') -Force }
        Rename-Item -LiteralPath $target -NewName (Split-Path -Leaf $old)
        try {
            Rename-Item -LiteralPath $staging -NewName (Split-Path -Leaf $target)
        } catch {
            Rename-Item -LiteralPath $old -NewName (Split-Path -Leaf $target)
            throw
        }
        Remove-Item -LiteralPath $old -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        Rename-Item -LiteralPath $staging -NewName (Split-Path -Leaf $target)
    }
    Write-Ok "DewDrop $($Source.Version) is in $target"
    return $target
}

function Set-SteamLaunchOption([string]$Game, [string]$ModFolder) {
    Write-Step 'Setting up Steam to start the game with SMAPI'
    $steamRoot = Get-SteamRoot
    if (-not $steamRoot) { Write-Warn2 "Couldn't find Steam's folder."; return $false }
    $smapiExe = Join-Path $Game 'StardewModdingAPI.exe'
    $selected = Join-Path $env:APPDATA 'Stardrop\Data\Selected Mods'

    # Only the signed-in account: other people's Steam accounts on this PC are left alone.
    $file = $null
    $accountId = Get-SteamActiveAccountId $steamRoot
    if ($accountId) {
        $candidate = Join-Path $steamRoot "userdata\$accountId\config\localconfig.vdf"
        if (Test-Path -LiteralPath $candidate) { $file = $candidate }
    }
    if (-not $file) {
        $file = Get-SteamUserConfigs $steamRoot | Sort-Object { (Get-Item -LiteralPath $_).LastWriteTime } -Descending | Select-Object -First 1
    }
    if (-not $file) { Write-Warn2 "Couldn't find Steam's user settings."; return $false }

    $text = [IO.File]::ReadAllText($file, [Text.Encoding]::UTF8)
    $block = Find-SteamAppBlock $text
    if ($block.AppsOpen -lt 0) { Write-Warn2 "Steam's settings file has an unexpected layout, so I won't edit it."; return $false }
    $current = if ($block.Options) { ConvertFrom-VdfString $block.Options.Groups[1].Value } else { '' }

    # SMAPI from this game folder, keeping anything else the option has (and the player's own spelling of the path).
    $program = '"' + $smapiExe + '"'
    $rest = ' %command%'
    $ask = $null
    if ($current -match 'StardewModdingAPI') {
        $parts = Get-LaunchOptionExe $current
        if (Test-SamePath $parts.Exe $smapiExe) {
            $program = '"' + $parts.Exe + '"'
        } else {
            Write-Warn2 "Steam's launch option starts SMAPI from somewhere else: $current"
            if (-not (Test-Path -LiteralPath $parts.Exe)) { Write-Note "That file doesn't exist, so Steam can't start SMAPI with it." }
            if (-not (Read-YesNo "Point it at this game's SMAPI instead? (Steam will close for a moment)" $true)) { return $false }
        }
        $rest = $parts.Rest
        if ($rest -notmatch '%command%') { $rest = $rest.TrimEnd() + ' %command%' }
    } elseif ($current.Trim()) {
        Write-Warn2 "Steam already has launch options for Stardew Valley: $current"
        if (-not (Read-YesNo 'Replace them with the SMAPI launch option? (Steam will close for a moment)' $false)) { return $false }
    } else {
        $ask = 'Set it now? (Steam will close for a moment)'
    }

    # Starting in the DewDrop profile. The installer never adds --mods-path itself: the game brings the profile links up
    # to date as it closes and adds it then, so Steam can't be pointed at links that are missing or out of date. An
    # existing one is kept while it's still wanted and the links exist; one pointing at another folder is the player's.
    $inProfile = Get-StartInProfileChoice $current $selected
    $points = Get-ModsPathArgument $current
    $ours = $points -and (Test-SamePath $points $selected)
    if ($ours -and -not ($inProfile -and (Test-ProfileLinks $selected))) { $rest = Remove-ModsPathArgument $rest }
    $wanted = $program + $rest
    if ($inProfile -and -not ($ours -and (Test-ProfileLinks $selected))) {
        Write-Note 'DewDrop finishes this the next time you quit the game: Steam closes and opens again once, and from then on'
        Write-Note 'it starts the game in your DewDrop profile.'
    }

    if ($wanted -ceq $current) {
        Write-Ok "Steam already starts SMAPI: $current"
        Set-DewDropConfigValue $ModFolder 'StartInProfileFromSteam' $inProfile
        return $true
    }
    if ($ask) {
        Write-Note 'I can set this launch option for you, so Steam starts SMAPI with achievements and playtime working:'
        Write-Note "  $wanted"
        if (-not (Read-YesNo $ask $true)) { return $false }
    }
    if ($DryRun) {
        Write-Dry "close Steam, back up $file and set the launch option to: $wanted"
        Set-DewDropConfigValue $ModFolder 'StartInProfileFromSteam' $inProfile
        return $true
    }

    $steam = @(Get-Process -Name 'steam' -ErrorAction SilentlyContinue)
    if ($steam.Count -gt 0) {
        Write-Note 'Closing Steam (it saves its settings when it exits, so it has to be closed first)...'
        Start-Process -FilePath (Join-Path $steamRoot 'steam.exe') -ArgumentList '-shutdown' | Out-Null
        $deadline = (Get-Date).AddSeconds(60)
        while (@(Get-Process -Name 'steam' -ErrorAction SilentlyContinue).Count -gt 0) {
            if ((Get-Date) -gt $deadline) { Write-Warn2 "Steam didn't close."; return $false }
            Start-Sleep -Milliseconds 500
        }
        Start-Sleep -Seconds 1
        $script:ClosedSteam = $true
        Write-Ok 'Steam closed.'
    }

    # Re-read in case Steam wrote the file while closing.
    $text = [IO.File]::ReadAllText($file, [Text.Encoding]::UTF8)
    $b = Find-SteamAppBlock $text
    if ($b.AppsOpen -lt 0) { Write-Warn2 "Steam's settings file has an unexpected layout, so I won't edit it."; return $false }
    $escaped = ConvertTo-VdfString $wanted
    if ($b.Options) {
        $g = $b.Options.Groups[1]
        $new = $text.Substring(0, $g.Index) + $escaped + $text.Substring($g.Index + $g.Length)
    } elseif ($b.AppOpen -ge 0) {
        $indent = "`n" + ("`t" * $b.AppDepth)
        $new = $text.Substring(0, $b.AppOpen + 1) + $indent + '"LaunchOptions"' + "`t`t" + '"' + $escaped + '"' + $text.Substring($b.AppOpen + 1)
    } else {
        $indent = "`n" + ("`t" * $b.AppsDepth)
        $insert = $indent + '"' + $SteamAppId + '"' + $indent + '{' + $indent + "`t" + '"LaunchOptions"' + "`t`t" + '"' + $escaped + '"' + $indent + '}'
        $new = $text.Substring(0, $b.AppsOpen + 1) + $insert + $text.Substring($b.AppsOpen + 1)
    }
    # Sanity check before writing: the edit must parse and read back as the wanted value.
    $check = Find-SteamAppBlock $new
    if (-not $check.Options -or (ConvertFrom-VdfString $check.Options.Groups[1].Value) -ne $wanted) {
        Write-Warn2 "The change to Steam's settings didn't check out, so I left them alone."
        return $false
    }
    $backup = $file + '.dewdrop-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    Copy-Item -LiteralPath $file -Destination $backup -Force
    [IO.File]::WriteAllText($file, $new, (New-Object Text.UTF8Encoding $false))
    Write-Ok "Set the launch option (backup: $backup)"
    Set-DewDropConfigValue $ModFolder 'StartInProfileFromSteam' $inProfile
    return $true
}

# A launch option without SMAPI's --mods-path (quoted or bare value).
function Remove-ModsPathArgument([string]$Option) {
    return ($Option -replace '\s*--mods-path(?:\s+|=)(?:"[^"]*"|\S+)', '')
}

# The folder a launch option's --mods-path points at, or $null.
function Get-ModsPathArgument([string]$Option) {
    $m = [regex]::Match($Option, '--mods-path(?:\s+|=)(?:"([^"]*)"|(\S+))')
    if (-not $m.Success) { return $null }
    if ($m.Groups[1].Success) { return $m.Groups[1].Value }
    return $m.Groups[2].Value
}

# Whether DewDrop's profile folder has a link to DewDrop (every profile it links has one). Without it, starting the game
# in that folder would show no Mods button.
function Test-ProfileLinks([string]$Selected) {
    if (-not (Test-Path -LiteralPath $Selected)) { return $false }
    foreach ($dir in @(Get-ChildItem -LiteralPath $Selected -Directory -ErrorAction SilentlyContinue)) {
        $manifest = Join-Path $dir.FullName 'manifest.json'
        if ((Test-Path -LiteralPath $manifest) -and (Read-ManifestId $manifest).Id -eq $DewDropId) { return $true }
    }
    return $false
}

# Whether Steam should start the game in the DewDrop profile ("Start in my profile from Steam" in DewDrop).
function Get-StartInProfileChoice([string]$Current, [string]$Selected) {
    $points = Get-ModsPathArgument $Current
    $has = $points -and (Test-SamePath $points $Selected)
    if ($NoStartInProfile) { return $false }
    if ($StartInProfile -or $has) { return $true }
    Write-Note 'Steam can start the game in your DewDrop profile, so mods you turn off stay off without an Apply & Restart.'
    Write-Note '(You can change this any time in the game: Mods > Account > "Start in my profile from Steam".)'
    return (Read-YesNo 'Start the game in your DewDrop profile when you press Play in Steam?' $true)
}

# Sets one setting in DewDrop's config.json, so the game agrees with what was set in Steam.
function Set-DewDropConfigValue([string]$ModFolder, [string]$Name, $Value) {
    if (-not $ModFolder) { return }
    $config = Join-Path $ModFolder 'config.json'
    if ($DryRun) { Write-Dry "set $Name to $Value in $config"; return }
    try {
        $data = $null
        if (Test-Path -LiteralPath $config) { $data = ConvertFrom-JsonLoose ([IO.File]::ReadAllText($config)) }
        if ($null -eq $data) { $data = New-Object PSObject }
        if ($data.PSObject.Properties[$Name]) { $data.$Name = $Value } else { $data | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
        [IO.File]::WriteAllText($config, ($data | ConvertTo-Json -Depth 10), (New-Object Text.UTF8Encoding $false))
    } catch {
        Write-Warn2 "Couldn't update DewDrop's settings in ${config}: $($_.Exception.Message)"
    }
}

function Show-SteamManualSteps([string]$Game) {
    $wanted = '"' + (Join-Path $Game 'StardewModdingAPI.exe') + '" %command%'
    $copied = $false
    if (-not $DryRun -and -not $Yes) { try { Set-Clipboard -Value $wanted; $copied = $true } catch { } }
    Write-Note 'To start SMAPI from Steam yourself:'
    Write-Note '  1. In Steam, right-click Stardew Valley and choose Properties.'
    Write-Note '  2. Under General > Launch Options, replace the text with:'
    Write-Host "         $wanted" -ForegroundColor White
    if ($copied) { Write-Note '     (It''s already copied: just paste it with Ctrl+V.)' }
}

function Set-XboxLauncher([string]$Game) {
    Write-Step 'Setting up the Xbox app to start SMAPI'
    $exe = Join-Path $Game 'Stardew Valley.exe'
    $smapi = Join-Path $Game 'StardewModdingAPI.exe'
    $original = Join-Path $Game 'Stardew Valley original.exe'
    if (-not (Test-Path -LiteralPath $smapi)) {
        if ($DryRun) { Write-Dry 'set up the Xbox app once SMAPI is installed' } else { Write-Warn2 'SMAPI is missing, so the Xbox app can''t be set up.' }
        return
    }
    try {
        if ((Test-Path -LiteralPath $exe) -and (Get-FileHash -LiteralPath $exe).Hash -eq (Get-FileHash -LiteralPath $smapi).Hash) {
            Write-Ok 'The Xbox app already starts SMAPI.'
            return
        }
    } catch { }
    Write-Note 'The Xbox app always starts "Stardew Valley.exe", so SMAPI''s guide swaps in a copy of SMAPI under that name.'
    Write-Note 'You''ll need to do this again after the game updates (just run this installer again).'
    if (-not (Read-YesNo 'Do that now?' $true)) { return }
    if ($DryRun) { Write-Dry "keep the game exe as 'Stardew Valley original.exe' and copy StardewModdingAPI.exe to 'Stardew Valley.exe'"; return }
    try {
        # "Stardew Valley.exe" is either the real game (first run, or after a game update) or an older SMAPI copy
        # (after a SMAPI update). Only the real game is moved aside, so "Stardew Valley original.exe" always holds it.
        $info = if (Test-Path -LiteralPath $exe) { (Get-Item -LiteralPath $exe).VersionInfo } else { $null }
        $isSmapiCopy = $info -and ($info.InternalName -like 'StardewModdingAPI*' -or $info.ProductName -eq 'SMAPI')
        if ($info -and -not $isSmapiCopy) { Move-Item -LiteralPath $exe -Destination $original -Force }
        Copy-Item -LiteralPath $smapi -Destination $exe -Force
        Write-Ok 'Done. Start the game from the Xbox app as usual.'
    } catch {
        Write-Warn2 "Windows didn't allow it ($($_.Exception.Message)). See https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app"
    }
}

function New-DesktopShortcut([string]$Game) {
    $smapi = Join-Path $Game 'StardewModdingAPI.exe'
    try {
        # As administrator, the per-user Desktop may be someone else's; the shared Desktop shows for everyone.
        $desktop = if (Test-Elevated) { [Environment]::GetFolderPath('CommonDesktopDirectory') } else { [Environment]::GetFolderPath('Desktop') }
        if (-not $desktop) { throw "Windows didn't say where your desktop is." }
        $link = Join-Path $desktop 'Stardew Valley (SMAPI).lnk'
        if (Test-Path -LiteralPath $link) { Write-Ok 'The "Stardew Valley (SMAPI)" desktop shortcut is already there.'; return }
        if (-not (Read-YesNo 'Add a "Stardew Valley (SMAPI)" shortcut to your desktop?' $true)) { return }
        if ($DryRun) { Write-Dry "create $link"; return }
        $shell = New-Object -ComObject WScript.Shell
        $sc = $shell.CreateShortcut($link)
        $sc.TargetPath = $smapi
        $sc.WorkingDirectory = $Game
        $sc.IconLocation = (Join-Path $Game 'Stardew Valley.exe') + ',0'
        $sc.Description = 'Start Stardew Valley with SMAPI and DewDrop'
        $sc.Save()
        Write-Ok "Shortcut added: $link"
    } catch {
        Write-Warn2 "Couldn't add the shortcut ($($_.Exception.Message))."
        Write-Note "To play with mods, start $smapi"
    }
}

# --------------------------------------------------------------------------------------------------------------
# Main

$exitCode = 0
$transcript = $false
$script:ClosedSteam = $false
$script:HandedOff = $false
try {
    try { Start-Transcript -Path $LogPath -Force | Out-Null; $transcript = $true } catch { }
    Write-Banner

    $game = Find-Game
    Test-GameVersion $game
    Wait-GameClosed
    $store = Get-GameStore $game

    $smapiVersion = Install-Smapi $game
    $source = Get-DewDropSource
    $target = $null
    if ($source.Folder -or $DryRun) { $target = Install-DewDrop $game $source }

    $stardrop = Join-Path $env:APPDATA 'Stardrop\Data\Settings.json'
    if (Test-Path -LiteralPath $stardrop) {
        Write-Step 'Stardrop is installed too'
        Write-Note 'DewDrop shares Stardrop''s profiles and Nexus login. The first time, start the game from Steam or the'
        Write-Note 'SMAPI shortcut (not from Stardrop) so DewDrop can add itself to your Stardrop profiles.'
    }

    $launch = $null
    if ($store -eq 'Steam' -and -not $NoSteamLaunchOptions) {
        $set = $false
        try { $set = Set-SteamLaunchOption $game $target } catch { Write-Warn2 "Couldn't change Steam's settings: $($_.Exception.Message)" }
        if ($set) { $launch = "steam://rungameid/$SteamAppId" } else { Show-SteamManualSteps $game }
    } elseif ($store -eq 'Xbox') {
        Set-XboxLauncher $game
    } elseif ($store -eq 'GOG') {
        Write-Note 'GOG Galaxy: start StardewModdingAPI.exe (or the shortcut) to play with mods. To make Galaxy''s Play button'
        Write-Note 'start SMAPI, see https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#GOG_Galaxy'
    }

    if (-not $launch -and $store -ne 'Xbox' -and -not $NoShortcut) {
        Write-Step 'Shortcut'
        New-DesktopShortcut $game
    }

    Write-Host ''
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor DarkCyan
    Write-Host '   All done!' -ForegroundColor Green
    Write-Host "   SMAPI $smapiVersion and DewDrop $($source.Version) are installed in:" -ForegroundColor Gray
    Write-Host "   $game" -ForegroundColor Gray
    Write-Host ''
    Write-Host '   Start the game and click the new Mods button on the title screen.' -ForegroundColor White
    Write-Host '   Guide: https://github.com/friessssss/DewDrop#readme' -ForegroundColor Gray
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor DarkCyan

    $started = $false
    if (-not $NoLaunch -and $store -ne 'Xbox') {
        if (Read-YesNo 'Start Stardew Valley now?' $true) {
            $started = $true
            if ($DryRun) {
                Write-Dry "start $(if ($launch) { $launch } else { 'StardewModdingAPI.exe' })"
            } elseif ($launch) {
                Start-AsUser $launch
            } else {
                Start-AsUser (Join-Path $game 'StardewModdingAPI.exe') $game
            }
        }
    }
    if ($script:ClosedSteam -and -not $started) { Write-Note 'Steam was closed to change its settings; start it again whenever you like.' }
} catch [InstallerStop] {
    Write-Host ''
    if ($script:HandedOff) {
        Write-Note $_.Exception.Message
    } else {
        Write-Bad $_.Exception.Message
        $exitCode = 10
    }
} catch {
    $exitCode = 10
    Write-Host ''
    Write-Bad "Something went wrong: $($_.Exception.Message)"
    if ($_.InvocationInfo) { Write-Note "Line $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim())" }
    $inner = $_.Exception
    while ($inner.InnerException) { $inner = $inner.InnerException }
    if ($inner -is [UnauthorizedAccessException] -or $_.Exception -is [UnauthorizedAccessException]) {
        Write-Note 'Windows refused access to a file. If your game is under Program Files, right-click "Install on Windows.bat"'
        Write-Note 'and choose "Run as administrator". Also check that the game is closed.'
    }
    Write-Note 'Nothing was half-installed: DewDrop is only swapped in once its new copy is ready.'
    Write-Note "If you need help, open an issue at https://github.com/$DewDropRepo/issues and attach the log below."
} finally {
    if (Test-Path -LiteralPath $Work) { Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue }
    if ($transcript) {
        try { Stop-Transcript | Out-Null } catch { }
        Write-Host "    Log saved to $LogPath" -ForegroundColor DarkGray
    }
    if ($FromLauncher) {
        Write-Host ''
        Write-Host '    Press Enter to close this window.' -ForegroundColor DarkGray -NoNewline
        [void](Read-Host)
    }
}
exit $exitCode
