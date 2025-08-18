using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

// Ajusta los namespaces si tu solución usa otros
using OperativaLogistica.Models;
using OperativaLogistica.Services;

namespace OperativaLogistica.ViewModels
{
    /// <summary>
    /// VM raíz de la aplicación.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ========= Eventos =========
        public event PropertyChangedEventHandler? PropertyChanged;

        // ========= Servicios =========
        public ConfigService Config { get; } = new ConfigService();
        public PdfService PdfService { get; } = new PdfService();

        // ========= Estado global =========
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

        // Colección de pestañas y la pestaña activa
        public ObservableCollection<TabViewModel> Pestanas { get; } = new ObservableCollection<TabViewModel>();

        private TabViewModel? _sesionActual;
        public TabViewModel? SesionActual
        {
            get => _sesionActual;
            set { _sesionActual = value; OnPropertyChanged(); }
        }

        // ========= Comandos =========
        public ICommand NuevaPestanaCommand { get; }
        public ICommand CerrarPestanaCommand { get; }
        public ICommand SaveJornadaPdfCommand { get; }

        // ========= Ctor =========
        public MainViewModel()
        {
            // Al menos una pestaña en blanco al iniciar
            NuevaPestanaCore();

            NuevaPestanaCommand   = new RelayCommand(_ => NuevaPestana());
            CerrarPestanaCommand  = new RelayCommand(_ => CerrarPestana(), _ => SesionActual != null);
            SaveJornadaPdfCommand = new RelayCommand(_ => SaveJornadaPdf(), _ => SesionActual != null);
        }

        // ========= Métodos expuestos =========

        /// <summary> Crea una nueva pestaña en blanco y la activa. </summary>
        public void NuevaPestana() => NuevaPestanaCore();

        /// <summary> Cierra la pestaña activa. </summary>
        public void CerrarPestana()
        {
            if (SesionActual is null) return;
            var idx = Pestanas.IndexOf(SesionActual);
            if (idx >= 0) Pestanas.RemoveAt(idx);
            SesionActual = Pestanas.Count > 0 ? Pestanas[Math.Max(0, idx - 1)] : null;
        }

        /// <summary>
        /// Punto de entrada si alguna acción quiere invocar el guardado PDF desde el VM.
        /// (El guardado real lo haces con diálogo en el code-behind usando PdfService).
        /// </summary>
        public void SaveJornadaPdf()
        {
            // Intencionadamente vacío (compatibilidad con bindings/comandos).
        }

        /// <summary>
        /// Placeholder por compatibilidad si tu XAML hace referencia a "Operaciones"
        /// como acción (no interfiere con la propiedad Operaciones de TabViewModel).
        /// </summary>
        public void Operaciones() { /* placeholder */ }

        // ========= Helpers internos =========

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
    /// VM de cada pestaña.
    /// </summary>
    public class TabViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private string _titulo = "Operativa";
        public string Titulo
        {
            get => _titulo;
            set { _titulo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Operacion> Operaciones { get; set; } = new ObservableCollection<Operacion>();
    }

    /// <summary>
    /// RelayCommand simple para comandos en el VM.
    /// </summary>
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
