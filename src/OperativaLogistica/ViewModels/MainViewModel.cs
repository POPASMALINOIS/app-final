
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperativaLogistica.Models;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;

namespace OperativaLogistica.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        [ObservableProperty]
        private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

        [ObservableProperty]
        private ObservableCollection<Operacion> operaciones = new();

        public IRelayCommand LoadCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand NewDayCommand { get; }   // 🚩 NUEVO COMANDO

        public MainViewModel()
        {
            _db = new DatabaseService();

            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);
            NewDayCommand = new RelayCommand(NewDay); // 🚩 inicialización del comando

            Load();
        }

        private void Load()
        {
            Operaciones.Clear();
            var ops = _db.GetByDate(SelectedDate);
            foreach (var op in ops)
            {
                Operaciones.Add(op);
            }
        }

        private void Save()
        {
            foreach (var op in Operaciones)
            {
                _db.Upsert(op);
            }
        }

        // 🚩 NUEVO MÉTODO: Borrar la jornada y dejar el día vacío
        private void NewDay()
        {
            _db.DeleteByDate(SelectedDate); // borra las operaciones de la fecha
            Operaciones.Clear();            // limpia la vista
        }
    }
}
