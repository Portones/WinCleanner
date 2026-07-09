# Reglas de Juego para el Desarrollo de WinCleaner

Estas reglas guían el comportamiento y las prioridades de los agentes de IA en este espacio de trabajo.

## Reglas y Directrices

1. **Actualización Constante de Documentación**: Siempre que realices cambios significativos, incorpores nuevas funciones o refactorices componentes, actualiza los archivos de documentación (como `README.md` o guías de uso) de manera proactiva.
2. **Respeto a la Estructura del Código**: Mantén siempre la arquitectura limpia definida en el proyecto (MVVM en WPF: Views, ViewModels, Models, Services, Helpers) respetando la modularidad, los namespaces y el patrón de inyección de dependencias.
3. **Commits Frecuentes y Probados**: Realiza confirmaciones (commits) de Git cada vez que completes cambios utilizables, estables y que hayan sido compilados/probados exitosamente con `dotnet build`.
4. **Desacoplamiento de Commits y Releases (Metodología de Hito de Versión)**:
   * **Commits frecuentes (Trabajo diario)**: Realiza commits libres de Git cada vez que completes un cambio funcional o corrección estable y probado con `dotnet build`. No modifiques el número de versión ni generes notas de lanzamiento para estos commits intermedios de desarrollo.
   * **Lanzamientos (Hito de Versión)**: Incrementa la versión del proyecto en `MainWindow.xaml` y `README.md` únicamente cuando se decida consolidar un hito de lanzamiento estable (Release). Solo en este momento se creará el tag de Git correspondiente (`git tag vX.X.X`) y se generará el archivo consolidado de notas de lanzamiento en `Releases/ReleaseNotes-vX.X.X.md` resumiendo todos los commits agrupados.

