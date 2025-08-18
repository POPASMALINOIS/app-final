using System.Collections.Generic;
using System.Windows.Controls;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Aplica y guarda el ancho de columnas del DataGrid por LADO.
    /// </summary>
    public class ColumnLayoutService
    {
        private readonly ConfigService _config;

        public ColumnLayoutService(ConfigService config) => _config = config;

        public void ApplyColumnLayout(DataGrid grid, string lado)
        {
            var layout = _config.LoadOrCreateColumnLayout(lado);
            foreach (var col in grid.Columns)
            {
                var key = col.Header?.ToString();
                if (!string.IsNullOrWhiteSpace(key) &&
                    layout.Widths.TryGetValue(key!, out var w) && w > 0)
                {
                    col.Width = w;
                }
            }
        }

        public void SaveColumnLayout(DataGrid grid, string lado)
        {
            var map = new Dictionary<string, double>();
            foreach (var col in grid.Columns)
            {
                var key = col.Header?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    // ActualWidth da el ancho real en píxeles
                    map[key!] = col.ActualWidth;
                }
            }
            _config.SaveColumnLayout(lado, map);
        }
    }
}
