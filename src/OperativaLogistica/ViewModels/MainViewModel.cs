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
    /// VM raíz de la aplicación. Expone todas las propiedades/métodos que
    /// el XAML y el code-behind están intentando usar (según tus logs de build).
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ========= Eventos =========
        public event PropertyChangedEventHandler? PropertyChanged;

        // ========= Servicios expuestos (para que compile todo el XAML/CodeBehind) =========
        // Si ya los tienes implementados en tu proyecto, estos getters solo referencian a los tuyos.
        // Si no existen, crea stubs con esos nombres en OperativaLogistica.Services.
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

        // ========= Comandos opcionales =========
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

        // ========= Métodos que el XAML/CodeBehind están esperando =========

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
        /// Guardar PDF de la jornada actual. El CodeBehind puede llamar a PdfService directamente,
        /// pero dejamos aquí el método para bindings que lo invoquen.
        /// </summary>
        public void SaveJornadaPdf()
        {
            // Punto de entrada opcional si quieres lanzar la exportación desde un Command.
            // El SavePdf_Click del code-behind ya usa PdfService.SaveJornadaPdf(file, ops, fecha, lado).
            // Aquí no forzamos ruta para no interferir con el guardado vía diálogo.
        }

        /// <summary>
        /// Métodos placeholders para bindings que hacen referencia a "Config" u "Operaciones" como acción.
        /// No realizan lógica, pero evitan errores del compilador si el XAML los invoca.
        /// </summary>
        public void Config() { /* placeholder por compatibilidad */ }

        public void Operaciones() { /* placeholder por compatibilidad */ }

        // ========= Helpers internos =========

        private void NuevaPestanaCore()
        {
            var titulo = !string.IsNullOrWhiteSpace(LadoSeleccionado) ? LadoSeleccionado : "Operativa";
            var tab = new TabViewModel
            {
                Titulo = titulo,
                // Arrancamos con lista vacía
                Operaciones = new ObservableCollection<Operacion>()
            };
            Pestanas.Add(tab);
            SesionActual = tab;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    /// <summary>
    /// VM de cada pestaña. Debe exponer Operaciones para que el XAML
    /// pueda bindear ItemsSource="{Binding Operaciones}" sin error.
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
    /// RelayCommand muy simple para usar comandos en el VM.
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
