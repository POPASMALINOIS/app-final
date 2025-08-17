
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
        public DatabaseService()
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OperativaLogistica");
            Directory.CreateDirectory(baseDir);
            _dbPath = Path.Combine(baseDir, "data.db");
            Initialize();
        }
        private void Initialize()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Operaciones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Transportista TEXT,
    Matricula TEXT,
    Muelle TEXT,
    Estado TEXT,
    Destino TEXT,
    Llegada TEXT,
    LlegadaReal TEXT,
    SalidaReal TEXT,
    SalidaTope TEXT,
    Observaciones TEXT,
    Incidencias TEXT,
    Fecha TEXT
);";
            cmd.ExecuteNonQuery();
        }
        public void Upsert(Operacion op)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            if (op.Id == 0)
            {
                cmd.CommandText = @"
INSERT INTO Operaciones
(Transportista, Matricula, Muelle, Estado, Destino, Llegada, LlegadaReal, SalidaReal, SalidaTope, Observaciones, Incidencias, Fecha)
VALUES ($t,$m,$mu,$e,$d,$l,$lr,$sr,$st,$o,$i,$f);
SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$t", op.Transportista);
                cmd.Parameters.AddWithValue("$m", op.Matricula);
                cmd.Parameters.AddWithValue("$mu", op.Muelle);
                cmd.Parameters.AddWithValue("$e", op.Estado);
                cmd.Parameters.AddWithValue("$d", op.Destino);
                cmd.Parameters.AddWithValue("$l", op.Llegada);
                cmd.Parameters.AddWithValue("$lr", (object?)op.LlegadaReal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$sr", (object?)op.SalidaReal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$st", op.SalidaTope);
                cmd.Parameters.AddWithValue("$o", op.Observaciones);
                cmd.Parameters.AddWithValue("$i", op.Incidencias);
                cmd.Parameters.AddWithValue("$f", op.Fecha.ToString("yyyy-MM-dd"));
                var newId = (long)cmd.ExecuteScalar()!;
                op.Id = (int)newId;
            }
            else
            {
                cmd.CommandText = @"
UPDATE Operaciones SET
Transportista=$t, Matricula=$m, Muelle=$mu, Estado=$e, Destino=$d, Llegada=$l, LlegadaReal=$lr, SalidaReal=$sr,
SalidaTope=$st, Observaciones=$o, Incidencias=$i, Fecha=$f
WHERE Id=$id";
                cmd.Parameters.AddWithValue("$t", op.Transportista);
                cmd.Parameters.AddWithValue("$m", op.Matricula);
                cmd.Parameters.AddWithValue("$mu", op.Muelle);
                cmd.Parameters.AddWithValue("$e", op.Estado);
                cmd.Parameters.AddWithValue("$d", op.Destino);
                cmd.Parameters.AddWithValue("$l", op.Llegada);
                cmd.Parameters.AddWithValue("$lr", (object?)op.LlegadaReal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$sr", (object?)op.SalidaReal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$st", op.SalidaTope);
                cmd.Parameters.AddWithValue("$o", op.Observaciones);
                cmd.Parameters.AddWithValue("$i", op.Incidencias);
                cmd.Parameters.AddWithValue("$f", op.Fecha.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$id", op.Id);
                cmd.ExecuteNonQuery();
            }
        }
        public List<Operacion> GetByDate(DateOnly date)
        {public void DeleteByDate(DateOnly date)
{
    using var connection = new SqliteConnection($"Data Source={_dbPath}");
    connection.Open();
    var cmd = connection.CreateCommand();
    cmd.CommandText = "DELETE FROM Operaciones WHERE Fecha=$f";
    cmd.Parameters.AddWithValue("$f", date.ToString("yyyy-MM-dd"));
    cmd.ExecuteNonQuery();
}

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Operaciones WHERE Fecha=$f ORDER BY Id ASC";
            cmd.Parameters.AddWithValue("$f", date.ToString("yyyy-MM-dd"));
            using var reader = cmd.ExecuteReader();
            var list = new List<Operacion>();
            while (reader.Read())
            {
                list.Add(new Operacion
                {
                    Id = reader.GetInt32(0),
                    Transportista = reader.GetString(1),
                    Matricula = reader.GetString(2),
                    Muelle = reader.GetString(3),
                    Estado = reader.GetString(4),
                    Destino = reader.GetString(5),
                    Llegada = reader.GetString(6),
                    LlegadaReal = reader.IsDBNull(7) ? null : reader.GetString(7),
                    SalidaReal = reader.IsDBNull(8) ? null : reader.GetString(8),
                    SalidaTope = reader.GetString(9),
                    Observaciones = reader.GetString(10),
                    Incidencias = reader.GetString(11),
                    Fecha = DateOnly.Parse(reader.GetString(12))
                });
            }
            return list;
        }
    }
}
