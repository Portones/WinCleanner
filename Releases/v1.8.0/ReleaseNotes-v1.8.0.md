# 🚀 WinCleaner v1.8.0 - Release Notes

**Fecha de Lanzamiento**: 18 de Julio de 2026  
**Versión**: v1.8.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 🌡️ 1. Dashboard de Temperaturas Multi-Disco

* **Detección de Todos los Discos Físicos**: El módulo de Temperaturas ahora detecta y muestra la temperatura, nombre del modelo y estado térmico de **todos los discos físicos conectados** (SSD, NVMe, HDD), enumerados por letra de unidad (C:, D:, E:...).
* **Badges de Estado Térmico**: Cada tarjeta de disco muestra su estado en tiempo real (*Normal*, *Caliente*, *Crítico*) con codificación de color clara.
* **Alertas Automáticas**: El ViewModel genera alertas proactivas si cualquier disco supera el umbral de temperatura segura.

### 🔔 2. Monitoreo Silencioso y Bandeja del Sistema (System Tray)

* **Icono en la Bandeja del Sistema**: WinCleaner permanece activo junto al reloj de Windows al minimizar.
* **Menú Contextual Rápido**: Acceso a *Abrir WinCleaner*, *⚡ Optimizar RAM ahora*, *📊 Generar Reporte de Diagnóstico* y *❌ Salir* directamente desde el icono de la bandeja.
* **Monitor en Segundo Plano**: Comprobaciones automáticas cada 30 segundos con notificaciones emergentes nativas de Windows en los siguientes eventos críticos:
  * Memoria **RAM por encima del 85%**.
  * **Espacio libre en C: inferior a 10 GB**.
  * **Temperatura de CPU o GPU superior a 80°C**.

### 📊 3. Generador de Reportes de Diagnóstico HTML

* Nuevo botón **"📊 Generar Reporte"** en la cabecera del Panel de Control (Dashboard).
* Genera y abre automáticamente un **informe HTML autocontenido** guardado en `Mis Documentos/WinCleaner_Reportes/`.
* El reporte incluye: uso de CPU, RAM libre, historial acumulado de espacio liberado, temperaturas de todos los sensores detectados y aplicaciones de alto impacto en el inicio de Windows.
* También accesible desde el menú contextual del icono en la bandeja del sistema.

### ⚙️ 4. Configuración de Notificaciones y Bandeja

* Nueva tarjeta **"Notificaciones y Bandeja del Sistema"** en la pantalla de Configuración.
* Interruptores individuales para activar o desactivar:
  * *Minimizar a la Bandeja del Sistema al cerrar*.
  * *Monitoreo Silencioso de Hardware*.
  * *Notificación por Memoria RAM Elevada (> 85%)*.
  * *Notificación por Poco Espacio en Disco C: (< 10 GB)*.
  * *Notificación por Temperatura Crítica (> 80°C)*.

### 🧠 5. Asistente Inteligente y Sugerencias Contextuales

* **Diagnóstico de Red y Vaciado de DNS**: Mide la latencia de red y, si supera los 100 ms, muestra una sugerencia en el Dashboard con un botón de 1 clic para ejecutar `ipconfig /flushdns`.
* **Detección de Puntos de Restauración Antiguos**: Detecta acumulación de múltiples puntos de restauración del sistema y ofrece eliminar las copias antiguas conservando la más reciente.
* **Descargas Antiguas**: Detecta archivos en la carpeta *Descargas* sin abrir durante más de 30 días y mayores de 10 MB, mostrando el tamaño total recuperable.
* **Capturas de Pantalla Acumuladas**: Avisa cuando la carpeta `Imágenes/Screenshots` supera 15 archivos.
* Todas las sugerencias se integran dinámicamente en el Panel de Control con **botones de acción inmediata**.

### 🌐 6. Limpiador de Navegadores Web

* Soporte completo para **Chrome, Edge, Firefox, Brave, Opera y Vivaldi**.
* Detecta automáticamente todos los perfiles instalados y cuantifica la caché acumulada: imágenes, Code Cache, Service Workers, GPU Cache y logs.
* Limpieza completa con barra de progreso visual.
* Nueva entrada **"Navegadores y Logs"** en el menú lateral de navegación.

### 📋 7. Limpiador de Registros de Eventos de Windows

* Pestaña integrada en la vista de Navegadores y Logs.
* Enumera los registros `Application`, `System`, `Security`, `Setup` y otros con número de entradas y tamaño de archivo `.evtx`.
* Permite seleccionar individualmente qué registros vaciar, con confirmación previa obligatoria.

### 🛡️ 8. Guardián de Desinstalaciones en Tiempo Real

* Se activa silenciosamente al arrancar WinCleaner como un monitor en segundo plano.
* Monitoriza el registro de Windows mediante **WMI `RegistryKeyChangeEvent`**.
* Emite automáticamente una **notificación nativa de Windows** cuando detecta que se ha completado una desinstalación, invitando al usuario a limpiar los residuos que hayan podido quedar.

---

## 🔧 Mejoras Internas

* Nuevo `BytesToSizeConverter` global en `App.xaml` para reutilizar el formateo de tamaños en todas las vistas XAML.
* Registro correcto de todos los nuevos servicios en el contenedor de inyección de dependencias de `App.xaml.cs`.
* Arranque automático del `UninstallWatcherService` durante el inicio de la aplicación.
* Arquitectura limpia MVVM mantenida con interfaces correctamente tipadas (`IConfigurationService`, `IRamBoosterService`).

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.8.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.8.0.zip`** | Paquete comprimido portable del instalador. |
