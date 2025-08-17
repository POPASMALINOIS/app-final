
using ClosedXML.Excel;
using OperativaLogistica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OperativaLogistica.Services
{
    public static class ImportService
    {
        public static List<Operacion> FromCsv(string path, DateOnly? dateOverride = null)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return new();
            var header = lines[0].Split(',').Select(s => s.Trim()).ToArray();
            var idx = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            for (int i=0; i<header.Length; i++) idx[header[i]] = i;

            int Ei(string key) => idx.TryGetValue(key, out var i) ? i : -1;

            var list = new List<Operacion>();
            for (int i=1; i<lines.Length; i++)
            {
                var parts = SplitCsvLine(lines[i]);
                var op = new Operacion
                {
                    Transportista = G(parts, Ei("TRANSPORTISTA")),
                    Matricula     = G(parts, Ei("MATRICULA")),
                    Muelle        = G(parts, Ei("MUELLE")),
                    Estado        = G(parts, Ei("ESTADO")),
                    Destino       = G(parts, Ei("DESTINO")),
                    Llegada       = G(parts, Ei("LLEGADA")),
                    SalidaTope    = G(parts, Ei("SALIDA TOPE")),
                    Observaciones = G(parts, Ei("OBSERVACIONES")),
                    Incidencias   = G(parts, Ei("INCIDENCIAS")),
                    Fecha         = dateOverride ?? DateOnly.FromDateTime(DateTime.Now)
                };
                list.Add(op);
            }
            return list;
        }

        public static List<Operacion> FromXlsx(string path, string? sheetName = null, DateOnly? dateOverride = null)
        {
            using var wb = new XLWorkbook(path);
            var ws = sheetName != null ? wb.Worksheet(sheetName) : wb.Worksheets.First();
            var headerRow = ws.FirstRowUsed();
            var headerCells = headerRow.Cells().Select(c => c.GetString().Trim()).ToList();
            var idx = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            for (int i=0; i<headerCells.Count; i++) idx[headerCells[i]] = i+1;

            int Ei(string key) => idx.TryGetValue(key, out var i) ? i : -1;
            string G(ClosedXML.Excel.IXLRow row, int col) => col > 0 ? row.Cell(col).GetString().Trim() : "";

            var list = new List<Operacion>();
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var op = new Operacion
                {
                    Transportista = G(row, Ei("TRANSPORTISTA")),
                    Matricula     = G(row, Ei("MATRICULA")),
                    Muelle        = G(row, Ei("MUELLE")),
                    Estado        = G(row, Ei("ESTADO")),
                    Destino       = G(row, Ei("DESTINO")),
                    Llegada       = G(row, Ei("LLEGADA")),
                    SalidaTope    = G(row, Ei("SALIDA TOPE")),
                    Observaciones = G(row, Ei("OBSERVACIONES")),
                    Incidencias   = G(row, Ei("INCIDENCIAS")),
                    Fecha         = dateOverride ?? DateOnly.FromDateTime(DateTime.Now)
                };
                if (!string.IsNullOrWhiteSpace(op.Transportista) ||
                    !string.IsNullOrWhiteSpace(op.Matricula) ||
                    !string.IsNullOrWhiteSpace(op.Destino))
                {
                    list.Add(op);
                }
            }
            return list;
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            bool inQuotes = false;
            var current = "";
            for (int i=0; i<line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"') inQuotes = !inQuotes;
                else if (ch == ',' && !inQuotes) { list.Add(current.Trim()); current = ""; }
                else current += ch;
            }
            list.Add(current.Trim());
            return list.ToArray();
        }

        private static string G(string[] parts, int idx) => (idx >=0 && idx < parts.Length) ? parts[idx] : "";
    }
}
