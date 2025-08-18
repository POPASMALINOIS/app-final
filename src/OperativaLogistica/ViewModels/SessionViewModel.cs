using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Alias para que el atributo [RelayCommand] use el del Toolkit
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommandAttribute;

namespace OperativaLogistica.ViewModels
{
    /// <summary>
    /// ViewModel de sesión que maneja el estado del usuario
    /// y los comandos de inicio/cierre de sesión.
    /// </summary>
    public partial class SessionViewModel : ObservableObject
    {
        // --- PROPIEDADES OBSERVABLES ---
        // Estas generan automáticamente propiedad + notificación de cambio (INotifyPropertyChanged).

        [ObservableProperty]
        private string? usuario;

        [ObservableProperty]
        private bool sesionIniciada;

        [ObservableProperty]
        private string? mensajeEstado;


        // --- COMANDOS ---
        // Se generan automáticamente como ICommand al compilar.

        [RelayCommand]
        private void IniciarSesion()
        {
            if (!string.IsNullOrWhiteSpace(Usuario))
            {
                SesionIniciada = true;
                MensajeEstado = $"Sesión iniciada para {Usuario}.";
            }
            else
            {
                MensajeEstado = "Debe introducir un usuario.";
            }
        }

        [RelayCommand]
        private void CerrarSesion()
        {
            SesionIniciada = false;
            MensajeEstado = "Sesión cerrada.";
            Usuario = null;
        }

        [RelayCommand]
        private void RefrescarSesion()
        {
            if (SesionIniciada)
            {
                MensajeEstado = $"La sesión de {Usuario} sigue activa.";
            }
            else
            {
                MensajeEstado = "No hay sesión activa.";
            }
        }
    }
}
