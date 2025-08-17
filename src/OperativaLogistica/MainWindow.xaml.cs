using System.Windows;
using System.Windows.Controls;
using OperativaLogistica.Services;      // ⬅️ Necesario para ConfigService/ColumnLayoutService
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private ConfigService? _cfg;

        public MainWindow()
        {
            InitializeComponent();

            _cfg = (DataContext as MainViewModel)?.Config;

            Loaded += (_, __) =>
            {
                if (FindName("GridOps") is DataGrid grid && _cfg != null)
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

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid grid && _cfg != null)
            {
                ColumnLayoutService.Apply(grid, _cfg);
                grid.ColumnWidthChanged -= Grid_ColumnWidthChanged;
                grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Hueco para futuras reglas de autogeneración
        }

        private void Grid_ColumnWidthChanged(object? sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid grid && _cfg != null)
                ColumnLayoutService.Capture(grid, _cfg);
        }
    }
}
