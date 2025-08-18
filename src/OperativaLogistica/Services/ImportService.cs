using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ImportService
    {
        /// <summary>
        /// Importa operaciones desde un archivo. Soporta CSV con separador ';'.
        /// Las columnas esperadas (cabecera) son:
        /// Id;Transportista;Matricula;Muelle;Estado;Destino;Llegada;LlegadaReal;SalidaReal;SalidaTope;
        /// Observaciones;Incidencias;Fecha;Precinto;Lex;Lado
        /// </summary>
        public IEnumerable<Operacion> Importar(string filePath, DateOnly fecha, string lado)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".csv")
                return ImportCsv(filePath, fecha, lado);

            // Si quieres soportar Excel, aquí podrías llamar a ClosedXML.
            // De momento devolvemos vacío para otras extensiones.
            return Array.Empty<Operacion>();
        }

        private static IEnumerable<Operacion> ImportCsv(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            if (!File.Exists(filePath)) return list;

            using var sr = new StreamReader(filePath);
            string? line = sr.ReadLine(); // cabecera (opcional)
            int lineNo = 1;

            while ((line = sr.ReadLine()) != null)
            {
                lineNo++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(';');
                // Asegura mínimo de columnas
                if (cols.Length < 16) Array.Resize(ref cols, 16);

                DateOnly ParseFecha(string s, DateOnly fallback)
                {
                    if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var d)) return d;

                    // Intenta dd/MM/yyyy
                    if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out d)) return d;

                    return fallback;
                }

                try
                {
                    var op = new Operacion
                    {
                        Id            = int.TryParse(cols[0], out var idVal) ? idVal : 0,
                        Transportista = cols[1] ?? string.Empty,
                        Matricula     = cols[2] ?? string.Empty,
                        Muelle        = cols[3] ?? string.Empty,
                        Estado        = cols[4] ?? string.Empty,
                        Destino       = cols[5] ?? string.Empty,
                        Llegada       = cols[6] ?? string.Empty,
                        LlegadaReal   = cols[7] ?? string.Empty,
                        SalidaReal    = cols[8] ?? string.Empty,
                        SalidaTope    = cols[9] ?? string.Empty,
                        Observaciones = cols[10] ?? string.Empty,
                        Incidencias   = cols[11] ?? string.Empty,
                        Fecha         = ParseFecha(cols[12] ?? string.Empty, fechaPorDefecto),
                        Precinto      = cols[13] ?? string.Empty,
                        Lex           = (cols[14] ?? string.Empty).Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                                        || (cols[14] ?? string.Empty).Trim() == "1",
                        Lado          = string.IsNullOrWhiteSpace(cols[15]) ? ladoPorDefecto : cols[15]
                    };

                    list.Add(op);
                }
                catch
                {
                    // Si una fila viene mal formada, la saltamos para no romper toda la importación.
                    // (Podrías loguear lineNo y la línea si te interesa)
                }
            }

            return list;
        }
    }
}
