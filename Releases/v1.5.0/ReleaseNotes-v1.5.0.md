# 🚀 WinCleaner v1.5.0 - Release Notes

¡Nos complace presentar la versión **v1.5.0** de WinCleaner! Este lanzamiento consolida dos grandes módulos diseñados para extender el diagnóstico del hardware en equipos portátiles y la salud de los controladores del sistema de forma segura.

---

## 🚀 Novedades y Módulos Añadidos

### 1. 🔋 Gestión de Batería y Energía (Power Optimizer)
* **Monitoreo en Tiempo Real**: Visualización circular (Gauge) del porcentaje de carga y estado del cargador (CA vs Portátil).
* **Cálculo de Autonomía**: Muestra estimaciones en horas/minutos restantes de funcionamiento a batería mediante P/Invoke nativos.
* **Degradación Física de Celdas**: Consulta las capacidades original de fábrica (`DesignedCapacity`) y máxima real actual (`FullChargedCapacity`) en mWh para calcular el porcentaje exacto de desgaste de la batería.
* **Gestión de Perfiles**: Permite listar y alternar entre los planes de energía nativos de Windows (Equilibrado, Economizador, Alto Rendimiento) directamente desde WinCleaner llamando de manera silenciosa a `powercfg.exe`.
* **Consejos Inteligentes**: Despliega avisos adaptativos y consejos para maximizar la duración de la batería de litio.
* **Diseño Seguro para Sobremesas**: Detecta automáticamente si no hay una batería presente en el equipo (PC de escritorio) mostrando un aviso informativo estático sin provocar bloqueos ni consumos.

### 2. 🔌 Gestor de Controladores (Driver Inspector)
* **Inventario de Controladores**: Escaneo rápido de controladores locales firmados usando consultas WMI (`Win32_PnPSignedDriver`).
* **Categorización Semántica**: Filtra y expone los dispositivos divididos en Vídeo, Red, Sonido, Bluetooth y Otros.
* **Semáforo de Antigüedad**: Identifica los controladores obsoletos (más de 3 años de antigüedad) con un color de advertencia Ámbar ("Antiguo") para avisar de posibles mejoras de estabilidad.
* **Búsqueda e Interfaz**: Caja de búsqueda en tiempo real e iconos informativos sobre la versión, fecha y proveedor (NVIDIA, AMD, Intel, Realtek, etc.).
* **Buscador de Actualizaciones Seguro (COM)**: Consulta de forma desatendida la API COM nativa de Windows Update buscando específicamente actualizaciones oficiales de tipo controlador (`IsInstalled=0 and Type='Driver'`).
* **Accesos Directos**: Abre de un clic el Administrador de Dispositivos (`devmgmt.msc`) o la sección de Actualizaciones de Controladores de Windows Update.

---

## 🛠️ Correcciones y Estabilidad
* **Ciclo de Vida de Timers**: Optimizada la navegación en `MainViewModel.cs` para detener los timers de actualización del panel de batería y temperaturas cuando el usuario sale de estas pestañas, reduciendo a cero el uso de CPU residual.
* **Fuga de Advertencias**: Resueltas todas las advertencias de compilación y posibles referencias nulas en el analizador de Roslyn.

---

### 📦 Detalles del Commit Consolidador
* **Commit**: `babc401` / Hito de lanzamiento.
* **Tag**: `v1.5.0`
