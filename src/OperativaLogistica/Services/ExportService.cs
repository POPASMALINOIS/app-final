using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ExportService
    {
        // Orden único de cabeceras (coincide con ImportService)
        private static readonly string[] Headers = new[]
        {
            "Id","Transportista","Matricula","Muelle","Estado","Destino",
            "Llegada","Llegada Real","Salida Real","Salida Tope",
            "Observaciones","Incidencias","Fecha","Precinto","Lex","Lado"
        };

        /// <summary>
        /// Exporta a CSV con separador ';' y UTF-8 con BOM.
        /// </summary>
        public void SaveCsv(string filePath, IEnumerable<Operacion> ops)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

            // UTF-8 BOM para que Excel en Windows lo abra bien con acentos
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            using var sw = new StreamWriter(fs, utf8Bom);

            sw.WriteLine(string.Join(";", Headers));

            foreach (var o in ops ?? Enumerable.Empty<Operacion>())
            {
                string S(object? v) => (v?.ToString() ?? "").Replace(';', ',');

                var line = string.Join(";",
                    S(o.Id),
                    S(o.Transportista),
                    S(o.Matricula),
                    S(o.Muelle),
                    S(o.Estado),
                    S(o.Destino),

                    S(o.Llegada),
                    S(o.LlegadaReal),
                    S(o.SalidaReal),
                    S(o.SalidaTope),

                    S(o.Observaciones),
                    S(o.Incidencias),

                    o.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    S(o.Precinto),
                    o.Lex ? "Sí" : "",                 // amigable y lo admite el import
                    S(o.Lado)
                );

                sw.WriteLine(line);
            }
        }

        /// <summary>
        /// Exporta a Excel (XLSX) formateando fecha/horas y aplicando estilos básicos.
        /// </summary>
        public void SaveExcel(string filePath, IEnumerable<Operacion> ops, DateOnly? fecha = null, string? lado = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Operativa");

            // Cabecera
            for (int c = 0; c < Headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = Headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 255);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Datos
            int r = 2;
            foreach (var o in ops ?? Enumerable.Empty<Operacion>())
            {
                int c = 1;

                ws.Cell(r, c++).Value = o.Id;
                ws.Cell(r, c++).Value = o.Transportista;
                ws.Cell(r, c++).Value = o.Matricula;
                ws.Cell(r, c++).Value = o.Muelle;
                ws.Cell(r, c++).Value = o.Estado;
                ws.Cell(r, c++).Value = o.Destino;

                WriteTime(ws.Cell(r, c++), o.Llegada);
                WriteTime(ws.Cell(r, c++), o.LlegadaReal);
                WriteTime(ws.Cell(r, c++), o.SalidaReal);
                WriteTime(ws.Cell(r, c++), o.SalidaTope);

                ws.Cell(r, c++).Value = o.Observaciones;
                ws.Cell(r, c++).Value = o.Incidencias;

                var fechaCell = ws.Cell(r, c++);
                fechaCell.Value = o.Fecha.ToDateTime(TimeOnly.MinValue);
                fechaCell.Style.DateFormat.Format = "yyyy-mm-dd";

                ws.Cell(r, c++).Value = o.Precinto;

                var lexCell = ws.Cell(r, c++);
                lexCell.Value = o.Lex ? "Sí" : "";
                lexCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(r, c++).Value = o.Lado;

                r++;
            }

            // Estética
            var used = ws.RangeUsed();
            if (used != null)
            {
                used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Ajustes de ancho razonables
                ws.Columns().AdjustToContents();
                // Forzar algunos anchos si quedan demasiado estrechos
                ws.Column(2).Width = Math.Max(ws.Column(2).Width, 22); // Transportista
                ws.Column(11).Width = Math.Max(ws.Column(11).Width, 30); // Observaciones
                ws.Column(12).Width = Math.Max(ws.Column(12).Width, 24); // Incidencias

                // Wrap en textos largos
                ws.Column(11).Style.Alignment.WrapText = true;
                ws.Column(12).Style.Alignment.WrapText = true;

                // Filtros y paneo
                ws.Range(1, 1, 1, Headers.Length).SetAutoFilter();
                ws.SheetView.FreezeRows(1);
            }

            // Info opcional en propiedades del libro
            if (fecha is not null) wb.Properties.Title = $"Jornada {fecha:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(lado)) wb.Properties.Subject = $"Lado {lado}";

            wb.SaveAs(filePath);
        }

        // ------------------------- helpers -------------------------

        /// <summary>
        /// Escribe una hora "HH:mm" si se puede parsear; si no, deja el texto tal cual.
        /// </summary>
        private static void WriteTime(IXLCell cell, string? value)
        {
            var s = (value ?? "").Trim();
            if (TimeSpan.TryParseExact(s, "g", CultureInfo.CurrentCulture, out var ts) ||
                TimeSpan.TryParseExact(s, "hh\\:mm", CultureInfo.InvariantCulture, out ts) ||
                TimeSpan.TryParse(s, out ts))
            {
                // Para que Excel muestre HH:mm, usamos DateTime base y formato
                var dt = new DateTime(1899, 12, 30).Add(ts); // base típica de Excel para horas
                cell.Value = dt;
                cell.Style.DateFormat.Format = "hh:mm";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                cell.Value = s;
            }
        }
    }
}

