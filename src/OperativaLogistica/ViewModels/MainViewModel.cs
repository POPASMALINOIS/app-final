
using OperativaLogistica.Commands;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;

namespace OperativaLogistica.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Operacion> Operaciones { get; } = new();
        private readonly DatabaseService _db = new();

        private string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set { _filterText = value; OnPropertyChanged(); View.Refresh(); }
        }

        private DateOnly _fecha = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly Fecha
        {
            get => _fecha;
            set { _fecha = value; OnPropertyChanged(); LoadFromDb(); }
        }

        public ICollectionView View { get; private set; }

        public ICommand ImportCommand { get; }
        public ICommand SaveDayCommand { get; }
        public ICommand MarkLlegadaCommand { get; }
        public ICommand MarkSalidaCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            View = CollectionViewSource.GetDefaultView(Operaciones);
            View.Filter = FilterPredicate;

            ImportCommand = new RelayCommand(_ => Import());
            SaveDayCommand = new RelayCommand(_ => SaveDay());
            MarkLlegadaCommand = new RelayCommand(op => MarkTime(op as Operacion, "LlegadaReal"));
            MarkSalidaCommand = new RelayCommand(op => MarkTime(op as Operacion, "SalidaReal"));

            LoadFromDb();
        }

    private void Import()
{
    var dlg = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Ficheros CSV o Excel|*.csv;*.xlsx",
        CheckFileExists = true,
        Title = "Selecciona el fichero con la operativa"
    };
    if (dlg.ShowDialog() == true)
    {
        var path = dlg.FileName;
        var list = path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? ImportService.FromXlsx(path, Fecha)
            : ImportService.FromCsv(path, Fecha);

        foreach (var op in list)
            _db.Upsert(op);

        LoadFromDb();

        System.Windows.MessageBox.Show(
            list.Count > 0
                ? $"Importación completada.\nFilas añadidas/actualizadas: {list.Count}"
                : "No se detectaron filas válidas en el fichero seleccionado.\n\nConsejo: verifica que la fila de cabecera contiene nombres de columnas reconocibles (transportista, matrícula, muelle, estado, destino, llegada, salida tope...).",
            "Importar operativa",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
}

        private void LoadFromDb()
        {
            Operaciones.Clear();
            foreach (var op in _db.GetByDate(Fecha))
                Operaciones.Add(op);
            View.Refresh();
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            if (obj is not Operacion op) return false;
            var ft = FilterText.Trim().ToLowerInvariant();
            return (op.Transportista + " " + op.Matricula + " " + op.Muelle + " " + op.Estado + " " +
                    op.Destino + " " + op.Llegada + " " + op.LlegadaReal + " " + op.SalidaReal + " " +
                    op.SalidaTope + " " + op.Observaciones + " " + op.Incidencias)
                    .ToLowerInvariant().Contains(ft);
        }

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
                    ? ImportService.FromXlsx(path, null, Fecha)
                    : ImportService.FromCsv(path, Fecha);

                foreach (var op in list)
                    _db.Upsert(op);

                LoadFromDb();
            }
        }

        private void SaveDay()
        {
            var pdf = PdfService.SaveDailyPdf(Operaciones, Fecha);
            System.Windows.MessageBox.Show($"PDF guardado en:\n{pdf}", "Operativa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void MarkTime(Operacion? op, string field)
        {
            if (op == null) return;
            var now = DateTime.Now.ToString("HH:mm");
            if (field == "LlegadaReal")
            {
                if (!string.IsNullOrWhiteSpace(op.LlegadaReal))
                {
                    if (System.Windows.MessageBox.Show($"La LLEGADA REAL ya es {op.LlegadaReal}. ¿Quieres sobrescribirla por {now}?",
                        "Confirmar", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                op.LlegadaReal = now;
            }
            else if (field == "SalidaReal")
            {
                if (!string.IsNullOrWhiteSpace(op.SalidaReal))
                {
                    if (System.Windows.MessageBox.Show($"La SALIDA REAL ya es {op.SalidaReal}. ¿Quieres sobrescribirla por {now}?",
                        "Confirmar", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                op.SalidaReal = now;
            }
            _db.Upsert(op);
            View.Refresh();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
