using System.Windows;
using System.Windows.Controls;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private ConfigService? _cfg;

        public MainWindow()
        {
            InitializeComponent();

            // Cargar configuración (anchos de columnas)
            _cfg = (DataContext as MainViewModel)?.Config;

            Loaded += (_, __) =>
            {
                // Aplica layout si existe el DataGrid del Tab activo
                var grid = FindName("GridOps") as DataGrid;
                if (grid != null && _cfg != null)
                    ColumnLayoutService.Apply(grid, _cfg);
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

        // Se llama desde XAML (DataGrid.LoadingRow)
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid grid && _cfg != null)
            {
                ColumnLayoutService.Apply(grid, _cfg);
                grid.ColumnWidthChanged -= Grid_ColumnWidthChanged;
                grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
            }
        }

        // Se llama desde XAML (DataGrid.AutoGeneratingColumn) si cambias auto-gen
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Intencionadamente vacío; mantener para futuras reglas de autogeneración
        }

        private void Grid_ColumnWidthChanged(object? sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid grid && _cfg != null)
                ColumnLayoutService.Capture(grid, _cfg);
        }
    }
}
