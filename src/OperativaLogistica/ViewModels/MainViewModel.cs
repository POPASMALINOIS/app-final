using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OperativaLogistica.Models;
using OperativaLogistica.Services;

namespace OperativaLogistica.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ConfigService Config { get; } = new ConfigService();
        public PdfService PdfService { get; } = new PdfService();

        private DateTime _fechaActual = DateTime.Today;
        public DateTime FechaActual
        {
            get => _fechaActual;
            set { _fechaActual = value; OnPropertyChanged(); }
        }

        private string _ladoSeleccionado = "LADO 0";
        public string LadoSeleccionado
        {
            get => _ladoSeleccionado;
            set { _ladoSeleccionado = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TabViewModel> Pestanas { get; } = new();

        private TabViewModel? _sesionActual;
        public TabViewModel? SesionActual
        {
            get => _sesionActual;
            set { _sesionActual = value; OnPropertyChanged(); }
        }

        public ICommand NuevaPestanaCommand { get; }
        public ICommand CerrarPestanaCommand { get; }
        public ICommand SaveJornadaPdfCommand { get; }

        public MainViewModel()
        {
            NuevaPestanaCore();

            NuevaPestanaCommand   = new RelayCommand(_ => NuevaPestana());
            CerrarPestanaCommand  = new RelayCommand(_ => CerrarPestana(), _ => SesionActual != null);
            SaveJornadaPdfCommand = new RelayCommand(_ => SaveJornadaPdf(), _ => SesionActual != null);
        }

        public void NuevaPestana() => NuevaPestanaCore();

        public void CerrarPestana()
        {
            if (SesionActual is null) return;
            var idx = Pestanas.IndexOf(SesionActual);
            if (idx >= 0) Pestanas.RemoveAt(idx);
            SesionActual = Pestanas.Count > 0 ? Pestanas[Math.Max(0, idx - 1)] : null;
        }

        public void SaveJornadaPdf()
        {
            // Se lanza desde el code-behind con diálogo.
        }

        public void Config() { }
        public void Operaciones() { }

        private void NuevaPestanaCore()
        {
            var titulo = !string.IsNullOrWhiteSpace(LadoSeleccionado) ? LadoSeleccionado : "Operativa";
            var tab = new TabViewModel
            {
                Titulo = titulo,
                Operaciones = new ObservableCollection<Operacion>()
            };
            Pestanas.Add(tab);
            SesionActual = tab;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    /// <summary>
    /// PESTAÑA: ahora Operaciones notifica cuando se REASIGNA la colección.
    /// </summary>
    public class TabViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private string _titulo = "Operativa";
        public string Titulo
        {
            get => _titulo;
            set { _titulo = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Operacion> _operaciones = new();
        public ObservableCollection<Operacion> Operaciones
        {
            get => _operaciones;
            set
            {
                if (!ReferenceEquals(_operaciones, value))
                {
                    _operaciones = value;
                    OnPropertyChanged(); // <— notifica el cambio de referencia
                }
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
