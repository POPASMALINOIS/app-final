using System.Linq;
using System.Collections.Generic;

namespace OperativaLogistica.ViewModels
{
    /// <summary>
    /// Proveedor de opciones para el desplegable "LADO" (LADO 0 ... LADO 9).
    /// En XAML: ItemsSource="{Binding Source={x:Static vm:TabLadosProvider.Items}}"
    /// </summary>
    public static class TabLadosProvider
    {
        // Lista inmutable de opciones
        public static IReadOnlyList<string> Items { get; } =
            Enumerable.Range(0, 10).Select(i => $"LADO {i}").ToArray();
    }
}
