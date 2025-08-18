using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Servicio muy simple de configuración persistida en JSON.
    /// Carpeta: Escritorio\APP OPERATIVAS\config.json
    /// Contenido: anchos de columna, colores por estado, etc.
    /// </summary>
    public class ConfigService
    {
        private const string AppFolderName = "APP OPERATIVAS";
        private const string ConfigFileName = "config.json";

        private readonly string _appFolderPath;
        private readonly string _configPath;

        private ConfigDto _config = new ConfigDto();

        public ConfigService()
        {
            _appFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppFolderName);
            _configPath = Path.Combine(_appFolderPath, ConfigFileName);
            Directory.CreateDirectory(_appFolderPath);
            Load();
        }

        public IReadOnlyDictionary<string, double> ColumnWidths => _config.ColumnWidths;
        public IReadOnlyDictionary<string, string> EstadoColors => _config.EstadoColors;

        public void SetColumnWidth(string columnKey, double width)
        {
            if (string.IsNullOrWhiteSpace(columnKey)) return;
            _config.ColumnWidths[columnKey] = width;
            Save();
        }

        public double GetColumnWidth(string columnKey, double fallback = 120)
        {
            if (string.IsNullOrWhiteSpace(columnKey)) return fallback;
            return _config.ColumnWidths.TryGetValue(columnKey, out var w) ? w : fallback;
        }

        public void SetEstadoColor(string estado, string colorHex)
        {
            if (string.IsNullOrWhiteSpace(estado)) return;
            _config.EstadoColors[estado] = colorHex;
            Save();
        }

        public string GetEstadoColor(string estado, string fallback = "#FFFFFF")
        {
            if (string.IsNullOrWhiteSpace(estado)) return fallback;
            return _config.EstadoColors.TryGetValue(estado, out var c) ? c : fallback;
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<ConfigDto>(json) ?? new ConfigDto();
                }
                else
                {
                    // Valores por defecto
                    _config = new ConfigDto
                    {
                        EstadoColors = new Dictionary<string, string>
                        {
                            // Config colores por defecto
                            ["CARGANDO"] = "#FFF59D",
                            ["OK"]       = "#C8E6C9",
                            ["ANULADO"]  = "#FFCDD2",
                            ["RETRASO TRANSPORTISTA"] = "#FFCDD2",
                            ["RETRASO DOCUMENTACION"] = "#FFCDD2",
                            ["INCIDENCIA REMOLQUE"]   = "#FFCDD2",
                            ["SIN INCIDENCIAS"]       = "#FFFFFF"
                        }
                    };
                    Save();
                }
            }
            catch
            {
                // Si hay problema leyendo JSON, usamos defaults
                _config = new ConfigDto();
            }
        }

        private sealed class ConfigDto
        {
            public Dictionary<string, double> ColumnWidths { get; set; } = new();
            public Dictionary<string, string> EstadoColors { get; set; } = new();
        }
    }
}
