using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class ContextMenuViewModel : ViewModelBase
    {
        private readonly IContextMenuService _contextMenuService;

        private List<ContextMenuItem> _items = new();
        private bool _isLoading;
        private string _statusMessage = "Listo";

        public List<ContextMenuItem> ContextMenuItems
        {
            get => _items;
            set => SetProperty(ref _items, value);
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

        public ICommand LoadItemsCommand { get; }
        public ICommand ToggleItemCommand { get; }

        public ContextMenuViewModel(IContextMenuService contextMenuService)
        {
            _contextMenuService = contextMenuService ?? throw new ArgumentNullException(nameof(contextMenuService));

            LoadItemsCommand = new AsyncRelayCommand(LoadItemsAsync);
            ToggleItemCommand = new AsyncRelayCommand<ContextMenuItem>(ToggleItemAsync);

            // Cargar elementos al iniciar
            _ = LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Escaneando extensiones del menú contextual...";

            try
            {
                var list = await _contextMenuService.GetContextMenuItemsAsync(CancellationToken.None);
                ContextMenuItems = list;
                StatusMessage = $"Se encontraron {ContextMenuItems.Count} elementos en el menú contextual.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al escanear los menús contextuales.";
                Serilog.Log.Error(ex, "Error en ContextMenuViewModel.LoadItemsAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleItemAsync(ContextMenuItem? item)
        {
            if (item == null) return;

            // Almacenar estado previo en caso de fallo
            bool previousState = !item.IsEnabled;
            string actionText = item.IsEnabled ? "activar" : "desactivar";

            try
            {
                // El Checkbox cambia el valor en la UI (TwoWay) antes de ejecutar el comando, 
                // por lo que item.IsEnabled es el NUEVO estado deseado.
                bool success = await _contextMenuService.ToggleContextMenuItemAsync(item, item.IsEnabled, CancellationToken.None);
                if (success)
                {
                    StatusMessage = $"Manejador '{item.Name}' modificado correctamente.";
                }
                else
                {
                    // Fallback
                    item.IsEnabled = previousState;
                    StatusMessage = $"No se pudo {actionText} '{item.Name}'.";
                }
            }
            catch (Exception ex)
            {
                // Revertir UI
                item.IsEnabled = previousState;
                StatusMessage = $"Error al cambiar estado de '{item.Name}': {ex.Message}";
                MessageBox.Show($"Ocurrió un error al intentar {actionText} el menú de contexto.\nDetalle: {ex.Message}\n\nNota: Modificar el menú contextual del explorador requiere permisos de Administrador.", 
                                "Error de Permisos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
