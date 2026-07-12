# 🚀 WinCleaner v1.1.2 - Release Notes

Esta versión añade una característica avanzada de desinstalación de aplicaciones ("Forzar eliminación") para resolver entradas huérfanas o rotas en la lista de programas instalados de Windows, además de mejoras en la empaquetación automatizada.

---

## 🛠️ Novedades Clave

### 1. ⚙️ Desinstalación Inteligente con "Forzar Eliminación"
* **Solución a desinstaladores rotos**: Cuando intentas desinstalar una aplicación cuyo archivo desinstalador nativo ha sido eliminado o falla (dando error en Windows), WinCleaner te ofrecerá la opción de **forzar su eliminación**.
* **Limpieza de Registro e Historial**: Elimina de forma directa y segura la clave de registro del listado de programas de Windows (`Uninstall` key).
* **Búsqueda de Residuos Huérfanos**: Tras quitar la entrada rota, realiza un análisis del disco (`AppData`, `Program Files`, etc.) buscando archivos residuales de la aplicación eliminada para eliminarlos también.

### 2. 📦 Mejoras en el Script de Compilación (`build-release.ps1`)
* Compresión a `.zip` automatizada del instalador compilado por Inno Setup.
* Limpieza previa de archivos para evitar duplicados en el empaquetado.

---

## 💾 Instrucciones de Instalación
1. Descarga el archivo de instalación único **`WinCleanerSetup-v1.1.2.exe`** (o su versión comprimida `.zip`) desde esta release.
2. Ejecútalo para instalar o actualizar tu versión actual del optimizador en tu sistema.
