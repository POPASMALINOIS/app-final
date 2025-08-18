using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OperativaLogistica.Models;

namespace OperativaLogistica.ViewModels
{
    public class TabViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }

        private ObservableCollection<Operacion> _operaciones = new();
        public ObservableCollection<Operacion> Operaciones
        {
            get => _operaciones;
            set
            {
                if (_operaciones != value)
                {
                    _operaciones = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
