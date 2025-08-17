using System;
using System.ComponentModel;

namespace OperativaLogistica.Models
{
    public class Operacion
    {
        [Browsable(false)] public int Id { get; set; }

        public string Transportista { get; set; } = "";
        public string Matricula { get; set; } = "";
        public string Muelle { get; set; } = "";

        // Desplegable
        public string Estado { get; set; } = "";

        public string Destino { get; set; } = "";
        public string Llegada { get; set; } = "";
        public string? LlegadaReal { get; set; }
        public string? SalidaReal { get; set; }
        public string SalidaTope { get; set; } = "";
        public string Observaciones { get; set; } = "";

        // Desplegable
        public string Incidencias { get; set; } = "";

        // Nuevos
        public string Precinto { get; set; } = "";
        public bool Lex { get; set; } = false;

        [Browsable(false)] public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}
