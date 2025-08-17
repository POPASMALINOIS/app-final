
using System.Windows;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.Fecha = System.DateOnly.FromDateTime(System.DateTime.Now);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Operativa Logística\nOffline, .NET 8 WPF\nLicencia MIT", "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
