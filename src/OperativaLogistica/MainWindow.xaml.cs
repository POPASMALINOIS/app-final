using Microsoft.Win32;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        // Servicios
        private readonly ConfigService _config;
        private readonly ColumnLayoutService _cols;
        private readonly ImportService _import;
        private readonly PdfService _pdf;
        private readonly DatabaseService _db;

        // VM raíz (ya tenías uno con CommunityToolkit o INotifyPropertyChanged)
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _config = new ConfigService();
            _cols   = new ColumnLayoutService(_config);
            _import = new ImportService();
            _pdf    = new PdfService();
            _db     = new DatabaseService();

            _vm = new MainViewModel();
            DataContext = _vm;

            // Si el XAML tiene nombres distintos, cambia aquí:
            // Aplica layout cuando el grid se cargue
            if (FindName("gridOperaciones") is DataGrid g)
            {
                g.Loaded += (_, __) => _cols.ApplyColumnLayout(g, _vm.LadoSeleccionado);
                g.ColumnWidthChanged += (_, __) => _cols.SaveColumnLayout(g, _vm.LadoSeleccionado);
                g.SizeChanged += (_, __) => _cols.SaveColumnLayout(g, _vm.LadoSeleccionado);
            }
        }

        // ------------------- MENÚ Archivo -------------------

        private void Menu_NuevaPestana_Click(object sender, RoutedEventArgs e)
        {
            _vm.NuevaPestana();
            RenombraPestanaConLado();
        }

        private void Menu_CerrarPestana_Click(object sender, RoutedEventArgs e)
        {
            _vm.CerrarPestana();
        }

        private void Menu_Importar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                Title = "Importar operativa"
            };

            if (dlg.ShowDialog(this) != true) return;

            var fecha = _vm.FechaActual; // DateTime (NO DateOnly)
            var lado  = _vm.LadoSeleccionado; // "LADO 0"..."LADO 9"

            var ops = _import.Importar(dlg.FileName, fecha, lado);

            if (_vm.SesionActual == null) _vm.NuevaPestana();
            _vm.SesionActual!.Operaciones = new ObservableCollection<Operacion>(ops);

            // Aplica layout por lado
            if (FindName("gridOperaciones") is DataGrid g)
            {
                _cols.ApplyColumnLayout(g, lado);
            }
        }

        private void Menu_ExportarCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SesionActual == null || _vm.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"operativa_{_vm.FechaActual:yyyyMMdd}_{_vm.LadoSeleccionado}.csv"
            };

            if (dlg.ShowDialog(this) != true) return;

            CsvExporter.Export(dlg.FileName, _vm.SesionActual.Operaciones);
            MessageBox.Show("CSV guardado.");
        }

        private void Menu_GuardarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SesionActual == null || _vm.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar a PDF.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"operativa_{_vm.FechaActual:yyyyMMdd}_{_vm.LadoSeleccionado}.pdf"
            };

            if (dlg.ShowDialog(this) != true) return;

            _pdf.SaveJornadaPdf(
                dlg.FileName,
                _vm.SesionActual.Operaciones.ToList(),
                _vm.FechaActual,                  // DateTime
                _vm.LadoSeleccionado              // string ("LADO X")
            );

            MessageBox.Show("PDF guardado.");
        }

        private void Menu_NuevaJornada_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SesionActual == null) _vm.NuevaPestana();
            _vm.SesionActual!.Operaciones.Clear();
            // Limpia también en DB si lo usas por Fecha+Lado
            _db.DeleteDay(_vm.FechaActual.Date);
        }

        private void Menu_Salir_Click(object sender, RoutedEventArgs e) => Close();

        // ------------------- Barra superior: Fecha / Lado -------------------

        // DatePicker Seleccionado -> siempre DateTime
        private void Fecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker dp)
            {
                _vm.FechaActual = dp.SelectedDate ?? DateTime.Today;
            }
        }

        // ComboBox Lado cambiado -> renombra pestaña activa
        private void Lado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is string s && !string.IsNullOrWhiteSpace(s))
            {
                _vm.LadoSeleccionado = s;
                RenombraPestanaConLado();
                // Reaplica layout por lado
                if (FindName("gridOperaciones") is DataGrid g)
                    _cols.ApplyColumnLayout(g, s);
            }
        }

        private void RenombraPestanaConLado()
        {
            if (_vm.SesionActual != null)
                _vm.SesionActual.Titulo = _vm.LadoSeleccionado;
        }

        // ------------------- Botones por fila (Llegada/Salida real) -------------------

        private void BtnLlegadaReal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is Operacion op)
            {
                op.LlegadaReal = DateTime.Now.ToString("HH:mm");
            }
        }

        private void BtnSalidaReal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is Operacion op)
            {
                op.SalidaReal = DateTime.Now.ToString("HH:mm");
            }
        }
    }

    // ---------- Export CSV helper muy simple ----------
    internal static class CsvExporter
    {
        public static void Export(string filePath, ObservableCollection<Operacion> ops)
        {
            using var sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            sw.WriteLine("TRANSPORTISTA,MATRICULA,MUELLE,ESTADO,DESTINO,LLEGADA,LLEGADA REAL,SALIDA REAL,SALIDA TOPE,OBSERVACIONES,INCIDENCIAS,PRECINTO,FECHA");
            foreach (var o in ops)
            {
                string esc(string? s) =>
                    string.IsNullOrEmpty(s) ? "" :
                    s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

                sw.WriteLine(string.Join(",",
                    esc(o.Transportista),
                    esc(o.Matricula),
                    esc(o.Muelle),
                    esc(o.Estado),
                    esc(o.Destino),
                    esc(o.Llegada),
                    esc(o.LlegadaReal),
                    esc(o.SalidaReal),
                    esc(o.SalidaTope),
                    esc(o.Observaciones),
                    esc(o.Incidencias),
                    esc(o.Precinto),
                    o.Fecha.ToString("yyyy-MM-dd")
                ));
            }
        }
    }
}
