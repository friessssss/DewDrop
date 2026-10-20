# Tests installer/internal/install.ps1: its helpers (Steam launch options, manifests) on synthetic input, then whole
# runs against fake game folders in %TEMP%. It never touches a real game or Steam install.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File installer\tests\install-ps1.test.ps1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$installer = (Resolve-Path (Join-Path $PSScriptRoot '..\internal\install.ps1')).Path

# Load the installer's functions and constants (everything before its main body).
$src = Get-Content -LiteralPath $installer -Raw
$lib = Join-Path ([IO.Path]::GetTempPath()) ('dewdrop-installer-lib-' + [guid]::NewGuid().ToString('N') + '.ps1')
Set-Content -LiteralPath $lib -Value $src.Substring(0, $src.IndexOf('# Main')) -Encoding ASCII
. $lib
Remove-Item -LiteralPath $lib

$fail = 0
function Check([string]$Name, [bool]$Ok, [string]$Detail = '') {
    if ($Ok) { Write-Host "PASS $Name" -ForegroundColor Green }
    else { Write-Host "FAIL $Name" -ForegroundColor Red; if ($Detail) { Write-Host $Detail }; $script:fail++ }
}

# ---------------------------------------------------------------------------------------------------------------
Write-Host '== Steam launch options'

$wanted = '"D:\Games\Stardew Valley\StardewModdingAPI.exe" %command%'
$esc = ConvertTo-VdfString $wanted
Check 'escaping round-trips' ((ConvertFrom-VdfString $esc) -eq $wanted)

# The same three edits install.ps1 makes: replace the value, add the key, or add the whole app block.
function Edit-Vdf([string]$Text) {
    $b = Find-SteamAppBlock $Text
    if ($b.Options) {
        $g = $b.Options.Groups[1]
        return $Text.Substring(0, $g.Index) + $esc + $Text.Substring($g.Index + $g.Length)
    } elseif ($b.AppOpen -ge 0) {
        $indent = "`n" + ("`t" * $b.AppDepth)
        return $Text.Substring(0, $b.AppOpen + 1) + $indent + '"LaunchOptions"' + "`t`t" + '"' + $esc + '"' + $Text.Substring($b.AppOpen + 1)
    } else {
        $indent = "`n" + ("`t" * $b.AppsDepth)
        return $Text.Substring(0, $b.AppsOpen + 1) + $indent + '"413150"' + $indent + '{' + $indent + "`t" + '"LaunchOptions"' + "`t`t" + '"' + $esc + '"' + $indent + '}' + $Text.Substring($b.AppsOpen + 1)
    }
}
function Read-Option([string]$Text) {
    $b = Find-SteamAppBlock $Text
    if ($b.Options) { return ConvertFrom-VdfString $b.Options.Groups[1].Value }
    return $null
}

$base = @"
"UserLocalConfigStore"
{
	"Software"
	{
		"Valve"
		{
			"Steam"
			{
				"apps"
				{
					"730"
					{
						"LaunchOptions"		"-novid"
					}
					"413150"
					{
						"LastPlayed"		"1"
						"cloud"
						{
							"LaunchOptions"		"nested-should-not-match"
						}
					}
				}
			}
		}
	}
	"413150"		"deadbeef"
}
"@

$b = Find-SteamAppBlock $base
Check 'finds the Stardew Valley block' ($b.AppOpen -ge 0 -and $b.AppsOpen -ge 0)
Check 'ignores other games and nested keys' ($null -eq $b.Options)
$new = Edit-Vdf $base
Check 'adds the option to an existing block' ((Read-Option $new) -eq $wanted)
Check 'leaves other games alone' ($new -match '"730"\s*\{\s*"LaunchOptions"\s*"-novid"')
Check 'adds exactly one key and value' ((Get-VdfTokens $new).Count -eq (Get-VdfTokens $base).Count + 2)
$replaced = Edit-Vdf $new
Check 'replaces an existing option' ((Read-Option $replaced) -eq $wanted -and (Get-VdfTokens $replaced).Count -eq (Get-VdfTokens $new).Count)
$noApp = $base -replace '(?s)\t\t\t\t\t"413150".*?\n\t\t\t\t\t\}\r?\n', ''
$b = Find-SteamAppBlock $noApp
Check 'notices when the block is missing' ($b.AppOpen -lt 0 -and $b.AppsOpen -ge 0)
Check 'creates the block' ((Read-Option (Edit-Vdf $noApp)) -eq $wanted)
$odd = $base.Replace('"Software"', '"software"').Replace('"apps"', '"Apps"').Replace('"LastPlayed"', ('"LaunchOptions"' + "`t`t" + '"old \"quoted\" C:\\path"' + "`n`t`t`t`t`t`t" + '"LastPlayed"'))
Check 'matches case-insensitively and unescapes values' ((Read-Option $odd) -eq 'old "quoted" C:\path')
Check 'reports a missing apps block' ((Find-SteamAppBlock '"UserLocalConfigStore" { "Software" { "Valve" { "Steam" { } } } }').AppsOpen -lt 0)

