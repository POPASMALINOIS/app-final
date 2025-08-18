using Microsoft.Win32;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private readonly ImportService _importService = new();
        public ObservableCollection<Operacion> Operaciones { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleccionar archivo",
                Filter = "Archivos Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Todos los archivos (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string filePath = dlg.FileName;
                    DateOnly hoy = DateOnly.FromDateTime(DateTime.Now);
                    string ladoPorDefecto = "LADO 1";

                    // Llama al servicio de importación
                    var nuevos = _importService.Importar(filePath, hoy, ladoPorDefecto);

                    // Refresca la colección SIN reasignar
                    Operaciones.Clear();
                    foreach (var op in nuevos)
                        Operaciones.Add(op);

                    MessageBox.Show($"Se importaron {Operaciones.Count} operaciones.",
                        "Importación completada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
