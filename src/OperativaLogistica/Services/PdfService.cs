
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
                    page.Margin(20);
                    page.Header().Text($"Operativa Logística - {date:dd/MM/yyyy}").SemiBold().FontSize(16);
                    page.Content().Table(table =>
                    {
                        var columns = new[]
                        {
                            "TRANSPORTISTA","MATRICULA","MUELLE","ESTADO","DESTINO",
                            "LLEGADA","LLEGADA REAL","SALIDA REAL","SALIDA TOPE","OBSERVACIONES","INCIDENCIAS"
                        };
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(0.7f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.0f);
                        });
                        foreach (var h in columns)
                            table.Header(c => c.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(h).SemiBold());

                        foreach (var op in data)
                        {
                            table.Cell().Padding(3).Text(op.Transportista);
                            table.Cell().Padding(3).Text(op.Matricula);
                            table.Cell().Padding(3).Text(op.Muelle);
                            table.Cell().Padding(3).Text(op.Estado);
                            table.Cell().Padding(3).Text(op.Destino);
                            table.Cell().Padding(3).Text(op.Llegada);
                            table.Cell().Padding(3).Text(op.LlegadaReal ?? "");
                            table.Cell().Padding(3).Text(op.SalidaReal ?? "");
                            table.Cell().Padding(3).Text(op.SalidaTope);
                            table.Cell().Padding(3).Text(op.Observaciones);
                            table.Cell().Padding(3).Text(op.Incidencias);
                        }
                    });
                    page.Footer().AlignRight().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf(path);

            return path;
        }
    }
}
