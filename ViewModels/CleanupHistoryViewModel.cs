using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class CleanupHistoryViewModel : ViewModelBase
    {
        private readonly IConfigurationService _configService;

        private ObservableCollection<CleanupHistoryItem> _historyItems = new();
        private ObservableCollection<HistoryBarItem> _bars = new();

        public ObservableCollection<CleanupHistoryItem> HistoryItems
        {
            get => _historyItems;
            set => SetProperty(ref _historyItems, value);
        }

        public ObservableCollection<HistoryBarItem> Bars
        {
            get => _bars;
            set => SetProperty(ref _bars, value);
        }

        public long TotalCleanedBytes => HistoryItems.Sum(h => h.BytesCleaned);
        public string TotalCleanedSizeText => CleanableItem.FormatSize(TotalCleanedBytes);
        public int TotalOperationsCount => HistoryItems.Count;

        public string AverageCleanedSizeText
        {
            get
            {
                if (TotalOperationsCount == 0) return "0 Bytes";
                return CleanableItem.FormatSize(TotalCleanedBytes / TotalOperationsCount);
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public CleanupHistoryViewModel(IConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            RefreshCommand = new RelayCommand(LoadHistory);
            ClearHistoryCommand = new RelayCommand(ClearHistory);

            LoadHistory();
        }

        public void LoadHistory()
        {
            var rawList = _configService.CurrentSettings.CleanupHistory ?? new List<CleanupHistoryItem>();
            HistoryItems = new ObservableCollection<CleanupHistoryItem>(rawList.OrderByDescending(h => h.DateTime));

            OnPropertyChanged(nameof(TotalCleanedBytes));
            OnPropertyChanged(nameof(TotalCleanedSizeText));
            OnPropertyChanged(nameof(TotalOperationsCount));
            OnPropertyChanged(nameof(AverageCleanedSizeText));

            GenerateChartBars(rawList);
        }

        private void GenerateChartBars(List<CleanupHistoryItem> history)
        {
            var barsList = new List<HistoryBarItem>();
            var today = DateTime.Today;

            // Tomar los últimos 7 días
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                long bytesOnDay = history
                    .Where(h => h.DateTime.Date == targetDate)
                    .Sum(h => h.BytesCleaned);

                barsList.Add(new HistoryBarItem
                {
                    DateLabel = targetDate.ToString("dd MMM"),
                    BytesCleaned = bytesOnDay
                });
            }

            long maxBytes = barsList.Max(b => b.BytesCleaned);
            if (maxBytes <= 0) maxBytes = 1;

            foreach (var bar in barsList)
            {
                // Mínimo de 10px para visualización de barra vacía, máximo 130px
                double height = 10 + ((double)bar.BytesCleaned / maxBytes * 120);
                bar.HeightValue = Math.Max(10, Math.Min(130, height));
            }

            Bars = new ObservableCollection<HistoryBarItem>(barsList);
        }

        private void ClearHistory()
        {
            var result = MessageBox.Show(
                "¿Estás seguro de que deseas borrar todo el historial de limpiezas guardado?",
                "Confirmar Borrado de Historial",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _configService.CurrentSettings.CleanupHistory.Clear();
                _configService.SaveSettings();
                LoadHistory();
            }
        }
    }
}
