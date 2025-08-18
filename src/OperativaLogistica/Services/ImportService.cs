using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ImportService
    {
        /// <summary>
        /// Importa operaciones desde un archivo:
        ///  - CSV con separador ';'
        ///  - Excel (XLS/XLSX) con cabeceras en la primera fila del rango usado
        /// Cabeceras admitidas (case-insensitive y tolerantes a tildes/sinónimos):
        /// Id, Transportista (o Proveedor), Matricula, Muelle, Estado, Destino,
        /// Llegada, Llegada Real (o Entrada Real), Salida Real, Salida Tope (o Tope Salida),
        /// Observaciones, Incidencias, Fecha (o Día), Precinto, Lex, Lado
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
            string? line = sr.ReadLine(); // cabecera (se ignora si existe)
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

            // ---- Mapeo de cabeceras robusto (normaliza tildes, signos y espacios; acepta sinónimos)
            var canon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Básicas
                ["ID"] = "ID",
                ["TRANSPORTISTA"] = "TRANSPORTISTA",
                ["PROVEEDOR"] = "TRANSPORTISTA",

                ["MATRICULA"] = "MATRICULA",
                ["MATRÍCULA"] = "MATRICULA",

                ["MUELLE"] = "MUELLE",
                ["ESTADO"] = "ESTADO",
                ["DESTINO"] = "DESTINO",

                // Tiempos
                ["LLEGADA"] = "LLEGADA",
                ["LLEGADA REAL"] = "LLEGADA REAL",
                ["ENTRADA REAL"] = "LLEGADA REAL",
                ["SALIDA REAL"]  = "SALIDA REAL",
                ["SALIDA TOPE"]  = "SALIDA TOPE",
                ["TOPE SALIDA"]  = "SALIDA TOPE",

                // Texto/varios
                ["OBSERVACIONES"] = "OBSERVACIONES",
                ["INCIDENCIAS"]   = "INCIDENCIAS",
                ["FECHA"]         = "FECHA",
                ["DIA"]           = "FECHA",
                ["DÍA"]           = "FECHA",
                ["PRECINTO"]      = "PRECINTO",
                ["LEX"]           = "LEX",
                ["LADO"]          = "LADO",
            };

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // lee y normaliza cabeceras de la primera fila del rango usado (r0)
            for (int c = c0; c <= c1; c++)
            {
                var raw = ws.Cell(r0, c).GetString().Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                var norm = NormalizeHeader(raw);
                if (canon.TryGetValue(norm, out var key))
                    map[key] = c;
                else
                    map[norm] = c; // por si ya coincide tras normalizar
            }

            bool tieneCabecerasUtiles =
                map.ContainsKey("ID") || map.ContainsKey("TRANSPORTISTA") || map.ContainsKey("MATRICULA");

            // Helpers seguros
            string S(int row, string header)
            {
                if (!map.TryGetValue(header, out int c)) return "";
                if (c < c0 || c > c1) return "";
                return ws.Cell(row, c).GetString().Trim();
            }

            string FechaStr(int row)
            {
                if (!map.TryGetValue("FECHA", out int c)) return "";
                if (c < c0 || c > c1) return "";
                var cell = ws.Cell(row, c);
                if (TryGetDateOnly(cell, out var d))
                    return d.ToString("yyyy-MM-dd");
                return cell.GetString().Trim();
            }

            // Leer por índice absoluto (fallback sin cabeceras)
            string SByIndex(int row, int col)
                => (col >= c0 && col <= c1) ? ws.Cell(row, col).GetString().Trim() : string.Empty;

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

                Operacion op;

                if (tieneCabecerasUtiles)
                {
                    // Con cabeceras reconocidas
                    op = CreateOperacion(
                        S(r, "ID"), S(r, "TRANSPORTISTA"), S(r, "MATRICULA"), S(r, "MUELLE"), S(r, "ESTADO"), S(r, "DESTINO"),
                        S(r, "LLEGADA"), S(r, "LLEGADA REAL"), S(r, "SALIDA REAL"), S(r, "SALIDA TOPE"),
                        S(r, "OBSERVACIONES"), S(r, "INCIDENCIAS"), FechaStr(r), S(r, "PRECINTO"), S(r, "LEX"), S(r, "LADO"),
                        fechaPorDefecto, ladoPorDefecto);
                }
                else
                {
                    // Sin cabeceras o irreconocibles -> por posición (A..P)
                    // Id, Transportista, Matricula, Muelle, Estado, Destino,
                    // Llegada, Llegada Real, Salida Real, Salida Tope,
                    // Observaciones, Incidencias, Fecha, Precinto, Lex, Lado
                    op = CreateOperacion(
                        SByIndex(r, c0 + 0),  SByIndex(r, c0 + 1),  SByIndex(r, c0 + 2),  SByIndex(r, c0 + 3),  SByIndex(r, c0 + 4),  SByIndex(r, c0 + 5),
                        SByIndex(r, c0 + 6),  SByIndex(r, c0 + 7),  SByIndex(r, c0 + 8),  SByIndex(r, c0 + 9),
                        SByIndex(r, c0 +10),  SByIndex(r, c0 +11),  SByIndex(r, c0 +12),  SByIndex(r, c0 +13),  SByIndex(r, c0 +14),  SByIndex(r, c0 +15),
                        fechaPorDefecto, ladoPorDefecto);
                }

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

        private static string NormalizeHeader(string s)
        {
            s = s.Trim().ToUpperInvariant();

            // Quita tildes/acentos
            s = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            s = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

            // Sustituye no alfanum por espacios
            sb.Clear();
            foreach (var ch in s)
                sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            s = sb.ToString();

            // Colapsa espacios
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
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
