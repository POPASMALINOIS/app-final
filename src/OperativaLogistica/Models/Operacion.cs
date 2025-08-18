using System;

namespace OperativaLogistica.Models
{
    public class Operacion
    {
        public int Id { get; set; }

        public string Transportista { get; set; } = string.Empty;
        public string Matricula     { get; set; } = string.Empty;
        public string Muelle        { get; set; } = string.Empty;
        public string Estado        { get; set; } = string.Empty;
        public string Destino       { get; set; } = string.Empty;

        public string Llegada       { get; set; } = string.Empty;
        public string LlegadaReal   { get; set; } = string.Empty;
        public string SalidaReal    { get; set; } = string.Empty;
        public string SalidaTope    { get; set; } = string.Empty;

        public string Observaciones { get; set; } = string.Empty;
        public string Incidencias   { get; set; } = string.Empty;

        /// <summary>Día de la operativa (DateOnly para evitar horas).</summary>
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        /// <summary>Lado operativo (LADO 0..LADO 9, etc).</summary>
        public string Lado { get; set; } = "LADO 0";
    }
}
