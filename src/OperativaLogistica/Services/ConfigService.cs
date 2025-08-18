using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Servicio de configuración simple basado en JSON.
    /// Guarda ficheros en Escritorio\APP OPERATIVAS\config
    /// </summary>
    public class ConfigService
    {
        private readonly string _root;

        public ConfigService()
        {
            var desk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _root = Path.Combine(desk, "APP OPERATIVAS", "config");
            Directory.CreateDirectory(_root);
        }

        public string GetConfigPath(string fileName) => Path.Combine(_root, fileName);

        // ---------- Helpers genéricos ----------
        public T LoadOrCreate<T>(string filePath, Func<T> createDefault)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var obj = JsonSerializer.Deserialize<T>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (obj != null) return obj;
                }
                catch { /* si falla, devolvemos default */ }
            }

            var def = createDefault();
            Save(filePath, def);
            return def;
        }

        public void Save<T>(string filePath, T data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // ---------- Layout de columnas ----------
        public ColumnLayout LoadOrCreateColumnLayout(string lado)
        {
            var path = GetConfigPath($"columns_{lado}.json");
            return LoadOrCreate(path, () => new ColumnLayout());
        }

        public void SaveColumnLayout(string lado, IReadOnlyDictionary<string, double> widths)
        {
            var dto = new ColumnLayout
            {
                Widths = new Dictionary<string, double>(widths)
            };
            var path = GetConfigPath($"columns_{lado}.json");
            Save(path, dto);
        }
    }

    public class ColumnLayout
    {
        public Dictionary<string, double> Widths { get; set; } = new Dictionary<string, double>();
    }
}
