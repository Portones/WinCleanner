# 🚀 WinCleaner v1.7.0 - Release Notes

**Fecha de Lanzamiento**: 18 de Julio de 2026  
**Versión**: v1.7.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 📌 1. Integración con el Inicio de Windows
* **Arranque Automático Configurable**: Nueva opción dentro del menú de Configuración para permitir que WinCleaner se lance automáticamente con Windows al encender el equipo.

### 🧹 2. Desinstalador Inteligente con Escaneo de Huérfanos e Inicio
* **Refresco Automático**: Al desinstalar un programa, la lista de aplicaciones se actualiza de inmediato sin requerir recarga manual.
* **Escaneo de Huérfanos e Inicio de Windows**: Tras desinstalar una aplicación, WinCleaner ofrece buscar archivos, carpetas en AppData/Program Files, claves de registro y **accesos/registros de inicio automático residuales** dejados por el programa eliminado.

### ⚡ 3. Ronda de Correcciones, Usabilidad y Rendimiento (10 Mejoras)
* **Organización de Releases**: Subcarpetas individuales por versión (`Releases/vX.X.X/`) para alojar instaladores, notas y archivos zip.
* **ScrollBars Ergonómicas**: Rediseño global de barras de desplazamiento a `10px` con deslizables (`Thumb`) de altura fija legible en listas extensas.
* **Eliminación Ultrarrápida en Limpieza Avanzada**: Corrección de cuello de botella en remoción de listas WPF, acelerando el procesado masivo de miles de elementos.
* **Escaneo Seguro y Acelerado de Disco**: Omitidos puntos de reanálisis/junctions (`System Volume Information`, `$Recycle.Bin`) y tolerancia a carpetas denegadas.
* **ComboBox Estilizado en Energía**: Diseño oscuro coherente `#1E293B` en el selector de planes de energía.
* **Redimensionado de Columnas y Contador de Selección**: Redimensionado libre de columnas en tablas y visualización detallada (`X MB en Y elementos`).
* **Barras de Progreso Interactivas**: Integradas en Limpieza Avanzada y Gestor de Controladores.
* **Ordenación en Gestión de Inicio**: Ordenación rápida por estado (*Habilitados / Deshabilitados*), nombre e impacto.
* **Búsqueda en Menú Contextual**: Filtrado en tiempo real en la gestión de extensiones de shell del Explorador.
* **Estabilidad Global**: Corrección del recurso `ModernTextBox` en el Menú Contextual.

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.7.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.7.0.zip`** | Paquete comprimido portable del instalador. |
