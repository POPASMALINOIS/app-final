using System.Linq;
using System.Windows.Controls;

namespace OperativaLogistica.Services
{
    public static class ColumnLayoutService
    {
        public static void Apply(DataGrid grid, ConfigService cfg)
        {
            foreach (var col in grid.Columns)
            {
                if (!string.IsNullOrEmpty(col.Header?.ToString()) &&
                    cfg.ColumnWidths.TryGetValue(col.Header.ToString()!, out var w))
                {
                    col.Width = w;
                }
            }
        }

        public static void Capture(DataGrid grid, ConfigService cfg)
        {
            foreach (var col in grid.Columns)
            {
                var key = col.Header?.ToString();
                if (!string.IsNullOrEmpty(key))
                    cfg.ColumnWidths[key!] = col.ActualWidth;
            }
            cfg.SaveColumnLayout();
        }
    }
}
