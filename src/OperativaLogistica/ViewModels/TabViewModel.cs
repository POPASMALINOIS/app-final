using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OperativaLogistica.Models;

namespace OperativaLogistica.ViewModels
{
    public class TabViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private string _titulo = "Operativa";
        public string Titulo
        {
            get => _titulo;
            set { if (_titulo != value) { _titulo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); } }
        }

        // Alias opcional por si en algún XAML antiguo se usa "Title"
        public string Title
        {
            get => _titulo;
            set { if (_titulo != value) { _titulo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Titulo)); } }
        }

        private ObservableCollection<Operacion> _operaciones = new();
        public ObservableCollection<Operacion> Operaciones
        {
            get => _operaciones;
            set
            {
                if (!ReferenceEquals(_operaciones, value))
                {
                    _operaciones = value ?? new ObservableCollection<Operacion>();
                    OnPropertyChanged();
                }
            }
        }
    }
}
