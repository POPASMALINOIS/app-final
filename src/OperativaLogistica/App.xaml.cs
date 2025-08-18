using System.Windows;

namespace OperativaLogistica
{
    public partial class App : Application
    {
        static App()
        {
            // Inicializa el proveedor nativo de SQLite (seguro llamar más de una vez)
            SQLitePCL.Batteries_V2.Init();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SQLitePCL.Batteries_V2.Init();   // por si el estático no se ejecuta antes
            base.OnStartup(e);
        }
    }
}
