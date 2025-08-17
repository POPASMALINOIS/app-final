using OperativaLogistica.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OperativaLogistica.Services
{
    public static class PdfService
    {
        public static string SaveDailyPdf(IEnumerable<Operacion> ops, DateOnly fecha, string lado)
        {
            AppPaths.Ensure();
            var list = ops.ToList();

            var file = Path.Combine(AppPaths.Pdfs,
                $"Operativa_{fecha:yyyyMMdd}_{lado.Replace(' ', '_')}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeTable);
                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Generado: ").SemiBold();
                        txt.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}");
                    });

                    void ComposeHeader(IContainer h)
                    {
                        h.Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("OPERATIVA LOGÍSTICA").FontSize(16).SemiBold();
                                t.Line($"{fecha:dd/MM/yyyy}   •   {lado}").FontSize(11);
                            });
                        });
                    }

                    void ComposeTable(IContainer c)
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(160); // Transportista
                                cols.ConstantColumn(90);  // Matricula
                                cols.ConstantColumn(60);  // Muelle
                                cols.ConstantColumn(85);  // Estado
                                cols.RelativeColumn(2);   // Destino
                                cols.ConstantColumn(55);  // Llegada
                                cols.ConstantColumn(70);  // LlegadaReal
                                cols.ConstantColumn(70);  // SalidaReal
                                cols.ConstantColumn(65);  // SalidaTope
                                cols.RelativeColumn(2);   // Observ
                                cols.RelativeColumn(2);   // Incidencias
                                cols.ConstantColumn(80);  // Precinto
                                cols.ConstantColumn(35);  // LEX
                            });

                            // Cabecera
                            table.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("TRANSPORTISTA");
                                h.Cell().Element(CellHeader).Text("MATRICULA");
                                h.Cell().Element(CellHeader).Text("MUELLE");
                                h.Cell().Element(CellHeader).Text("ESTADO");
                                h.Cell().Element(CellHeader).Text("DESTINO");
                                h.Cell().Element(CellHeader).Text("LLEGADA");
                                h.Cell().Element(CellHeader).Text("LLEGADA REAL");
                                h.Cell().Element(CellHeader).Text("SALIDA REAL");
                                h.Cell().Element(CellHeader).Text("SALIDA TOPE");
                                h.Cell().Element(CellHeader).Text("OBSERVACIONES");
                                h.Cell().Element(CellHeader).Text("INCIDENCIAS");
                                h.Cell().Element(CellHeader).Text("PRECINTO");
                                h.Cell().Element(CellHeader).Text("LEX");

                                static IContainer CellHeader(IContainer c2) =>
                                    c2.BorderBottom(1).BorderColor(Colors.Grey.Darken2).PaddingBottom(4);
                            });

                            foreach (var op in list)
                            {
                                table.Cell().Element(CellBody).Text(op.Transportista);
                                table.Cell().Element(CellBody).Text(op.Matricula);
                                table.Cell().Element(CellBody).Text(op.Muelle);
                                table.Cell().Element(CellBody).Text(op.Estado);
                                table.Cell().Element(CellBody).Text(op.Destino);
                                table.Cell().Element(CellBody).AlignCenter().Text(op.Llegada);
                                table.Cell().Element(CellBody).AlignCenter().Text(op.LlegadaReal);
                                table.Cell().Element(CellBody).AlignCenter().Text(op.SalidaReal);
                                table.Cell().Element(CellBody).AlignCenter().Text(op.SalidaTope);
                                table.Cell().Element(CellBody).Text(op.Observaciones);
                                table.Cell().Element(CellBody).Text(op.Incidencias);
                                table.Cell().Element(CellBody).Text(op.Precinto);
                                table.Cell().Element(CellBody).AlignCenter().Text(op.Lex ? "✓" : "");

                                static IContainer CellBody(IContainer c2) =>
                                    c2.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2);
                            }
                        });
                    }
                });
            }).GeneratePdf(file);

            return file;
        }
    }
}
