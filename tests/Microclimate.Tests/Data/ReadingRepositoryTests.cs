using Microclimate.Core.Models;
using Microclimate.Data;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Data;

public class ReadingRepositoryTests
{
    private static (TestDb Db, ReadingRepository Repo, int RoomId) Setup()
    {
        var db = new TestDb();
        var room = new RoomRepository(db.Database).Add("Office");
        return (db, new ReadingRepository(db.Database), room.Id);
    }

    [Fact]
    public void Save_And_GetLatest_NewestFirst_WithRoundtrippedValues()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            var t0 = TestData.BaseTime;
            repo.SaveWithAlerts(roomId, TestData.Make(20, 40, 500, t0), []);
            repo.SaveWithAlerts(roomId, TestData.Make(24, 50, 700, t0.AddMinutes(5)), []);

            var latest = repo.GetLatest(roomId, 5);

            Assert.Equal(2, latest.Count);
            Assert.Equal(t0.AddMinutes(5), latest[0].TimestampUtc);
            Assert.Equal(24, latest[0].TemperatureC);
            Assert.Equal(roomId, latest[0].RoomId);
            Assert.True(latest[0].Id > 0);
        }
    }

    [Fact]
    public void GetLatest_RespectsCount()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            for (var i = 0; i < 5; i++)
                repo.SaveWithAlerts(roomId, TestData.Make(at: TestData.BaseTime.AddMinutes(i)), []);

            Assert.Equal(3, repo.GetLatest(roomId, 3).Count);
        }
    }

    [Fact]
    public void GetLatest_NonPositiveCount_Throws()
    {
        var (db, repo, roomId) = Setup();
        using (db)
            Assert.Throws<ArgumentOutOfRangeException>(() => repo.GetLatest(roomId, 0));
    }

    [Fact]
    public void Save_UnknownRoom_Throws_AndStoresNothing()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            Assert.Throws<InvalidOperationException>(() => repo.SaveWithAlerts(999, TestData.Make(), []));
            Assert.Empty(repo.GetLatest(roomId, 10));
        }
    }

    [Fact]
    public void Save_InvalidReading_Throws()
    {
        var (db, repo, roomId) = Setup();
        using (db)
            Assert.Throws<ArgumentException>(() => repo.SaveWithAlerts(roomId, TestData.Make(humidity: 150), []));
    }

    [Fact]
    public void Save_NullArguments_Throw()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            Assert.Throws<ArgumentNullException>(() => repo.SaveWithAlerts(roomId, null!, []));
            Assert.Throws<ArgumentNullException>(() => repo.SaveWithAlerts(roomId, TestData.Make(), null!));
        }
    }

    [Fact]
    public void GetRange_ReturnsOnlyReadingsInside_Ascending()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            foreach (var h in new[] { 10, 11, 12 })
                repo.SaveWithAlerts(roomId, TestData.Make(at: new DateTime(2026, 1, 1, h, 0, 0, DateTimeKind.Utc)), []);

            var range = repo.GetRange(roomId,
                new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(2, range.Count);
            Assert.True(range[0].TimestampUtc < range[1].TimestampUtc);
        }
    }

    [Fact]
    public void GetRange_FromAfterTo_Throws()
    {
        var (db, repo, roomId) = Setup();
        using (db)
            Assert.Throws<ArgumentException>(() => repo.GetRange(roomId, TestData.BaseTime.AddHours(1), TestData.BaseTime));
    }

    [Fact]
    public void GetStats_EmptyRoom_ReturnsNull()
    {
        var (db, repo, roomId) = Setup();
        using (db)
            Assert.Null(repo.GetStats(roomId));
    }

    [Fact]
    public void GetStats_ComputesAveragesAndMax()
    {
        var (db, repo, roomId) = Setup();
        using (db)
        {
            repo.SaveWithAlerts(roomId, TestData.Make(20, 40, 500, TestData.BaseTime), []);
            repo.SaveWithAlerts(roomId, TestData.Make(24, 50, 700, TestData.BaseTime.AddMinutes(5)), []);

            var stats = repo.GetStats(roomId)!;

            Assert.Equal(2, stats.Count);
            Assert.Equal(22, stats.AvgTemperatureC, 6);
            Assert.Equal(45, stats.AvgHumidityPercent, 6);
            Assert.Equal(600, stats.AvgCo2Ppm, 6);
            Assert.Equal(700, stats.MaxCo2Ppm, 6);
        }
    }
}
