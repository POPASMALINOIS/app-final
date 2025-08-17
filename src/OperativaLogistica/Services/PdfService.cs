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

            // Limpieza: borrar PDFs con más de 30 días
            foreach (var file in Directory.GetFiles(dir, "*.pdf"))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.CreationTimeUtc < DateTime.UtcNow.AddDays(-30))
                        File.Delete(file);
                }
                catch { /* ignorar errores puntuales de IO */ }
            }

            var fileName = $"Operativa_{date:yyyyMMdd}_{DateTime.Now:HHmm}.pdf";
            var path = Path.Combine(dir, fileName);

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    // --- A4 apaisado + layout compacto ---
                    page.Size(PageSizes.A4.Landscape);
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

                        // Definición de anchos relativos por columna
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
                                h.Cell().Backgrou

