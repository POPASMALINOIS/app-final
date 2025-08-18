using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    public class ImportService
    {
        /// <summary>
        /// Importa un CSV/Excel previamente convertido a líneas de texto.
        /// Devuelve la lista de Operacion lista para bindear.
        /// Ojo: si el modelo usa DateOnly, se hace la conversión explícita.
        /// </summary>
        public ObservableCollection<Operacion> ImportFromLines(IEnumerable<string> lines, char sep = ';')
        {
            var ops = new ObservableCollection<Operacion>();
            bool first = true;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // Salta cabecera si la hay
                if (first)
                {
                    first = false;
                    // heurística simple: si contiene cabeceras conocidas, la saltamos
                    if (raw.IndexOf("TRANSPORTISTA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        raw.IndexOf("MATRICULA", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }
                }

                var cols = raw.Split(sep);

                // Ajusta índices a tu estructura real
                string transportista = cols.Length > 0 ? cols[0].Trim() : "";
                string matricula     = cols.Length > 1 ? cols[1].Trim() : "";
                string muelle        = cols.Length > 2 ? cols[2].Trim() : "";
                string estado        = cols.Length > 3 ? cols[3].Trim() : "";
                string destino       = cols.Length > 4 ? cols[4].Trim() : "";
                string llegadaPlan   = cols.Length > 5 ? cols[5].Trim() : "";
                string salidaTope    = cols.Length > 6 ? cols[6].Trim() : "";
                string observ        = cols.Length > 7 ? cols[7].Trim() : "";
                string incidencias   = cols.Length > 8 ? cols[8].Trim() : "";
                string fechaTxt      = cols.Length > 9 ? cols[9].Trim() : "";

                // Parse de fecha (admite varios formatos habituales)
                DateTime fechaDt = ParseFechaFlexible(fechaTxt);

                // *** PUNTO CLAVE: si tu modelo usa DateOnly, convierto explícitamente ***
                var op = new Operacion
                {
                    Transportista = transportista,
                    Matricula     = matricula,
                    Muelle        = muelle,
                    Estado        = estado,
                    Destino       = destino,
                    Llegada       = llegadaPlan,
                    SalidaTope    = salidaTope,
                    Observaciones = observ,
                    Incidencias   = incidencias
                };

                // Detecta el tipo de la propiedad Fecha de tu modelo y asigna correctamente
                var prop = typeof(Operacion).GetProperty("Fecha");
                if (prop != null)
                {
                    if (prop.PropertyType == typeof(DateOnly))
                    {
                        prop.SetValue(op, DateOnly.FromDateTime(fechaDt));
                    }
                    else if (prop.PropertyType == typeof(DateTime))
                    {
                        prop.SetValue(op, fechaDt);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(op, fechaDt.ToString("yyyy-MM-dd"));
                    }
                }

                // Si tu modelo tiene LlegadaReal/SalidaReal y son DateTime? o string, no pasa nada por dejarlas vacías aquí.

                ops.Add(op);
            }

            return ops;
        }

        /// <summary>
        /// Ejemplo de carga desde un CSV en disco. Ajusta si usas Excel directamente.
        /// </summary>
        public ObservableCollection<Operacion> ImportFromCsvFile(string filePath, char sep = ';')
        {
            var all = File.ReadAllLines(filePath);
            return ImportFromLines(all, sep);
        }

        private static DateTime ParseFechaFlexible(string? txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
                return DateTime.Today;

            // Prueba varios formatos
            var fmts = new[]
            {
                "yyyy-MM-dd","dd/MM/yyyy","d/M/yyyy","MM/dd/yyyy","M/d/yyyy",
                "dd-MM-yyyy","d-M-yyyy","yyyyMMdd","ddMMyyyy"
            };

            foreach (var f in fmts)
            {
                if (DateTime.TryParseExact(txt, f, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out var dt))
                    return dt;
            }

            // última oportunidad: parse normal
            if (DateTime.TryParse(txt, out var any))
                return any;

            return DateTime.Today;
        }
    }
}
