using System.Globalization;
using Microclimate.Core.Models;

namespace Microclimate.Data;

public sealed class AlertRepository
{
    private readonly SqliteDatabase _db;

    public AlertRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>Останні сповіщення приміщення (від новіших до старіших).</summary>
    public IReadOnlyList<StoredAlert> GetByRoom(int roomId, int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");

        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.id, a.reading_id, r.timestamp_utc, a.type, a.severity, a.message
            FROM alerts a
            JOIN readings r ON r.id = a.reading_id
            WHERE r.room_id = $room
            ORDER BY r.timestamp_utc DESC, a.id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$room", roomId);
        command.Parameters.AddWithValue("$limit", limit);

        var result = new List<StoredAlert>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var timestamp = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var alert = new Alert(
                Enum.Parse<AlertType>(reader.GetString(3)),
                Enum.Parse<AlertSeverity>(reader.GetString(4)),
                reader.GetString(5));
            result.Add(new StoredAlert(reader.GetInt64(0), reader.GetInt64(1), timestamp, alert));
        }

        return result;
    }

    /// <summary>Кількість сповіщень приміщення за рівнем серйозності.</summary>
    public IReadOnlyDictionary<AlertSeverity, int> CountBySeverity(int roomId)
    {
        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.severity, COUNT(*)
            FROM alerts a
            JOIN readings r ON r.id = a.reading_id
            WHERE r.room_id = $room
            GROUP BY a.severity;
            """;
        command.Parameters.AddWithValue("$room", roomId);

        var result = new Dictionary<AlertSeverity, int>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result[Enum.Parse<AlertSeverity>(reader.GetString(0))] = reader.GetInt32(1);

        return result;
    }
}
