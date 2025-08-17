using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OperativaLogistica.Models;

namespace OperativaLogistica.Converters
{
    // Semáforo de tiempos: compara LlegadaReal/SalidaReal con SalidaTope
    public class RowStatusToBrushConverter : IValueConverter
    {
        private static Brush Parse(string hex) => (SolidColorBrush)(new BrushConverter().ConvertFrom(hex) ?? Brushes.Transparent);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Operacion op)
            {
                // SalidaTope -> HH:mm
                if (!TimeSpan.TryParse(op.SalidaTope, out var tope)) return Brushes.Transparent;

                // Si ya tiene salida real, la comparamos
                if (TimeSpan.TryParse(op.SalidaReal, out var sal))
                {
                    if (sal > tope) return new SolidColorBrush(Color.FromArgb(40, 244, 67, 54));   // rojo tenue
                    return new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));                   // verde tenue
                }

                // Si aún no tiene salida, usamos llegada real para semáforo preventivo
                if (TimeSpan.TryParse(op.LlegadaReal, out var llr))
                {
                    var now = DateTime.Now.TimeOfDay;
                    var restante = tope - now;
                    if (restante.TotalMinutes <= 0) return new SolidColorBrush(Color.FromArgb(40, 244, 67, 54));
                    if (restante.TotalMinutes <= 30) return new SolidColorBrush(Color.FromArgb(30, 255, 193, 7)); // ámbar
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
