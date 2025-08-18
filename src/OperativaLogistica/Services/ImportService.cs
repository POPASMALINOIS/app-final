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
    public class ImportService
    {
        /// <summary>
        /// Importa operaciones desde un archivo CSV/Excel.
        /// - CSV: separador ';' con cabecera en primera línea.
        /// - Excel: primera fila = cabecera. Mapeo por nombre de columna (tolerante a acentos/espacios).
        /// </summary>
        public IEnumerable<Operacion> Importar(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return Array.Empty<Operacion>();

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".csv"  => ImportCsv(filePath, fechaPorDefecto, ladoPorDefecto),
                ".xls"  => ImportExcel(filePath, fechaPorDefecto, ladoPorDefecto),
                ".xlsx" => ImportExcel(filePath, fechaPorDefecto, ladoPorDefecto),
                _       => Array.Empty<Operacion>()
            };
        }

        // =====================================================================
        // CSV
        // =====================================================================
        private static IEnumerable<Operacion> ImportCsv(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            using var sr = new StreamReader(filePath, DetectEncoding(filePath));

            // Cabecera
            var headerLine = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine)) return list;

            var headers = headerLine.Split(';').Select(Norm).ToArray();
            var map = BuildHeaderMap(headers);

            // Filas
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');

                string Get(string k) => GetByHeader(map, headers, cols, k);
                string GetFecha()    => Get("FECHA");

                var op = CreateOperacion(
                    Get("ID"),
                    Get("TRANSPORTISTA"),
                    Get("MATRICULA"),
                    Get("MUELLE"),
                    Get("ESTADO"),
                    Get("DESTINO"),
                    Get("LLEGADA"),
                    Get("LLEGADA REAL"),
                    Get("SALIDA REAL"),
                    Get("SALIDA TOPE"),
                    Get("OBSERVACIONES"),
                    Get("INCIDENCIAS"),
                    GetFecha(),
                    Get("PRECINTO"),
                    Get("LEX"),
                    Get("LADO"),
                    fechaPorDefecto, ladoPorDefecto);

                list.Add(op);
            }

            return list;
        }

        private static Encoding DetectEncoding(string path)
        {
            // Sencillo: si empieza con BOM UTF8, usa UTF8; si no, Default.
            using var fs = File.OpenRead(path);
            var bom = new byte[3];
            _ = fs.Read(bom, 0, 3);
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
            return Encoding.UTF8;
        }

        private static string GetByHeader(Dictionary<string, int> map, string[] headers, string[] cols, string key)
        {
            if (!map.TryGetValue(key, out var idx) || idx < 0 || idx >= cols.Length) return "";
            return (cols[idx] ?? "").Trim();
        }

        // =====================================================================
        // Excel
        // =====================================================================
        private static IEnumerable<Operacion> ImportExcel(string filePath, DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            var list = new List<Operacion>();
            using var wb = new XLWorkbook(filePath);

            var ws = wb.Worksheets.Count > 0 ? wb.Worksheet(1) : null;
            if (ws is null) return list;

            var used = ws.RangeUsed();
            if (used is null) return list;

            int r0 = used.FirstRow().RowNumber();
            int c0 = used.FirstColumn().ColumnNumber();
            int r1 = used.LastRow().RowNumber();
            int c1 = used.LastColumn().ColumnNumber();

            // Cabeceras normalizadas
            var headers = new List<string>();
            for (int c = c0; c <= c1; c++)
            {
                var raw = ws.Cell(r0, c).GetString();
                headers.Add(Norm(raw));
            }
            var map = BuildHeaderMap(headers.ToArray());

            bool RowIsEmpty(int row)
            {
                for (int c = c0; c <= c1; c++)
                    if (!string.IsNullOrWhiteSpace(ws.Cell(row, c).GetString()))
                        return false;
                return true;
            }

            string Get(int row, string key)
            {
                if (!map.TryGetValue(key, out int colIndex)) return "";
                if (colIndex < 0) return "";
                int c = c0 + colIndex;        // map guarda índice relativo desde c0
                if (c < c0 || c > c1) return "";
                return ws.Cell(row, c).GetString().Trim();
            }

            string GetFecha(int row)
            {
                if (!map.TryGetValue("FECHA", out int colIndex)) return "";
                int c = c0 + colIndex;
                if (c < c0 || c > c1) return "";
                var cell = ws.Cell(row, c);
                if (TryGetDateOnly(cell, out var d))
                    return d.ToString("yyyy-MM-dd");
                return cell.GetString().Trim();
            }

            for (int r = r0 + 1; r <= r1; r++)
            {
                if (RowIsEmpty(r)) continue;

                var op = CreateOperacion(
                    Get(r, "ID"),
                    Get(r, "TRANSPORTISTA"),
                    Get(r, "MATRICULA"),
                    Get(r, "MUELLE"),
                    Get(r, "ESTADO"),
                    Get(r, "DESTINO"),
                    Get(r, "LLEGADA"),
                    Get(r, "LLEGADA REAL"),
                    Get(r, "SALIDA REAL"),
                    Get(r, "SALIDA TOPE"),
                    Get(r, "OBSERVACIONES"),
                    Get(r, "INCIDENCIAS"),
                    GetFecha(r),
                    Get(r, "PRECINTO"),
                    Get(r, "LEX"),
                    Get(r, "LADO"),
                    fechaPorDefecto, ladoPorDefecto);

                list.Add(op);
            }

            return list;
        }

        private static bool TryGetDateOnly(IXLCell cell, out DateOnly d)
        {
            // Numérico/DateTime de Excel
            if (cell.DataType == XLDataType.DateTime)
            {
                d = DateOnly.FromDateTime(cell.GetDateTime());
                return true;
            }

            var s = (cell.GetString() ?? "").Trim();
            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return true;
            if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return true;

            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
            {
                d = DateOnly.FromDateTime(dt);
                return true;
            }

            d = default;
            return false;
        }

        // =====================================================================
        // COMMON
        // =====================================================================

        /// <summary>
        /// Normaliza una cabecera: Trim, UpperInvariant, colapsa espacios y quita acentos.
        /// </summary>
        private static string Norm(string? s)
        {
            s ??= "";
            s = s.Trim();
            // colapsar espacios
            s = string.Join(" ", s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            // quitar acentos y a mayúsculas
            s = RemoveDiacritics(s).ToUpperInvariant();
            return s;
        }

        private static string RemoveDiacritics(string text)
        {
            var norm = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: norm.Length);
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Construye un mapa cabecera->índice (0..N-1) sobre una lista de cabeceras normalizadas.
        /// Tolera alias típicos y columnas faltantes (se asigna -1).
        /// </summary>
        private static Dictionary<string, int> BuildHeaderMap(string[] normHeaders)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Primero, registra lo que venga del fichero
            for (int i = 0; i < normHeaders.Length; i++)
                map[normHeaders[i]] = i;

            // Aliases habituales -> clave estándar
            var alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // estándar -> variaciones aceptadas
                ["ID"]               = "ID",
                ["TRANSPORTISTA"]    = "TRANSPORTISTA",
                ["MATRICULA"]        = "MATRICULA",          // acepta MATRÍCULA por diacríticos (ya removidos)
                ["MUELLE"]           = "MUELLE",
                ["ESTADO"]           = "ESTADO",
                ["DESTINO"]          = "DESTINO",

                ["LLEGADA"]          = "LLEGADA",
                ["LLEGADA REAL"]     = "LLEGADA REAL",
                ["SALIDA REAL"]      = "SALIDA REAL",
                ["SALIDA TOPE"]      = "SALIDA TOPE",

                ["OBSERVACIONES"]    = "OBSERVACIONES",
                ["INCIDENCIAS"]      = "INCIDENCIAS",
                ["FECHA"]            = "FECHA",

                ["PRECINTO"]         = "PRECINTO",
                ["LEX"]              = "LEX",
                ["LADO"]             = "LADO",
            };

            // Para cada clave estándar, intenta localizar su índice por nombre o por variantes
            var final = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var std in alias.Keys)
            {
                // si existe la estándar tal cual
                if (TryFindIndex(normHeaders, std, out var idx))
                {
                    final[std] = idx;
                    continue;
                }

                // Variaciones razonables adicionales
                var candidates = std switch
                {
                    "MATRICULA"     => new[] { "MATRICULA", "MATRICULAS", "MATRICULA CAMION", "MATRICULA CAMIÓN" },
                    "LLEGADA REAL"  => new[] { "LLEGADA REAL", "LLEGADAREAL" },
                    "SALIDA REAL"   => new[] { "SALIDA REAL", "SALIDAREAL" },
                    "SALIDA TOPE"   => new[] { "SALIDA TOPE", "SALIDATOPE", "SALIDA PLAN" },
                    "OBSERVACIONES" => new[] { "OBSERVACIONES", "OBSERVACION" },
                    "INCIDENCIAS"   => new[] { "INCIDENCIAS", "INCIDENCIA" },
                    _               => new[] { std }
                };

                var found = candidates.FirstOrDefault(c => TryFindIndex(normHeaders, c, out _));
                if (found != null && TryFindIndex(normHeaders, found, out var idx2))
                    final[std] = idx2;
                else
                    final[std] = -1; // no existe
            }

            return final;

            static bool TryFindIndex(string[] arr, string key, out int index)
            {
                for (int i = 0; i < arr.Length; i++)
                    if (string.Equals(arr[i], key, StringComparison.OrdinalIgnoreCase))
                    { index = i; return true; }
                index = -1; return false;
            }
        }

        // =====================================================================
        // Crear modelo
        // =====================================================================
        private static Operacion CreateOperacion(
            string id, string transportista, string matricula, string muelle, string estado, string destino,
            string llegada, string llegadaReal, string salidaReal, string salidaTope,
            string observaciones, string incidencias, string fecha, string precinto, string lex, string lado,
            DateOnly fechaPorDefecto, string ladoPorDefecto)
        {
            static DateOnly ParseFecha(string s, DateOnly fallback)
            {
                if (string.IsNullOrWhiteSpace(s)) return fallback;

                if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d)) return d;

                if (DateOnly.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) return d;

                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                    return DateOnly.FromDateTime(dt);

                return fallback;
            }

            static bool ParseBool(string s)
            {
                var t = (s ?? "").Trim();
                return t.Equals("true", StringComparison.OrdinalIgnoreCase)
                       || t.Equals("sí", StringComparison.OrdinalIgnoreCase)
                       || t.Equals("si", StringComparison.OrdinalIgnoreCase)
                       || t.Equals("x", StringComparison.OrdinalIgnoreCase)
                       || t == "1";
            }

            return new Operacion
            {
                Id            = int.TryParse(id, out var idVal) ? idVal : 0,
                Transportista = transportista ?? string.Empty,
                Matricula     = matricula ?? string.Empty,
                Muelle        = muelle ?? string.Empty,
                Estado        = estado ?? string.Empty,
                Destino       = destino ?? string.Empty,

                Llegada       = llegada ?? string.Empty,
                LlegadaReal   = llegadaReal ?? string.Empty,
                SalidaReal    = salidaReal ?? string.Empty,
                SalidaTope    = salidaTope ?? string.Empty,

                Observaciones = observaciones ?? string.Empty,
                Incidencias   = incidencias ?? string.Empty,

                Fecha         = ParseFecha(fecha, fechaPorDefecto),
                Precinto      = precinto ?? string.Empty,
                Lex           = ParseBool(lex),
                Lado          = string.IsNullOrWhiteSpace(lado) ? ladoPorDefecto : lado.Trim()
            };
        }
    }
}
