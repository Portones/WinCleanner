# Tareas - Fase 6: Optimización de Rendimiento e Integración de Sistema

- [ ] Configurar `<UseWindowsForms>true</UseWindowsForms>` en `WinCleaner.csproj`
- [ ] Crear el modelo de datos de servicios `Models/ServicesModels.cs` y de menús contextuales `Models/ContextMenuModels.cs`
- [ ] Implementar el optimizador de RAM `Services/Implementations/RamBoosterService.cs` con P/Invoke
- [ ] Implementar el servicio de servicios `Services/Implementations/WindowsServicesService.cs`
- [ ] Implementar el limpiador de menú contextual `Services/Implementations/ContextMenuService.cs`
- [ ] Integrar el botón "Optimizar RAM" en `ViewModels/DashboardViewModel.cs` y `Views/DashboardView.xaml`
- [ ] Crear el ViewModel de servicios `ViewModels/ServicesViewModel.cs` y la vista `Views/ServicesView.xaml`
- [ ] Crear el ViewModel de menú contextual `ViewModels/ContextMenuViewModel.cs` y la vista `Views/ContextMenuView.xaml`
- [ ] Modificar `ViewModels/MainViewModel.cs` y `Views/MainWindow.xaml` para habilitar la navegación lateral hacia las nuevas vistas
- [ ] Modificar `App.xaml.cs` para gestionar el icono en la bandeja del sistema (SysTray) y registrar los nuevos servicios de DI
- [ ] Compilar y verificar el funcionamiento de las optimizaciones, servicios, menús contextuales y segundo plano
