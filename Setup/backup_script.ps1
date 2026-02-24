$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "This script must run in an elevated PowerShell session (Run as Administrator)."
}

$serviceName = "DottoressaIorio.App"
$appRoot = "C:\ProgramData\DottoressaIorio"
$sourcePath = Join-Path $appRoot "app.db"
$localBackupRoot = Join-Path $appRoot "_db_backup"
$retentionCount = 30

$oneDrivePath = Join-Path $env:USERPROFILE "OneDrive"
$vaultPath = Join-Path $oneDrivePath "Personal Vault"
$vaultBackupRoot = Join-Path $vaultPath $serviceName

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$backupFileName = "app_backup_$timestamp.db"
$localBackupPath = Join-Path $localBackupRoot $backupFileName
$vaultBackupPath = Join-Path $vaultBackupRoot $backupFileName

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

    if (Test-Path $vaultPath -PathType Container) {
        if (-not (Test-Path $vaultBackupRoot -PathType Container)) {
            New-Item -Path $vaultBackupRoot -ItemType Directory | Out-Null
        }

        Copy-Item -Path $sourcePath -Destination $vaultBackupPath
        Get-ChildItem -Path $vaultBackupRoot -Filter "app_backup_*.db" -File |
            Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $retentionCount |
            Remove-Item -Force
    }
    else {
        Write-Warning "Personal Vault path not available: $vaultPath. Local backup still created."
    }
}
finally {
    if ($serviceWasRunning) {
        Start-Service -Name $serviceName
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(60))
    }
}

Write-Host "Backup completed: $localBackupPath"
