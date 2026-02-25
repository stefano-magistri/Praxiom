$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "This script must run in an elevated PowerShell session (Run as Administrator)."
}

$serviceName = "DottoressaIorio.App"
$appRoot = "C:\ProgramData\DottoressaIorio"
$sourcePath = Join-Path $appRoot "app.db"
$localBackupRoot = Join-Path $appRoot "_db_backup"
$retentionCount = 360

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
    Join-Path $oneDriveRoot "DottoressaIorio\_db_backup"
} else {
    $null
}

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
