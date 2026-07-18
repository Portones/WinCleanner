# 🚀 WinCleaner v1.9.0 - Release Notes

**Fecha de Lanzamiento**: 18 de Julio de 2026  
**Versión**: v1.9.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 🔄 1. Sistema de Comprobación y Actualización Automática
* **Actualización desde GitHub Releases**: WinCleaner ahora puede verificar automáticamente si hay una nueva versión disponible directamente desde los Releases públicos del repositorio de GitHub.
* **Interfaz Visual de Actualización**: Añadida una sección y un botón dinámico en la pantalla de Configuración para buscar actualizaciones y descargarlas de forma interactiva.
* **Estilado Mejorado**: Corregido el estilo del botón de actualización y mejorada la detección de la versión actual para evitar falsos positivos.

### 🛡️ 2. Estabilidad de la Bandeja de Sistema (SysTray) y Cierre
* **Mantener en Bandeja de Entrada**: Corregido el comportamiento al pulsar el botón de cerrar de la ventana principal; ahora el evento `Closing` se enlaza correctamente para ocultar la aplicación en la bandeja del sistema en lugar de destruirla por completo, si está activada dicha configuración.
* **Corrección de Icono en Compilado**: Resuelto el problema de carga del icono de la bandeja del sistema al ejecutar el instalador compilado final.

### 🎨 3. Ajustes de UI y Limpieza de Navegadores
* **BrowserCleanupView**: Mejoras visuales en el estilado del limpiador de navegadores.

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.9.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.9.0.zip`** | Paquete comprimido portable del instalador. |
