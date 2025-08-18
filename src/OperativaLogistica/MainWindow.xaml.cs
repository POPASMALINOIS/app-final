using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

// Si ya tienes estos espacios de nombres en tu solución, déjalos.
// No pasa nada si alguno no existe: quita el using que te marque como no usado.
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

// Para .xlsx: recuerda tener ClosedXML instalado en el proyecto (dotnet add package ClosedXML)
using ClosedXML.Excel;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _debounce;
        private bool _layoutDirty;
        private ConfigService? _cfg;

        public MainWindow()
        {
            InitializeComponent();

            // IMPORTANTE: el menú necesita que el Window tenga MainViewModel como DataContext
            if (DataContext is not MainViewModel)
                DataContext = new MainViewModel();

            _cfg = (DataContext as MainViewModel)?.Config;

            _debounce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _debounce.Tick += (_, __) =>
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
                        if (!_debounce.IsEnabled) _debounce.Start();
                    };
                }
            };
        }

        #region Acceso a VM y lista activa
        private MainViewModel? VM => DataContext as MainViewModel;

        private ObservableCollection<Operacion> GetActiveOps(bool createIfNull = true)
        {
            // Preferimos Operaciones de la sesión actual del VM
            var ops = VM?.SesionActual?.Operaciones;
            if (ops == null && createIfNull)
            {
                ops = new ObservableCollection<Operacion>();
                if (VM?.SesionActual != null)
                    VM.SesionActual.Operaciones = ops;
            }
            return ops ?? new ObservableCollection<Operacion>();
        }
        #endregion

        #region MENÚ: Click handlers
        // Estos métodos puedes enlazarlos desde el XAML con Click="Import_Click", etc.
        // Si ya tienes ICommand en el VM, no pasa nada: conviven sin problema.

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            VM?.NuevaPestana();
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            VM?.CerrarPestana();
        }

        private void NewDay_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Se vaciarán las operaciones del día actual. ¿Continuar?",
                                "Nueva jornada", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var ops = GetActiveOps();
                ops.Clear();
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Selecciona un Excel/CSV de operativa",
                Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|Todos (*.*)|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog(this) != true) return;

            try
            {
                int añadidas = 0;
                if (ofd.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    añadidas = ImportFromXlsx(ofd.FileName);
                }
                else
                {
                    añadidas = ImportFromCsv(ofd.FileName);
                }

                MessageBox.Show($"Importación completada.\nFilas añadidas: {añadidas}",
                                "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo importar el fichero.\n\n{ex.Message}",
                                "Importar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Exportar a CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"operativa_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog(this) != true) return;

            try
            {
                var ops = GetActiveOps(false);
                using var sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
                // Cabecera
                sw.WriteLine("TRANSPORTISTA;MATRICULA;MUELLE;ESTADO;DESTINO;LLEGADA;LLEGADA_REAL;SALIDA_REAL;SALIDA_TOPE;OBSERVACIONES;INCIDENCIAS;PRECINTO;LEX;FECHA");
                foreach (var o in ops)
                {
                    sw.WriteLine(string.Join(';', new[]
                    {
                        Csv(o.Transportista), Csv(o.Matricula), Csv(o.Muelle), Csv(o.Estado),
                        Csv(o.Destino), Csv(o.Llegada), Csv(o.LlegadaReal), Csv(o.SalidaReal),
                        Csv(o.SalidaTope), Csv(o.Observaciones), Csv(o.Incidencias),
                        Csv(o.Precinto), o.Lex ? "1" : "0", o.Fecha.ToString("yyyy-MM-dd")
                    }));
                }
                MessageBox.Show("CSV exportado correctamente.", "Exportar CSV",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo exportar el CSV.\n\n{ex.Message}",
                                "Exportar CSV", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Guardar jornada en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"operativa_{DateTime.Now:yyyyMMdd}.pdf"
            };
            if (sfd.ShowDialog(this) != true) return;

            try
            {
                var ops = GetActiveOps(false).ToList();

                // 1) Intentar usar tu PdfService si está disponible en el VM
                var pdfSvc = (VM as dynamic)?.PdfService as PdfService;
                if (pdfSvc != null)
                {
                    pdfSvc.SaveJornadaPdf(sfd.FileName, ops, VM?.FechaActual ?? DateTime.Today, VM?.LadoSeleccionado ?? "LADO 0");
                    MessageBox.Show("PDF generado correctamente.", "Guardar PDF",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2) Fallback sencillo: si no hay PdfService, generamos un TXT con la jornada
                var txtAlt = Path.ChangeExtension(sfd.FileName, ".txt");
                using (var sw = new StreamWriter(txtAlt, false, Encoding.UTF8))
                {
                    sw.WriteLine($"PLM INDITEX EXPEDICION - {DateTime.Now:dd/MM/yyyy} - {VM?.LadoSeleccionado ?? "LADO 0"}");
                    sw.WriteLine(new string('=', 80));
                    foreach (var o in ops)
                    {
                        sw.WriteLine($"{o.Transportista} | {o.Matricula} | Muelle: {o.Muelle} | {o.Estado} | Destino: {o.Destino}");
                        sw.WriteLine($"Llegada: {o.Llegada} | LlegadaReal: {o.LlegadaReal} | SalidaReal: {o.SalidaReal} | SalidaTope: {o.SalidaTope}");
                        sw.WriteLine($"Obs: {o.Observaciones} | Incidencias: {o.Incidencias} | Precinto: {o.Precinto} | LEX:{(o.Lex ? "Sí" : "No")}");
                        sw.WriteLine(new string('-', 80));
                    }
                }
                MessageBox.Show("No se encontró PdfService. Se ha generado un TXT alternativo junto al PDF.",
                                "Guardar PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo generar el PDF.\n\n{ex.Message}",
                                "Guardar PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Operativa Logística offline (.NET 8 WPF)\nPLM INDITEX EXPEDICION\nLicencia MIT",
                            "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Import helpers
        private int ImportFromCsv(string filePath)
        {
            var ops = GetActiveOps();
            int count = 0;

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0) return 0;

            // Detectar separador (coma o punto y coma)
            char sep = lines[0].Count(c => c == ';') >= lines[0].Count(c => c == ',') ? ';' : ',';

            // Si la primera línea parece cabecera, la saltamos
            int start = 0;
            var header = lines[0].ToLowerInvariant();
            if (header.Contains("transportista") || header.Contains("matricula") || header.Contains("muelle"))
                start = 1;

            for (int i = start; i < lines.Length; i++)
            {
                var parts = SplitCsv(lines[i], sep);
                if (parts.Length == 0) continue;

                // Mapeo flexible por posiciones (ajusta si tu layout varía)
                var op = new Operacion
                {
                    Transportista = Get(parts, 0),
                    Matricula     = Get(parts, 1),
                    Muelle        = Get(parts, 2),
                    Estado        = Get(parts, 3),
                    Destino       = Get(parts, 4),
                    Llegada       = Get(parts, 5),
                    LlegadaReal   = Get(parts, 6),
                    SalidaReal    = Get(parts, 7),
                    SalidaTope    = Get(parts, 8),
                    Observaciones = Get(parts, 9),
                    Incidencias   = Get(parts,10),
                    Precinto      = Get(parts,11),
                    Lex           = ParseBool(Get(parts,12)),
                    Fecha         = ParseDate(Get(parts,13)) ?? (VM?.FechaActual ?? DateTime.Today)
                };

                ops.Add(op);
                count++;
            }

            return count;
        }

        private int ImportFromXlsx(string filePath)
        {
            var ops = GetActiveOps();
            int count = 0;

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            // Localiza rango usado y salta cabecera si la primera fila tiene títulos
            var rng = ws.RangeUsed();
            if (rng == null) return 0;

            var firstRow = rng.FirstRowUsed();
            var hasHeader = firstRow.Cell(1).GetString().ToLower().Contains("transp")
                            || firstRow.Cell(2).GetString().ToLower().Contains("matr")
                            || firstRow.Cell(3).GetString().ToLower().Contains("muelle");

            var rows = hasHeader ? rng.RowsUsed().Skip(1) : rng.RowsUsed();

            foreach (var r in rows)
            {
                string C(int i) => r.Cell(i).GetString()?.Trim() ?? "";

                var op = new Operacion
                {
                    Transportista = C(1),
                    Matricula     = C(2),
                    Muelle        = C(3),
                    Estado        = C(4),
                    Destino       = C(5),
                    Llegada       = C(6),
                    LlegadaReal   = C(7),
                    SalidaReal    = C(8),
                    SalidaTope    = C(9),
                    Observaciones = C(10),
                    Incidencias   = C(11),
                    Precinto      = C(12),
                    Lex           = ParseBool(C(13)),
                    Fecha         = ParseDate(C(14)) ?? (VM?.FechaActual ?? DateTime.Today)
                };

                ops.Add(op);
                count++;
            }

            return count;
        }
        #endregion

        #region Utilidades CSV/Parseo
        private static string Csv(string? s)
        {
            if (s is null) return "";
            s = s.Replace("\"", "\"\"");
            return s.Contains(';') || s.Contains('"') ? $"\"{s}\"" : s;
        }

        private static string Get(string[] arr, int index) =>
            index >= 0 && index < arr.Length ? arr[index]?.Trim() ?? "" : "";

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            return s is "1" or "true" or "sí" or "si" or "x";
        }

        private static DateTime? ParseDate(string s)
        {
            if (DateTime.TryParse(s, out var d)) return d;
            string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out d))
                return d;
            return null;
        }

        private static string[] SplitCsv(string line, char sep)
        {
            // Soporta valores entrecomillados con el separador dentro
            var list = new System.Collections.Generic.List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (ch == sep && !inQuotes)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            list.Add(sb.ToString());
            return list.ToArray();
        }
        #endregion

        #region DataGrid hooks (por si los necesitas para layout)
        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid g && _cfg != null)
                ColumnLayoutService.Apply(g, _cfg);
        }
        #endregion
    }
}
