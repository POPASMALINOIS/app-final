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
        // ViewModel raíz y servicios
        private readonly MainViewModel _vm;
        private readonly ImportService _importService = new ImportService();
        private readonly DatabaseService _databaseService = new DatabaseService();
        private readonly ExportService _exportService = new ExportService(); // si no lo tienes aún, puedes quitarlo o añadir el archivo

        public MainWindow()
        {
            InitializeComponent();

            // Usa el VM ya asignado en XAML, o crea uno.
            _vm = DataContext as MainViewModel ?? new MainViewModel();
            DataContext = _vm;

            // Asegura una pestaña inicial en blanco
            if (_vm.SesionActual is null)
                _vm.NuevaPestana();
        }

        // ===================== MENÚ / BOTONES =====================

        private void NewTab_Click(object sender, RoutedEventArgs e) => _vm.NuevaPestana();

        private void CloseTab_Click(object sender, RoutedEventArgs e) => _vm.CerrarPestana();

        /// <summary>
        /// IMPORTAR (Excel/CSV). No reemplaza la colección: limpia y añade para refrescar el DataGrid al instante.
        /// </summary>
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Importar operativa (Excel/CSV)",
                    Filter = "Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Todos (*.*)|*.*"
                };
                if (dlg.ShowDialog() != true) return;

                // Si no hay pestaña activa, crea una
                if (_vm.SesionActual is null)
                    _vm.NuevaPestana();

                var fecha = DateOnly.FromDateTime(_vm.FechaActual);
                var lado  = string.IsNullOrWhiteSpace(_vm.LadoSeleccionado) ? "LADO 0" : _vm.LadoSeleccionado;

                // Importa desde el servicio (tolerante a cabeceras)
                var items = _importService.Importar(dlg.FileName, fecha, lado) ?? Enumerable.Empty<Operacion>();

                // ⚠️ CLAVE: NO reasignar la colección; limpiar y añadir
                var target = _vm.SesionActual!.Operaciones;
                target.Clear();
                foreach (var op in items)
                    target.Add(op);

                MessageBox.Show(this, $"Importación completada. Registros: {target.Count}",
                    "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // _exportService.SaveCsv(dlg.FileName, _vm.SesionActual.Operaciones);

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

                var fechaOnly = DateOnly.FromDateTime(_vm.FechaActual);
                var lado      = string.IsNullOrWhiteSpace(_vm.LadoSeleccionado) ? "LADO 0" : _vm.LadoSeleccionado;

                // Si tu PdfService espera DateTime, convierte desde DateOnly:
                var fechaDateTime = fechaOnly.ToDateTime(TimeOnly.MinValue);

                _vm.PdfService.SaveJornadaPdf(dlg.FileName, _vm.SesionActual.Operaciones, fechaDateTime, lado);

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
                _vm.SesionActual?.Operaciones.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo vaciar la jornada: {ex.Message}",
                    "Nueva jornada", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "PLM INDITEX EXPEDICIÓN\n\nAplicación de operativa logística.",
                "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===================== BOTONES DE RELOJ =====================
        // Estos métodos son invocados por los Button de los DataGridTemplateColumn (icono de reloj)

        // Marca la hora actual en LlegadaReal (formato HH:mm)
        private void SetLlegadaRealNow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Operacion op)
            {
                op.LlegadaReal = DateTime.Now.ToString("HH:mm");
                // Si quieres persistir inmediatamente:
                // _databaseService.UpdateLlegadaReal(op.Id, op.LlegadaReal);
            }
        }

        // Marca la hora actual en SalidaReal (formato HH:mm)
        private void SetSalidaRealNow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Operacion op)
            {
                op.SalidaReal = DateTime.Now.ToString("HH:mm");
                // Si quieres persistir inmediatamente:
                // _databaseService.UpdateSalidaReal(op.Id, op.SalidaReal);
            }
        }

        // ===================== UTILIDADES =====================

        private static void ExportarCsv(string filePath, IEnumerable<Operacion> ops)
        {
            var headers = new[]
            {
                "Id","Transportista","Matricula","Muelle","Estado","Destino",
                "Llegada","Llegada Real","Salida Real","Salida Tope",
                "Observaciones","Incidencias","Fecha","Precinto","Lex","Lado"
            };

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
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
                    S(o.Fecha),       // si quieres formato fijo: o.Fecha.ToString("yyyy-MM-dd")
                    S(o.Precinto),
                    S(o.Lex),
                    S(o.Lado)
                );
                sw.WriteLine(line);
            }
        }
    }
}
