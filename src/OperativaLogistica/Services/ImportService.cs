using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ImportService
    {
        /// <summary>
        /// Importa operaciones desde un archivo:
        ///  - CSV con separador ';'
        ///  - Excel (XLS/XLSX) con cabeceras en la primera fila
        /// Cabeceras admitidas (case-insensitive):
        /// Id, Transportista, Matricula, Muelle, Estado, Destino,
        /// Llegada, Llegada Real, Salida Real, Salida Tope,
        /// Observaciones, Incidencias, Fecha, Precinto, Lex, Lado
        /// </summary>
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
            string? line = sr.ReadLine(); // header
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (cols.Length < 16) Array.Resize(ref cols, 16);

                list.Add(CreateOperacion(
                    cols[0], cols[1], cols[2], cols[3], cols[4], cols[5],
                    cols[6], cols[7], cols[8], cols[9],
                    cols[10], cols[11], cols[12], cols[13], cols[14], cols[15],
                    fechaPorDefecto, ladoPorDefecto));
            }
            return list;
        }

        // ---------------- Excel (ClosedXML) ----------------

        private static IEnumerable<Operacion> ImportExcel(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            if (!File.Exists(filePath)) return list;

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.Count > 0 ? wb.Worksheet(1) : null;
            if (ws == null) return list;

            var used = ws.RangeUsed();
            if (used is null) return list;

            // límites del rango usado (1-based)
            int r0 = used.FirstRow().RowNumber();
            int c0 = used.FirstColumn().ColumnNumber();
            int r1 = used.LastRow().RowNumber();
            int c1 = used.LastColumn().ColumnNumber();

            // Mapa de cabeceras -> columna (1-based dentro de [c0..c1])
            var headerRow = ws.Row(r0).Cells(c0, c1);
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = c0; c <= c1; c++)
            {
                var raw = ws.Cell(r0, c).GetString().Trim();
                if (string.IsNullOrEmpty(raw)) continue;
                var key = raw.ToUpperInvariant();
                map[key] = c;
            }

            // Asegura claves esperadas (si no están, no se accede a celda)
            string[] expected =
            {
                "ID","TRANSPORTISTA","MATRICULA","MUELLE","ESTADO","DESTINO",
                "LLEGADA","LLEGADA REAL","SALIDA REAL","SALIDA TOPE",
                "OBSERVACIONES","INCIDENCIAS","FECHA","PRECINTO","LEX","LADO"
            };
            foreach (var k in expected) map.TryAdd(k, -1); // -1 indica "no existe"

            // Helpers seguros
            string GetStr(int row, string header)
            {
                if (!map.TryGetValue(header, out int c)) return "";
                if (c < c0 || c > c1) return "";
                return ws.Cell(row, c).GetString().Trim();
            }

            string GetFechaStr(int row)
            {
                if (!map.TryGetValue("FECHA", out int c)) return "";
                if (c < c0 || c > c1) return "";
                var cell = ws.Cell(row, c);
                if (TryGetDateOnly(cell, out var d))
                    return d.ToString("yyyy-MM-dd");
                return cell.GetString().Trim();
            }

            bool RowIsEmpty(int row)
            {
                for (int c = c0; c <= c1; c++)
                    if (!string.IsNullOrWhiteSpace(ws.Cell(row, c).GetString()))
                        return false;
                return true;
            }

            // Recorre filas de datos
            for (int r = r0 + 1; r <= r1; r++)
            {
                if (RowIsEmpty(r)) continue;

                var op = CreateOperacion(
                    GetStr(r, "ID"),
                    GetStr(r, "TRANSPORTISTA"),
                    GetStr(r, "MATRICULA"),
                    GetStr(r, "MUELLE"),
                    GetStr(r, "ESTADO"),
                    GetStr(r, "DESTINO"),
                    GetStr(r, "LLEGADA"),
                    GetStr(r, "LLEGADA REAL"),
                    GetStr(r, "SALIDA REAL"),
                    GetStr(r, "SALIDA TOPE"),
                    GetStr(r, "OBSERVACIONES"),
                    GetStr(r, "INCIDENCIAS"),
                    GetFechaStr(r),
                    GetStr(r, "PRECINTO"),
                    GetStr(r, "LEX"),
                    GetStr(r, "LADO"),
                    fechaPorDefecto, ladoPorDefecto);

                list.Add(op);
            }

            return list;
        }

        private static bool TryGetDateOnly(IXLCell cell, out DateOnly d)
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                d = DateOnly.FromDateTime(cell.GetDateTime());
                return true;
            }

            var s = cell.GetString().Trim();
            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) return true;
            if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) return true;

            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
            {
                d = DateOnly.FromDateTime(dt);
                return true;
            }

            d = default;
            return false;
        }

        // ---------------- Util común ----------------

        private static Operacion CreateOperacion(
            string id, string transportista, string matricula, string muelle, string estado, string destino,
            string llegada, string llegadaReal, string salidaReal, string salidaTope,
            string observaciones, string incidencias, string fecha, string precinto, string lex, string lado,
            DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            static DateOnly ParseFecha(string s, DateOnly fallback)
            {
                if (string.IsNullOrWhiteSpace(s)) return fallback;

                if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d)) return d;

                if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) return d;

                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                    return DateOnly.FromDateTime(dt);

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
                Transportista = transportista ?? string.Empty,
                Matricula     = matricula ?? string.Empty,
                Muelle        = muelle ?? string.Empty,
                Estado        = estado ?? string.Empty,
                Destino       = destino ?? string.Empty,

                Llegada       = llegada ?? string.Empty,
                LlegadaReal   = llegadaReal ?? string.Empty,
                SalidaReal    = salidaReal ?? string.Empty,
                SalidaTope    = salidaTope ?? string.Empty,

                Observaciones = observaciones ?? string.Empty,
                Incidencias   = incidencias ?? string.Empty,

                Fecha         = ParseFecha(fecha, fechaPorDefecto),
                Precinto      = precinto ?? string.Empty,
                Lex           = ParseBool(lex),
                Lado          = string.IsNullOrWhiteSpace(lado) ? ladoPorDefecto : lado
            };
        }
    }
}
