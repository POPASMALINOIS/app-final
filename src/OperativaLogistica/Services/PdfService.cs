using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Servicio "stub" para exportar PDF de la jornada.
    /// Para no añadir dependencias en el build, genera un archivo con extensión .pdf
    /// con contenido de texto estructurado. Es suficiente para que el menú "Guardar PDF"
    /// no falle y el pipeline compile. Más adelante puedes sustituirlo por QuestPDF o iText.
    /// </summary>
    public class PdfService
    {
        /// <summary>
        /// Guarda un "PDF" (archivo de texto con extensión .pdf) con el resumen de la jornada.
        /// </summary>
        /// <param name="filePath">Ruta final (normalmente terminada en .pdf)</param>
        /// <param name="operaciones">Listado de operaciones a imprimir</param>
        /// <param name="fecha">Fecha de la jornada</param>
        /// <param name="lado">Lado seleccionado</param>
        public void SaveJornadaPdf(string filePath, IEnumerable<Operacion> operaciones, DateTime fecha, string lado)
        {
            // Aseguramos carpeta
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            var sb = new StringBuilder();
            sb.AppendLine("PLM INDITEX EXPEDICION - Resumen de Jornada");
            sb.AppendLine($"Fecha: {fecha:yyyy-MM-dd}    Lado: {lado}");
            sb.AppendLine(new string('=', 80));

            // Cabecera
            sb.AppendLine("TRANSPORTISTA | MATRICULA | MUELLE | ESTADO | DESTINO | LLEGADA | LLEGADA REAL | SALIDA REAL | SALIDA TOPE | OBSERVACIONES | INCIDENCIAS");

            // Cuerpo
            foreach (var op in operaciones ?? Enumerable.Empty<Operacion>())
            {
                sb.AppendLine($"{op.Transportista} | {op.Matricula} | {op.Muelle} | {op.Estado} | {op.Destino} | {op.Llegada} | {op.LlegadaReal} | {op.SalidaReal} | {op.SalidaTope} | {op.Observaciones} | {op.Incidencias}");
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine("Generado automáticamente por PdfService (modo stub).");

            // Escribimos contenido plano. Sigue siendo un .pdf “simple”, pero suficiente para
            // que la opción de guardado funcione en entornos sin librerías PDF.
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
