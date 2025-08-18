using System;
using System.Data.SQLite;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Acceso mínimo a SQLite para operaciones sencillas.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService()
        {
            var baseDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OperativaLogistica");

            System.IO.Directory.CreateDirectory(baseDir);
            _dbPath = System.IO.Path.Combine(baseDir, "operativa.db");
        }

        /// <summary>
        /// Abre y devuelve una conexión SQLite lista para usar.
        /// Quien llama es responsable de disponerla.
        /// </summary>
        public SQLiteConnection GetConnection()
        {
            var cs = $"Data Source={_dbPath};Version=3;";
            var cn = new SQLiteConnection(cs);
            cn.Open();
            return cn;
        }

        /// <summary>
        /// Borra todas las filas del día/lado indicado (utilidad para “Nueva jornada”).
        /// </summary>
        public void DeleteDay(DateOnly fecha, string lado)
        {
            using var cn = GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"DELETE FROM Operaciones WHERE Fecha = @f AND Lado = @lado;";
            cmd.Parameters.AddWithValue("@f", fecha.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@lado", lado ?? string.Empty);
            cmd.ExecuteNonQuery();
        }
    }
}
