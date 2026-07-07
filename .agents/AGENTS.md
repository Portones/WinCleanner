# Reglas de Juego para el Desarrollo de WinCleaner

Estas reglas guían el comportamiento y las prioridades de los agentes de IA en este espacio de trabajo.

## Reglas y Directrices

1. **Actualización Constante de Documentación**: Siempre que realices cambios significativos, incorpores nuevas funciones o refactorices componentes, actualiza los archivos de documentación (como `README.md` o guías de uso) de manera proactiva.
2. **Respeto a la Estructura del Código**: Mantén siempre la arquitectura limpia definida en el proyecto (MVVM en WPF: Views, ViewModels, Models, Services, Helpers) respetando la modularidad, los namespaces y el patrón de inyección de dependencias.
3. **Commits Frecuentes y Probados**: Realiza confirmaciones (commits) de Git cada vez que completes cambios utilizables, estables y que hayan sido compilados/probados exitosamente con `dotnet build`.
4. **Versionado Adaptativo**: Incrementa o ajusta la versión del proyecto (ej. en la interfaz o en `README.md`) acorde al impacto de las modificaciones añadidas.
5. **Incremento de Versión Proactivo**: En cada cambio funcional o de corrección de errores (bugs), incrementa la versión en `MainWindow.xaml` y `README.md` (según los criterios de SemVer: Patch para correcciones, Minor para características) de forma que el proyecto quede preparado para que el usuario compile la release en cualquier momento.
