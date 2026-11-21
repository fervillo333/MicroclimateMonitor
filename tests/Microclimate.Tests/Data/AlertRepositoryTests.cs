using Microclimate.Core.Models;
using Microclimate.Data;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Data;

public class AlertRepositoryTests
{
    [Fact]
    public void Alerts_SavedWithReading_AreReadBack_NewestFirst()
    {
        using var db = new TestDb();
        var room = new RoomRepository(db.Database).Add("Office");
        var readings = new ReadingRepository(db.Database);
        var alerts = new AlertRepository(db.Database);

        readings.SaveWithAlerts(room.Id, TestData.Make(at: TestData.BaseTime),
            [new Alert(AlertType.Co2High, AlertSeverity.Warning, "old")]);
        readings.SaveWithAlerts(room.Id, TestData.Make(at: TestData.BaseTime.AddMinutes(5)),
            [new Alert(AlertType.TemperatureHigh, AlertSeverity.Critical, "new")]);

        var result = alerts.GetByRoom(room.Id, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("new", result[0].Alert.Message);
        Assert.Equal(AlertType.TemperatureHigh, result[0].Alert.Type);
        Assert.Equal(AlertSeverity.Critical, result[0].Alert.Severity);
    }

    [Fact]
    public void GetByRoom_RespectsLimit_AndOtherRoomsAreIgnored()
    {
        using var db = new TestDb();
        var rooms = new RoomRepository(db.Database);
        var a = rooms.Add("A");
        var b = rooms.Add("B");
        var readings = new ReadingRepository(db.Database);
        var alerts = new AlertRepository(db.Database);

        for (var i = 0; i < 3; i++)
            readings.SaveWithAlerts(a.Id, TestData.Make(at: TestData.BaseTime.AddMinutes(i)),
                [new Alert(AlertType.Co2High, AlertSeverity.Warning, "x")]);
        readings.SaveWithAlerts(b.Id, TestData.Make(), [new Alert(AlertType.Co2High, AlertSeverity.Warning, "y")]);

        Assert.Equal(2, alerts.GetByRoom(a.Id, 2).Count);
        Assert.Single(alerts.GetByRoom(b.Id, 10));
    }

    [Fact]
    public void GetByRoom_NonPositiveLimit_Throws()
    {
        using var db = new TestDb();
        Assert.Throws<ArgumentOutOfRangeException>(() => new AlertRepository(db.Database).GetByRoom(1, 0));
    }

    [Fact]
    public void CountBySeverity_GroupsCorrectly()
    {
        using var db = new TestDb();
        var room = new RoomRepository(db.Database).Add("Office");
        var readings = new ReadingRepository(db.Database);

        readings.SaveWithAlerts(room.Id, TestData.Make(),
        [
            new Alert(AlertType.Co2High, AlertSeverity.Warning, "a"),
            new Alert(AlertType.MoldRisk, AlertSeverity.Warning, "b"),
            new Alert(AlertType.TemperatureHigh, AlertSeverity.Critical, "c")
        ]);

        var counts = new AlertRepository(db.Database).CountBySeverity(room.Id);

        Assert.Equal(2, counts[AlertSeverity.Warning]);
        Assert.Equal(1, counts[AlertSeverity.Critical]);
        Assert.False(counts.ContainsKey(AlertSeverity.Info));
    }
}
