using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using OperativaLogistica.Models;

namespace OperativaLogistica.Services
{
    /// <summary>
    /// Servicio de acceso a datos SQLite usando Microsoft.Data.Sqlite.
    /// - BBDD en %LOCALAPPDATA%/OperativaLogistica/operativa.db (o junto al exe si prefieres).
    /// - Expone GetConnection() (pedido por tu código existente).
    /// - Implementa DeleteDay(DateOnly) que invoca tu UI.
    /// - Crea el esquema si no existe.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService(string? dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                               "OperativaLogistica", "operativa.db")
                : dbPath;

            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            EnsureDatabase();
        }

        /// <summary>
        /// Devuelve una conexión ABIERTA lista para usar.
        /// </summary>
        public SqliteConnection GetConnection()
        {
            var cn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadWriteCreate;");
            cn.Open();
            return cn;
        }

        /// <summary>
        /// Borra todas las operaciones de un día (Fecha = day).
        /// Llamado por “Nueva jornada”.
        /// </summary>
        public void DeleteDay(DateOnly day)
        {
            using var cn = GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                DELETE FROM Operaciones
                WHERE Fecha = $fecha;
            """;
            cmd.Parameters.AddWithValue("$fecha", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Devuelve las operaciones del día indicado.
        /// </summary>
        public List<Operacion> GetByDate(DateOnly day)
        {
            var list = new List<Operacion>();
            using var cn = GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Transportista, Matricula, Muelle, Estado, Destino,
                       Llegada, LlegadaReal, SalidaReal, SalidaTope,
                       Observaciones, Incidencias, Fecha, Precinto, LEX
                FROM Operaciones
                WHERE Fecha = $fecha
                ORDER BY Id;
            """;
            cmd.Parameters.AddWithValue("$fecha", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var op = new Operacion
                {
                    Id = rd.GetInt32(0),
                    Transportista = rd.IsDBNull(1) ? null : rd.GetString(1),
                    Matricula     = rd.IsDBNull(2) ? null : rd.GetString(2),
                    Muelle        = rd.IsDBNull(3) ? null : rd.GetString(3),
                    Estado        = rd.IsDBNull(4) ? null : rd.GetString(4),
                    Destino       = rd.IsDBNull(5) ? null : rd.GetString(5),
                    Llegada       = rd.IsDBNull(6) ? null : rd.GetString(6),
                    LlegadaReal   = rd.IsDBNull(7) ? null : rd.GetString(7),
                    SalidaReal    = rd.IsDBNull(8) ? null : rd.GetString(8),
                    SalidaTope    = rd.IsDBNull(9) ? null : rd.GetString(9),
                    Observaciones = rd.IsDBNull(10) ? null : rd.GetString(10),
                    Incidencias   = rd.IsDBNull(11) ? null : rd.GetString(11),
                    Fecha         = DateOnly.ParseExact(rd.GetString(12), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Precinto      = rd.IsDBNull(13) ? null : rd.GetString(13),
                    LEX           = !rd.IsDBNull(14) && rd.GetBoolean(14),
                };
                list.Add(op);
            }
            return list;
        }

        /// <summary>
        /// Inserta/actualiza una operación simple (ejemplo). Ajusta a tu esquema real si ya lo tienes.
        /// </summary>
        public void Upsert(Operacion op)
        {
            using var cn = GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Operaciones
                  (Id, Transportista, Matricula, Muelle, Estado, Destino, Llegada, LlegadaReal,
                   SalidaReal, SalidaTope, Observaciones, Incidencias, Fecha, Precinto, LEX)
                VALUES
                  ($id, $transportista, $matricula, $muelle, $estado, $destino, $llegada, $llegadaReal,
                   $salidaReal, $salidaTope, $obs, $inc, $fecha, $precinto, $lex)
                ON CONFLICT(Id) DO UPDATE SET
                  Transportista = excluded.Transportista,
                  Matricula     = excluded.Matricula,
                  Muelle        = excluded.Muelle,
                  Estado        = excluded.Estado,
                  Destino       = excluded.Destino,
                  Llegada       = excluded.Llegada,
                  LlegadaReal   = excluded.LlegadaReal,
                  SalidaReal    = excluded.SalidaReal,
                  SalidaTope    = excluded.SalidaTope,
                  Observaciones = excluded.Observaciones,
                  Incidencias   = excluded.Incidencias,
                  Fecha         = excluded.Fecha,
                  Precinto      = excluded.Precinto,
                  LEX           = excluded.LEX;
            """;

            cmd.Parameters.AddWithValue("$id", op.Id);
            cmd.Parameters.AddWithValue("$transportista", (object?)op.Transportista ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$matricula",     (object?)op.Matricula ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$muelle",        (object?)op.Muelle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$estado",        (object?)op.Estado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$destino",       (object?)op.Destino ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$llegada",       (object?)op.Llegada ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$llegadaReal",   (object?)op.LlegadaReal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$salidaReal",    (object?)op.SalidaReal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$salidaTope",    (object?)op.SalidaTope ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$obs",           (object?)op.Observaciones ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$inc",           (object?)op.Incidencias ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$fecha",         op.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$precinto",      (object?)op.Precinto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$lex",           op.LEX);
            cmd.ExecuteNonQuery();
        }

        private void EnsureDatabase()
        {
            using var cn = GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                PRAGMA journal_mode = WAL;

                CREATE TABLE IF NOT EXISTS Operaciones
                (
                    Id            INTEGER PRIMARY KEY,
                    Transportista TEXT    NULL,
                    Matricula     TEXT    NULL,
                    Muelle        TEXT    NULL,
                    Estado        TEXT    NULL,
                    Destino       TEXT    NULL,
                    Llegada       TEXT    NULL,
                    LlegadaReal   TEXT    NULL,
                    SalidaReal    TEXT    NULL,
                    SalidaTope    TEXT    NULL,
                    Observaciones TEXT    NULL,
                    Incidencias   TEXT    NULL,
                    Fecha         TEXT    NOT NULL, -- 'yyyy-MM-dd'
                    Precinto      TEXT    NULL,
                    LEX           INTEGER NOT NULL DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS IX_Operaciones_Fecha ON Operaciones(Fecha);
            """;
            cmd.ExecuteNonQuery();
        }
    }
}
