using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class StartupViewModel : ViewModelBase
    {
        private readonly IStartupManagerService _startupManager;
        private readonly IBootAnalyzerService _bootAnalyzer;
        private bool _isLoading;
        private List<StartupApp> _startupApps = new();
        private string _statusMessage = "Listo para escanear programas de inicio.";
        private BootInfo? _bootInfo;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<StartupApp> StartupApps
        {
            get => _startupApps;
            set => SetProperty(ref _startupApps, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public BootInfo? BootInfo
        {
            get => _bootInfo;
            set => SetProperty(ref _bootInfo, value);
        }

        public string LastBootDurationText => BootInfo != null ? $"{BootInfo.LastBootDurationSeconds:F1}s" : "Cargando...";
        public string LastBootTimeText => BootInfo != null ? BootInfo.LastBootDateTime.ToString("dd/MM/yyyy HH:mm") : "Cargando...";
        public string UptimeText => BootInfo != null ? BootInfo.UptimeText : "Cargando...";
        public List<BootHistoryItem> BootHistory => BootInfo?.BootHistory ?? new List<BootHistoryItem>();

        public string BootStatusText => BootInfo == null ? "Analizando..." : 
                                        BootInfo.LastBootDurationSeconds < 20 ? "Arranque Rápido (Optimizado)" : 
                                        BootInfo.LastBootDurationSeconds < 35 ? "Arranque Moderado" : 
                                        "Arranque Lento (Requiere Optimización)";
        
        public string BootStatusColor => BootInfo == null ? "#94A3B8" : 
                                         BootInfo.LastBootDurationSeconds < 20 ? "#10B981" : 
                                         BootInfo.LastBootDurationSeconds < 35 ? "#F59E0B" : 
                                         "#EF4444";

        public ICommand LoadAppsCommand { get; }
        public ICommand ToggleAppCommand { get; }

        public StartupViewModel(IStartupManagerService startupManager, IBootAnalyzerService bootAnalyzer)
        {
            _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
            _bootAnalyzer = bootAnalyzer ?? throw new ArgumentNullException(nameof(bootAnalyzer));

            LoadAppsCommand = new AsyncRelayCommand(LoadAppsAsync);
            ToggleAppCommand = new AsyncRelayCommand<StartupApp>(ToggleAppAsync);

            // Carga automática inicial
            _ = LoadAppsAsync();
        }

        private async Task LoadAppsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Cargando programas de inicio automático...";
            StartupApps = new List<StartupApp>();

            try
            {
                var token = CancellationToken.None;
                
                // Cargar métricas de arranque
                BootInfo = await _bootAnalyzer.GetBootInfoAsync();
                OnPropertyChanged(nameof(LastBootDurationText));
                OnPropertyChanged(nameof(LastBootTimeText));
                OnPropertyChanged(nameof(UptimeText));
                OnPropertyChanged(nameof(BootHistory));
                OnPropertyChanged(nameof(BootStatusText));
                OnPropertyChanged(nameof(BootStatusColor));

                var apps = await _startupManager.GetStartupAppsAsync(token);
                StartupApps = apps.OrderBy(x => x.Name).ToList();
                StatusMessage = $"Se encontraron {StartupApps.Count} aplicaciones en el inicio.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al cargar los programas de inicio.";
                Log.Error(ex, "Error al cargar programas de inicio en StartupViewModel.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleAppAsync(StartupApp? app)
        {
            if (app == null) return;

            bool targetState = !app.IsEnabled;
            string actionText = targetState ? "activar" : "desactivar";

            try
            {
                var token = CancellationToken.None;
                bool success = await _startupManager.ToggleStartupAppAsync(app, targetState, token);
                
                if (success)
                {
                    app.IsEnabled = targetState;
                    StatusMessage = $"Aplicación '{app.Name}' {actionText}da correctamente.";
                }
                else
                {
                    MessageBox.Show($"No se pudo {actionText} la aplicación '{app.Name}'.", 
                                    "Error al Modificar Inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"No tiene permisos suficientes para {actionText} la aplicación '{app.Name}'.\n\nPor favor, ejecute WinCleaner como Administrador para modificar aplicaciones del Registro de Máquina (HKLM) o carpetas de inicio comunes.", 
                                "Acceso Denegado (Requiere Administrador)", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al intentar {actionText} la aplicación '{app.Name}':\n{ex.Message}", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error al conmutar inicio de {Name}.", app.Name);
            }
        }
    }
}
