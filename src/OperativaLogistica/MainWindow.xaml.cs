using System;
using System.Collections.Generic;
using System.ComponentModel; // DependencyPropertyDescriptor
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OperativaLogistica
{
    public partial class MainWindow : Window
    {
        // ------- HOOK para cambios de columnas --------

        // Guardamos las suscripciones para poder liberarlas en Unloaded
        private readonly List<(DataGridColumn col, DependencyPropertyDescriptor dpd)> _widthSubscriptions
            = new();

        // Se llama cuando el DataGrid está listo: suscribimos a cambios de Width de cada columna
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Por si el Loaded disparase más de una vez, limpiamos antes
            UnsubscribeAllColumnWidthChanges();

            if (dg.Columns is null || dg.Columns.Count == 0)
                return;

            foreach (var col in dg.Columns)
                SubscribeToColumnWidth(col);

            // Si alguna vez generas columnas por código/AutoGenerate,
            // puedes volver a suscribirte aquí cuando las cambies.
            TakeAndPersistLayoutSnapshot("Loaded");
        }

        // Liberamos todas las suscripciones cuando se descarga el control
        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeAllColumnWidthChanges();
        }

        // Reordenación de columnas (arrastrando encabezados)
        private void DataGrid_ColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            TakeAndPersistLayoutSnapshot("ColumnReordered");
        }

        // Cambio de tamaño del grid (puede afectar a las columnas si usas star/auto)
        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TakeAndPersistLayoutSnapshot("SizeChanged");
        }

        // --------- Suscripción a cambios de Width por columna ---------

        private void SubscribeToColumnWidth(DataGridColumn col)
        {
            // DataGridColumn es DependencyObject y Width es DependencyProperty
            var dpd = DependencyPropertyDescriptor.FromProperty(
                DataGridColumn.WidthProperty, typeof(DataGridColumn));

            if (dpd is null) return;

            EventHandler handler = (_, __) =>
            {
                // Cada vez que cambia el ancho de una columna, tomamos snapshot
                TakeAndPersistLayoutSnapshot("WidthChanged");
            };

            dpd.AddValueChanged(col, handler);
            _widthSubscriptions.Add((col, dpd));
        }

        private void UnsubscribeAllColumnWidthChanges()
        {
            if (_widthSubscriptions.Count == 0) return;

            // Quitamos los handlers de todas las columnas suscritas
            foreach (var (col, dpd) in _widthSubscriptions.ToArray())
            {
                try
                {
                    dpd.RemoveValueChanged(col, (EventHandler)((_, __) =>
                    {
                        // No necesitamos lógica aquí; solo quitamos el handler.
                    }));
                }
                catch
                {
                    // Si por cualquier motivo falla al quitar, lo ignoramos.
                }
            }

            _widthSubscriptions.Clear();
        }

        // --------- Persistencia (o solo inspección) del layout ---------

        private void TakeAndPersistLayoutSnapshot(string reason)
        {
            // Montamos un snapshot ligero del layout actual
            var snapshot = dg.Columns.Select(c => new ColumnLayout
            {
                Header       = c.Header?.ToString() ?? "",
                DisplayIndex = c.DisplayIndex,
                Width        = c.Width.DisplayValue, // ancho real en px
                IsVisible    = c.Visibility == Visibility.Visible
            }).OrderBy(c => c.DisplayIndex).ToList();

            // Aquí puedes llamar a tu servicio de configuración para guardarlo.
            // Ejemplo (si tienes un ConfigService con SaveColumnLayout):
            //
            // _vm?.Config?.SaveColumnLayout("principal", snapshot);
            //
            // Mientras tanto, lo dejamos en Debug para ver que funciona:
            Debug.WriteLine($"[DataGridLayout:{reason}] " +
                            string.Join(" | ", snapshot.Select(s => $"{s.Header}:{s.Width}px@{s.DisplayIndex}")));
        }

        private class ColumnLayout
        {
            public string Header { get; set; } = "";
            public int DisplayIndex { get; set; }
            public double Width { get; set; }
            public bool IsVisible { get; set; }
        }

        // ------- FIN HOOK columnas -------
    }
}
