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

$destinationDbPath = Join-Path $AppRoot "app.db"
$backupRoot = Join-Path $AppRoot "_db_backup"
if (-not (Test-Path -Path $backupRoot -PathType Container)) {
    New-Item -Path $backupRoot -ItemType Directory | Out-Null
}

$service = Get-Service -Name $ServiceName -ErrorAction Stop
$serviceWasRunning = $service.Status -ne "Stopped"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$preRestoreBackupPath = Join-Path $backupRoot "app_pre_restore_$timestamp.db"

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
