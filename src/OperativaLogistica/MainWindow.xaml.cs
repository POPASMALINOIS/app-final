using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

using OperativaLogistica.Models;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        // VM raíz y servicios
        private readonly MainViewModel _vm;
        private readonly ImportService _importService = new ImportService();
        private readonly DatabaseService _databaseService = new DatabaseService();

        public MainWindow()
        {
            InitializeComponent();

            // Usa el VM ya asignado en XAML, o crea uno.
            _vm = DataContext as MainViewModel ?? new MainViewModel();
            DataContext = _vm;

            // Asegura una pestaña inicial
            if (_vm.SesionActual is null)
                _vm.NuevaPestana();
        }

        // ===================== MENÚ / BOTONES =====================

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            _vm.NuevaPestana();
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            _vm.CerrarPestana();
        }

        /// <summary>
        /// IMPORTAR (Excel/CSV). No reemplaza la colección: limpia y añade para que el DataGrid refresque.
        /// </summary>
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title  = "Importar operativa (Excel/CSV)",
                    Filter = "Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Todos (*.*)|*.*"
                };
                if (dlg.ShowDialog() != true) return;
        
                // Asegura que haya pestaña activa
                if (_vm?.SesionActual is null)
                    _vm?.NuevaPestana();
        
                // Fecha y lado desde el VM (con fallback por si acaso)
                var fecha = DateOnly.FromDateTime(_vm?.FechaActual ?? DateTime.Today);
                var lado  = string.IsNullOrWhiteSpace(_vm?.LadoSeleccionado) ? "LADO 0" : _vm!.LadoSeleccionado;
        
                // Importa
                var importService = new ImportService();
                var nuevos = importService.Importar(dlg.FileName, fecha, lado) ?? Enumerable.Empty<Operacion>();
        
                // 🚀 CLAVE: NO reasignar la colección -> vaciar y añadir para refrescar el DataGrid
                var target = _vm!.SesionActual!.Operaciones;
                target.Clear();
                foreach (var op in nuevos)
                    target.Add(op);
        
                MessageBox.Show(this, "Importación completada.", "Importar",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al importar: {ex.Message}", "Importar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.SesionActual?.Operaciones is null || _vm.SesionActual.Operaciones.Count == 0)
                {
                    MessageBox.Show(this, "No hay datos para exportar.", "Exportar CSV",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Exportar CSV",
                    Filter = "CSV (*.csv)|*.csv",
                    FileName = $"operativa_{DateOnly.FromDateTime(_vm.FechaActual):yyyyMMdd}_{_vm.LadoSeleccionado}.csv"
                };
                if (dlg.ShowDialog() != true) return;

                ExportarCsv(dlg.FileName, _vm.SesionActual.Operaciones);
                MessageBox.Show(this, "CSV exportado correctamente.", "Exportar CSV",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al exportar CSV: {ex.Message}", "Exportar CSV",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.SesionActual?.Operaciones is null || _vm.SesionActual.Operaciones.Count == 0)
                {
                    MessageBox.Show(this, "No hay datos para guardar en PDF.", "Guardar PDF",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Guardar jornada (PDF)",
                    Filter = "PDF (*.pdf)|*.pdf",
                    FileName = $"jornada_{DateOnly.FromDateTime(_vm.FechaActual):yyyyMMdd}_{_vm.LadoSeleccionado}.pdf"
                };
                if (dlg.ShowDialog() != true) return;

                var fecha = DateOnly.FromDateTime(_vm.FechaActual);
                var lado = string.IsNullOrWhiteSpace(_vm.LadoSeleccionado) ? "LADO 0" : _vm.LadoSeleccionado;

                _vm.PdfService.SaveJornadaPdf(dlg.FileName, _vm.SesionActual.Operaciones, fecha, lado);
                MessageBox.Show(this, "PDF generado correctamente.", "Guardar PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al generar PDF: {ex.Message}", "Guardar PDF",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewDay_Click(object sender, RoutedEventArgs e)
        {
            var fecha = DateOnly.FromDateTime(_vm.FechaActual);
            var r = MessageBox.Show(this,
                $"Se va a vaciar la jornada del {fecha:dd/MM/yyyy}.\n\n¿Continuar?",
                "Nueva jornada",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                _databaseService.DeleteDay(fecha);

                if (_vm.SesionActual != null)
                    _vm.SesionActual.Operaciones.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo vaciar la jornada: {ex.Message}",
                    "Nueva jornada", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "PLM INDITEX EXPEDICIÓN\n\nAplicación de operativa logística.",
                "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Hook opcional por si en XAML está enganchado a DataGrid.ColumnWidthChanged.
        /// </summary>
        private void DataGrid_ColumnWidthChanged(object sender, DataGridColumnEventArgs e)
        {
            // Si algún día quieres guardar layout:
            // _vm.Config.SaveColumnLayout("principal", (DataGrid)sender);
        }

        // ===================== UTILIDADES =====================

        private static void ExportarCsv(string filePath, IEnumerable<Operacion> ops)
        {
            var headers = new[]
            {
                "Id","Transportista","Matricula","Muelle","Estado","Destino",
                "Llegada","LlegadaReal","SalidaReal","SalidaTope",
                "Observaciones","Incidencias","Fecha","Precinto","Lex","Lado"
            };

            using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
            sw.WriteLine(string.Join(";", headers));

            static string S(object? v) => (v?.ToString() ?? "").Replace(';', ',');

            foreach (var o in ops)
            {
                var line = string.Join(";",
                    S(o.Id),
                    S(o.Transportista),
                    S(o.Matricula),
                    S(o.Muelle),
                    S(o.Estado),
                    S(o.Destino),
                    S(o.Llegada),
                    S(o.LlegadaReal),
                    S(o.SalidaReal),
                    S(o.SalidaTope),
                    S(o.Observaciones),
                    S(o.Incidencias),
                    S(o.Fecha),      // DateOnly ToString() -> cultura actual; ajusta si quieres yyyy-MM-dd
                    S(o.Precinto),
                    S(o.Lex),
                    S(o.Lado)
                );
                sw.WriteLine(line);
            }
        }
    }
}
