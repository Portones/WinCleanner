# Release Notes — WinCleaner v1.9.0

**Fecha de lanzamiento:** 2026-07-18  
**Plataforma:** Windows 10 / Windows 11  
**Framework:** .NET 9.0 WPF (x64)  
**Tag:** `v1.9.0`

---

## 📋 Resumen del Hito

Esta versión consolida tres grandes bloques de desarrollo que elevan WinCleaner de una herramienta de limpieza a una **suite de diagnóstico y optimización inteligente** con monitoreo en tiempo real, notificaciones proactivas y módulos de limpieza avanzada.

---

## 🌡️ v1.8.0 — Temperaturas Multi-Disco (commit `1319ed2`)

- **Dashboard de Temperaturas ampliado**: detección y visualización de temperatura, modelo y estado térmico de **todos los discos físicos** conectados (SSD, NVMe, HDD), enumerados por letra de unidad.
- Nuevas propiedades `ComponentCategory`, `DriveLetter` y `ModelName` en `TemperatureItem`.
- Alertas de temperatura crítica en el ViewModel para cualquier disco que supere el umbral seguro.
- Refinamiento de `TemperatureView.xaml`: badges de estado, nombre del modelo y letra de unidad por tarjeta de disco.

---

## 🔔 v1.9.0 — Monitoreo Silencioso, Notificaciones Nativas y Reportes HTML (commits `22183c5`)

### System Tray y Monitoreo Silencioso
- **Icono en la bandeja del sistema** (area de notificación junto al reloj).
- Menú contextual rápido: **Abrir WinCleaner**, **⚡ Optimizar RAM ahora**, **📊 Generar Reporte**, **❌ Salir**.
- Monitor en segundo plano configurable con **comprobaciones cada 30 segundos**.

### Notificaciones Nativas de Windows
- Alerta emergente cuando la **RAM supera el 85%**.
- Alerta emergente cuando el **espacio libre en C: cae por debajo de 10 GB**.
- Alerta emergente cuando la **temperatura del procesador (CPU) o GPU supera los 80°C**.

### Generador de Reportes de Diagnóstico HTML
- Botón **"📊 Generar Reporte"** en el Dashboard.
- Exporta un informe HTML autocontenido y estilizado a `Mis Documentos/WinCleaner_Reportes/`.
- El reporte incluye: resumen de CPU, RAM libre, espacio liberado histórico, sensores de temperatura y apps de inicio de impacto alto.

### Configuración de Notificaciones
- Nueva tarjeta en **Configuración** con interruptores individuales para cada tipo de notificación y el comportamiento de minimizar a la bandeja.

---

## 🧠 v1.9.0 — Asistente Inteligente y Sugerencias Contextuales (commit `2744ff5`)

- **Diagnóstico de Red y Flush DNS**: mide latencia en tiempo real; si supera 100 ms, propone vaciar la caché DNS con 1 clic.
- **Puntos de Restauración Antiguos**: detecta acumulación de copias de sombra y ofrece limpiarlas conservando la más reciente.
- **Descargas Antiguas**: detecta archivos pesados (> 10 MB) sin abrir durante más de 30 días en la carpeta `Descargas`.
- **Capturas de Pantalla acumuladas**: avisa cuando `Imágenes/Screenshots` supera 15 archivos.
- Todas las sugerencias aparecen directamente en el **Panel de Control** con botones de acción inmediata.

---

## 🧹 v1.9.0 — Módulos de Limpieza y Mantenimiento Avanzado (commit `f9cd0cf`)

### Limpiador de Navegadores Web
- Soporte completo para **Chrome, Edge, Firefox, Brave, Opera y Vivaldi**.
- Detecta automáticamente todos los perfiles instalados y enumera caché de imágenes, Code Cache, Service Workers, GPU Cache y logs.
- Limpieza completa con barra de progreso.
- Nueva entrada **"Navegadores y Logs"** en el menú lateral.

### Limpiador de Registros de Eventos de Windows
- Enumera y muestra los registros `Application`, `System`, `Security`, `Setup` y otros con número de entradas y tamaño de archivo `.evtx`.
- Permite seleccionar individualmente qué registros vaciar, con confirmación previa.

### Guardián de Desinstalaciones en Tiempo Real
- Se activa silenciosamente al arrancar WinCleaner.
- Monitoriza el registro de Windows mediante **WMI `RegistryKeyChangeEvent`**.
- Emite automáticamente una **notificación nativa de Windows** cuando detecta que se ha desinstalado una aplicación, invitando al usuario a limpiar los residuos.

---

## 🔧 Mejoras Internas

- Nuevo `BytesToSizeConverter` registrado globalmente en `App.xaml` para reutilizar el formateo de tamaños en todas las vistas XAML.
- Registro correcto de todos los nuevos servicios en el contenedor DI de `App.xaml.cs`.
- Arranque automático del `UninstallWatcherService` durante el inicio de la aplicación.

---

## 📦 Archivos del Lanzamiento

| Archivo | Descripción |
|---|---|
| `WinCleaner-v1.9.0-Setup.exe` | Instalador de WinCleaner (Inno Setup) |
| `WinCleaner-v1.9.0-Portable.zip` | Versión portátil (sin instalación) |

> ⚠️ Los binarios de instalación/portátil no se incluyen en el repositorio Git (excluidos por `.gitignore`). Descárgalos desde la sección **Releases** de GitHub.

---

## ⬆️ Cómo Actualizar

1. Desinstala la versión anterior desde **Panel de Control → Programas**.
2. Descarga e instala `WinCleaner-v1.9.0-Setup.exe`.
3. Tu configuración guardada se conservará automáticamente.
