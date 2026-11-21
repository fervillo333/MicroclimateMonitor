using Microclimate.Data;
using Microsoft.Data.Sqlite;

namespace Microclimate.Tests.Helpers;

/// <summary>Тимчасова файлова БД для тестів; видаляється після тесту.</summary>
internal sealed class TestDb : IDisposable
{
    public string FilePath { get; }
    public SqliteDatabase Database { get; }

    public TestDb()
    {
        FilePath = Path.Combine(Path.GetTempPath(), $"microclimate-test-{Guid.NewGuid():N}.db");
        Database = SqliteDatabase.ForFile(FilePath);
        Database.Initialize();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
