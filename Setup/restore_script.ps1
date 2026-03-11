param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$ServiceName = "Praxiom.App",
    [string]$AppRoot = "C:\ProgramData\Praxiom"
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

function Get-StoredEncryptionKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppRoot
    )

    $secretPath = Join-Path $AppRoot "secrets\encryption-key.json"
    if (-not (Test-Path -Path $secretPath -PathType Leaf)) {
        return $null
    }

    $config = Get-Content -Raw -Path $secretPath | ConvertFrom-Json
    $property = $config.PSObject.Properties["EncryptionKey"]
    if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace($property.Value)) {
        return $property.Value
    }

    $protectionProperty = $config.PSObject.Properties["Protection"]
    $payloadProperty = $config.PSObject.Properties["Payload"]
    if ($null -eq $protectionProperty -or $null -eq $payloadProperty) {
        return $null
    }

    if ($protectionProperty.Value -ne "DPAPI-LocalMachine" -or [string]::IsNullOrWhiteSpace($payloadProperty.Value)) {
        return $null
    }

    $protectedBytes = [Convert]::FromBase64String($payloadProperty.Value)
    $rawBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $protectedBytes,
        $null,
        [System.Security.Cryptography.DataProtectionScope]::LocalMachine)
    $encryptionKey = [System.Text.Encoding]::UTF8.GetString($rawBytes)
    if ([string]::IsNullOrWhiteSpace($encryptionKey)) {
        return $null
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
    $configuredEncryptionKey = Get-StoredEncryptionKey -AppRoot $AppRoot
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
