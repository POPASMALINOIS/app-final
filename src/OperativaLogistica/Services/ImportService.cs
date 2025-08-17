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
        // Sinónimos para mapear cabeceras "libres" a tus 11 categorías
        private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRANSPORTISTA"] = new[] { "transportista","carrier","empresa","proveedor","transport" },
            ["MATRICULA"]     = new[] { "matricula","matrícula","mat","plate","placa","license","registration" },
            ["MUELLE"]        = new[] { "muelle","dock","rampa","bay","door" },
            ["ESTADO"]        = new[] { "estado","status","situacion","situación" },
            ["DESTINO"]       = new[] { "destino","dest","destination","city","poblacion","población" },
            ["LLEGADA"]       = new[] { "llegada","eta","hora llegada","arrival","hora prevista","hora entrada teorica","hora entrada teórica" },
            ["SALIDA TOPE"]   = new[] { "salida tope","cutoff","cut-off","tope salida","lsl","hora salida tope" },
            ["OBSERVACIONES"] = new[] { "observaciones","observ","comentarios","comments","notes","nota" },
            ["INCIDENCIAS"]   = new[] { "incidencias","incid","issues","averias","averías","retrasos","anomalías","anomalias" },
            ["LLEGADA REAL"]  = new[] { "llegada real","hora entrada real","real arrival" },
            ["SALIDA REAL"]   = new[] { "salida real","hora salida real","real departure" },
        };

        public static List<Operacion> FromCsv(string path, DateOnly fecha)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return new();

            var headerCells = SplitCsvLine(lines[0]).ToList();
            var map = BuildIndex(headerCells);

            var list = new List<Operacion>();
            for (int r = 1; r < lines.Length; r++)
            {
                var parts = SplitCsvLine(lines[r]);
                string Col(string key) =>
                    map.TryGetValue(key, out var i) && i >= 0 && i < parts.Length ? parts[i].Trim() : "";

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
                    Fecha         = fecha
                };
                if (!IsRowEmpty(op)) list.Add(op);
            }
            return list;
        }

        public static List<Operacion> FromXlsx(string path, DateOnly fecha)
        {
            using var wb = new XLWorkbook(path);
            var ws = PickWorksheet(wb);

            // Detectar fila de cabecera: elegimos entre las 15 primeras la que más celdas de texto tiene
            var candidateRows = ws.Rows(1, Math.Min(15, ws.LastRowUsed()?.RowNumber() ?? 1));
            var headerRow = candidateRows
                .Select(r => new { Row = r, Score = r.Cells(1, ws.LastColumnUsed().ColumnNumber())
                                            .Count(c => !string.IsNullOrWhiteSpace(c.GetString())) })
                .OrderByDescending(x => x.Score)
                .First().Row;

            var headers = headerRow.Cells(1, ws.LastColumnUsed().ColumnNumber()).Select(c => c.GetString()).ToList();
            var map = BuildIndex(headers);

            var list = new List<Operacion>();
            var lastRow = ws.LastRowUsed().RowNumber();

            for (int r = headerRow.RowNumber() + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);
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
                    Fecha         = fecha
                };

                if (!IsRowEmpty(op)) list.Add(op);
            }
            return list;
        }

        // --------- Utilidades ---------

        private static IXLWorksheet PickWorksheet(XLWorkbook wb)
        {
            // Prioriza la primera hoja con datos
            foreach (var ws in wb.Worksheets)
                if (ws.FirstRowUsed() != null) return ws;
            return wb.Worksheets.First();
        }

        private static Dictionary<string,int> BuildIndex(IReadOnlyList<string> headers)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var normHeaders = headers.Select(Normalize).ToArray();

            foreach (var target in Synonyms.Keys)
            {
                var wanted = Synonyms[target].Select(Normalize).ToArray();

                // 1) Exacta
                for (int i = 0; i < normHeaders.Length; i++)
                    if (wanted.Contains(normHeaders[i])) { idx[target] = i; goto next; }

                // 2) Contiene
                for (int i = 0; i < normHeaders.Length; i++)
                    if (wanted.Any(w => normHeaders[i].Contains(w))) { idx[target] = i; goto next; }

                next:;
            }
            return idx;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();

            // quitar tildes
            var nf = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in nf)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            s = sb.ToString().Normalize(NormalizationForm.FormC);

            // quitar no alfanuméricos
            return new string(s.Where(char.IsLetterOrDigit).ToArray());
        }

        private static bool IsRowEmpty(Operacion op) =>
            string.IsNullOrWhiteSpace(op.Transportista)
            && string.IsNullOrWhiteSpace(op.Matricula)
            && string.IsNullOrWhiteSpace(op.Destino);

        private static string FromExcelTime(double excelNumber)
        {
            // Excel horas = fracción de día
            var ts = TimeSpan.FromDays(excelNumber);
            if (ts.TotalHours < 0 || ts.TotalHours > 48) return "";
            return new DateTime(1,1,1).Add(ts).ToString("HH:mm");
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            bool q = false; string cur = "";
            foreach (var ch in line)
            {
                if (ch == '"') q = !q;
                else if (ch == ',' && !q) { list.Add(cur.Trim()); cur = ""; }
                else cur += ch;
            }
            list.Add(cur.Trim());
            return list.ToArray();
        }

        private static string NormalizeTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim().ToLowerInvariant().Replace("h", "").Replace(".", ":");
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                if (num < 24 && Math.Abs(num - Math.Truncate(num)) < 1e-6) return $"{(int)num:00}:00";

            var parts = value.Split(':');
            if (parts.Length == 1) return $"{parts[0].PadLeft(2,'0')}:00";
            string hh = parts[0].PadLeft(2,'0');
            string mm = parts[1].PadLeft(2,'0');
