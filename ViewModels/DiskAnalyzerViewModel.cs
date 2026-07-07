using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class DiskAnalyzerViewModel : ViewModelBase
    {
        private readonly IDiskAnalyzerService _analyzerService;

        private string _rootPath = string.Empty;
        private DiskNode? _rootNode;
        private DiskNode? _currentViewNode;
        private bool _isLoading;
        private double _scanProgress;
        private string _statusMessage = "Listo. Seleccione una carpeta para iniciar el análisis.";
        private Stack<DiskNode> _history = new();

        public string RootPath
        {
            get => _rootPath;
            set => SetProperty(ref _rootPath, value);
        }

        public DiskNode? RootNode
        {
            get => _rootNode;
            set => SetProperty(ref _rootNode, value);
        }

        public DiskNode? CurrentViewNode
        {
            get => _currentViewNode;
            set
            {
                if (SetProperty(ref _currentViewNode, value))
                {
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(NavigationPath));
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(NoData));
                    TriggerRedraw();
                }
            }
        }

        public bool HasData => CurrentViewNode != null;
        public bool NoData => CurrentViewNode == null;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public double ScanProgress
        {
            get => _scanProgress;
            set => SetProperty(ref _scanProgress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool CanGoBack => _history.Count > 0;

        public string NavigationPath
        {
            get
            {
                if (CurrentViewNode == null) return "Ninguno";
                return CurrentViewNode.Path;
            }
        }

        public ICommand SelectFolderCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand DrillDownCommand { get; }
        public ICommand GoBackCommand { get; }

        // Evento que la Vista escuchará para redibujar el Canvas
        public event EventHandler? RedrawRequested;

        public DiskAnalyzerViewModel(IDiskAnalyzerService analyzerService)
        {
            _analyzerService = analyzerService ?? throw new ArgumentNullException(nameof(analyzerService));

            SelectFolderCommand = new RelayCommand(SelectFolder);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync);
            DrillDownCommand = new RelayCommand<DiskNode>(DrillDown);
            GoBackCommand = new RelayCommand(GoBack);

            // Ruta inicial predeterminada (perfil de usuario)
            RootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void SelectFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta o unidad para analizar",
                InitialDirectory = Directory.Exists(RootPath) ? RootPath : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            if (dialog.ShowDialog() == true)
            {
                RootPath = dialog.FolderName;
            }
        }

        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath))
            {
                MessageBox.Show("Seleccione una carpeta de origen válida.", "Directorio no encontrado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsLoading = true;
            ScanProgress = 0;
            StatusMessage = "Analizando archivos y calculando estructura...";
            RootNode = null;
            CurrentViewNode = null;
            _history.Clear();

            try
            {
                var progress = new Progress<double>(p => ScanProgress = p);
                var token = CancellationToken.None;

                var resultNode = await _analyzerService.AnalyzeDirectoryAsync(RootPath, progress, token);
                
                RootNode = resultNode;
                CurrentViewNode = resultNode;
                
                StatusMessage = $"Análisis completado. Tamaño total: {RootNode.SizeText}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error durante el análisis: {ex.Message}";
                Log.Error(ex, "Error en DiskAnalyzerViewModel.AnalyzeAsync");
                MessageBox.Show($"Ocurrió un error al analizar la carpeta:\n{ex.Message}", "Error de Análisis", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void DrillDown(DiskNode? node)
        {
            if (node == null || !node.IsFolder || node.Children.Count == 0) return;

            if (CurrentViewNode != null)
            {
                _history.Push(CurrentViewNode);
            }
            CurrentViewNode = node;
        }

        private void GoBack()
        {
            if (_history.Count > 0)
            {
                CurrentViewNode = _history.Pop();
            }
        }

        public void RecalculateLayout(double width, double height)
        {
            if (CurrentViewNode != null)
            {
                _analyzerService.CalculateTreemapLayout(CurrentViewNode, new Rect(0, 0, width, height));
            }
        }

        public void TriggerRedraw()
        {
            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