$p = Get-LaunchOptionExe '"C:\Old Place\StardewModdingAPI.exe" --mods-path "X" %command%'
Check 'reads the program from a quoted launch option' ($p.Exe -eq 'C:\Old Place\StardewModdingAPI.exe' -and $p.Rest -eq ' --mods-path "X" %command%')
$p = Get-LaunchOptionExe 'C:\Games\StardewModdingAPI.exe %command%'
Check 'reads the program from an unquoted launch option' ($p.Exe -eq 'C:\Games\StardewModdingAPI.exe' -and $p.Rest -eq ' %command%')
Check 'compares paths case-insensitively' (Test-SamePath 'C:\steam\a\StardewModdingAPI.exe' 'c:\Steam\A\StardewModdingAPI.exe')
Check 'tells different paths apart' (-not (Test-SamePath 'C:\Program Files (x86)\Steam\StardewModdingAPI.exe' 'D:\SteamLibrary\StardewModdingAPI.exe'))

# Starting in the DewDrop profile: SMAPI's --mods-path in the launch option
Check 'removes a quoted --mods-path' ((Remove-ModsPathArgument ' --mods-path "C:\A B" %command%') -eq ' %command%')
Check 'removes a bare --mods-path' ((Remove-ModsPathArgument ' --mods-path C:\Mods %command% -x') -eq ' %command% -x')
Check 'leaves an option without --mods-path alone' ((Remove-ModsPathArgument ' %command%') -eq ' %command%')
Check 'reads a quoted --mods-path' ((Get-ModsPathArgument '"C:\S.exe" --mods-path "C:\A B" %command%') -eq 'C:\A B')
Check 'reads a bare --mods-path' ((Get-ModsPathArgument '"C:\S.exe" --mods-path C:\Mods %command%') -eq 'C:\Mods')
Check 'reads no --mods-path as nothing' ($null -eq (Get-ModsPathArgument '"C:\S.exe" %command%'))

$StartInProfile = $false
$NoStartInProfile = $false
$Yes = $true
$selected = Join-Path ([IO.Path]::GetTempPath()) ('dewdrop-ps-links-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
Check 'no profile folder means no links yet' (-not (Test-ProfileLinks $selected))
Check 'with -Yes the answer is yes even before the links exist (the game links before adding it)' (Get-StartInProfileChoice '' $selected)
New-Item -ItemType Directory -Path (Join-Path $selected 'Some Mod') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $selected 'Some Mod\manifest.json') -Value '{ "Name": "Other", "UniqueID": "someone.Other", "Version": "1.0.0" }'
Check 'a folder without DewDrop has no links yet' (-not (Test-ProfileLinks $selected))
New-Item -ItemType Directory -Path (Join-Path $selected 'DewDrop') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $selected 'DewDrop\manifest.json') -Value ('{ "Name": "DewDrop", "UniqueID": "' + $DewDropId + '", "Version": "0.1.0" }')
Check 'DewDrop in the profile folder means the links are made' (Test-ProfileLinks $selected)
Check 'with links and -Yes, Steam starts in the profile' (Get-StartInProfileChoice '' $selected)
$StartInProfile = $true
$Yes = $false
Check '-StartInProfile answers yes without asking' (Get-StartInProfileChoice '' $selected)
$StartInProfile = $false
$Yes = $true
$NoStartInProfile = $true
Check '-NoStartInProfile turns it off, even when it was on' (-not (Get-StartInProfileChoice ('"C:\S.exe" --mods-path "' + $selected + '" %command%') $selected))
$NoStartInProfile = $false
$Yes = $false
Check 'a launch option already in the profile keeps it without asking' (Get-StartInProfileChoice ('"C:\S.exe" --mods-path "' + $selected + '" %command%') $selected)
Remove-Item -LiteralPath $selected -Recurse -Force

