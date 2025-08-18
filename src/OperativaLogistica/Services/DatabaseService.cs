using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Servicio SQLite muy simple. Crea la BD si no existe y permite vaciar un día.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connString;

        public DatabaseService()
        {
            // BD local en carpeta de la app
            _dbPath = Path.Combine(AppContext.BaseDirectory, "operativa.db");
            _connString = $"Data Source={_dbPath};Cache=Shared";
            EnsureCreated();
        }

        /// <summary>Devuelve una conexión abierta.</summary>
        public SqliteConnection GetConnection()
        {
            var cnn = new SqliteConnection(_connString);
            cnn.Open();
            return cnn;
        }

        /// <summary>Crea tabla si no existe (columnas coherentes con el modelo).</summary>
        private void EnsureCreated()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

            using var cnn = GetConnection();
            using var cmd = cnn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Operaciones (
    Id            INTEGER PRIMARY KEY,
    Transportista TEXT    NOT NULL DEFAULT '',
    Matricula     TEXT    NOT NULL DEFAULT '',
    Muelle        TEXT    NOT NULL DEFAULT '',
    Estado        TEXT    NOT NULL DEFAULT '',
    Destino       TEXT    NOT NULL DEFAULT '',
    Llegada       TEXT    NOT NULL DEFAULT '',
    LlegadaReal   TEXT    NOT NULL DEFAULT '',
    SalidaReal    TEXT    NOT NULL DEFAULT '',
    SalidaTope    TEXT    NOT NULL DEFAULT '',
    Observaciones TEXT    NOT NULL DEFAULT '',
    Incidencias   TEXT    NOT NULL DEFAULT '',
    Precinto      TEXT    NOT NULL DEFAULT '',
    Lex           INTEGER NOT NULL DEFAULT 0,
    Fecha         TEXT    NOT NULL,           -- ISO yyyy-MM-dd
    Lado          TEXT    NOT NULL
);
";
            cmd.ExecuteNonQuery();
        }

        /// <summary>Elimina todas las operaciones de una fecha.</summary>
        public void DeleteDay(DateOnly fecha)
        {
            var iso = fecha.ToString("yyyy-MM-dd");
            using var cnn = GetConnection();
            using var cmd = cnn.CreateCommand();
            cmd.CommandText = "DELETE FROM Operaciones WHERE Fecha = $f";
            cmd.Parameters.AddWithValue("$f", iso);
            cmd.ExecuteNonQuery();
        }
    }
}
