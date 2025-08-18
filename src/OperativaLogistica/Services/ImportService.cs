using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Importación de operativas desde CSV (y fallback simple para .xlsx renombrados a .csv).
    /// - Auto-mapea por cabeceras conocidas.
    /// - Rellena Fecha con el parámetro 'fecha'.
    /// - El parámetro 'lado' se usa tal cual en caso de que lo quieras registrar en Observaciones.
    /// 
    /// NOTA: Para simplificar la build en GitHub Actions no dependemos de librerías externas.
    ///       Si el fichero es .xlsx sugiere exportarlo a .csv y volver a importar.
    /// </summary>
    public class ImportService
    {
        public List<Operacion> Importar(string filePath, DateTime fecha, string lado)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".csv")
                return ImportarCsv(filePath, fecha, lado);

            // Fallback: si no es CSV, intentamos leerlo como texto con separador de comas
            // (muchos Excel guardados como .xlsx pero que en realidad llevan CSV).
            try
            {
                return ImportarCsv(filePath, fecha, lado);
            }
            catch
            {
                throw new NotSupportedException(
                    "Formato no soportado. Por favor exporta la hoja a CSV (separado por comas) y vuelve a importar.");
            }
        }

        private List<Operacion> ImportarCsv(string filePath, DateTime fecha, string lado)
        {
            var ops = new List<Operacion>();
            using var sr = new StreamReader(filePath);

            // Lee cabecera
            var header = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                return ops;

            var headers = SplitCsvLine(header).Select(h => h.Trim().ToUpperInvariant()).ToArray();

            // Func para buscar índice por nombre aproximado
            int idx(params string[] names)
            {
                foreach (var n in names)
                {
                    var j = Array.FindIndex(headers, h => h.Contains(n.ToUpperInvariant()));
                    if (j >= 0) return j;
                }
                return -1;
            }

            int iTransportista = idx("TRANSPORTISTA");
            int iMatricula     = idx("MATRICULA", "MATRÍCULA", "PLACA");
            int iMuelle        = idx("MUELLE", "DOCK");
            int iEstado        = idx("ESTADO", "STATUS");
            int iDestino       = idx("DESTINO");
            int iLlegada       = idx("LLEGADA", "HORA LLEGADA", "HORA ENTRADA");
            int iSalidaTope    = idx("SALIDA TOPE", "HORA SALIDA TOPE", "TOPE");
            int iObs           = idx("OBSERVACIONES", "OBS");
            int iInc           = idx("INCIDENCIAS", "INCIDENCIA");
            int iPrecinto      = idx("PRECINTO");
            // Puedes añadir más campos si tu hoja tiene otros nombres

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsvLine(line);

                string get(int index)
                    => (index >= 0 && index < cols.Count) ? cols[index]?.Trim() ?? "" : "";

                var op = new Operacion
                {
                    Transportista = get(iTransportista),
                    Matricula     = get(iMatricula),
                    Muelle        = get(iMuelle),
                    Estado        = get(iEstado),
                    Destino       = get(iDestino),
                    Llegada       = NormalizaHora(get(iLlegada)),
                    SalidaTope    = NormalizaHora(get(iSalidaTope)),
                    Observaciones = get(iObs),
                    Incidencias   = get(iInc),
                    Precinto      = get(iPrecinto),
                    Fecha         = fecha
                };

                ops.Add(op);
            }

            return ops;
        }

        private static string NormalizaHora(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return "";
            // intenta HH:mm o HH.mm o H:mm
            var v = val.Trim().Replace('.', ':');
            if (TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out var ts))
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}";
            return val;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(line)) return res;

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // doble comilla -> comilla literal
                        current.Append('"'); i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    res.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            res.Add(current.ToString());
            return res;
        }
    }
}
