# 🚀 WinCleaner v1.13.0 - Release Notes

**Fecha de Lanzamiento**: 19 de Julio de 2026  
**Versión**: v1.13.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 🎨 1. Rediseño Completo de Navegación Lateral (UI/UX)
* **Categorías Compactadas**: Compactado el menú lateral a 6 categorías principales utilizando un control de pestañas (`TabControl`) que optimiza el espacio de pantalla y mejora la organización visual.
* **NavigationMenuItem**: Refactorizado el menú lateral utilizando un modelo desacoplado mediante la clase `NavigationMenuItem`.

### ⚡ 2. Optimización de Rendimiento en Duplicados y Fotos
* **Búsqueda Paralela con Hash Parcial**: Aceleración masiva del análisis de archivos duplicados mediante un algoritmo en paralelo que evalúa hashes parciales antes de calcular firmas completas SHA-256.
* **Optimización de RAM en Miniaturas**: Optimizado el uso de memoria RAM del módulo limpiador de fotos durante la visualización de miniaturas de imágenes para evitar consumos de memoria elevados.

### 🛠️ 3. Modularización SRP en Desinstalador
* **AppUninstallerService**: Refactorización profunda siguiendo el Principio de Responsabilidad Única (SRP) para independizar el escaneo de programas y procesos de desinstalación silenciosa.

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.13.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.13.0.zip`** | Paquete comprimido portable del instalador. |
