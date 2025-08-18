using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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
        // ¡OJO! Solo UNA propiedad 'Config' para evitar el CS0102.
        public ConfigService Config { get; }
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

        // Pestañas y pestaña activa
        public ObservableCollection<TabViewModel> Pestanas { get; } = new();

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

        // ========= Constructores =========
        public MainViewModel() : this(new ConfigService())
        {
        }

        public MainViewModel(ConfigService config)
        {
            Config = config;

            // Al menos una pestaña al iniciar
            NuevaPestanaCore();

            NuevaPestanaCommand   = new RelayCommand(_ => NuevaPestana());
            CerrarPestanaCommand  = new RelayCommand(_ => CerrarPestana(), _ => SesionActual != null);
            SaveJornadaPdfCommand = new RelayCommand(_ => SaveJornadaPdf(), _ => SesionActual != null);
        }

        // ========= API pública usada por la vista =========
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
        /// Punto de entrada opcional si quieres lanzar la exportación a PDF desde Command.
        /// El code-behind puede llamar a PdfService directamente con el diálogo de guardado.
        /// </summary>
        public void SaveJornadaPdf()
        {
            // Aquí no forzamos la ruta de guardado para no interferir con el diálogo UI.
            // Deja esta función como “hook” si la necesitas desde XAML.
        }

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
    /// RelayCommand para comandos simples en el VM.
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
