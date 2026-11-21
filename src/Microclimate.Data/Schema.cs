namespace Microclimate.Data;

/// <summary>SQL-схема. Таблиці у 3НФ: rooms 1—N readings 1—N alerts.</summary>
internal static class Schema
{
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS rooms (
            id       INTEGER PRIMARY KEY AUTOINCREMENT,
            name     TEXT NOT NULL UNIQUE CHECK (length(trim(name)) > 0),
            location TEXT
        );

        CREATE TABLE IF NOT EXISTS readings (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            room_id          INTEGER NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
            timestamp_utc    TEXT NOT NULL,
            temperature_c    REAL NOT NULL CHECK (temperature_c BETWEEN -50 AND 100),
            humidity_percent REAL NOT NULL CHECK (humidity_percent BETWEEN 0 AND 100),
            co2_ppm          REAL NOT NULL CHECK (co2_ppm >= 0)
        );

        CREATE INDEX IF NOT EXISTS ix_readings_room_time ON readings(room_id, timestamp_utc);

        CREATE TABLE IF NOT EXISTS alerts (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            reading_id INTEGER NOT NULL REFERENCES readings(id) ON DELETE CASCADE,
            type       TEXT NOT NULL,
            severity   TEXT NOT NULL,
            message    TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_alerts_reading ON alerts(reading_id);
        """;
}
