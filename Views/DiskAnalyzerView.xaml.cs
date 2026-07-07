using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinCleaner.ViewModels;

namespace WinCleaner.Views
{
    /// <summary>
    /// Lógica de interacción para DiskAnalyzerView.xaml
    /// </summary>
    public partial class DiskAnalyzerView : UserControl
    {
        private DiskAnalyzerViewModel? _viewModel;

        public DiskAnalyzerView()
        {
            InitializeComponent();
            DataContextChanged += DiskAnalyzerView_DataContextChanged;
            TreemapCanvas.SizeChanged += TreemapCanvas_SizeChanged;
        }

        private void DiskAnalyzerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Desuscribir del viejo ViewModel si existía
            if (_viewModel != null)
            {
                _viewModel.RedrawRequested -= ViewModel_RedrawRequested;
            }

            _viewModel = DataContext as DiskAnalyzerViewModel;

            if (_viewModel != null)
            {
                _viewModel.RedrawRequested += ViewModel_RedrawRequested;
            }
        }

        private void TreemapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawTreemap();
        }

        private void ViewModel_RedrawRequested(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(DrawTreemap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void DrawTreemap()
        {
            if (TreemapCanvas == null) return;

            TreemapCanvas.Children.Clear();

            if (_viewModel == null || _viewModel.CurrentViewNode == null || _viewModel.CurrentViewNode.Children.Count == 0)
                return;

            double width = TreemapCanvas.ActualWidth;
            double height = TreemapCanvas.ActualHeight;

            // Evitar dibujar si el canvas no tiene tamaño medido en pantalla aún
            if (width < 10 || height < 10) return;

            // Calcular las posiciones de los rectángulos del treemap
            _viewModel.RecalculateLayout(width, height);

            foreach (var child in _viewModel.CurrentViewNode.Children)
            {
                // Omitir bloques demasiado pequeños para evitar sobrecarga visual
                if (child.Bounds.Width < 2 || child.Bounds.Height < 2) 
                    continue;

                // Restar 1px de margen en bordes para crear espaciado visual
                double itemW = Math.Max(0.5, child.Bounds.Width - 1);
                double itemH = Math.Max(0.5, child.Bounds.Height - 1);

                var border = new Border
                {
                    Width = itemW,
                    Height = itemH,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(child.ColorHex)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Contorno oscuro
                    BorderThickness = new Thickness(0.5),
                    CornerRadius = new CornerRadius(3),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{child.Name}\nTamaño: {child.SizeText}\nTipo: {(child.IsFolder ? "Carpeta" : "Archivo")}"
                };

                // Si es carpeta, asignar color índigo/azul vibrante
                if (child.IsFolder)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)); // Indigo
                }

                // Escribir nombre del archivo y tamaño si el rectángulo es suficientemente grande
                if (itemW > 55 && itemH > 35)
                {
                    var tbName = new TextBlock
                    {
                        Text = child.Name,
                        FontSize = 9.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(5, 4, 5, 0),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };

                    var tbSize = new TextBlock
                    {
                        Text = child.SizeText,
                        FontSize = 8.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Gris slate claro
                        Margin = new Thickness(5, 0, 5, 4),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };

                    var grid = new Grid();
                    grid.Children.Add(tbName);
                    grid.Children.Add(tbSize);
                    border.Child = grid;
                }

                // Posicionar en el Canvas
                Canvas.SetLeft(border, child.Bounds.Left);
                Canvas.SetTop(border, child.Bounds.Top);

                // Evento de doble clic para profundizar (Drill-Down)
                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        if (child.IsFolder && child.Children.Count > 0)
                        {
                            _viewModel.DrillDownCommand.Execute(child);
                        }
                    }
                };

                TreemapCanvas.Children.Add(border);
            }
        }
    }
}
