using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;          // DispatcherTimer
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private ConfigService? _cfg;

        // Debounce para no guardar anchos en cada repaint
        private readonly DispatcherTimer _debounceTimer;
        private bool _layoutDirty;

        public MainWindow()
        {
            InitializeComponent();

            _cfg = (DataContext as MainViewModel)?.Config;

            // Timer para capturar anchos tras cambios de layout
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _debounceTimer.Tick += (_, __) =>
            {
                if (!_layoutDirty) return;
                _layoutDirty = false;

                if (FindName("GridOps") is DataGrid g && _cfg != null)
                    ColumnLayoutService.Capture(g, _cfg);
            };

            Loaded += (_, __) =>
            {
                if (FindName("GridOps") is DataGrid g)
                {
                    // Aplica layout guardado al abrir
                    if (_cfg != null) ColumnLayoutService.Apply(g, _cfg);

                    // Marca “dirty” cuando el layout cambie (p.ej. redimensionar columnas)
                    g.LayoutUpdated += (_, __2) =>
                    {
                        _layoutDirty = true;
                        if (!_debounceTimer.IsEnabled)
                            _debounceTimer.Start();
                    };
                }
            };
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Today_Click(object sender, RoutedEventArgs e) { }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Operativa Logística offline (.NET 8 WPF)\nLicencia MIT",
                "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Sigue existiendo en XAML por si autogenerases columnas en el futuro.
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Reservado para reglas futuras de autogeneración
        }

        // También lo llama el XAML; aquí sólo aplicamos layout la primera vez.
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid g && _cfg != null)
                ColumnLayoutService.Apply(g, _cfg);
        }
    }
}
