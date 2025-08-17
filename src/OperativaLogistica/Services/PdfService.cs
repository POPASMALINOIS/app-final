
using OperativaLogistica.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace OperativaLogistica.Services
{
    public static class PdfService
    {
        public static string SaveDailyPdf(IEnumerable<Operacion> data, DateOnly date, string? desktopOverride = null)
        {
            var desktop = desktopOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var dir = Path.Combine(desktop, "Operativa_Historico");
            Directory.CreateDirectory(dir);
            foreach (var file in Directory.GetFiles(dir, "*.pdf"))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.CreationTimeUtc < DateTime.UtcNow.AddDays(-30))
                        File.Delete(file);
                } catch {}
            }

            var fileName = $"Operativa_{date:yyyyMMdd}_{DateTime.Now:HHmm}.pdf";
            var path = Path.Combine(dir, fileName);

            QuestPDF.Settings.License = LicenseType.Community;

           Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape);
        page.Margin(20);
        page.Header()
            .Text($"Operativa Logística - {date:dd/MM/yyyy}")
            .SemiBold().FontSize(14);

        page.Content().Table(table =>
        {
            var columns = new[]
            {
                "TRANSPORTISTA","MATRICULA","MUELLE","ESTADO","DESTINO",
                "LLEGADA","LLEGADA REAL","SALIDA REAL","SALIDA TOPE","OBSERVACIONES","INCIDENCIAS"
            };

            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.2f);  // TRANSPORTISTA
                c.RelativeColumn(0.9f);  // MATRICULA
                c.RelativeColumn(0.7f);  // MUELLE
                c.RelativeColumn(0.9f);  // ESTADO
                c.RelativeColumn(1.4f);  // DESTINO
                c.RelativeColumn(0.8f);  // LLEGADA
                c.RelativeColumn(1.0f);  // LLEGADA REAL
                c.RelativeColumn(1.0f);  // SALIDA REAL
                c.RelativeColumn(0.9f);  // SALIDA TOPE
                c.RelativeColumn(1.4f);  // OBSERVACIONES
                c.RelativeColumn(1.2f);  // INCIDENCIAS
            });

            // Cabecera
            table.Header(h =>
            {
                foreach (var t in columns)
                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(t).SemiBold().FontSize(9);
            });

            // Filas
            foreach (var op in data)
            {
                table.Cell().Padding(3).Text(op.Transportista).FontSize(9);
                table.Cell().Padding(3).Text(op.Matricula).FontSize(9);
                table.Cell().Padding(3).Text(op.Muelle).FontSize(9);
                table.Cell().Padding(3).Text(op.Estado).FontSize(9);
                table.Cell().Padding(3).Text(op.Destino).FontSize(9);
                table.Cell().Padding(3).Text(op.Llegada).FontSize(9);
                table.Cell().Padding(3).Text(op.LlegadaReal ?? "").FontSize(9);
                table.Cell().Padding(3).Text(op.SalidaReal ?? "").FontSize(9);
                table.Cell().Padding(3).Text(op.SalidaTope).FontSize(9);
                table.Cell().Padding(3).Text(op.Observaciones).FontSize(9);
                table.Cell().Padding(3).Text(op.Incidencias).FontSize(9);
            }
        });

        page.Footer().AlignRight().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
    });
}).GeneratePdf(path);
