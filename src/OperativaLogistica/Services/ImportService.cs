using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Importación muy sencilla de CSV/Excel. Para Excel se intenta
    /// CSV fallback si el usuario lo guarda como CSV.
    /// </summary>
    public class ImportService
    {
        /// <summary>
        /// Importa al <paramref name="destino"/> las filas de <paramref name="filePath"/>.
        /// La fecha y el lado indicados se asignan a cada operación importada.
        /// </summary>
        public void Importar(ObservableCollection<Operacion> destino, string filePath, DateOnly fecha, string lado)
        {
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("No se encontró el archivo a importar.", filePath);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            // De momento soportamos CSV directamente. Si es XLS/XLSX pedimos CSV.
            if (ext is ".csv" or ".txt")
            {
                ImportarCsv(destino, filePath, fecha, lado);
                return;
            }

            // Fallback: no rompemos el flujo, sólo informamos.
            // Puedes conectar aquí tu lector Excel (EPPlus/ClosedXML) si lo prefieres.
            throw new NotSupportedException("Por ahora el importador espera un CSV. Guarda el Excel como CSV e inténtalo de nuevo.");
        }

        private static void ImportarCsv(ObservableCollection<Operacion> destino, string filePath, DateOnly fecha, string lado)
        {
            destino.Clear();

            using var fs = File.OpenRead(filePath);
            using var sr = new StreamReader(fs);

            // Intentamos detectar si hay cabecera
            var firstLine = sr.ReadLine();
            if (firstLine is null) return;

            bool tieneCabecera = firstLine.Contains("TRANSPORTISTA", StringComparison.OrdinalIgnoreCase)
                              || firstLine.Contains("MATRICULA", StringComparison.OrdinalIgnoreCase);

            if (!tieneCabecera)
            {
                // La primera línea ya es dato
                ParseAndAdd(destino, firstLine, fecha, lado);
            }

            // Resto de líneas
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                ParseAndAdd(destino, line!, fecha, lado);
            }
        }

        /// <summary>
        /// Parser muy tolerante: asume columnas separadas por ; o , en orden habitual.
        /// Ajusta aquí si tu plantilla cambia.
        /// </summary>
        private static void ParseAndAdd(ObservableCollection<Operacion> destino, string line, DateOnly fecha, string lado)
        {
            var cols = line.Split(new[] { ';', ',' }, StringSplitOptions.None);

            // Protegemos accesos fuera de rango
            string Get(int i) => i >= 0 && i < cols.Length ? cols[i].Trim() : string.Empty;

            var op = new Operacion
            {
                Transportista = Get(0),
                Matricula     = Get(1),
                Muelle        = Get(2),
                Estado        = Get(3),
                Destino       = Get(4),
                Llegada       = Get(5),
                LlegadaReal   = Get(6),
                SalidaReal    = Get(7),
                SalidaTope    = Get(8),
                Observaciones = Get(9),
                Incidencias   = Get(10),
                // Campos que nos piden los logs:
                Fecha         = fecha,
                Lado          = string.IsNullOrWhiteSpace(Get(11)) ? lado : Get(11)
            };

            destino.Add(op);
        }
    }
}
