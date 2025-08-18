using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OperativaLogistica.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ===== PROPIEDADES PRINCIPALES =====
        private DateTime _fechaActual = DateTime.Today;
        public DateTime FechaActual
        {
            get => _fechaActual;
            set { _fechaActual = value; OnPropertyChanged(); }
        }

        private string _ladoSeleccionado;
        public string LadoSeleccionado
        {
            get => _ladoSeleccionado;
            set { _ladoSeleccionado = value; OnPropertyChanged(); }
        }

        private object _sesionActual;
        public object SesionActual
        {
            get => _sesionActual;
            set { _sesionActual = value; OnPropertyChanged(); }
        }

        // ===== LISTA DE PESTAÑAS =====
        public ObservableCollection<object> Pestanas { get; set; } = new ObservableCollection<object>();

        // ===== COMANDOS =====
        public ICommand NuevaPestana { get; }
        public ICommand CerrarPestana { get; }
        public ICommand SaveDownloadPdf { get; }

        public MainViewModel()
        {
            NuevaPestana = new RelayCommand(_ => AgregarPestana());
            CerrarPestana = new RelayCommand(_ => EliminarPestana(), _ => SesionActual != null);
            SaveDownloadPdf = new RelayCommand(_ => GuardarPdf());
        }

        // ===== MÉTODOS =====
        private void AgregarPestana()
        {
            var nueva = new object(); // aquí deberías instanciar tu ViewModel de operativa
            Pestanas.Add(nueva);
            SesionActual = nueva;
        }

        private void EliminarPestana()
        {
            if (SesionActual != null)
            {
                Pestanas.Remove(SesionActual);
                SesionActual = null;
            }
        }

        private void GuardarPdf()
        {
            // ⚠ Aquí va tu lógica de exportar PDF
            System.Diagnostics.Debug.WriteLine("Guardar PDF ejecutado.");
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ===== RelayCommand =====
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
