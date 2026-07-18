# 🚀 WinCleaner v1.10.0 - Release Notes

**Fecha de Lanzamiento**: 19 de Julio de 2026  
**Versión**: v1.10.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 🛠️ 1. Reparador de Archivos de Sistema (SFC & DISM)
* **Integración de Comprobación y Reparación**: Permite ejecutar directamente el Comprobador de Archivos de Sistema (`sfc /scannow`) y la Herramienta de Administración y Mantenimiento de Imágenes de Implementación (`DISM`) de Windows para corregir archivos dañados del sistema operativo.
* **Soporte de Codificación OEM 850**: Se ha registrado `CodePagesEncodingProvider` al inicio de la aplicación para procesar y mostrar correctamente la salida de texto acentuada e internacional generada por SFC y DISM en la consola de comandos de Windows de habla hispana.

### 🔎 2. Crash Inspector (Inspector de Fallos)
* **Historial de Errores de Windows**: Nueva interfaz visual para escanear e inspeccionar informes de cuelgues, errores críticos del Visor de Eventos y fallos del sistema.
* **Ajustes de UI**: Corregido el estilo de selección y efectos hover en los elementos de tipo `ListView` dentro de la interfaz del Inspector de Fallos para mayor coherencia con el diseño global del software.

### 🧹 3. Limpiador de Caché de Desarrollo y Aplicaciones (Dev/App Cache)
* **Optimización de Espacio**: Nuevo módulo de limpieza especializado en vaciar de manera segura directorios de caché de desarrollo y de aplicaciones del sistema.

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.10.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.10.0.zip`** | Paquete comprimido portable del instalador. |
