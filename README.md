# WinCleaner - Panel de Control y Limpieza Profesional (v1.8.0)

Sistema de Diagnóstico, Limpieza y Optimización para Windows 10 y Windows 11 desarrollado con .NET 9, WPF y arquitectura MVVM.

---

## 🚀 Instalación y Ejecución

### 💻 Para Usuarios (Método Recomendado)
Si solo deseas utilizar WinCleaner en tu ordenador, no necesitas compilar código ni instalar herramientas de desarrollo:

1. Ve a la sección lateral de **[Releases](https://github.com/RodrigoPortones/WinCleaner/releases)** a la derecha de este repositorio.
2. Descarga la versión más reciente del instalador único: **`WinCleanerSetup.exe`**.
3. Ejecuta el instalador. Este colocará la aplicación de forma segura en tu sistema, creará un acceso directo en el Escritorio e integrará la utilidad de desinstalación de Windows.

> [!IMPORTANT]
> **Privilegios de Administrador**: WinCleaner realiza análisis profundos de disco, optimizaciones de RAM, vaciado de DNS, control de servicios de sistema y actualizaciones silenciosas mediante Winget. Por ello, la aplicación solicitará **ejecutarse como Administrador** al abrirse. Esto es totalmente normal y necesario para el correcto funcionamiento de todas las herramientas.

---

### 🛠️ Para Desarrolladores (Compilación desde Código)
Si deseas estudiar el código, modificar la aplicación o ejecutarla de forma local:

#### Requisitos Previos
* **Sistema Operativo**: Windows 10 o Windows 11.
* **SDK de .NET 9**: Asegúrate de tener instalado el [SDK de .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) para escritorio.

#### Cómo Lanzar la Aplicación
1. Abre una terminal con privilegios de **Administrador** en la carpeta raíz del proyecto.
2. Ejecuta el comando para compilar y arrancar la aplicación:
   ```bash
   dotnet run
   ```

---

## 🛠️ Características Principales y Novedades

### 1. Panel de Control (Dashboard) Unificado
* **Gráficas Circulares**: Anillos de progreso circulares y estilizados para medir de un vistazo el uso de **CPU, Memoria RAM y Almacenamiento**.
* **Métrica de GPU**: Tarjeta dedicada que reconoce las GPUs del sistema e informa sobre su VRAM y la versión del driver.
* **Salud del Sistema**: Indicador dinámico animado (pulso de color) que evalúa el estado general del equipo y sugiere acciones automáticas.

### 2. Navegación Categorizada
* Acceso organizado mediante un panel lateral dividido en 4 categorías:
  * **Diagnóstico y Estado**: Dashboard, Analizador de Disco.
  * **Limpieza de Espacio**: Limpieza Avanzada, Buscador de Duplicados, Limpiador de Fotos, Desinstalador de Apps.
  * **Optimización y Sistema**: Rendimiento y Red, Actualizar Apps, Gestión de Inicio, Servicios, Menú Contextual.
  * **Opciones**: Configuración de tema/idioma.
* **Indicador de Página Activa**: Resaltado en azul índigo del apartado abierto actualmente.

### 3. Rediseño Estético de Tablas y Scrollbars
* **Scrollbars Oscuras Globales**: Desplazamiento moderno y estilizado (barra fina de 8px con hover animado) adaptado al tema oscuro en todas las ventanas.
* **Tablas Homogéneas**: ListView con cabeceras planas y oscuras, bordes redondeados y filas alternadas con resaltado táctil.

### 4. Optimizador de Red y DNS Speed Test
* **DNS Speed Test**: Mide el ping en ms de los principales servidores DNS públicos (Cloudflare, Google, Quad9, OpenDNS).
* **Speed Test de Descarga**: Descarga activa de bloques de datos de 10 MB desde Cloudflare CDN para reportar la velocidad de bajada real en **Mbps**.
* **Flush DNS**: Vaciado rápido de la caché local de resolución DNS.

### 5. Actualizador de Aplicaciones Desatendido (Winget)
* **Escaneo de Actualizaciones**: Conexión con el Gestor de Paquetes de Windows (`winget`) para listar programas del sistema con versiones nuevas disponibles.
* **Instalación Silenciosa**: Descarga e instala en segundo plano las aplicaciones seleccionadas de forma automática, omitiendo los asistentes interactivos.
* **Filtros e Interfaz**: Caja de búsqueda interactiva por nombre o ID del paquete, y visualización de progreso global.

### 6. Limpiador Inteligente de Fotos
* **Capturas Obsoletas**: Escaneo en directorios comunes (Imágenes, Escritorio, Descargas) para encontrar capturas de pantalla de antigüedad configurable (ej. > 30 días) con previsualización eficiente en miniaturas.
* **Fotos Duplicadas (SHA-256)**: Algoritmo de doble filtro (agrupamiento rápido por peso y posterior coincidencia de firma SHA-256) para identificar y comparar imágenes idénticas lado a lado con casillas de acción.

### 7. Desinstalador por Lotes y Debloater de Windows
* **Doble Pestaña**: Separación clara entre aplicaciones instaladas comunes y aplicaciones UWP nativas preinstaladas por el fabricante (Bloatware de Windows).
* **Desinstalación por Lotes**: Selección de múltiples aplicaciones a la vez mediante casillas de verificación para ejecutarlas secuencialmente en segundo plano con indicadores de progreso dinámicos.

### 8. Desactivador de Telemetría y Privacidad (Privacy Tweaker)
* **Control Completo**: Interruptores modernos tipo ToggleSwitch en el apartado de Rendimiento para desactivar la telemetría corporativa (DiagTrack), los informes de error de Windows (WER), el ID de publicidad invasivo para anuncios personalizados, el asistente de Cortana y las conexiones entre dispositivos en segundo plano (Rome SDK).

### 9. Optimizador de RAM e Inicio
* **Limpieza de RAM**: Compactación inteligente de la memoria física activa con un solo clic.
* **Procesos en Tiempo Real**: Lista interactiva de procesos activos que permite buscar y finalizar tareas consumidoras.
* **Tiempos de Arranque**: Análisis histórico de velocidad de inicio basado en el Visor de Eventos nativo de Windows.

### 10. Monitoreo Térmico y Mantenimiento Programado
* **Temperaturas**: Dashboard térmico en tiempo real para CPU, GPU y Almacenamiento con Gauges circulares y alertas de calor.
* **Programación**: Configuración de tareas de limpieza silenciosas en segundo plano (`--silent-clean`) registradas de forma segura en Windows Task Scheduler.

### 11. Gestión de Batería y Energía
* **Gráfica de Carga**: Gauge circular e indicador dinámico de tiempo de autonomía restante.
* **Salud Física de Celdas**: Contraste entre la capacidad nominal original y la máxima de carga actual (en mWh) para reportar desgaste del hardware.
* **Planes de Energía**: Listado y conmutación de esquemas de energía (Equilibrado, Economizador, Alto Rendimiento) mediante `powercfg.exe`.

### 12. Gestor de Controladores (Driver Inspector)
* **Escaneo Local**: Consulta WMI de controladores firmados clasificados por categorías con indicador de antigüedad (> 3 años).
* **Actualizaciones Oficiales**: Detección dinámica de controladores pendientes mediante la API COM nativa del agente de Windows Update.
* **Herramientas de Windows**: Acceso directo al Administrador de Dispositivos (`devmgmt.msc`) y ventana de actualización de controladores opcionales.

### 13. Motor de Sugerencias Contextuales Inteligentes
* **Limpieza Avanzada**: Detección inteligente de instaladores redundantes (programas que ya tienes instalados en el sistema) y descargas pesadas obsoletas de más de 6 meses.
* **Optimización de Arranque**: Banner de alerta superior que identifica programas habilitados con impacto de inicio "Alto" y permite desactivarlos en lote de un solo clic.
* **Eficiencia en Portátiles**: Detección activa si el equipo funciona en batería con carga baja y perfil de alto consumo, habilitando el botón rápido "Activar Ahorro" para alternar al plan economizador.
* **Diagnósticos de Estabilidad**: Alerta si el hardware principal cuenta con controladores desactualizados, facilitando su puesta al día oficial.

---

## 🛠️ Configuración y Logs

* **Ajustes (`settings.json`)**: Ubicado en `%APPDATA%\WinCleaner\settings.json`. Permite cambiar el idioma ("es"/"en"), tema ("Dark"/"Light") y directorios excluidos (lista negra de sistema como System32).
* **Diagnósticos (Logs)**: Historial detallado del sistema en `%APPDATA%\WinCleaner\Logs\cleaner_log.txt` gestionado por **Serilog**.

---

## ⚠️ Descargo de Responsabilidad (Disclaimer)

Esta herramienta realiza tareas de limpieza, optimización y modificación del sistema, incluyendo la eliminación permanente de archivos (evitando la Papelera de reciclaje) y la gestión de procesos o servicios. 

El uso de **WinCleaner** es bajo su propia responsabilidad. Los desarrolladores no se hacen responsables de pérdidas de datos, fallos en el sistema o cualquier otro perjuicio derivado de la ejecución de esta aplicación. Se recomienda encarecidamente realizar copias de seguridad de sus datos importantes antes de proceder con limpiezas profundas o modificaciones críticas.

