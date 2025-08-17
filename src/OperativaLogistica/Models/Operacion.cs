
using System;

namespace OperativaLogistica.Models
{
    public class Operacion
    {
        public int Id { get; set; }
        public string Transportista { get; set; } = "";
        public string Matricula { get; set; } = "";
        public string Muelle { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Destino { get; set; } = "";
        public string Llegada { get; set; } = "";
        public string? LlegadaReal { get; set; }
        public string? SalidaReal { get; set; }
        public string SalidaTope { get; set; } = "";
        public string Observaciones { get; set; } = "";
        public string Incidencias { get; set; } = "";
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}
