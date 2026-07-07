# Tareas - Estado del Proyecto (WinCleaner)

## 🏁 Historial de Fases Completadas

### Fase 6: Optimización de Rendimiento e Integración de Sistema
- [x] Configurar `<UseWindowsForms>true</UseWindowsForms>` en `WinCleaner.csproj`
- [x] Crear el modelo de datos de servicios `Models/ServicesModels.cs` y de menús contextuales `Models/ContextMenuModels.cs`
- [x] Implementar el optimizador de RAM `Services/Implementations/RamBoosterService.cs` con P/Invoke
- [x] Implementar el servicio de servicios `Services/Implementations/WindowsServicesService.cs`
- [x] Implementar el limpiador de menú contextual `Services/Implementations/ContextMenuService.cs`
- [x] Integrar el botón "Optimizar RAM" en `ViewModels/DashboardViewModel.cs` y `Views/DashboardView.xaml`
- [x] Crear el ViewModel de servicios `ViewModels/ServicesViewModel.cs` y la vista `Views/ServicesView.xaml`
- [x] Crear el ViewModel de menú contextual `ViewModels/ContextMenuViewModel.cs` y la vista `Views/ContextMenuView.xaml`
- [x] Modificar `ViewModels/MainViewModel.cs` y `Views/MainWindow.xaml` para habilitar la navegación lateral hacia las nuevas vistas
- [x] Modificar `App.xaml.cs` para gestionar el icono en la bandeja del sistema (SysTray) y registrar los nuevos servicios de DI
- [x] Compilar y verificar el funcionamiento de las optimizaciones, servicios, menús contextuales y segundo plano

### Lanzamiento Oficial (v1.1.0 & v1.1.1 Patch)
- [x] Integrar **Actualizador Automático** desatendido con Winget
- [x] Integrar **Limpiador Inteligente de Fotos** (capturas de pantalla obsoletas y duplicados SHA-256) con prevención de bloqueos de archivos en disco
- [x] Estructurar el directorio de recursos en `Assets/` y configurar el logotipo oficial `.ico`
- [x] Resolver la compatibilidad nativa de WPF en Single File compilando a través de un instalador autocontenido con **Inno Setup**
- [x] Crear el script automatizado de publicación `build-release.ps1` con versionado dinámico y empaquetado a `.zip` automático
- [x] Cambiar el icono del System Tray del escudo genérico al logotipo oficial de WinCleaner
- [x] Diseñar reglas automatizadas de versionado SemVer y generación automática de Notas de Versión en `Releases/`
