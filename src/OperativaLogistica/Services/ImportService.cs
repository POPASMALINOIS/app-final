using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ImportService
    {
        public IEnumerable<Operacion> Importar(string filePath, DateOnly fecha, string lado)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".csv"  => ImportCsv(filePath, fecha, lado),
                ".xls"  => ImportExcel(filePath, fecha, lado),
                ".xlsx" => ImportExcel(filePath, fecha, lado),
                _       => Array.Empty<Operacion>()
            };
        }

        // ---------------- CSV ----------------
        private static IEnumerable<Operacion> ImportCsv(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            if (!File.Exists(filePath)) return list;

            using var sr = new StreamReader(filePath);
            string? headerLine = sr.ReadLine();
            if (headerLine == null) return list;

            // Normaliza cabeceras (quita espacios, tildes, mayúsculas)
            var headers = headerLine.Split(';')
                .Select(h => Normalize(h))
                .ToArray();

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');

                list.Add(CreateOperacionFromArray(cols, headers, fechaPorDefecto, ladoPorDefecto));
            }
            return list;
        }

        // ---------------- Excel (ClosedXML) ----------------
        private static IEnumerable<Operacion> ImportExcel(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            if (!File.Exists(filePath)) return list;

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) return list;

            var used = ws.RangeUsed();
            if (used == null) return list;

            int r0 = used.FirstRow().RowNumber();
            int c0 = used.FirstColumn().ColumnNumber();
            int r1 = used.LastRow().RowNumber();
            int c1 = used.LastColumn().ColumnNumber();

            // Normaliza cabeceras
            var headers = new Dictionary<int, string>();
            for (int c = c0; c <= c1; c++)
            {
                var raw = ws.Cell(r0, c).GetString();
                if (!string.IsNullOrWhiteSpace(raw))
                    headers[c] = Normalize(raw);
            }

            for (int r = r0 + 1; r <= r1; r++)
            {
                var cols = new string[c1 - c0 + 1];
                for (int c = c0; c <= c1; c++)
                    cols[c - c0] = ws.Cell(r, c).GetString();

                var headerNames = headers.Values.ToArray();
                list.Add(CreateOperacionFromArray(cols, headerNames, fechaPorDefecto, ladoPorDefecto));
            }

            return list;
        }

        // ---------------- Helpers ----------------

        private static Operacion CreateOperacionFromArray(string[] cols, string[] headers, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            string Get(string name)
            {
                int idx = Array.FindIndex(headers, h => h == Normalize(name));
                return (idx >= 0 && idx < cols.Length) ? cols[idx].Trim() : "";
            }

            return CreateOperacion(
                Get("Id"),
                Get("Transportista"),
                Get("Matricula"),
                Get("Muelle"),
                Get("Estado"),
                Get("Destino"),
                Get("Llegada"),
                Get("Llegada Real"),
                Get("Salida Real"),
                Get("Salida Tope"),
                Get("Observaciones"),
                Get("Incidencias"),
                Get("Fecha"),
                Get("Precinto"),
                Get("Lex"),
                Get("Lado"),
                fechaPorDefecto,
                ladoPorDefecto
            );
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim().ToUpperInvariant();

            // elimina tildes
            s = s.Normalize(NormalizationForm.FormD);
            var chars = s.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        private static Operacion CreateOperacion(
            string id, string transportista, string matricula, string muelle, string estado, string destino,
            string llegada, string llegadaReal, string salidaReal, string salidaTope,
            string observaciones, string incidencias, string fecha, string precinto, string lex, string lado,
            DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            static DateOnly ParseFecha(string s, DateOnly fallback)
            {
                if (string.IsNullOrWhiteSpace(s)) return fallback;

                if (DateOnly.TryParse(s, out var d)) return d;
                if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);

                return fallback;
            }

            static bool ParseBool(string s)
            {
                var t = (s ?? "").Trim();
                return t.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("sí", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("si", StringComparison.OrdinalIgnoreCase)
                    || t == "1" || t.Equals("x", StringComparison.OrdinalIgnoreCase);
            }

            return new Operacion
            {
                Id            = int.TryParse(id, out var idVal) ? idVal : 0,
                Transportista = transportista,
                Matricula     = matricula,
                Muelle        = muelle,
                Estado        = estado,
                Destino       = destino,

                Llegada       = llegada,
                LlegadaReal   = llegadaReal,
                SalidaReal    = salidaReal,
                SalidaTope    = salidaTope,

                Observaciones = observaciones,
                Incidencias   = incidencias,

                Fecha         = ParseFecha(fecha, fechaPorDefecto),
                Precinto      = precinto,
                Lex           = ParseBool(lex),
                Lado          = string.IsNullOrWhiteSpace(lado) ? ladoPorDefecto : lado
            };
        }
    }
}