$configDir = Join-Path ([IO.Path]::GetTempPath()) ('dewdrop-ps-config-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $configDir | Out-Null
$DryRun = $false
Set-Content -LiteralPath (Join-Path $configDir 'config.json') -Value "{`n  // a comment`n  ""ShowAdultContent"": true,`n}"
Set-DewDropConfigValue $configDir 'StartInProfileFromSteam' $true
$cfg = Get-Content -LiteralPath (Join-Path $configDir 'config.json') -Raw | ConvertFrom-Json
Check 'sets the Steam setting in config.json and keeps the others' ($cfg.StartInProfileFromSteam -eq $true -and $cfg.ShowAdultContent -eq $true)
Set-DewDropConfigValue $configDir 'StartInProfileFromSteam' $false
Check 'turns the Steam setting back off' ((Get-Content -LiteralPath (Join-Path $configDir 'config.json') -Raw | ConvertFrom-Json).StartInProfileFromSteam -eq $false)
Remove-Item -LiteralPath (Join-Path $configDir 'config.json')
Set-DewDropConfigValue $configDir 'StartInProfileFromSteam' $true
Check 'creates config.json when there is none' ((Get-Content -LiteralPath (Join-Path $configDir 'config.json') -Raw | ConvertFrom-Json).StartInProfileFromSteam -eq $true)
Remove-Item -LiteralPath $configDir -Recurse -Force

# ---------------------------------------------------------------------------------------------------------------
Write-Host '== Manifests'

$root = Join-Path ([IO.Path]::GetTempPath()) ('dewdrop-ps-test-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $root | Out-Null
try {
    $m = Join-Path $root 'loose.json'
    Set-Content -LiteralPath $m -Encoding UTF8 -Value @'
{
  // a comment with "quotes" and a URL https://example.com
  "Name": "DewDrop", /* block comment */
  "Version": "1.2.3",
  "UniqueID": "friessssss.DewDrop",
  "UpdateKeys": [ "Nexus:1", ],
}
'@
    $info = Read-ManifestId $m
    Check 'reads a manifest with comments and trailing commas' ($info.Id -eq 'friessssss.DewDrop' -and $info.Version -eq '1.2.3')
    Set-Content -LiteralPath $m -Encoding UTF8 -Value '{"Name":"Addon","Dependencies":[{"UniqueID":"friessssss.DewDrop"}],"UniqueID":"someone.Addon","Version":"1.0.0"}'
    Check 'reads its own UniqueID, not a dependency''s' ((Read-ManifestId $m).Id -eq 'someone.Addon')
    Set-Content -LiteralPath $m -Encoding UTF8 -Value '{ "UniqueID": "broken'
    Check 'treats an unreadable manifest as not DewDrop' ((Read-ManifestId $m).Id -eq '')

    # -----------------------------------------------------------------------------------------------------------
    Write-Host '== Whole runs against fake game folders'

    # Any .NET DLL with a version of 4.x stands in for both the game DLL and SMAPI's DLL.
    $versioned = [object].Assembly.Location
    function New-FakeGame([string]$Path, [switch]$NoSmapi) {
        New-Item -ItemType Directory -Path (Join-Path $Path 'Mods') -Force | Out-Null
        Copy-Item -LiteralPath $versioned -Destination (Join-Path $Path 'Stardew Valley.dll')
        if (-not $NoSmapi) {
            Copy-Item -LiteralPath $versioned -Destination (Join-Path $Path 'StardewModdingAPI.dll')
            Set-Content -LiteralPath (Join-Path $Path 'StardewModdingAPI.exe') -Value 'exe'
        }
    }
    function New-FakeMod([string]$Folder, [string]$Version) {
        New-Item -ItemType Directory -Path $Folder -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $Folder 'manifest.json') -Encoding UTF8 -Value ('{ "Name": "DewDrop", "Version": "' + $Version + '", "UniqueID": "friessssss.DewDrop", "EntryDll": "DewDrop.dll" }')
        Set-Content -LiteralPath (Join-Path $Folder 'DewDrop.dll') -Value $Version
    }
    function Invoke-Installer([string[]]$Arguments) {
        $out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer @Arguments -Yes -NoLaunch -NoShortcut -SkipSmapi *>&1 | Out-String
        return [pscustomobject]@{ Code = $LASTEXITCODE; Out = $out }
    }

    $modDir = Join-Path $root 'release'
    New-FakeMod (Join-Path $modDir 'DewDrop') '1.0.0'
    $zip = Join-Path $root 'DewDrop-1.0.0.zip'
    Compress-Archive -Path (Join-Path $modDir 'DewDrop') -DestinationPath $zip

    $game = Join-Path $root 'Games\Stardew Valley'
    New-FakeGame $game
    $r = Invoke-Installer @('-GamePath', $game, '-ModSource', $zip)
    Check 'fresh install exits 0' ($r.Code -eq 0) $r.Out
    Check 'fresh install puts DewDrop in Mods' ((Read-ManifestId (Join-Path $game 'Mods\DewDrop\manifest.json')).Version -eq '1.0.0')

    Set-Content -LiteralPath (Join-Path $game 'Mods\DewDrop\config.json') -Value '{"ShowAdultContent": true}'
    $addon = Join-Path $game 'Mods\Addon'
    New-Item -ItemType Directory -Path $addon | Out-Null
    Set-Content -LiteralPath (Join-Path $addon 'manifest.json') -Encoding UTF8 -Value '{"Name":"Addon","Dependencies":[{"UniqueID":"friessssss.DewDrop"}],"UniqueID":"someone.Addon"}'
    $r = Invoke-Installer @('-GamePath', $game, '-ModSource', $zip)
    Check 'update exits 0' ($r.Code -eq 0) $r.Out
    Check 'update keeps config.json' ((Get-Content -LiteralPath (Join-Path $game 'Mods\DewDrop\config.json') -Raw) -match 'ShowAdultContent')
    Check 'a mod that depends on DewDrop is left alone' ((Test-Path -LiteralPath $addon) -and -not ($r.Out -match 'other copies of DewDrop'))

    New-FakeMod (Join-Path $game 'Mods\DewDrop') '9.9.9'
    $r = Invoke-Installer @('-GamePath', $game, '-ModSource', $zip)
    Check 'a newer installed DewDrop is kept by default' ($r.Code -eq 0 -and (Read-ManifestId (Join-Path $game 'Mods\DewDrop\manifest.json')).Version -eq '9.9.9') $r.Out

    $r = Invoke-Installer @('-GamePath', $game, '-ModSource', (Join-Path $root 'Typo.zip'))
    Check 'a missing -ModSource stops with exit code 10' ($r.Code -eq 10 -and $r.Out -match "doesn't exist") $r.Out

    $notGame = Join-Path $root 'Empty'
    New-Item -ItemType Directory -Path $notGame | Out-Null
    $r = Invoke-Installer @('-GamePath', $notGame, '-ModSource', $zip)
    Check 'a folder without the game stops with exit code 10' ($r.Code -eq 10) $r.Out

    $other = Join-Path $root 'Other\Stardew Valley'
    New-FakeGame $other
    New-Item -ItemType Directory -Path (Join-Path $other 'Mods\DewDrop') | Out-Null
    Set-Content -LiteralPath (Join-Path $other 'Mods\DewDrop\manifest.json') -Value '{"UniqueID": "someone.else"}'
    $r = Invoke-Installer @('-GamePath', $other, '-ModSource', $zip)
    Check 'never overwrites a different mod named DewDrop' ($r.Code -eq 10 -and (Get-Content -LiteralPath (Join-Path $other 'Mods\DewDrop\manifest.json') -Raw) -match 'someone.else') $r.Out

    # The one-line installer (irm ... | iex), with options as its comment shows, fetching install.ps1 from this checkout.
    $oneLine = Join-Path $root 'OneLine\Stardew Valley'
    New-FakeGame $oneLine
    $env:DEWDROP_INSTALLER_BASE = Split-Path -Parent $installer
    try {
        $bootstrap = [scriptblock]::Create((Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\..\install.ps1') -Raw))
        $out = & $bootstrap -GamePath $oneLine -ModSource $zip -Yes -NoLaunch -NoShortcut -SkipSmapi *>&1 | Out-String
        $code = $LASTEXITCODE
    } finally {
        Remove-Item -LiteralPath env:DEWDROP_INSTALLER_BASE
    }
    Check 'the one-line installer runs install.ps1 with its options' ($code -eq 0 -and (Read-ManifestId (Join-Path $oneLine 'Mods\DewDrop\manifest.json')).Version -eq '1.0.0') $out

    $xbox = Join-Path $root 'XboxGames\Stardew Valley\Content'
    New-FakeGame $xbox -NoSmapi
    $before = @(Get-ChildItem -LiteralPath $xbox -Recurse -Force).Count
    $out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -GamePath $xbox -ModSource $zip -DryRun -Yes -NoLaunch *>&1 | Out-String
    $code = $LASTEXITCODE
    Check 'an Xbox dry run without SMAPI ends cleanly' (($code -eq 0 -or $code -eq 10) -and $out -notmatch 'PropertyNotFound|InvocationInfo') $out
    Check 'a dry run changes nothing' (@(Get-ChildItem -LiteralPath $xbox -Recurse -Force).Count -eq $before)
} finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

if ($fail) { Write-Host "$fail failed" -ForegroundColor Red; exit 1 }
Write-Host 'All passed' -ForegroundColor Green
# Otherwise the exit code is the last installer run's: the Xbox dry run exits 10 when GitHub can't be reached for
# the latest SMAPI, and CI's "powershell -command" step turns that 10 into a failing 1.
exit 0
