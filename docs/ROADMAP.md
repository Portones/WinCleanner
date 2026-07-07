# Plan de Futuras Versiones y Mejoras (Roadmap) - WinCleaner

Este documento recopila las ideas, propuestas y refinamientos planificados para las próximas versiones de WinCleaner, divididos por áreas de impacto.

---

## ✅ Completado (Implementado en v1.1.0)
* **📸 Detección Automática de Capturas de Pantalla Obsoletas**:
  - Identificación de capturas en la carpeta del usuario con antigüedad superior a 30 días.
* **📦 Actualización Desatendida con Winget**:
  - Pestaña de actualizaciones que detecta programas obsoletos y permite su instalación silenciosa en segundo plano.
* **🔔 Automatización y Segundo Plano (SysTray)**:
  - Icono unificado en la bandeja del sistema con menús rápidos para optimización de RAM y lanzamiento ágil.
* **🔒 Seguridad de Elevación**:
  - Ejecución nativa con privilegios elevados (`app.manifest`) para operaciones silenciosas limpias.

---

## 📸 1. Limpieza Inteligente de Imágenes y Multimedia (Próxima v1.2.0)
* **Algoritmo de Similaridad Visual (Detección de Fotos Repetidas)**:
  - Implementar un motor de comparación perceptiva rápida (ej. *Difference Hash - dHash*) para identificar fotos duplicadas o ráfagas similares en lugar de depender únicamente de hash SHA-256 exactos.
* **Filtro de Compresión y Calidad**:
  - Resaltar imágenes de gran tamaño o baja resolución que puedan ser candidatas seguras a borrado.

---

## 🚀 2. Gestión de Aplicaciones y Desinstalación
* **Desinstalador por Lotes (Batch Uninstall)**:
  - Permitir seleccionar múltiples programas Win32 o UWP a la vez y desinstalarlos de forma secuencial sin requerir clics repetidos del usuario.
* **Eliminador de Bloatware de Windows**:
  - Añadir una lista segura de aplicaciones precargadas del sistema (como juegos patrocinados, Cortana, Bing News) que consumen memoria y permitir su desinstalación con un clic.

---

## ⚡ 3. Optimización Avanzada del Sistema, Privacidad y Red
* **Limpieza de Windows Update**:
  - Limpieza segura de los instaladores descargados que quedan huérfanos en `C:\Windows\SoftwareDistribution\Download` una vez completadas las actualizaciones de Windows.
* **Autoselección de DNS más Rápido**:
  - Una vez finalizado el test de latencia DNS, sugerir y permitir aplicar de forma automática el DNS con menor tiempo de respuesta de forma transparente con un botón ("Aplicar el DNS más rápido").
* **Desactivador de Telemetría e Intrusión de Windows**:
  - Panel para deshabilitar servicios de diagnóstico invasivos, rastreadores de publicidad de Windows y el envío automático de informes para ganar privacidad y rendimiento.
* **Optimización de OneDrive**:
  - Desconexión y desactivación segura del inicio automático de OneDrive si el usuario no tiene una cuenta configurada o activa, liberando recursos del sistema.

---

## 🔔 4. Automatización y Monitorización de Hardware
* **Programación de Tareas de Limpieza**:
  - Permitir al usuario programar análisis y limpiezas automáticas semanales o mensuales en segundo plano.
* **Monitorización Activa y Alertas de Sistema**:
  - Notificaciones enriquecidas a través de la bandeja del sistema (SysTray) si:
    - La temperatura del procesador (CPU) supera el umbral de seguridad (ej. 85°C).
    - El espacio de almacenamiento de la unidad C: baja del 5%.
    - El uso de memoria RAM se mantiene por encima del 90% durante más de 3 minutos.
