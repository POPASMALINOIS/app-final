using System.Windows.Controls;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Stub sin lógica real. Evita errores de compilación por referencias a métodos inexistentes
    /// de ConfigService. Completar en el futuro si quieres persistir el layout de columnas.
    /// </summary>
    public class ColumnLayoutService
    {
        public ColumnLayoutService(ConfigService _)
        {
            // no-op
        }

        public void LoadOrCreateColumnLayout(DataGrid _)
        {
            // no-op (antes delegaba en ConfigService.LoadOrCreateColumnLayout)
        }

        public void SaveColumnLayout(DataGrid _)
        {
            // no-op (antes delegaba en ConfigService.SaveColumnLayout)
        }
    }
}
