using Microclimate.Core.Analysis;
using Microclimate.Core.Simulation;
using Microclimate.Core.Validation;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Core;

public class SensorSimulatorTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMinutes(5);

    [Fact]
    public void SameSeed_GivesSameData()
    {
        var a = new SensorSimulator(1).Generate(10, TestData.BaseTime, Step).ToList();
        var b = new SensorSimulator(1).Generate(10, TestData.BaseTime, Step).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Generate_ReturnsRequestedCount_OfValidReadings()
    {
        var readings = new SensorSimulator().Generate(50, TestData.BaseTime, Step).ToList();
        Assert.Equal(50, readings.Count);
        Assert.All(readings, r => Assert.True(ReadingValidator.IsValid(r)));
    }

    [Fact]
    public void NormalRun_ProducesNoAlerts()
    {
        var pipeline = new MonitoringPipeline();
        foreach (var r in new SensorSimulator().Generate(288, TestData.BaseTime, Step))
            Assert.Empty(pipeline.Process(r));
    }

    [Fact]
    public void Anomaly_IsInjectedAtRequestedIndex_AndDetected()
    {
        var pipeline = new MonitoringPipeline();
        var readings = new SensorSimulator().Generate(40, TestData.BaseTime, Step, anomalyAt: 25).ToList();

        for (var i = 0; i < readings.Count; i++)
        {
            var alerts = pipeline.Process(readings[i]);
            Assert.Equal(i == 25, alerts.Any(a => a.Type == Microclimate.Core.Models.AlertType.Anomaly));
        }
    }

    [Fact]
    public void Generate_NegativeCount_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SensorSimulator().Generate(-1, TestData.BaseTime, Step));

    [Fact]
    public void Generate_ZeroStep_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SensorSimulator().Generate(1, TestData.BaseTime, TimeSpan.Zero));

    [Fact]
    public void Generate_ZeroCount_ReturnsEmpty() =>
        Assert.Empty(new SensorSimulator().Generate(0, TestData.BaseTime, Step));
}
