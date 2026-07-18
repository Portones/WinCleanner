# 🚀 WinCleaner v1.12.0 - Release Notes

**Fecha de Lanzamiento**: 19 de Julio de 2026  
**Versión**: v1.12.0  
**Licencia**: MIT  

---

## 🚀 Novedades y Características Principales

### 📊 1. Historial Gráfico de Limpieza (Cleanup History Graph)
* **Gráfica de Barras WPF Nativa**: Añadido un nuevo panel visual interactivo con un gráfico de barras desarrollado nativamente en WPF, libre de dependencias de terceros, para visualizar el volumen de espacio recuperado día a día.
* **Estadísticas Históricas**: Muestra métricas detalladas del total histórico liberado por la aplicación, número de tareas ejecutadas y el promedio de ahorro por operación.

### 💾 2. Optimizador de SSD (TRIM Manual)
* **Detección por WMI**: El optimizador de SSD identifica si la unidad conectada es una unidad de estado sólido (SSD) o un disco duro tradicional (HDD) mediante consultas WMI de bajo nivel.
* **Re-TRIM Asíncrono**: Permite ejecutar de forma segura la instrucción `Optimize-Volume -ReTrim` de Windows en segundo plano, maximizando la velocidad y vida útil del almacenamiento SSD sin congelar la aplicación.

### 📚 3. Manual de Usuario y Documentación
* **Manual Integrado**: Se ha reestructurado el manual en [README.md](file:///c:/Users/Rodrigo%20Portones/Programacion/WinCleanner/README.md) detallando el funcionamiento de todas las 19 herramientas de diagnóstico, limpieza profunda y optimización del sistema añadidas en estas últimas versiones.

---

## 📥 Archivos de Descarga

| Archivo | Descripción |
| :--- | :--- |
| **`WinCleanerSetup-v1.12.0.exe`** | Instalador ejecutable único autocontenido (.NET 9 x64). |
| **`WinCleanerSetup-v1.12.0.zip`** | Paquete comprimido portable del instalador. |
