using Microclimate.App;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.App;

public class CommandRunnerTests
{
    private sealed class Harness : IDisposable
    {
        private readonly TestDb _db = new();
        public StringWriter Out { get; } = new();
        public StringWriter Err { get; } = new();
        public CommandRunner Runner { get; }

        public Harness() => Runner = new CommandRunner(_db.Database, Out, Err);

        public int Run(params string[] args) => Runner.Run(args);

        public void Dispose() => _db.Dispose();
    }

    [Fact]
    public void NoArguments_PrintsHelp_AndReturns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run());
        Assert.Contains("Usage", h.Out.ToString());
    }

    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_Returns0(string arg)
    {
        using var h = new Harness();
        Assert.Equal(0, h.Run(arg));
    }

    [Fact]
    public void UnknownCommand_Returns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run("dance"));
        Assert.Contains("Unknown command", h.Err.ToString());
    }

    [Fact]
    public void AddRoom_ThenRooms_ListsIt()
    {
        using var h = new Harness();
        Assert.Equal(0, h.Run("add-room", "Office", "2nd floor"));
        Assert.Equal(0, h.Run("rooms"));
        Assert.Contains("Office", h.Out.ToString());
        Assert.Contains("2nd floor", h.Out.ToString());
    }

    [Fact]
    public void Rooms_WhenEmpty_SaysSo()
    {
        using var h = new Harness();
        Assert.Equal(0, h.Run("rooms"));
        Assert.Contains("No rooms", h.Out.ToString());
    }

    [Fact]
    public void AddRoom_Duplicate_Returns1()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");
        Assert.Equal(1, h.Run("add-room", "Office"));
        Assert.Contains("already exists", h.Err.ToString());
    }

    [Fact]
    public void AddRoom_WithoutName_Returns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run("add-room"));
    }

    [Fact]
    public void Simulate_UnknownRoom_Returns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run("simulate", "Nope"));
        Assert.Contains("not found", h.Err.ToString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("99999")]
    public void Simulate_InvalidCount_Returns1(string count)
    {
        using var h = new Harness();
        h.Run("add-room", "Office");
        Assert.Equal(1, h.Run("simulate", "Office", count));
    }

    [Fact]
    public void Simulate_WithoutRoom_Returns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run("simulate"));
    }

    [Fact]
    public void Simulate_Normal_ThenReport_ShowsStats_AndNoAlerts()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");

        Assert.Equal(0, h.Run("simulate", "Office", "30"));
        Assert.Contains("Saved 30 readings, 0 alerts", h.Out.ToString());

        Assert.Equal(0, h.Run("report", "Office"));
        Assert.Contains("Readings: 30", h.Out.ToString());
        Assert.Contains("comfort score", h.Out.ToString());

        Assert.Equal(0, h.Run("alerts", "Office"));
        Assert.Contains("No alerts", h.Out.ToString());
    }

    [Fact]
    public void Simulate_WithAnomaly_ProducesAlerts()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");

        Assert.Equal(0, h.Run("simulate", "Office", "40", "--anomaly"));
        Assert.Equal(0, h.Run("alerts", "Office", "5"));

        var output = h.Out.ToString();
        Assert.Contains("Anomaly", output);
        Assert.Contains("Critical", output);
    }

    [Fact]
    public void Simulate_DefaultCount_Is20()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");
        h.Run("simulate", "Office");
        Assert.Contains("Saved 20 readings", h.Out.ToString());
    }

    [Fact]
    public void Report_NoReadings_SaysSo()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");
        Assert.Equal(0, h.Run("report", "Office"));
        Assert.Contains("No readings", h.Out.ToString());
    }

    [Fact]
    public void Report_UnknownRoom_Or_MissingArg_Returns1()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Run("report", "Nope"));
        Assert.Equal(1, h.Run("report"));
    }

    [Fact]
    public void Alerts_InvalidLimit_UnknownRoom_MissingArg_Return1()
    {
        using var h = new Harness();
        h.Run("add-room", "Office");
        Assert.Equal(1, h.Run("alerts", "Office", "zero"));
        Assert.Equal(1, h.Run("alerts", "Nope"));
        Assert.Equal(1, h.Run("alerts"));
    }
}
