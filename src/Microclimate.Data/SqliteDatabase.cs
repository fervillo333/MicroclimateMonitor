using Microsoft.Data.Sqlite;

namespace Microclimate.Data;

/// <summary>Створення підключень та ініціалізація схеми БД (SQLite — працює на Windows/Linux/macOS).</summary>
public sealed class SqliteDatabase
{
    private readonly string _connectionString;

    public SqliteDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public static SqliteDatabase ForFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false
        };
        return new SqliteDatabase(builder.ToString());
    }

    /// <summary>Відкриває нове підключення з увімкненими зовнішніми ключами.</summary>
    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    /// <summary>Створює таблиці та індекси, якщо їх ще немає (можна викликати повторно).</summary>
    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = Schema.Sql;
        command.ExecuteNonQuery();
    }
}
