# 🚀 WinCleaner v1.4.0 - Release Notes

Esta versión presenta grandes novedades orientadas al diagnóstico térmico del hardware, análisis avanzado del arranque de Windows, optimización de la memoria en tiempo real y automatización de tareas de mantenimiento en segundo plano.

## 🚀 Nuevas Características

1. **🧠 Optimizador de RAM y Procesos en Tiempo Real**
   - Panel de control interactivo para monitorizar y gestionar los procesos activos del sistema.
   - Algoritmo "Smart Boot" para liberar y compactar la memoria física ocupada de forma segura mediante un solo clic.
   - Lista detallada con filtros de búsqueda rápida por nombre de proceso y ordenación por consumo de RAM/CPU.
   - Capacidad para terminar procesos de forma forzada directamente desde la interfaz.

2. **🔍 Analizador de Impacto de Inicio y Tiempos de Arranque (Startup & Boot Analyzer)**
   - Dashboard superior integrado en la pestaña de inicio que muestra la velocidad del último arranque del sistema operativo.
   - Diagnósticos avanzados de velocidad mediante lectura directa del registro nativo de eventos de Windows (`Microsoft-Windows-Diagnostics-Performance/Operational`).
   - Muestra el historial cronológico detallado de las duraciones de los últimos 5 arranques en segundos.
   - Clasificación inteligente del estado de arranque (Rápido/Normal/Lento) mediante códigos de color (Verde/Ámbar/Rojo).

3. **🌡️ Dashboard de Temperaturas en Tiempo Real**
   - Nuevo panel de diagnóstico para monitorizar las temperaturas de los componentes de hardware principales: CPU, GPU y Unidades de Almacenamiento (SSD/HDD).
   - Gauges circulares estilizados con animaciones fluidas y colores temáticos del tema oscuro.
   - Sistema de alertas térmicas que notifica al usuario si algún componente alcanza temperaturas elevadas o críticas.
   - Lecturas mediante WMI con motores de fallback dinámicos para garantizar compatibilidad completa con diversas configuraciones de hardware.

4. **📅 Tareas de Mantenimiento Programadas**
   - Integración con el Programador de Tareas nativo de Windows (`schtasks.exe`) para programar limpiezas automáticas.
   - Soporte para frecuencias: Semanal (selección de día), Mensual (día del mes), Al iniciar sesión y Al estar inactivo (Idle).
   - Registro de la tarea silenciosa con privilegios elevados (`/rl HIGHEST`) para saltar de forma segura el control de cuentas de usuario (UAC).
   - Soporte de ejecución por línea de comandos mediante el argumento `--silent-clean` para ejecutar escaneos y limpiezas en segundo plano de forma desatendida.

## 🛠️ Correcciones y Estabilidad

- **Corrección de ProgressBar en RAM**: Se resolvió el comportamiento visual del gráfico de memoria que a veces mostraba la barra vacía debido a problemas de límites de binding de WPF.
- **Corrección de StaticResources**: Se solucionaron errores críticos del motor WPF al cargar recursos de estilos de cabecera (`ColumnHeaderStyle`) en las vistas locales de optimización de RAM y temperatura.
- **Optimización de Procesos en Segundo Plano**: Los temporizadores de refresco (RAM y temperatura) se suspenden automáticamente al navegar a otras pestañas, reduciendo el consumo de procesamiento en segundo plano.
