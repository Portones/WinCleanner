# WinCleaner - Panel de Control y Limpieza Profesional

Sistema de Diagnóstico, Limpieza y Optimización para Windows 10 y Windows 11 desarrollado con .NET 9, WPF y arquitectura MVVM.

---

## 🚀 Requisitos Previos y Ejecución

### Requisitos Previos
* **Sistema Operativo**: Windows 10 o Windows 11.
* **SDK o Runtime de .NET 9**: Asegúrate de tener instalado [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) (WPF/Desktop Runtime).

### Cómo Lanzar la Aplicación (Terminal)
1. Abre una terminal (PowerShell o CMD) en la carpeta raíz del proyecto:
   `c:\Users\Rodrigo Portones\Programacion\WinCleanner`
2. Ejecuta el comando para restaurar, compilar y arrancar la aplicación:
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
  * **Limpieza de Espacio**: Limpieza Avanzada, Buscador de Duplicados, Desinstalador de Apps.
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

---

## 🛠️ Configuración y Logs

* **Ajustes (`settings.json`)**: Ubicado en `%APPDATA%\WinCleaner\settings.json`. Permite cambiar el idioma ("es"/"en"), tema ("Dark"/"Light") y directorios excluidos (lista negra de sistema como System32).
* **Diagnósticos (Logs)**: Historial detallado del sistema en `%APPDATA%\WinCleaner\Logs\cleaner_log.txt` gestionado por **Serilog**.
