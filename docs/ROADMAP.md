# Plan de Futuras Versiones y Mejoras (Roadmap) - WinCleaner

Este documento recopila las ideas, propuestas y refinamientos planificados para las próximas versiones de WinCleaner, divididos por áreas de impacto.

---

## 📸 1. Limpieza Inteligente de Imágenes y Multimedia
* **Detección Automática de Capturas de Pantalla Obsoletas**:
  - Identificar capturas en carpetas del sistema (Escritorio, Imágenes/Screenshots) basándose en nombres de archivo estándar ("Screenshot", "Captura de pantalla", "202x-xx-xx_xx") y con una antigüedad superior a 30 días.
* **Algoritmo de Similaridad Visual (Detección de Fotos Repetidas)**:
  - Implementar un motor de comparación perceptiva rápida (ej. *Difference Hash - dHash*) para identificar fotos duplicadas o ráfagas similares en lugar de depender únicamente de hash SHA-256 exactos.
* **Filtro de Compresión y Calidad**:
  - Resaltar imágenes de gran tamaño o baja resolución que puedan ser candidatas seguras a borrado.

## 🚀 2. Gestión de Aplicaciones y Winget
* **Actualización Desatendida con Winget**:
  - Integrar comandos del Gestor de Paquetes de Windows (`winget upgrade`) en segundo plano de forma no intrusiva.
  - Diseñar una pestaña de "Actualizaciones" que escanee versiones instaladas desactualizadas y ofrezca un botón "Actualizar todo en segundo plano".
* **Desinstalador por Lotes (Batch Uninstall)**:
  - Permitir seleccionar múltiples programas Win32 o UWP a la vez y desinstalarlos de forma secuencial sin requerir clics repetidos del usuario.
* **Eliminador de Bloatware de Windows**:
  - Añadir una lista segura de aplicaciones precargadas del sistema (como juegos patrocinados, Cortana, Bing News) que consumen memoria y permitir su desinstalación con un clic.

## ⚡ 3. Optimización Avanzada del Sistema y Red
* **Limpieza de Windows Update**:
  - Limpieza segura de los instaladores descargados que quedan huérfanos en `C:\Windows\SoftwareDistribution\Download` una vez completadas las actualizaciones de Windows.
* **Autoselección de DNS más Rápido**:
  - Una vez finalizado el test de latencia DNS, sugerir y permitir aplicar de forma automática el DNS con menor tiempo de respuesta de forma transparente.
* **Optimización de OneDrive y Telemetría Extendida**:
  - Desconexión y desactivación segura del inicio automático de OneDrive si el usuario no tiene una cuenta configurada o activa, liberando recursos del sistema.

## 🔔 4. Automatización y Segundo Plano (SysTray)
* **Programación de Tareas de Limpieza**:
  - Permitir al usuario programar análisis y limpiezas automáticas semanales o mensuales en segundo plano.
* **Monitorización Activa y Alertas**:
  - Notificaciones enriquecidas a través de la bandeja del sistema (SysTray) si:
    - La temperatura del procesador (CPU) supera el umbral de seguridad (ej. 85°C).
    - El espacio de almacenamiento de la unidad C: baja del 5%.
    - El uso de memoria RAM se mantiene por encima del 90% durante más de 3 minutos.
