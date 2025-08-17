using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OperativaLogistica.Services
{
    public class ConfigService
    {
        public Dictionary<string, string> Mapping { get; set; } = new();

        public Dictionary<string, string> EstadoColors { get; set; } = new()
        {
            ["OK"] = "#FF2E7D32",
            ["CARGANDO"] = "#FFFFA000",
            ["ANULADO"] = "#FFB71C1C"
        };

        public Dictionary<string, string> MuelleColors { get; set; } = new();

        public Dictionary<string, double> ColumnWidths { get; set; } = new();

        public static ConfigService LoadOrCreate()
        {
            AppPaths.Ensure();
            var cfg = new ConfigService();

            if (File.Exists(AppPaths.MappingJson))
            {
                try
                {
                    var json = File.ReadAllText(AppPaths.MappingJson);
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (map != null) cfg.Mapping = map;
                }
                catch { }
            }

            if (File.Exists(AppPaths.ColorsJson))
            {
                try
                {
                    var json = File.ReadAllText(AppPaths.ColorsJson);
                    var tmp = JsonSerializer.Deserialize<ConfigService>(json);
                    if (tmp != null)
                    {
                        if (tmp.EstadoColors?.Count > 0) cfg.EstadoColors = tmp.EstadoColors;
                        if (tmp.MuelleColors != null) cfg.MuelleColors = tmp.MuelleColors;
                    }
                }
                catch { }
            }

            if (File.Exists(AppPaths.ColumnLayoutJson))
            {
                try
                {
                    var json = File.ReadAllText(AppPaths.ColumnLayoutJson);
                    var widths = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                    if (widths != null) cfg.ColumnWidths = widths;
                }
                catch { }
            }

            return cfg;
        }

        public void SaveMapping() =>
            File.WriteAllText(AppPaths.MappingJson, JsonSerializer.Serialize(Mapping, new JsonSerializerOptions { WriteIndented = true }));

        public void SaveColors() =>
            File.WriteAllText(AppPaths.ColorsJson, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

        public void SaveColumnLayout() =>
            File.WriteAllText(AppPaths.ColumnLayoutJson, JsonSerializer.Serialize(ColumnWidths, new JsonSerializerOptions { WriteIndented = true }));
    }
}
