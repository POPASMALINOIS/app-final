using System;
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
        private readonly ImportService _importService = new ImportService();
        private readonly PdfService _pdfService = new PdfService();
        private readonly DatabaseService _db = new DatabaseService(); // si no existe, crea stub vacío
        private readonly ConfigService _config = new ConfigService();  // idem

        public MainWindow()
        {
            InitializeComponent();
            if (DataContext is MainViewModel vm)
            {
                // Por si quieres inicializar algo del VM al abrir
                vm.PropertyChanged += (_, __) => { /* opcional */ };
            }
        }

        // ========== Helpers ==========

        private MainViewModel? VM => DataContext as MainViewModel;

        private DateTime FechaSeleccionada()
        {
            // Asume que tienes un DatePicker con x:Name="DpFecha" en tu XAML
            if (FindName("DpFecha") is DatePicker dp && dp.SelectedDate.HasValue)
                return dp.SelectedDate.Value.Date;

            return DateTime.Today;
        }

        private TabViewModel? PestañaActual() => VM?.SesionActual;

        // ========== Handlers pedidos por el XAML ==========

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            VM?.NuevaPestana();
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            VM?.CerrarPestana();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var vm = VM ?? return_;

            var dlg = new OpenFileDialog
            {
                Title = "Importar operativa (CSV)",
                Filter = "CSV (*.csv)|*.csv|Todos (*.*)|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                var ops = _importService.ImportFromCsvFile(dlg.FileName);
                if (vm.SesionActual == null)
                {
                    vm.NuevaPestana();
                }

                vm.SesionActual!.Operaciones = ops;
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var vm = VM;
            if (vm?.SesionActual?.Operaciones == null || vm.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para exportar.", "Exportar CSV",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Exportar CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"operativa_{FechaSeleccionada():yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog(this) == true)
            {
                var sb = new StringBuilder();
                // cabecera
                sb.AppendLine("TRANSPORTISTA;MATRICULA;MUELLE;ESTADO;DESTINO;LLEGADA;SALIDA_TOPE;OBSERVACIONES;INCIDENCIAS;FECHA");

                foreach (var o in vm.SesionActual!.Operaciones)
                {
                    var fechaStr = GetFechaString(o);
                    sb.AppendLine($"{o.Transportista};{o.Matricula};{o.Muelle};{o.Estado};{o.Destino};{o.Llegada};{o.SalidaTope};{o.Observaciones};{o.Incidencias};{fechaStr}");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show(this, "Exportado correctamente.", "Exportar CSV",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            var vm = VM;
            if (vm?.SesionActual?.Operaciones == null || vm.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para guardar en PDF.", "Guardar PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Guardar jornada en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"jornada_{FechaSeleccionada():yyyyMMdd}.pdf"
            };

            if (sfd.ShowDialog(this) == true)
            {
                // El servicio PDF original ya lo tenías en el repo
                _pdfService.SaveJornadaPdf(
                    sfd.FileName,
                    vm.SesionActual!.Operaciones.ToList(),
                    FechaSeleccionada(),
                    vm.LadoSeleccionado
                );

                MessageBox.Show(this, "PDF generado correctamente.", "PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NewDay_Click(object sender, RoutedEventArgs e)
        {
            var fecha = FechaSeleccionada();
            if (MessageBox.Show(this, $"¿Vaciar la jornada del {fecha:dd/MM/yyyy}?",
                    "Nueva jornada", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _db.DeleteDay(fecha); // si tu DatabaseService usa DateOnly, conviertes aquí:
                    // _db.DeleteDay(DateOnly.FromDateTime(fecha));
                    if (VM?.SesionActual != null)
                        VM.SesionActual.Operaciones.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Borrado", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "PLM INDITEX EXPEDICIÓN\n© 2025", "Acerca de",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Hook para “ColumnWidthChanged” usado en XAML (si lo tienes cableado)
        // No hace nada crítico, sólo guarda/recupera layout si quieres.
        private void ColumnWidthChanged(object sender, EventArgs e)
        {
            // Si quieres persistir anchos:
            // _config.SaveColumnLayout(DataGridNameHere);
        }

        // Si tu DatePicker (x:Name="DpFecha") dispara SelectedDateChanged, puedes validar así:
        private void DpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker dp && dp.SelectedDate.HasValue)
            {
                // Sólo DateTime, nada de DateOnly para comparar
                var sel = dp.SelectedDate.Value.Date;
                var hoy = DateTime.Today;

                // Si no quieres permitir futuras, lo corriges
                if (sel > hoy) dp.SelectedDate = hoy;
            }
        }

        // ========== Utilidades privadas ==========

        private static string GetFechaString(Operacion o)
        {
            var prop = typeof(Operacion).GetProperty("Fecha");
            if (prop == null) return "";

            var val = prop.GetValue(o);
            return val switch
            {
                DateOnly dOnly => dOnly.ToString("yyyy-MM-dd"),
                DateTime dt    => dt.ToString("yyyy-MM-dd"),
                string s       => s,
                _              => ""
            };
        }

        // truco para evitar warning en “var vm = VM ?? return_;”
        private MainViewModel return_ => null!;
    }
}
