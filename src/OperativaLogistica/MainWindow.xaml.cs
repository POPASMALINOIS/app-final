using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _debounceTimer;
        private bool _layoutDirty;
        private ConfigService? _cfg;

        public MainWindow()
        {
            InitializeComponent();

            // 👇 MUY IMPORTANTE: el menú necesita que el Window tenga MainViewModel como DataContext
            if (DataContext is not MainViewModel)
                DataContext = new MainViewModel();

            _cfg = (DataContext as MainViewModel)?.Config;

            // Debounce para capturar y guardar anchos sin saturar
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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
                    if (_cfg != null) ColumnLayoutService.Apply(g, _cfg);

                    g.LayoutUpdated += (_, __2) =>
                    {
                        _layoutDirty = true;
                        if (!_debounceTimer.IsEnabled) _debounceTimer.Start();
                    };
                }
            };
        }

        // ---- Menú: handlers de Ayuda/Salir (los demás son ICommand en el VM) ----
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Operativa Logística offline (.NET 8 WPF)\nLicencia MIT",
                "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Si en XAML mantienes estos eventos, aquí no hace falta añadir lógica.
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid g && _cfg != null)
                ColumnLayoutService.Apply(g, _cfg);
        }
    }
}
