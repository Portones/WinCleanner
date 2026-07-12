# 🚀 WinCleaner v1.2.2 - Release Notes

Esta release unifica todas las grandes características y optimizaciones implementadas desde la versión base, preparadas para su distribución pública.

---

## 🛠️ Novedades Clave en esta Release

### 1. ⚡ Optimización DNS con un Clic

- **Selección inteligente de DNS**: Añadido el botón `"⚡ Aplicar DNS más rápido"` en la pestaña de Rendimiento.
- **Medición en tiempo real**: Ejecuta pruebas de latencia (ping) en segundo plano para Cloudflare, Google, Quad9 y OpenDNS, encuentra el de menor tiempo de respuesta y lo aplica automáticamente a todos tus adaptadores de red activos en Windows (requiere privilegios).

### 2. 📊 Historial de Limpiezas y Estadísticas en Dashboard

- **Monitoreo de Espacio**: El Dashboard ahora muestra la cantidad acumulada de espacio de disco liberado desde que instalaste WinCleaner.
- **Sesiones Recientes**: Muestra una lista dinámica en el panel con las últimas 5 limpiezas, indicando fecha, cantidad de archivos depurados y tamaño recuperado.

### 3. 🔍 Analizador de Impacto de Inicio (Optimización de Arranque)

- **Clasificación Automática**: Se ha añadido la columna **"Impacto"** en la pestaña de Gestión de Inicio de Windows.
- **Heurística inteligente**: Identifica qué aplicaciones ralentizan más el arranque (Impacto Alto en rojo para navegadores, clientes pesados de mensajería/juegos, etc.; Impacto Bajo en verde para drivers de sonido, vídeo o el antivirus nativo).
- **Análisis de peso de binario**: Escanea el tamaño en disco de los ejecutables para refinar el impacto (pesados de >40 MB suben a Alto; pequeños de <1.5 MB bajan a Bajo).

### ⚙️ 4. Desinstalador Avanzado con "Forzar Eliminación"

- **Desinstaladores Rotos**: Si el desinstalador nativo de una aplicación falla o no se encuentra (por ejemplo, al borrarla manualmente), WinCleaner te ofrecerá la opción de **forzar su eliminación**.
- **Limpieza de Registro e Historial**: Elimina directamente la clave de registro del sistema y realiza un escaneo inmediato para depurar archivos residuales huérfanos.

### 🎨 5. Unificación Visual y Tray Icon

- **Icono de la Bandeja de Sistema**: El icono del system tray ahora se extrae directamente del ejecutable para mantener la identidad visual del logotipo de WinCleaner sin depender de iconos genéricos de Windows.
- **Medidas de Seguridad**: Añadido un aviso visual si `winget` no está instalado en el equipo de destino, evitando así bloqueos al buscar actualizaciones.

---

## 💾 Instrucciones de Instalación

1. Descarga el instalador único **`WinCleanerSetup-v1.2.2.exe`** (o su versión comprimida `.zip`) desde esta release.
2. Ejecútalo para instalar o actualizar tu suite de WinCleaner en tu sistema.
