using ClosedXML.Excel;
using OperativaLogistica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OperativaLogistica.Services
{
    public static class ImportService
    {
        // Mapa de claves "internas" -> lista de sinónimos en cabecera
        private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRANSPORTISTA"] = new[] { "transportista", "carrier", "proveedor", "empresa", "transport" },
            ["MATRICULA"]     = new[] { "matricula", "matrícula", "mat", "plate", "placa", "license", "registration" },
            ["MUELLE"]        = new[] { "muelle", "dock", "rampa", "bay", "door" },
            ["ESTADO"]        = new[] { "estado", "status", "est", "situacion", "situación" },
            ["DESTINO"]       = new[] { "destino", "dest", "destination", "city", "poblacion", "población" },
            ["LLEGADA"]       = new[] { "llegada", "eta", "hora llegada", "arrival", "hora prevista", "hora entrada teórica" },
            ["SALIDA TOPE"]   = new[] { "salida tope", "cutoff", "cut-off", "tope salida", "lsl", "hora salida tope" },
            ["OBSERVACIONES"] = new[] { "observaciones", "observ", "comentarios", "comments", "notes" },
            ["INCIDENCIAS"]   = new[] { "incidencias", "incid", "issues", "observaciones incidencias", "averías", "retrasos" },
            // Las reales se rellenan en la app
            ["LLEGADA REAL"]  = new[] { "llegada real", "hora entrada real", "real arrival" },
            ["SALIDA REAL"]   = new[] { "salida real", "hora salida real", "real departure" },
        };

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            // quitar tildes
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in norm)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);
            // quitar espacios/puntuación
            var allowed = s.Where(c => char.IsLetterOrDigit(c));
            return new string(allowed.ToArray());
        }

        private static Dictionary<string,int> BuildIndex(IReadOnlyList<string> headers)
        {
            // headers: lista 1-based si viene de Excel, 0-based si CSV
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var normHeaders = headers.Select(Normalize).ToArray();

            foreach (var target in Synonyms.Keys)
            {
                var wanted = Synonyms[target].Select(Normalize).ToArray();
                // 1) coincidencia exacta normalizada
                for (int i = 0; i < normHeaders.Length; i++)
                {
                    if (wanted.Contains(normHeaders[i]))
                    {
                        idx[target] = i; // índice 0-based para lectura posterior
                        goto nextKey;
                    }
                }
                // 2) contains (cabeceras largas tipo "hora llegada prevista")
                for (int i = 0; i < normHeaders.Length; i++)
                {
                    if (wanted.Any(w => normHeaders[i].Contains(w)))
                    {
                        idx[target] = i;
                        goto nextKey;
                    }
                }
                // 3) sin coincidencia → no se mapea (se quedará vacío)
                nextKey:;
            }
            return idx;
        }

        public static List<Operacion> FromCsv(string path, DateOnly? dateOverride = null)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return new();

            var headerCells = SplitCsvLine(lines[0]).ToList();
            var map = BuildIndex(headerCells);

            var list = new List<Operacion>();
            for (int r = 1; r < lines.Length; r++)
            {
                var parts = SplitCsvLine(lines[r]);
                string Col(string key)
                {
                    return map.TryGetValue(key, out var i) && i >= 0 && i < parts.Length ? parts[i].Trim() : "";
                }

                var op = new Operacion
                {
                    Transportista = Col("TRANSPORTISTA"),
                    Matricula     = Col("MATRICULA"),
                    Muelle        = Col("MUELLE"),
                    Estado        = Col("ESTADO"),
                    Destino       = Col("DESTINO"),
                    Llegada       = NormalizeTime(Col("LLEGADA")),
                    SalidaTope    = NormalizeTime(Col("SALIDA TOPE")),
                    Observaciones = Col("OBSERVACIONES"),
                    Incidencias   = Col("INCIDENCIAS"),
                    Fecha         = dateOverride ?? DateOnly.FromDateTime(DateTime.Now)
                };
                if (!IsRowEmpty(op)) list.Add(op);
            }
            return list;
        }

        public static List<Operacion> FromXlsx(string path, string? sheetName = null, DateOnly? dateOverride = null)
        {
            using var wb = new XLWorkbook(path);
            var ws = sheetName != null ? wb.Worksheet(sheetName) : ChooseWorksheet(wb);

            // localizar la fila de cabeceras (busca en las primeras 10 filas)
            var headerRow = ws.Rows(1, Math.Min(10, ws.RowCount()))
                             .FirstOrDefault(r => r.CellsUsed().Any());
            if (headerRow == null) return new();

            var headerCells = headerRow.CellsUsed().Select(c => c.GetString()).ToList();
            var map = BuildIndex(headerCells);

            var list = new List<Operacion>();
            foreach (var row in ws.Rows(headerRow.RowNumber() + 1, ws.LastRowUsed().RowNumber()))
            {
                if (!row.CellsUsed().Any()) continue;

                string Get(string key)
                {
                    if (!map.TryGetValue(key, out var i)) return "";
                    var cell = row.Cell(i + 1); // headers 0-based → Excel 1-based
                    if (cell.DataType == XLDataType.Number && (key == "LLEGADA" || key == "SALIDA TOPE"))
                        return FromExcelTime(cell.GetDouble());
                    return cell.GetString().Trim();
                }

                var op = new Operacion
                {
                    Transportista = Get("TRANSPORTISTA"),
                    Matricula     = Get("MATRICULA"),
                    Muelle        = Get("MUELLE"),
                    Estado        = Get("ESTADO"),
                    Destino       = Get("DESTINO"),
                    Llegada       = NormalizeTime(Get("LLEGADA")),
                    SalidaTope    = NormalizeTime(Get("SALIDA TOPE")),
                    Observaciones = Get("OBSERVACIONES"),
                    Incidencias   = Get("INCIDENCIAS"),
                    Fecha         = dateOverride ?? DateOnly.FromDateTime(DateTime.Now)
                };
                if (!IsRowEmpty(op)) list.Add(op);
            }
            return list;
        }

        private static IXLWorksheet ChooseWorksheet(XLWorkbook wb)
        {
            // Si hay una sola hoja, esa; si hay varias, prioriza la primera que tenga datos
            foreach (var ws in wb.Worksheets)
                if (ws.FirstRowUsed() != null) return ws;
            return wb.Worksheets.First();
        }

        private static bool IsRowEmpty(Operacion op)
        {
            return string.IsNullOrWhiteSpace(op.Transportista)
                && string.IsNullOrWhiteSpace(op.Matricula)
                && string.IsNullOrWhiteSpace(op.Destino);
        }

        private static string FromExcelTime(double excelNumber)
        {
            // Excel guarda horas como fracción de día (0..1). Formateamos HH:mm.
            var ts = TimeSpan.FromDays(excelNumber);
            if (ts.TotalHours < 0 || ts.TotalHours > 48) return "";
            return new DateTime(1,1,1).Add(ts).ToString("HH:mm");
        }

        private static string NormalizeTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim().ToLowerInvariant().Replace("h", "").Replace(".", ":");
            // 7 -> 07:00, 730 -> 07:30, 7:0 -> 07:00, 7:30 -> 07:30
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            {
                if (num < 24 && Math.Abs(num - Math.Truncate(num)) < 0.0001) return $"{(int)num:00}:00";
            }
            var parts = value.Split(':');
            if (parts.Length == 1) return $"{parts[0].PadLeft(2,'0')}:00";
            string hh = parts[0].PadLeft(2,'0');
            string mm = parts[1].PadLeft(2,'0');
            if (mm.Length > 2) mm = mm[..2];
            return $"{hh}:{mm}";
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            bool inQuotes = false; var cur = "";
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"') inQuotes = !inQuotes;
                else if (ch == ',' && !inQuotes) { list.Add(cur.Trim()); cur = ""; }
                else cur += ch;
            }
            list.Add(cur.Trim());
            return list.ToArray();
        }
    }
}
