using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OperativaLogistica.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace OperativaLogistica.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<SessionViewModel> Sessions { get; } = new();
        [ObservableProperty] private SessionViewModel? selectedSession;

        private readonly Timer _autoTimer = new(180000); // 3 minutos
        public ConfigService Config { get; } = ConfigService.LoadOrCreate();

        public MainViewModel()
        {
            AppPaths.Ensure();
            NewTab();
            _autoTimer.Elapsed += (_, __) => SelectedSession?.AutoSave();
            _autoTimer.Start();
        }

        [RelayCommand]
        private void NewTab()
        {
            var s = new SessionViewModel();
            Sessions.Add(s);
            SelectedSession = s;
        }

        [RelayCommand]
        private void CloseTab()
        {
            if (SelectedSession == null) return;
            var idx = Sessions.IndexOf(SelectedSession);
            Sessions.Remove(SelectedSession);
            if (Sessions.Count == 0) NewTab();
            else SelectedSession = Sessions[Math.Max(0, idx - 1)];
        }

        [RelayCommand] private void NewDay() => SelectedSession?.Load(); // ya borra desde el menú específico si lo deseas

        [RelayCommand]
        private void Import()
        {
            if (SelectedSession == null) return;

            var dlg = new OpenFileDialog { Filter = "CSV/Excel|*.csv;*.xlsx" };
            if (dlg.ShowDialog() == true)
            {
                var list = dlg.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    ? ImportService.FromXlsx(dlg.FileName, SelectedSession.SelectedDate)
                    : ImportService.FromCsv(dlg.FileName, SelectedSession.SelectedDate);

                foreach (var op in list) SelectedSession.Operaciones.Add(op);
                SelectedSession.SaveAll();
            }
        }

        [RelayCommand]
        private void ExportCsv()
        {
            if (SelectedSession == null) return;
            var name = $"operativa_{SelectedSession.SelectedDate:yyyyMMdd}_{SelectedSession.SelectedLado}.csv";
            var path = Path.Combine(AppPaths.Base, name);
            using var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Transportista,Matricula,Muelle,Estado,Destino,Llegada,LlegadaReal,SalidaReal,SalidaTope,Observaciones,Incidencias,Precinto,Lex,Fecha,Lado");
            foreach (var op in SelectedSession.Operaciones)
                sw.WriteLine($"\"{op.Transportista}\",\"{op.Matricula}\",\"{op.Muelle}\",\"{op.Estado}\",\"{op.Destino}\",\"{op.Llegada}\",\"{op.LlegadaReal}\",\"{op.SalidaReal}\",\"{op.SalidaTope}\",\"{op.Observaciones}\",\"{op.Incidencias}\",\"{op.Precinto}\",{(op.Lex ? 1 : 0)},\"{SelectedSession.SelectedDate:yyyy-MM-dd}\",\"{SelectedSession.SelectedLado}\"");
        }

        [RelayCommand]
        private void SavePdf()
        {
            if (SelectedSession == null) return;
            var pdf = PdfService.SaveDailyPdf(SelectedSession.Operaciones, SelectedSession.SelectedDate, SelectedSession.SelectedLado);
            System.Windows.MessageBox.Show($"PDF guardado en:\n{pdf}", "PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void OpenMappingEditor()
        {
            Config.SaveMapping(); // crea si no existe
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", AppPaths.MappingJson) { UseShellExecute = true }); } catch { }
        }
    }
}
