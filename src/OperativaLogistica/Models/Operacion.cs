using System;

namespace OperativaLogistica.Models
{
    public class Operacion
    {
        // ===== Identificador =====
        public int Id { get; set; }

        // Alias de compatibilidad con código antiguo que usa "Idex"
        public int Idex
        {
            get => Id;
            set => Id = value;
        }

        // ===== Datos principales =====
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

        /// <summary>Precinto (requisito 17).</summary>
        public string Precinto { get; set; } = string.Empty;

        /// <summary>Alias de compatibilidad por si algún sitio usa el typo "Precinct".</summary>
        public string Precinct
        {
            get => Precinto;
            set => Precinto = value;
        }

        /// <summary>Marcaje LEX (requisito 19).</summary>
        public bool Lex { get; set; } = false;

        /// <summary>Día de la operativa (DateOnly para evitar horas).</summary>
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        /// <summary>Lado operativo (LADO 0..LADO 9, etc).</summary>
        public string Lado { get; set; } = "LADO 0";
    }
}
