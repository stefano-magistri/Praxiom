$ErrorActionPreference = "Stop"

# Manual/emergency fallback only.
# Routine backups are online backups handled by:
#   C:\ProgramData\Praxiom\DottoressaIorio.App.exe --online-backup
# This script performs stop-service cold copy and should not be used for routine daytime scheduling.

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "This script must run in an elevated PowerShell session (Run as Administrator)."
}

$serviceName = "Praxiom.App"
$appRoot = "C:\ProgramData\Praxiom"
$localBackupRoot = Join-Path $appRoot "_db_backup"
$retentionCount = 360

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

function Get-DatabasePath {
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
        Write-Warning "Preferred database file '$preferredPath' was not found. Falling back to '$fallbackPath'."
        return $fallbackPath
    }

    throw "Database file not found. Checked '$preferredPath' and '$fallbackPath'."
}

function Get-OneDriveRoot {
    $candidates = @(
        $env:OneDriveConsumer,
        $env:OneDriveCommercial,
        $env:OneDrive,
        (Join-Path $env:USERPROFILE "OneDrive")
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (Test-Path $candidate -PathType Container) {
            return $candidate
        }
    }

    return $null
}

$oneDriveRoot = Get-OneDriveRoot
$oneDriveBackupRoot = if ($null -ne $oneDriveRoot) {
    Join-Path $oneDriveRoot "Praxiom\_db_backup"
} else {
    $null
}
$sourcePath = Get-DatabasePath -AppRoot $appRoot

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$backupFileName = "app_backup_$timestamp.db"
$localBackupPath = Join-Path $localBackupRoot $backupFileName
$oneDriveBackupPath = if ($null -ne $oneDriveBackupRoot) {
    Join-Path $oneDriveBackupRoot $backupFileName
} else {
    $null
}

if (-not (Test-Path $sourcePath -PathType Leaf)) {
    throw "Database file not found: $sourcePath"
}

if (-not (Test-Path $localBackupRoot -PathType Container)) {
    New-Item -Path $localBackupRoot -ItemType Directory | Out-Null
}

$service = Get-Service -Name $serviceName
$serviceWasRunning = $service.Status -ne "Stopped"

try {
    if ($serviceWasRunning) {
        Stop-Service -Name $serviceName -Force
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Stopped, [TimeSpan]::FromSeconds(60))
    }

    Copy-Item -Path $sourcePath -Destination $localBackupPath
    Get-ChildItem -Path $localBackupRoot -Filter "app_backup_*.db" -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip $retentionCount |
        Remove-Item -Force

    if ($null -ne $oneDriveBackupRoot) {
        if (-not (Test-Path $oneDriveBackupRoot -PathType Container)) {
            New-Item -Path $oneDriveBackupRoot -ItemType Directory | Out-Null
        }

        Copy-Item -Path $sourcePath -Destination $oneDriveBackupPath
        Get-ChildItem -Path $oneDriveBackupRoot -Filter "app_backup_*.db" -File |
            Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $retentionCount |
            Remove-Item -Force
    }
    else {
        Write-Warning "OneDrive path not available. Local backup still created."
    }
}
finally {
    if ($serviceWasRunning) {
        Start-Service -Name $serviceName
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(60))
    }
}

Write-Host "Backup completed: $localBackupPath"
