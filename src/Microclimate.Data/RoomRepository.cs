using Microclimate.Core.Models;
using Microsoft.Data.Sqlite;

namespace Microclimate.Data;

public sealed class RoomRepository
{
    private const int SqliteConstraintError = 19;
    private readonly SqliteDatabase _db;

    public RoomRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <exception cref="ArgumentException">Порожня назва.</exception>
    /// <exception cref="InvalidOperationException">Приміщення з такою назвою вже існує.</exception>
    public Room Add(string name, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name must not be empty.", nameof(name));

        var trimmed = name.Trim();

        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO rooms (name, location) VALUES ($name, $location); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", trimmed);
        command.Parameters.AddWithValue("$location", (object?)location ?? DBNull.Value);

        try
        {
            var id = (long)command.ExecuteScalar()!;
            return new Room((int)id, trimmed, location);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintError)
        {
            throw new InvalidOperationException($"Room '{trimmed}' already exists.", ex);
        }
    }

    public Room? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, location FROM rooms WHERE name = $name;";
        command.Parameters.AddWithValue("$name", name.Trim());

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public IReadOnlyList<Room> GetAll()
    {
        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, location FROM rooms ORDER BY name;";

        var result = new List<Room>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(Map(reader));

        return result;
    }

    private static Room Map(SqliteDataReader reader) =>
        new(reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
}
