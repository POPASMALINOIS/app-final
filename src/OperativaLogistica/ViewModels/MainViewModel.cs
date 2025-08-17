using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;

namespace OperativaLogistica.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _db = new();

        public ObservableCollection<Operacion> Operaciones { get; } = new();

        private ICollectionView? _view;
        public ICollectionView View => _view ??= CollectionViewSource.GetDefaultView(Operaciones);

        [ObservableProperty]
        private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

        partial void OnSelectedDateChanged(DateOnly value)
        {
            LoadFromDb();
        }

        [ObservableProperty]
        private string filterText = "";

        partial void OnFilterTextChanged(string value)
        {
            View.Refresh();
        }

        public MainViewModel()
        {
            View.Filter = FilterPredicate;
            LoadFromDb();
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            if (obj is not Operacion op) return false;
            var q = FilterText.Trim().ToLowerInvariant();
            return ($"{op.Transportista} {op.Matricula} {op.Muelle} {op.Estado} {op.Destino} {op.Llegada} {op.LlegadaReal} {op.SalidaReal} {op.SalidaTope} {op.Observaciones} {op.Incidencias}")
                   .ToLowerInvariant().Contains(q);
        }

        // --------- COMANDOS ---------

        [RelayCommand]
        private void NewDay()
        {
            if (System.Windows.MessageBox.Show(
                $"Vas a iniciar una jornada en blanco para {SelectedDate:dd/MM/yyyy}.\nSe borrarán las entradas existentes de ese día.\n\n¿Continuar?",
                "Nueva jornada", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
                return;

            _db.DeleteByDate(SelectedDate);
            Operaciones.Clear();
            View.Refresh();
        }

        [RelayCommand]
        private void Import()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Ficheros CSV o Excel|*.csv;*.xlsx",
                CheckFileExists = true,
                Title = "Selecciona el fichero con la operativa"
            };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                var list = path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    ? ImportService.FromXlsx(path, SelectedDate)
                    : ImportService.FromCsv(path, SelectedDate);

                foreach (var op in list)
                    _db.Upsert(op);

                LoadFromDb();

                System.Windows.MessageBox.Show(
                    list.Count > 0
                        ? $"Importación completada.\nFilas añadidas/actualizadas: {list.Count}"
                        : "No se detectaron filas válidas.\nRevisa que la fila de cabecera tenga nombres de columnas reconocibles (transportista, matrícula, muelle, estado, destino, llegada, salida tope...).",
                    "Importar operativa",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void SaveDay()
        {
            var pdf = PdfService.SaveDailyPdf(Operaciones, SelectedDate);
            System.Windows.MessageBox.Show($"PDF guardado en:\n{pdf}", "Operativa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void MarkLlegada(Operacion? op)
        {
            MarkTime(op, isLlegada: true);
        }

        [RelayCommand]
        private void MarkSalida(Operacion? op)
        {
            MarkTime(op, isLlegada: false);
        }

        // --------- AUXILIARES ---------

        private void MarkTime(Operacion? op, bool isLlegada)
        {
            if (op == null) return;
            var now = DateTime.Now.ToString("HH:mm");
            if (isLlegada)
            {
                if (!string.IsNullOrWhiteSpace(op.LlegadaReal))
                {
                    if (System.Windows.MessageBox.Show(
                        $"La LLEGADA REAL ya es {op.LlegadaReal}. ¿Sobrescribir por {now}?",
                        "Confirmar", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                op.LlegadaReal = now;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(op.SalidaReal))
                {
                    if (System.Windows.MessageBox.Show(
                        $"La SALIDA REAL ya es {op.SalidaReal}. ¿Sobrescribir por {now}?",
                        "Confirmar", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                op.SalidaReal = now;
            }
            _db.Upsert(op);
            View.Refresh();
        }

        private void LoadFromDb()
        {
            Operaciones.Clear();
            foreach (var op in _db.GetByDate(SelectedDate))
                Operaciones.Add(op);
            View.Refresh();
        }
    }
}
