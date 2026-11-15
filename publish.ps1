<#
.SYNOPSIS
    DisplayFlip - Skrip Build & Distribusi Executable Mandiri (File Utuh)

.DESCRIPTION
    Mempublikasikan DisplayFlip (WPF .NET 10) ke format executable mandiri (.exe)
    untuk penggunaan harian tanpa memerlukan 'dotnet run'.

.PARAMETER Mode
    Pilihan mode distribusi:
    - FrameworkDependent (Default): Ukuran kecil (~7 MB), cepat, menggunakan .NET 10 Desktop Runtime yang terpasang.
    - Standalone: Ukuran ~78 MB terkompresi, paket lengkap (Self-Contained), dapat berjalan di Windows x64 mana pun tanpa instalasi .NET terpisah.
    - All: Membangun kedua varian sekaligus.

.PARAMETER Clean
    Membersihkan direktori artefak lama sebelum mempublikasikan.

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -Mode Standalone
    .\publish.ps1 -Mode All -Clean
#>

[CmdletBinding()]
param(
    [ValidateSet("FrameworkDependent", "Standalone", "All")]
    [string]$Mode = "FrameworkDependent",

    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  DisplayFlip - Build & Publish Distribution Engine" -ForegroundColor Cyan
Write-Host "  Target: Windows 11 Fluent UI (.NET 10 / WPF-UI 4.3)" -ForegroundColor DarkGray
Write-Host "==========================================================" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "`n[1/3] Membersihkan folder artefak lama..." -ForegroundColor Yellow
    $fddDir = Join-Path $repoRoot "artifacts\DisplayFlip"
    $standaloneDir = Join-Path $repoRoot "artifacts\DisplayFlip-Standalone"
    if (Test-Path $fddDir) {
        Remove-Item -Recurse -Force $fddDir
        Write-Host "  • Dihapus: $fddDir" -ForegroundColor DarkGray
    }
    if (Test-Path $standaloneDir) {
        Remove-Item -Recurse -Force $standaloneDir
        Write-Host "  • Dihapus: $standaloneDir" -ForegroundColor DarkGray
    }
}

$projectPath = Join-Path $repoRoot "src\DisplayFlip.Wpf\DisplayFlip.Wpf.csproj"

if ($Mode -in @("FrameworkDependent", "All")) {
    Write-Host "`n[Publish] Membangun Framework-Dependent Single-File (~7 MB)..." -ForegroundColor Green
    dotnet publish $projectPath /p:PublishProfile=FolderProfile-FrameworkDependent --nologo
    
    $exePath = Join-Path $repoRoot "artifacts\DisplayFlip\DisplayFlip.exe"
    if (Test-Path $exePath) {
        $sizeMb = [Math]::Round((Get-Item $exePath).Length / 1MB, 2)
        Write-Host "  -> Berhasil! File: $exePath ($sizeMb MB)" -ForegroundColor Green
    } else {
        Write-Error "File output tidak ditemukan di: $exePath"
    }
}

if ($Mode -in @("Standalone", "All")) {
    Write-Host "`n[Publish] Membangun Standalone Self-Contained Single-File (~78 MB)..." -ForegroundColor Green
    dotnet publish $projectPath /p:PublishProfile=FolderProfile-Standalone --nologo
    
    $exePath = Join-Path $repoRoot "artifacts\DisplayFlip-Standalone\DisplayFlip.exe"
    if (Test-Path $exePath) {
        $sizeMb = [Math]::Round((Get-Item $exePath).Length / 1MB, 2)
        Write-Host "  -> Berhasil! File: $exePath ($sizeMb MB)" -ForegroundColor Green
    } else {
        Write-Error "File output tidak ditemukan di: $exePath"
    }
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " Distribusi Siap Digunakan Harian!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Lokasi File Executable:" -ForegroundColor White
Write-Host "  • Utama (FDD, Rekomendasi): $repoRoot\artifacts\DisplayFlip\DisplayFlip.exe" -ForegroundColor Yellow
Write-Host "  • Standalone (Portabel):    $repoRoot\artifacts\DisplayFlip-Standalone\DisplayFlip.exe" -ForegroundColor Yellow
Write-Host "`nCara Pakai Harian:" -ForegroundColor White
Write-Host "  1. Jalankan langsung: Double-click 'DisplayFlip.exe' atau buat pintasan di Desktop/Taskbar."
Write-Host "  2. Berjalan di latar: Gunakan argumen '--tray' (DisplayFlip.exe --tray)."
Write-Host "  3. Aktifkan otomatis: Buka halaman 'Umum & Ikon', lalu aktifkan switch 'Mulai otomatis saat login Windows'."
Write-Host "  4. Kontrol CLI:       displayflip.exe --swap / --identify / --solo 1 / --list"
Write-Host "==========================================================" -ForegroundColor Cyan
