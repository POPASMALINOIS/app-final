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

            // Detectar fila de cabecera (entre las 15 primeras filas)
            var candidateRows = ws.Rows(1, Math.Min(15, ws.LastRowUsed()?.RowNumber() ?? 1));
            var headerRow = candidateRows
                .Select(r => new { Row = r, Score = r.Cells(1, ws.LastColumnUsed().ColumnNumber())
                                            .Count(c => !string.IsNullOrWhiteSpace(c.GetString())) })
                .OrderByDescending(x => x.Score)
                .First().Row;

            var headers = header
