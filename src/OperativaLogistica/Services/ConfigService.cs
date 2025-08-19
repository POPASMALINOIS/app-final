using System.Collections.Generic;
using System.Linq;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Configuración de la app. Genera la lista de LADOS de forma dinámica.
    /// Cambia los límites para mostrar más/menos lados.
    /// </summary>
    public class ConfigService
    {
        public int FirstLado { get; }
        public int LastLado  { get; }

        /// <summary>Lista de lados: "LADO 0"..."LADO 9" por defecto.</summary>
        public IReadOnlyList<string> Lados { get; }

        public ConfigService(int firstLado = 0, int lastLado = 9)
        {
            FirstLado = firstLado;
            LastLado  = lastLado;

            Lados = Enumerable.Range(FirstLado, LastLado - FirstLado + 1)
                              .Select(i => $"LADO {i}")
                              .ToList();
        }
    }
}
