# 🚀 WinCleaner v1.6.0 - Release Notes

¡Estamos orgullosos de presentar la versión **v1.6.0** de WinCleaner! Este lanzamiento introduce el **Motor de Sugerencias Contextuales Inteligentes**, transformando WinCleaner en un asistente proactivo que analiza tu sistema y te sugiere optimizaciones en cada sección.

---

## 🚀 Novedades y Mejoras Principales

### 1. 💡 Motor de Sugerencias Proactivas y Contextuales
Hemos integrado recomendaciones dinámicas directamente dentro de sus menús correspondientes para no saturar la pantalla principal y ofrecer la máxima contextualización:

* **Gestión de Inicio (Arranque)**:
  * El sistema analiza los programas configurados para arrancar automáticamente.
  * Si detecta aplicaciones con impacto de inicio **"Alto"** (como Discord, Spotify, Steam, etc.), muestra un banner superior rojo alertando del retardo y habilita el botón **"Optimizar Inicio"** para desactivarlas en bloque con un solo clic.
* **Energía y Batería (Eficiencia)**:
  * Si estás utilizando un portátil en modo batería (desconectado de la corriente), tu nivel de carga es inferior al 40% y mantienes un perfil de energía de alto consumo, aparecerá un banner de advertencia naranja.
  * Habilita un botón rápido **"Activar Ahorro"** para alternar de forma silenciosa al perfil "Economizador" y prolongar la autonomía útil en hasta 45 minutos.
* **Gestor de Controladores (Estabilidad)**:
  * Presenta un banner de estabilidad si el escaneo local detecta controladores de hardware obsoletos (con más de 3 años de antigüedad), aconsejándote buscar actualizaciones oficiales.

### 2. 🧹 Limpieza Inteligente Avanzada
Desarrollamos dos nuevos módulos de limpieza desacoplados basados en la interfaz `ICleanupModule`:
* **Instaladores Redundantes**: Escanea Descargas y Escritorio buscando instaladores `.exe` y `.msi` de aplicaciones que **ya tienes instaladas** en el equipo (ej: `ChromeSetup.exe` si Google Chrome ya está en el sistema), permitiendo eliminarlos de forma segura.
* **Descargas Olvidadas**: Identifica descargas de gran volumen (más de 100 MB) que no han sido modificadas ni abiertas en los últimos 180 días (6 meses).
* Ambos módulos se ejecutan en segundo plano durante el análisis general y se presentan con casillas de limpieza personalizables.

---

## 🛠️ Detalles del Lanzamiento
* **Commit**: `81b8a85` / Consolidación de sugerencias.
* **Tag**: `v1.6.0`
