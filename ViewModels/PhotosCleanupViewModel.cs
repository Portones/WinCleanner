using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class PhotosCleanupViewModel : ViewModelBase
    {
        private readonly IPhotosCleanupService _photosService;

        private List<PhotoItem> _screenshots = new();
        private List<DuplicatePhotoGroup> _duplicateGroups = new();
        private string _scanPath = string.Empty;
        private int _screenshotAgeLimit = 30; // 30 días por defecto
        private bool _isLoading;
        private string _statusMessage = "Listo. Seleccione una pestaña e inicie el escaneo.";

        public List<PhotoItem> Screenshots
        {
            get => _screenshots;
            set => SetProperty(ref _screenshots, value);
        }

        public List<DuplicatePhotoGroup> DuplicateGroups
        {
            get => _duplicateGroups;
            set => SetProperty(ref _duplicateGroups, value);
        }

        public string ScanPath
        {
            get => _scanPath;
            set => SetProperty(ref _scanPath, value);
        }

        public int ScreenshotAgeLimit
        {
            get => _screenshotAgeLimit;
            set => SetProperty(ref _screenshotAgeLimit, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand ScanScreenshotsCommand { get; }
        public ICommand ScanDuplicatesCommand { get; }
        public ICommand DeleteSelectedScreenshotsCommand { get; }
        public ICommand DeleteSelectedDuplicatesCommand { get; }
        public ICommand SelectFolderCommand { get; }
        public ICommand KeepNewestInGroupsCommand { get; }
        public ICommand KeepOldestInGroupsCommand { get; }
        public ICommand OpenPhotoCommand { get; }

        public PhotosCleanupViewModel(IPhotosCleanupService photosService)
        {
            _photosService = photosService ?? throw new ArgumentNullException(nameof(photosService));

            ScanScreenshotsCommand = new AsyncRelayCommand(ScanScreenshotsAsync);
            ScanDuplicatesCommand = new AsyncRelayCommand(ScanDuplicatesAsync);
            DeleteSelectedScreenshotsCommand = new AsyncRelayCommand(DeleteSelectedScreenshotsAsync);
            DeleteSelectedDuplicatesCommand = new AsyncRelayCommand(DeleteSelectedDuplicatesAsync);
            SelectFolderCommand = new RelayCommand(SelectFolder);
            KeepNewestInGroupsCommand = new RelayCommand(KeepNewestInGroups);
            KeepOldestInGroupsCommand = new RelayCommand(KeepOldestInGroups);
            OpenPhotoCommand = new RelayCommand<string>(OpenPhoto);

            // Inicializar ruta de escaneo de duplicados a Imágenes del usuario
            ScanPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        private void SelectFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta para buscar fotos duplicadas",
                InitialDirectory = Directory.Exists(ScanPath) ? ScanPath : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog() == true)
            {
                ScanPath = dialog.FolderName;
            }
        }

        private async Task ScanScreenshotsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = $"Buscando capturas de pantalla obsoletas (> {ScreenshotAgeLimit} días)...";
            Screenshots = new List<PhotoItem>();

            try
            {
                var list = await _photosService.GetObsoleteScreenshotsAsync(ScreenshotAgeLimit, CancellationToken.None);
                Screenshots = list;

                if (Screenshots.Count == 0)
                {
                    StatusMessage = "No se encontraron capturas de pantalla antiguas que borrar.";
                }
                else
                {
                    long totalSize = Screenshots.Sum(x => x.Size);
                    string sizeText = FormatSize(totalSize);
                    StatusMessage = $"Se detectaron {Screenshots.Count} capturas obsoletas (Total: {sizeText}).";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al buscar capturas: {ex.Message}";
                Log.Error(ex, "Error en PhotosCleanupViewModel.ScanScreenshotsAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ScanDuplicatesAsync()
        {
            if (IsLoading) return;
            if (string.IsNullOrWhiteSpace(ScanPath) || !Directory.Exists(ScanPath))
            {
                MessageBox.Show("Seleccione una ruta de escaneo válida.", "Directorio no encontrado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsLoading = true;
            StatusMessage = "Buscando imágenes duplicadas por firma digital SHA-256...";
            DuplicateGroups = new List<DuplicatePhotoGroup>();

            try
            {
                var list = await _photosService.GetDuplicatePhotosAsync(ScanPath, CancellationToken.None);
                DuplicateGroups = list;

                if (DuplicateGroups.Count == 0)
                {
                    StatusMessage = "No se encontraron imágenes duplicadas en la carpeta seleccionada.";
                }
                else
                {
                    int totalFiles = DuplicateGroups.Sum(x => x.Photos.Count);
                    int totalGroups = DuplicateGroups.Count;
                    StatusMessage = $"Se encontraron {totalGroups} grupos de imágenes duplicadas ({totalFiles} archivos en total).";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al buscar fotos duplicadas: {ex.Message}";
                Log.Error(ex, "Error en PhotosCleanupViewModel.ScanDuplicatesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedScreenshotsAsync()
        {
            var selected = Screenshots.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una captura para eliminar.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"¿Está seguro de que desea eliminar permanentemente las {selected.Count} capturas de pantalla seleccionadas del disco?", 
                                          "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            int deletedCount = 0;

            try
            {
                foreach (var item in selected)
                {
                    bool ok = await _photosService.DeletePhotoAsync(item.Path);
                    if (ok) deletedCount++;
                }

                MessageBox.Show($"Se eliminaron correctamente {deletedCount} capturas de pantalla.", "Limpieza Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                IsLoading = false;
                await ScanScreenshotsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al eliminar capturas: {ex.Message}";
                Log.Error(ex, "Error en PhotosCleanupViewModel.DeleteSelectedScreenshotsAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedDuplicatesAsync()
        {
            // Obtener todas las fotos marcadas en todos los grupos
            var selected = DuplicateGroups.SelectMany(g => g.Photos).Where(p => p.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una foto repetida para eliminar.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Advertir si el usuario seleccionó TODOS los elementos de algún grupo
            foreach (var g in DuplicateGroups)
            {
                if (g.Photos.All(p => p.IsSelected))
                {
                    var warning = MessageBox.Show($"¡Atención! Has seleccionado TODAS las copias en un grupo de duplicados (Archivo: {g.Photos.First().Name}).\nSi continúas, perderás todas las copias de esta imagen.\n\n¿Estás seguro de que quieres continuar?", 
                                                  "Advertencia de Pérdida de Datos", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (warning != MessageBoxResult.Yes) return;
                    break;
                }
            }

            var confirm = MessageBox.Show($"¿Desea eliminar permanentemente las {selected.Count} imágenes duplicadas seleccionadas?", 
                                          "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            int deletedCount = 0;

            try
            {
                foreach (var item in selected)
                {
                    bool ok = await _photosService.DeletePhotoAsync(item.Path);
                    if (ok) deletedCount++;
                }

                MessageBox.Show($"Se eliminaron correctamente {deletedCount} imágenes duplicadas.", "Limpieza Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                IsLoading = false;
                await ScanDuplicatesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al eliminar duplicados: {ex.Message}";
                Log.Error(ex, "Error en PhotosCleanupViewModel.DeleteSelectedDuplicatesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void KeepNewestInGroups()
        {
            // Conservar la más nueva = Seleccionar todas las copias menos la de fecha más reciente en cada grupo
            foreach (var g in DuplicateGroups)
            {
                if (g.Photos.Count < 2) continue;

                var sorted = g.Photos.OrderByDescending(p => p.DateCreated).ToList();
                // sorted[0] es la más nueva (no seleccionada para no borrarla)
                sorted[0].IsSelected = false;

                // Las demás son seleccionadas para ser eliminadas
                for (int i = 1; i < sorted.Count; i++)
                {
                    sorted[i].IsSelected = true;
                }
            }
            TriggerGroupsUpdate();
        }

        private void KeepOldestInGroups()
        {
            // Conservar la más antigua = Seleccionar todas las copias menos la de fecha más vieja en cada grupo
            foreach (var g in DuplicateGroups)
            {
                if (g.Photos.Count < 2) continue;

                var sorted = g.Photos.OrderBy(p => p.DateCreated).ToList();
                // sorted[0] es la más antigua (no seleccionada para no borrarla)
                sorted[0].IsSelected = false;

                // Las demás son seleccionadas para ser eliminadas
                for (int i = 1; i < sorted.Count; i++)
                {
                    sorted[i].IsSelected = true;
                }
            }
            TriggerGroupsUpdate();
        }

        private void TriggerGroupsUpdate()
        {
            var temp = DuplicateGroups;
            DuplicateGroups = null!;
            DuplicateGroups = temp;
        }

        private void OpenPhoto(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "No se pudo abrir la imagen {Path}", filePath);
            }
        }

        private static string FormatSize(long size)
        {
            if (size >= 1024 * 1024 * 1024)
                return $"{(double)size / (1024 * 1024 * 1024):F2} GB";
            if (size >= 1024 * 1024)
                return $"{(double)size / (1024 * 1024):F2} MB";
            if (size >= 1024)
                return $"{(double)size / 1024:F2} KB";
            return $"{size} B";
        }
    }
}
