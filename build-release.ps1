# Script de automatización de compilación y empaquetado para WinCleaner
# Ejecutar en PowerShell: .\build-release.ps1 [-Version "1.2.0"]

param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

# Intentar detectar la versión automáticamente desde la UI si no se pasa como parámetro
if ([string]::IsNullOrEmpty($Version)) {
    $xamlPath = "Views\MainWindow.xaml"
    if (Test-Path $xamlPath) {
        $content = Get-Content $xamlPath -Raw
        if ($content -match 'Versión\s+(\d+\.\d+\.\d+)') {
            $Version = $Matches[1]
        }
    }
    # Valor de respaldo si todo falla
    if ([string]::IsNullOrEmpty($Version)) {
        $Version = "1.1.0"
    }
}

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   WinCleaner - Generador de Release v$Version  " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

try {
    # 1. Limpieza de carpetas temporales y compilaciones previas
    Write-Host "`n[1/3] Limpiando carpetas temporales y compilaciones previas..." -ForegroundColor Yellow
    try { Remove-Item -Path "bin\Release" -Recurse -Force -ErrorAction SilentlyContinue } catch {}
    try { Remove-Item -Path "obj\Release" -Recurse -Force -ErrorAction SilentlyContinue } catch {}
    Write-Host "✔ Preparación de directorios completada." -ForegroundColor Green

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
        $releaseDir = "Releases\v$Version"
        if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }

        # Pasar la versión dinámica y directorio de salida usando los parámetros /D y /O de ISCC
        & $iscc "/DAppVersion=$Version" "/O$releaseDir" "installer.iss"
        Write-Host "`n✔ ¡Instalador WinCleanerSetup-v$Version.exe creado con éxito en $releaseDir!" -ForegroundColor Green

        # 4. Comprimir el instalador en un archivo ZIP
        Write-Host "`nComprimiendo instalador en un archivo ZIP..." -ForegroundColor Yellow
        $exePath = Join-Path $releaseDir "WinCleanerSetup-v$Version.exe"
        $zipPath = Join-Path $releaseDir "WinCleanerSetup-v$Version.zip"
        if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
        Compress-Archive -Path $exePath -DestinationPath $zipPath -Force
        Write-Host "✔ ¡Archivo ZIP WinCleanerSetup-v$Version.zip creado con éxito en $releaseDir!" -ForegroundColor Green
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
