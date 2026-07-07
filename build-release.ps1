# Script de automatización de compilación y empaquetado para WinCleaner
# Ejecutar en PowerShell: .\build-release.ps1

$ErrorActionPreference = "Stop"
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   WinCleaner - Generador de Release v1.1.0  " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

try {
    # 1. Limpieza de compilaciones anteriores
    Write-Host "`n[1/3] Limpiando carpetas temporales y compilaciones previas..." -ForegroundColor Yellow
    if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
    if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
    Write-Host "✔ Carpetas de caché eliminadas con éxito." -ForegroundColor Green

    # 2. Publicación de la aplicación (Autocontenida)
    Write-Host "`n[2/3] Publicando aplicación (Modo Release, Autocontenido x64)..." -ForegroundColor Yellow
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:PublishTrimmed=false
    Write-Host "✔ Publicación de .NET completada con éxito." -ForegroundColor Green

    # 3. Compilación del instalador con Inno Setup
    Write-Host "`n[3/3] Compilando instalador único con Inno Setup..." -ForegroundColor Yellow
    
    # Rutas típicas de instalación de Inno Setup 6 e Inno Setup 5
    $isccPaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
    )

    $iscc = $null
    foreach ($path in $isccPaths) {
        if (Test-Path $path) {
            $iscc = $path
            break
        }
    }

    if ($null -ne $iscc) {
        Write-Host "Encontrado compilador de Inno Setup en: $iscc" -ForegroundColor Gray
        & $iscc "installer.iss"
        Write-Host "`n✔ ¡Instalador WinCleanerSetup.exe creado con éxito en la raíz!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠ No se detectó 'ISCC.exe' en las rutas por defecto de Inno Setup." -ForegroundColor Orange
        Write-Host "Se omitió el empaquetado automático." -ForegroundColor Orange
        Write-Host "👉 Por favor, abre el archivo 'installer.iss' en la aplicación Inno Setup manualmente y pulsa F9 para compilarlo." -ForegroundColor Yellow
    }

    Write-Host "`n=============================================" -ForegroundColor Green
    Write-Host "   ¡PROCESO FINALIZADO CON ÉXITO!            " -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green

} catch {
    Write-Host "`n❌ ERROR: Ocurrió un fallo durante el proceso de empaquetado." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Exit 1
}
