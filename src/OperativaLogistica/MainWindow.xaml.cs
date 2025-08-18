using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Namespaces de tu solución
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using OperativaLogistica.ViewModels;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        // Servicios “ligeros” que usamos desde el code-behind
        private readonly ImportService _importService = new ImportService();
        private MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            // Si tu XAML ya fijó el DataContext, no hace falta esta línea.
            if (DataContext is not MainViewModel)
                DataContext = new MainViewModel();
        }

        // =============== Menú Archivo =================

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            VM.NuevaPestana();
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            VM.CerrarPestana();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (VM.SesionActual is null)
            {
                MessageBox.Show("No hay pestaña activa.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ofd = new OpenFileDialog
            {
                Title = "Importar operativa (Excel/CSV)",
                Filter = "Ficheros Excel (*.xlsx;*.xls)|*.xlsx;*.xls|CSV (*.csv)|*.csv|Todos (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    // Importa y sustituye el contenido de la pestaña actual
                    var ops = _importService.Importar(ofd.FileName, VM.FechaActual, VM.LadoSeleccionado);
                    VM.SesionActual.Operaciones = new ObservableCollection<Operacion>(ops ?? Enumerable.Empty<Operacion>());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo importar el fichero.\n{ex.Message}", "Importar", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (VM.SesionActual?.Operaciones is null || VM.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Exportar CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"operativa_{VM.FechaActual:yyyyMMdd}_{VM.LadoSeleccionado.Replace(' ', '_')}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // Exportación CSV simple
                    using var sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
                    // Cabecera
                    sw.WriteLine("Id;Transportista;Matricula;Muelle;Estado;Destino;Llegada;LlegadaReal;SalidaReal;SalidaTope;Observaciones;Incidencias;Fecha;Precinto;Lex");
                    // Filas
                    foreach (var o in VM.SesionActual.Operaciones)
                    {
                        string fmt(string? s) => (s ?? string.Empty).Replace(';', ',');
                        sw.WriteLine(string.Join(';', new[]
                        {
                            o.Id?.ToString() ?? "",
                            fmt(o.Transportista),
                            fmt(o.Matricula),
                            fmt(o.Muelle),
                            fmt(o.Estado),
                            fmt(o.Destino),
                            fmt(o.Llegada),
                            fmt(o.LlegadaReal),
                            fmt(o.SalidaReal),
                            fmt(o.SalidaTope),
                            fmt(o.Observaciones),
                            fmt(o.Incidencias),
                            o.Fecha?.ToString("yyyy-MM-dd") ?? "",
                            fmt(o.Precinto),
                            o.Lex ? "1" : "0"
                        }));
                    }

                    MessageBox.Show("Exportado correctamente.", "Exportar CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo exportar el CSV.\n{ex.Message}", "Exportar CSV", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (VM.SesionActual?.Operaciones is null || VM.SesionActual.Operaciones.Count == 0)
            {
                MessageBox.Show("No hay datos para guardar en PDF.", "Guardar PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Guardar jornada en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"jornada_{VM.FechaActual:yyyyMMdd}_{VM.LadoSeleccionado.Replace(' ', '_')}.pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // Usa el servicio expuesto por el VM
                    VM.PdfService.SaveJornadaPdf(
                        sfd.FileName,
                        VM.SesionActual.Operaciones.ToList(),
                        VM.FechaActual,
                        VM.LadoSeleccionado
                    );

                    MessageBox.Show("PDF guardado correctamente.", "Guardar PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo generar el PDF.\n{ex.Message}", "Guardar PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NewDay_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Esto vaciará la jornada actual.\n¿Continuar?", "Nueva jornada",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // Si tienes un método DB real, puedes descomentar/adaptar:
            // var db = new DatabaseService();
            // db.DeleteByDate(VM.FechaActual);

            // Dejamos la pestaña actual en blanco
            if (VM.SesionActual != null)
                VM.SesionActual.Operaciones = new ObservableCollection<Operacion>();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PLM INDITEX EXPEDICIÓN\n© 2025", "Acerca de", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =============== Otros handlers =================

        // Firma correcta para WPF: DataGridColumnEventHandler
        private void DataGrid_ColumnWidthChanged(object? sender, DataGridColumnEventArgs e)
        {
            // Aquí podrías persistir el ancho si ya tienes un servicio para ello.
            // Este cuerpo vacío evita errores de compilación y de evento.
            // Ejemplo si en el futuro tienes un ColumnLayoutService:
            // _columnLayoutService.SaveWidth(VM.Config, e.Column);
        }

        // (Opcional) si usas atajos de teclado
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) NewTab_Click(sender, e);
        }
    }
}
