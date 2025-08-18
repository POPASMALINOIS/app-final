using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Alias para que el atributo [RelayCommand] use el del Toolkit
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommandAttribute;

namespace OperativaLogistica.ViewModels
{
    // Debe ser partial + heredar de ObservableObject
    public partial class SessionViewModel : ObservableObject
    {
        // Aquí continúan tus propiedades y métodos actuales.
        // Ejemplo de uso correcto con el Toolkit:
        //
        // [ObservableProperty]
        // private string? usuario;
        //
        // [RelayCommand]
        // private void IniciarSesion()
        // {
        //     // lógica...
        // }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace OperativaLogistica.ViewModels
{
    public partial class SessionViewModel : ObservableObject
    {
        private readonly DatabaseService _db = new();

        [ObservableProperty] private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
        [ObservableProperty] private string selectedLado = "LADO 0";
        [ObservableProperty] private string filterText = "";

        public ObservableCollection<Operacion> Operaciones { get; } = new();
        private ICollectionView? _view;
        public ICollectionView View => _view ??= CollectionViewSource.GetDefaultView(Operaciones);

        public SessionViewModel()
        {
            View.Filter = FilterPredicate;
            Load();
        }

        public void Load()
        {
            Operaciones.Clear();
            foreach (var op in _db.GetByDate(SelectedDate)) Operaciones.Add(op);
            View.Refresh();
        }

        public void SaveAll()
        {
            foreach (var op in Operaciones) _db.Upsert(op);
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            if (obj is not Operacion op) return false;
            var q = FilterText.Trim().ToLowerInvariant();
            return ($"{op.Transportista} {op.Matricula} {op.Muelle} {op.Estado} {op.Destino} {op.Llegada} {op.LlegadaReal} {op.SalidaReal} {op.SalidaTope} {op.Observaciones} {op.Incidencias} {op.Precinto}")
                   .ToLowerInvariant().Contains(q);
        }

        [RelayCommand]
        private void MarkLlegada(object? parameter)
        {
            if (parameter is not Operacion op) return;
            op.LlegadaReal = DateTime.Now.ToString("HH:mm");
            _db.Upsert(op); View.Refresh();
        }

        [RelayCommand]
        private void MarkSalida(object? parameter)
        {
            if (parameter is not Operacion op) return;
            op.SalidaReal = DateTime.Now.ToString("HH:mm");
            _db.Upsert(op); View.Refresh();
        }

        [RelayCommand]
        private void DuplicateRow(object? parameter)
        {
            if (parameter is not Operacion op) return;
            var copy = new Operacion
            {
                Transportista = op.Transportista,
                Matricula = op.Matricula,
                Muelle = op.Muelle,
                Estado = op.Estado,
                Destino = op.Destino,
                Llegada = op.Llegada,
                LlegadaReal = "",
                SalidaReal = "",
                SalidaTope = op.SalidaTope,
                Observaciones = op.Observaciones,
                Incidencias = op.Incidencias,
                Precinto = op.Precinto,
                Lex = op.Lex,
                Fecha = SelectedDate
            };
            Operaciones.Add(copy);
            _db.Upsert(copy);
        }

        [RelayCommand]
        private void DeleteRow(object? parameter)
        {
            if (parameter is not Operacion op) return;
            Operaciones.Remove(op);
            // borrado simple: marcar como eliminado vía Update (opcional: crear método Delete en DB)
        }

        public void AutoSave()
        {
            try
            {
                var name = $"autosave_{SelectedDate:yyyyMMdd}_{DateTime.Now:HHmm}.csv";
                var path = Path.Combine(AppPaths.Autosaves, name);
                using var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8);
                sw.WriteLine("Transportista,Matricula,Muelle,Estado,Destino,Llegada,LlegadaReal,SalidaReal,SalidaTope,Observaciones,Incidencias,Precinto,Lex,Fecha,Lado");
                foreach (var op in Operaciones)
                {
                    sw.WriteLine(string.Join(",",
                        Safe(op.Transportista), Safe(op.Matricula), Safe(op.Muelle), Safe(op.Estado), Safe(op.Destino),
                        Safe(op.Llegada), Safe(op.LlegadaReal), Safe(op.SalidaReal), Safe(op.SalidaTope),
                        Safe(op.Observaciones), Safe(op.Incidencias), Safe(op.Precinto),
                        op.Lex ? "1" : "0",
                        SelectedDate.ToString("yyyy-MM-dd"), SelectedLado));
                }
            } catch { }
        }
        private static string Safe(string? s) => $"\"{(s ?? "").Replace("\"", "''")}\"";
    }
}
