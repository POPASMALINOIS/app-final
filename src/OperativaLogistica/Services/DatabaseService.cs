using Microsoft.Data.Sqlite;
using OperativaLogistica.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace OperativaLogistica.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService(string dbPath = "operativa.db")
        {
            _dbPath = dbPath;
            if (!File.Exists(_dbPath))
                Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Operaciones (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Fecha TEXT NOT NULL,
                Transportista TEXT,
                Matricula TEXT,
                Muelle TEXT,
                Estado TEXT,
                Destino TEXT,
                Llegada TEXT,
                SalidaTope TEXT,
                Observaciones TEXT,
                Incidencias TEXT,
                LlegadaReal TEXT,
                SalidaReal TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public List<Operacion> GetByDate(DateOnly date)
        {
            var list = new List<Operacion>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Operaciones WHERE Fecha=$f ORDER BY Id";
            cmd.Parameters.AddWithValue("$f", date.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Operacion
                {
                    Id = reader.GetInt32(0),
                    Fecha = DateOnly.Parse(reader.GetString(1)),
                    Transportista = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Matricula = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Muelle = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Estado = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Destino = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Llegada = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    SalidaTope = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Observaciones = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Incidencias = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    LlegadaReal = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    SalidaReal = reader.IsDBNull(12) ? "" : reader.GetString(12)
                });
            }

            return list;
        }

        public void DeleteByDate(DateOnly date)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Operaciones WHERE Fecha=$f";
            cmd.Parameters.AddWithValue("$f", date.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        public void Upsert(Operacion op)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();

            if (op.Id == 0)
            {
                cmd.CommandText =
                @"
                INSERT INTO Operaciones 
                (Fecha, Transportista, Matricula, Muelle, Estado, Destino, Llegada, SalidaTope, Observaciones, Incidencias, LlegadaReal, SalidaReal)
                VALUES ($f,$t,$m,$mu,$e,$d,$l,$s,$o,$i,$lr,$sr);
                ";
            }
            else
            {
                cmd.CommandText =
                @"
                UPDATE Operaciones SET
                    Fecha=$f, Transportista=$t, Matricula=$m, Muelle=$mu, Estado=$e, Destino=$d,
                    Llegada=$l, SalidaTope=$s, Observaciones=$o, Incidencias=$i,
                    LlegadaReal=$lr, SalidaReal=$sr
                WHERE Id=$id;
                ";
                cmd.Parameters.AddWithValue("$id", op.Id);
            }

            cmd.Parameters.AddWithValue("$f", op.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$t", op.Transportista ?? "");
            cmd.Parameters.AddWithValue("$m", op.Matricula ?? "");
            cmd.Parameters.AddWithValue("$mu", op.Muelle ?? "");
            cmd.Parameters.AddWithValue("$e", op.Estado ?? "");
            cmd.Parameters.AddWithValue("$d", op.Destino ?? "");
            cmd.Parameters.AddWithValue("$l", op.Llegada ?? "");
            cmd.Parameters.AddWithValue("$s", op.SalidaTope ?? "");
            cmd.Parameters.AddWithValue("$o", op.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$i", op.Incidencias ?? "");
            cmd.Parameters.AddWithValue("$lr", op.LlegadaReal ?? "");
            cmd.Parameters.AddWithValue("$sr", op.SalidaReal ?? "");

            cmd.ExecuteNonQuery();

            // Obtener el último Id insertado en SQLite (sustituto de LastInsertRowId)
            if (op.Id == 0)
            {
                using var lastCmd = connection.CreateCommand();
                lastCmd.CommandText = "SELECT last_insert_rowid();";
                var result = lastCmd.ExecuteScalar();
                if (result is long l) op.Id = (int)l;
            }
        }
    }
}
