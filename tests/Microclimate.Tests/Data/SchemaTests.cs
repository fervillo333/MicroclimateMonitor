using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Data;

public class SchemaTests
{
    [Theory]
    [InlineData("ix_readings_room_time")]
    [InlineData("ix_alerts_reading")]
    public void Indexes_Exist(string indexName)
    {
        using var db = new TestDb();
        using var conn = db.Database.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $n;";
        cmd.Parameters.AddWithValue("$n", indexName);
        Assert.Equal(1L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void Initialize_CanBeCalledTwice()
    {
        using var db = new TestDb();
        db.Database.Initialize();
    }

    [Fact]
    public void ForeignKeys_AreEnforced()
    {
        using var db = new TestDb();
        using var conn = db.Database.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO readings (room_id, timestamp_utc, temperature_c, humidity_percent, co2_ppm) VALUES (999, 'x', 20, 40, 500);";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => cmd.ExecuteNonQuery());
    }
}
