using System.Globalization;
using Microclimate.Core.Models;
using Microclimate.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Microclimate.Data;

public sealed class ReadingRepository
{
    private const int SqliteConstraintError = 19;
    private const string TimestampFormat = "O";

    private const string SelectColumns =
        "SELECT id, room_id, timestamp_utc, temperature_c, humidity_percent, co2_ppm FROM readings ";

    private readonly SqliteDatabase _db;

    public ReadingRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Зберігає вимірювання разом із сповіщеннями в ОДНІЙ транзакції:
    /// або збережеться все, або нічого.
    /// </summary>
    /// <returns>Id нового вимірювання.</returns>
    /// <exception cref="ArgumentException">Вимірювання некоректне.</exception>
    /// <exception cref="InvalidOperationException">Приміщення не існує.</exception>
    public long SaveWithAlerts(int roomId, Reading reading, IEnumerable<Alert> alerts)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(alerts);

        var errors = ReadingValidator.Validate(reading);
        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(reading));

        using var connection = _db.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            long readingId;
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText =
                    "INSERT INTO readings (room_id, timestamp_utc, temperature_c, humidity_percent, co2_ppm) " +
                    "VALUES ($room, $ts, $t, $h, $co2); SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("$room", roomId);
                insert.Parameters.AddWithValue("$ts", FormatTimestamp(reading.TimestampUtc));
                insert.Parameters.AddWithValue("$t", reading.TemperatureC);
                insert.Parameters.AddWithValue("$h", reading.HumidityPercent);
                insert.Parameters.AddWithValue("$co2", reading.Co2Ppm);
                readingId = (long)insert.ExecuteScalar()!;
            }

            foreach (var alert in alerts)
            {
                using var insertAlert = connection.CreateCommand();
                insertAlert.Transaction = transaction;
                insertAlert.CommandText =
                    "INSERT INTO alerts (reading_id, type, severity, message) VALUES ($r, $type, $sev, $msg);";
                insertAlert.Parameters.AddWithValue("$r", readingId);
                insertAlert.Parameters.AddWithValue("$type", alert.Type.ToString());
                insertAlert.Parameters.AddWithValue("$sev", alert.Severity.ToString());
                insertAlert.Parameters.AddWithValue("$msg", alert.Message);
                insertAlert.ExecuteNonQuery();
            }

            transaction.Commit();
            return readingId;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintError)
        {
            transaction.Rollback();
            throw new InvalidOperationException(
                $"Cannot save reading: room {roomId} does not exist or data violates constraints.", ex);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>Останні <paramref name="count"/> вимірювань (від новіших до старіших).</summary>
    public IReadOnlyList<Reading> GetLatest(int roomId, int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + "WHERE room_id = $room ORDER BY timestamp_utc DESC LIMIT $n;";
        command.Parameters.AddWithValue("$room", roomId);
        command.Parameters.AddWithValue("$n", count);
        return ReadAll(command);
    }

    /// <summary>Вимірювання за період [fromUtc, toUtc], від старіших до новіших.</summary>
    public IReadOnlyList<Reading> GetRange(int roomId, DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc > toUtc)
            throw new ArgumentException("'from' must not be later than 'to'.", nameof(fromUtc));

        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns +
            "WHERE room_id = $room AND timestamp_utc >= $from AND timestamp_utc <= $to ORDER BY timestamp_utc ASC;";
        command.Parameters.AddWithValue("$room", roomId);
        command.Parameters.AddWithValue("$from", FormatTimestamp(fromUtc));
        command.Parameters.AddWithValue("$to", FormatTimestamp(toUtc));
        return ReadAll(command);
    }

    /// <summary>Середні значення та максимум CO2; null, якщо вимірювань немає.</summary>
    public ReadingStats? GetStats(int roomId)
    {
        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*), AVG(temperature_c), AVG(humidity_percent), AVG(co2_ppm), MAX(co2_ppm) " +
            "FROM readings WHERE room_id = $room;";
        command.Parameters.AddWithValue("$room", roomId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var count = reader.GetInt32(0);
        if (count == 0)
            return null;

        return new ReadingStats(count, reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4));
    }

    private static List<Reading> ReadAll(SqliteCommand command)
    {
        var result = new List<Reading>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var timestamp = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            result.Add(new Reading(timestamp, reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5))
            {
                Id = reader.GetInt64(0),
                RoomId = reader.GetInt32(1)
            });
        }

        return result;
    }

    private static string FormatTimestamp(DateTime value) =>
        value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);
}
