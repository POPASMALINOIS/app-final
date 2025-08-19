using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OperativaLogistica.Models;
using OperativaLogistica.Services;

namespace OperativaLogistica.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ========= CONFIG (LADOS 0..9) =========
        public ConfigService Config { get; }

        private string _ladoSeleccionado = string.Empty;
        /// <summary>Lado seleccionado en la barra superior.</summary>
        public string LadoSeleccionado
        {
            get => _ladoSeleccionado;
            set
            {
                if (_ladoSeleccionado != value)
                {
                    _ladoSeleccionado = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _fechaActual = DateTime.Today;
        /// <summary>Fecha seleccionada en la barra superior.</summary>
        public DateTime FechaActual
        {
            get => _fechaActual;
            set
            {
                if (_fechaActual != value)
                {
                    _fechaActual = value;
                    OnPropertyChanged();
                }
            }
        }

        // ========= PESTAÑAS =========
        public ObservableCollection<TabViewModel> Pestañas { get; } = new();

        private TabViewModel? _sesionActual;
        /// <summary>Pestaña activa (la que se muestra en el DataGrid).</summary>
        public TabViewModel? SesionActual
        {
            get => _sesionActual;
            set
            {
                if (_sesionActual != value)
                {
                    _sesionActual = value;
                    OnPropertyChanged();
                }
            }
        }

        // ========= SERVICIOS =========
        /// <summary>Servicio de PDF usado por MainWindow.xaml.cs.</summary>
        public PdfService PdfService { get; }

        // ========= CTOR =========
        public MainViewModel()
        {
            // LADOS 0..9 (si quisieras 0..12: new ConfigService(0, 12))
            Config = new ConfigService(0, 9);

            // Valores iniciales
            LadoSeleccionado = Config.Lados.Count > 0 ? Config.Lados[0] : "LADO 0";
            FechaActual      = DateTime.Today;

            // Servicio PDF (si ya lo registras en otro sitio, puedes inyectarlo)
            PdfService = new PdfService();

            // Asegura una pestaña inicial en blanco
            NuevaPestana();
        }

        // ========= COMANDOS SENCILLOS =========
        public void NuevaPestana()
        {
            var tab = new TabViewModel
            {
                Titulo = $"Pestaña {Pestañas.Count + 1}",
                Operaciones = new ObservableCollection<Operacion>()
            };

            Pestañas.Add(tab);
            SesionActual = tab;
        }

        public void CerrarPestana()
        {
            if (SesionActual is null) return;

            var idx = Pestañas.IndexOf(SesionActual);
            if (idx >= 0)
            {
                Pestañas.RemoveAt(idx);
                SesionActual = Pestañas.Count > 0
                    ? Pestañas[Math.Clamp(idx - 1, 0, Pestañas.Count - 1)]
                    : null;
            }
        }
    }

    /// <summary>
    /// ViewModel de cada pestaña. Si ya tienes este tipo en otro archivo, deja solo el tuyo.
    /// </summary>
    public class TabViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _titulo = "Pestaña";
        public string Titulo
        {
            get => _titulo;
            set { if (_titulo != value) { _titulo = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<Operacion> _operaciones = new();
        public ObservableCollection<Operacion> Operaciones
        {
            get => _operaciones;
            set { if (_operaciones != value) { _operaciones = value; OnPropertyChanged(); } }
        }
    }
}
