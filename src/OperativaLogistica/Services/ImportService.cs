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

            var range = ws.RangeUsed();
            if (range == null) return list;

            // Mapa de cabeceras -> índice (1-based)
            var map = BuildHeaderMap(range.Row(1));

            // Recorre filas con datos
            for (int r = 2; r <= range.RowCount(); r++)
            {
                var row = range.Row(r);
                if (RowIsEmpty(row)) continue;

                string S(string key) => row.Cell(map.TryGetValue(key, out var c) ? c : int.MaxValue).GetFormattedString();

                // DateOnly desde celda (numérico excel o texto)
                string FechaCell()
                {
                    if (map.TryGetValue("FECHA", out var c))
                    {
                        var cell = row.Cell(c);
                        if (TryGetDateOnly(cell, out var d)) return d.ToString("yyyy-MM-dd");
                        return cell.GetFormattedString();
                    }
                    return string.Empty;
                }

                var op = CreateOperacion(
                    S("ID"), S("TRANSPORTISTA"), S("MATRICULA"), S("MUELLE"), S("ESTADO"), S("DESTINO"),
                    S("LLEGADA"), S("LLEGADA REAL"), S("SALIDA REAL"), S("SALIDA TOPE"),
                    S("OBSERVACIONES"), S("INCIDENCIAS"), FechaCell(), S("PRECINTO"), S("LEX"), S("LADO"),
                    fechaPorDefecto, ladoPorDefecto);

                list.Add(op);
            }

            return list;

            static Dictionary<string, int> BuildHeaderMap(IXLRangeRow headerRow)
            {
                // Normalizamos a UPPER y quitamos espacios extra.
                var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int c = 1; c <= headerRow.CellCount(); c++)
                {
                    var raw = headerRow.Cell(c).GetFormattedString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(raw)) continue;

                    var key = raw.ToUpperInvariant();

                    // Alias habituales (por si cambian levemente las cabeceras)
                    key = key switch
                    {
                        "LLEGADA REAL" => "LLEGADA REAL",
                        "SALIDA REAL"  => "SALIDA REAL",
                        "SALIDA TOPE"  => "SALIDA TOPE",
                        _              => key
                    };

                    dict[key] = c;
                }

                // Aseguramos claves esenciales aunque no existan (no romperá si no están)
                string[] expected = {
                    "ID","TRANSPORTISTA","MATRICULA","MUELLE","ESTADO","DESTINO",
                    "LLEGADA","LLEGADA REAL","SALIDA REAL","SALIDA TOPE",
                    "OBSERVACIONES","INCIDENCIAS","FECHA","PRECINTO","LEX","LADO"
                };
                foreach (var k in expected)
                    dict.TryAdd(k, int.MaxValue);

                return dict;
            }

            static bool RowIsEmpty(IXLRangeRow row)
            {
                foreach (var cell in row.Cells())
                    if (!string.IsNullOrWhiteSpace(cell.GetFormattedString()))
                        return false;
                return true;
            }

            static bool TryGetDateOnly(IXLCell cell, out DateOnly d)
            {
                // Si el tipo de celda es DateTime (numérico Excel)
                if (cell.DataType == XLDataType.DateTime)
                {
                    var dt = cell.GetDateTime();
                    d = DateOnly.FromDateTime(dt);
                    return true;
                }
                // Si es texto, intentamos varios formatos
                var s = cell.GetFormattedString()?.Trim() ?? "";
                if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out d)) return true;
                if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out d)) return true;
                return false;
            }
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
                // Último intento a parseo flexible
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
