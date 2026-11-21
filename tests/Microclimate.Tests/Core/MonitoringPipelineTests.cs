using Microclimate.Core.Analysis;
using Microclimate.Core.Models;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Core;

public class MonitoringPipelineTests
{
    [Fact]
    public void NormalReadings_ProduceNoAlerts()
    {
        var pipeline = new MonitoringPipeline();
        for (var i = 0; i < 15; i++)
            Assert.Empty(pipeline.Process(TestData.Make()));
    }

    [Fact]
    public void TemperatureSpike_ProducesThresholdAndAnomalyAlerts()
    {
        var pipeline = new MonitoringPipeline();
        for (var i = 0; i < 12; i++)
            pipeline.Process(TestData.Make());

        var alerts = pipeline.Process(TestData.Make(temperature: 40));

        Assert.Contains(alerts, a => a.Type == AlertType.TemperatureHigh);
        Assert.Contains(alerts, a => a.Type == AlertType.Anomaly);
    }

    [Fact]
    public void InvalidReading_Throws() =>
        Assert.Throws<ArgumentException>(() => new MonitoringPipeline().Process(TestData.Make(humidity: -5)));
}
