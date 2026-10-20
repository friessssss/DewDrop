# One-line DewDrop installer for Windows. Paste this into PowerShell (Start menu > type PowerShell > Enter):
#
#   irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex
#
# From Command Prompt instead:
#
#   powershell -c "irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex"
#
# It downloads the real installer (installer/internal/install.ps1) and runs it in Windows PowerShell, which then
# downloads the latest DewDrop release. To pass options:
#
#   & ([scriptblock]::Create((irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1))) -DryRun
#
# On macOS and Linux, use install.sh. This file must stay plain ASCII: Windows PowerShell 5.1 reads it as ANSI.

# Everything runs in its own scope so nothing is left behind in the PowerShell window it was pasted into, and it
# never calls exit, which would close that window.
& {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue' # Invoke-WebRequest is far slower with the progress bar in PowerShell 5.1
    try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch { }

    # $IsWindows only exists in PowerShell 6 and later; Windows PowerShell only runs on Windows.
    if ((Test-Path variable:IsWindows) -and -not $IsWindows) {
        Write-Host 'This is the Windows installer. On macOS or Linux, run this in Terminal instead:'
        Write-Host '  curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash'
        return
    }

    # DEWDROP_INSTALLER_BASE points at another copy of installer/internal (the tests use a local folder).
    $base = 'https://raw.githubusercontent.com/friessssss/DewDrop/master/installer/internal'
    if ($env:DEWDROP_INSTALLER_BASE) { $base = $env:DEWDROP_INSTALLER_BASE }
    # A fixed file rather than a temporary folder removed afterwards: the installer may restart itself from this file
    # as administrator after this has finished.
    $installer = Join-Path $env:TEMP 'DewDrop-install.ps1'
    try {
        if ($base -match '^https?://') {
            Invoke-WebRequest -UseBasicParsing -Uri "$base/install.ps1" -OutFile $installer -UserAgent 'DewDrop-Installer'
        } else {
            Copy-Item -LiteralPath (Join-Path $base 'install.ps1') -Destination $installer -Force
        }
    } catch {
        Write-Host "Couldn't download the DewDrop installer from $base/install.ps1. Check your internet connection." -ForegroundColor Red
        Write-Host "($($_.Exception.Message))"
        return
    }

    # Windows PowerShell, which install.ps1 is written for, even when this was pasted into PowerShell 7.
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer @args
} @args
