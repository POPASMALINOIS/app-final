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
        /// Importa operaciones desde CSV (;) o Excel (XLS/XLSX).
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

        // =====================================================================
        // CSV
        // =====================================================================
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

        // =====================================================================
        // Excel (ClosedXML) — robusto para cabeceras en filas 1..10 y alias
        // =====================================================================
        private static IEnumerable<Operacion> ImportExcel(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            if (!File.Exists(filePath)) return list;

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.Count > 0 ? wb.Worksheet(1) : null;
            if (ws == null) return list;

            // 1) Detectar la fila de cabeceras buscando TRANSPORTISTA/MATRICULA
            int headerRow = FindHeaderRow(ws);
            if (headerRow == -1) return list; // no reconocido

            // 2) Delimitar el rango útil por columnas con datos a partir de la cabecera
            int c0 = FirstUsedColumn(ws, headerRow);
            int c1 = LastUsedColumn(ws, headerRow);
            if (c0 == -1 || c1 == -1) return list;

            // 3) Construir mapa de cabeceras (con alias)
            var map = BuildHeaderMap(ws, headerRow, c0, c1);

            // 4) Recorrer filas de datos
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? ws.LastRowUsed().RowNumber();
            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                if (RowIsEmpty(ws, r, c0, c1)) continue;

                string GetStr(string header)
                {
                    if (!map.TryGetValue(header, out int c)) return "";
                    if (c < c0 || c > c1) return "";
                    return ws.Cell(r, c).GetString().Trim();
                }

                string GetFecha()
                {
                    if (!map.TryGetValue("FECHA", out int c)) return "";
                    if (c < c0 || c > c1) return "";
                    var cell = ws.Cell(r, c);
                    if (TryGetDateOnly(cell, out var d))
                        return d.ToString("yyyy-MM-dd");
                    return cell.GetString().Trim();
                }

                // Construir la operación. NOTA: en tu Excel “SALIDA” lo mapeamos a “SALIDA REAL”.
                var op = CreateOperacion(
                    GetStr("ID"),
                    GetStr("TRANSPORTISTA"),
                    GetStr("MATRICULA"),
                    GetStr("MUELLE"),
                    GetStr("ESTADO"),
                    GetStr("DESTINO"),
                    GetStr("LLEGADA"),
                    GetStr("LLEGADA REAL"),
                    GetStr("SALIDA REAL"),
                    GetStr("SALIDA TOPE"),
                    GetStr("OBSERVACIONES"),
                    GetStr("INCIDENCIAS"),
                    GetFecha(),
                    GetStr("PRECINTO"),
                    GetStr("LEX"),
                    GetStr("LADO"),
                    fechaPorDefecto, ladoPorDefecto);

                list.Add(op);
            }

            return list;
        }

        // ---------------- helpers Excel ----------------

        /// <summary>Busca la fila (1..10) que contenga al menos TRANSPORTISTA o MATRICULA.</summary>
        private static int FindHeaderRow(IXLWorksheet ws)
        {
            int maxRow = Math.Min(10, ws.LastRowUsed()?.RowNumber() ?? 10);
            for (int r = 1; r <= maxRow; r++)
            {
                bool foundTransportista = false;
                bool foundMatricula = false;

                var row = ws.Row(r);
                foreach (var cell in row.CellsUsed())
                {
                    var s = (cell.GetString() ?? "").Trim().ToUpperInvariant();
                    if (s == "TRANSPORTISTA") foundTransportista = true;
                    if (s == "MATRICULA" || s == "MATRÍCULA") foundMatricula = true;
                    if (foundTransportista && foundMatricula) return r;
                }
            }
            return -1;
        }

        private static int FirstUsedColumn(IXLWorksheet ws, int headerRow)
        {
            var cells = ws.Row(headerRow).CellsUsed();
            int min = int.MaxValue;
            foreach (var c in cells) min = Math.Min(min, c.Address.ColumnNumber);
            return min == int.MaxValue ? -1 : min;
        }

        private static int LastUsedColumn(IXLWorksheet ws, int headerRow)
        {
            var cells = ws.Row(headerRow).CellsUsed();
            int max = -1;
            foreach (var c in cells) max = Math.Max(max, c.Address.ColumnNumber);
            return max;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, int headerRow, int c0, int c1)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int c = c0; c <= c1; c++)
            {
                var raw = ws.Cell(headerRow, c).GetString()?.Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                var key = raw.ToUpperInvariant();

                // Normalización / alias típicos
                key = key switch
                {
                    "MATRÍCULA"        => "MATRICULA",
                    "LLEGADA REAL"     => "LLEGADA REAL",
                    "SALIDA REAL"      => "SALIDA REAL",
                    "SALIDA TOPE"      => "SALIDA TOPE",
                    "SALIDA"           => "SALIDA REAL",   // en tu Excel
                    "LLEGADA"          => "LLEGADA",
                    "REMOLQUE"         => "REMOLQUE",      // no se usa en el modelo, pero lo dejamos por si
                    _                  => key
                };

                dict[key] = c;
            }

            // Claves esperadas; si falta alguna la dejamos como “no existe” (-1)
            string[] expected =
            {
                "ID","TRANSPORTISTA","MATRICULA","MUELLE","ESTADO","DESTINO",
                "LLEGADA","LLEGADA REAL","SALIDA REAL","SALIDA TOPE",
                "OBSERVACIONES","INCIDENCIAS","FECHA","PRECINTO","LEX","LADO"
            };
            foreach (var k in expected) dict.TryAdd(k, -1);

            return dict;
        }

        private static bool RowIsEmpty(IXLWorksheet ws, int r, int c0, int c1)
        {
            for (int c = c0; c <= c1; c++)
                if (!string.IsNullOrWhiteSpace(ws.Cell(r, c).GetString()))
                    return false;
            return true;
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

        // =====================================================================
        // Util común
        // =====================================================================
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

                Llegada       = FormateaHora(llegada),
                LlegadaReal   = FormateaHora(llegadaReal),
                SalidaReal    = FormateaHora(salidaReal),
                SalidaTope    = FormateaHora(salidaTope),

                Observaciones = observaciones ?? string.Empty,
                Incidencias   = incidencias ?? string.Empty,

                Fecha         = ParseFecha(fecha, fechaPorDefecto),
                Precinto      = precinto ?? string.Empty,
                Lex           = ParseBool(lex),
                Lado          = string.IsNullOrWhiteSpace(lado) ? ladoPorDefecto : lado
            };
        }

        private static string FormateaHora(string s)
        {
            var t = (s ?? "").Trim();
            // Si ya viene como 08:00:00 o 8:00 => normalizamos a HH:mm
            if (TimeSpan.TryParse(t, CultureInfo.CurrentCulture, out var ts))
                return new DateTime(1, 1, 1, ts.Hours, ts.Minutes, 0).ToString("HH:mm");
            if (DateTime.TryParse(t, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                return dt.ToString("HH:mm");

            return t;
        }
    }
}
