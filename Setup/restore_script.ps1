param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$ServiceName = "DottoressaIorio.App",
    [string]$AppRoot = "C:\ProgramData\DottoressaIorio"
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "This script must run in an elevated PowerShell session (Run as Administrator)."
}

$expandedBackupPath = [Environment]::ExpandEnvironmentVariables($BackupPath)
if (-not (Test-Path -Path $expandedBackupPath -PathType Leaf)) {
    throw "Backup file not found: $BackupPath"
}
$resolvedBackupPath = (Resolve-Path -Path $expandedBackupPath).Path

$backupRoot = Join-Path $AppRoot "_db_backup"
if (-not (Test-Path -Path $backupRoot -PathType Container)) {
    New-Item -Path $backupRoot -ItemType Directory | Out-Null
}

function Get-ConfiguredEncryptionKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppRoot
    )

    $configPaths = @(
        (Join-Path $AppRoot "appsettings.json"),
        (Join-Path $AppRoot "appsettings.Production.json")
    )

    $encryptionKey = $null
    foreach ($configPath in $configPaths) {
        if (-not (Test-Path -Path $configPath -PathType Leaf)) {
            continue
        }

        $config = Get-Content -Raw -Path $configPath | ConvertFrom-Json
        $property = $config.PSObject.Properties["EncryptionKey"]
        if ($null -ne $property) {
            $encryptionKey = $property.Value
        }
    }

    return $encryptionKey
}

function Get-RestoreDestinationDbPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppRoot
    )

    $encryptedPath = Join-Path $AppRoot "app.db"
    $unencryptedPath = Join-Path $AppRoot "app_unencrypted.db"
    $configuredEncryptionKey = Get-ConfiguredEncryptionKey -AppRoot $AppRoot
    $preferredPath = if ([string]::IsNullOrWhiteSpace($configuredEncryptionKey)) {
        $unencryptedPath
    }
    else {
        $encryptedPath
    }

    if (Test-Path -Path $preferredPath -PathType Leaf) {
        return $preferredPath
    }

    $fallbackPath = if ($preferredPath -eq $encryptedPath) { $unencryptedPath } else { $encryptedPath }
    if (Test-Path -Path $fallbackPath -PathType Leaf) {
        Write-Warning "Preferred restore target '$preferredPath' was not found. Falling back to '$fallbackPath'."
        return $fallbackPath
    }

    return $preferredPath
}

$destinationDbPath = Get-RestoreDestinationDbPath -AppRoot $AppRoot

$service = Get-Service -Name $ServiceName -ErrorAction Stop
$serviceWasRunning = $service.Status -ne "Stopped"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$destinationDbFileName = [System.IO.Path]::GetFileNameWithoutExtension($destinationDbPath)
$preRestoreBackupPath = Join-Path $backupRoot "${destinationDbFileName}_pre_restore_$timestamp.db"

try {
    if ($serviceWasRunning) {
        Stop-Service -Name $ServiceName -Force
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Stopped, [TimeSpan]::FromSeconds(60))
    }

    if (Test-Path -Path $destinationDbPath -PathType Leaf) {
        Copy-Item -Path $destinationDbPath -Destination $preRestoreBackupPath -Force
    }

    $walPath = "$destinationDbPath-wal"
    $shmPath = "$destinationDbPath-shm"
    if (Test-Path -Path $walPath -PathType Leaf) {
        Remove-Item -Path $walPath -Force
    }
    if (Test-Path -Path $shmPath -PathType Leaf) {
        Remove-Item -Path $shmPath -Force
    }

    Copy-Item -Path $resolvedBackupPath -Destination $destinationDbPath -Force
}
finally {
    if ($serviceWasRunning) {
        Start-Service -Name $ServiceName
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(60))
    }
}

Write-Host "Restore completed from: $resolvedBackupPath"
Write-Host "Restored database path: $destinationDbPath"
if (Test-Path -Path $preRestoreBackupPath -PathType Leaf) {
    Write-Host "Pre-restore backup saved to: $preRestoreBackupPath"
}
