# Notas de Lanzamiento - WinCleaner v1.3.0

Esta versión presenta grandes novedades orientadas a la optimización, privacidad del sistema operativo y un control mucho más flexible sobre los programas del equipo, agrupando todos los cambios y mejoras desde la versión **v1.2.2**.

## 🚀 Nuevas Características

1. **📦 Desinstalador por Lotes (Batch Uninstall)**
   - Ahora es posible seleccionar múltiples aplicaciones estándar instaladas mediante casillas de verificación para desinstalarlas secuencialmente en segundo plano.
   - Cuenta con flujos mejorados de actualización de la UI al terminar las tareas de eliminación.

2. **🗑️ Eliminador de Bloatware de Windows**
   - Integración de una pestaña exclusiva para detectar y remover aplicaciones preinstaladas de Microsoft (Cortana, Xbox Game Bar, Solitario, Mapas, etc.) de forma masiva y silenciosa.

3. **🛡️ Optimizador de Privacidad y Telemetría (Privacy Tweaker)**
   - Incorporación de nuevos controles (switches) para desactivar el ID de publicidad de anuncios personalizados, el asistente Cortana y las experiencias compartidas (Rome SDK), complementando la desactivación de telemetría general y reportes de error.

4. **⚡ Filtrado Dinámico por Categorías en Limpieza Avanzada (de v1.2.3)**
   - El panel de limpieza ahora permite filtrar elementos detectados por categorías específicas (Archivos Temporales, Caché, Papelera, etc.), facilitando la limpieza selectiva.
   - Carga de categorías generada de forma 100% adaptativa según el análisis en tiempo real.

## 🎨 Mejoras de Interfaz y Estilo (de v1.2.3)

- **Combos Oscuros**: Se resolvieron problemas de legibilidad con dropdowns oscuros compatibles con temas del sistema, evitando el fondo blanco brillante predeterminado de Windows.
- **Diseño Segmentado**: Estilo visual homogéneo premium con el diseño oscuro segmentado del TabControl de WinUI 3 en las pestañas del desinstalador.

## 🛠️ Correcciones

- Corrección en el refresco de las listas de desinstalación de aplicaciones al completarse las desinstalaciones múltiples.
