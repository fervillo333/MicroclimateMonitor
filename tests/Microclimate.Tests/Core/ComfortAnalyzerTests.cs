using Microclimate.Core.Analysis;
using Microclimate.Core.Models;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Core;

public class ComfortAnalyzerTests
{
    private readonly ComfortAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_IdealReading_NoAlerts() =>
        Assert.Empty(_analyzer.Analyze(TestData.Make(22, 45, 600)));

    [Theory]
    [InlineData(26.0, false)]
    [InlineData(26.1, true)]
    public void Analyze_TemperatureBoundary(double t, bool alert) =>
        Assert.Equal(alert, _analyzer.Analyze(TestData.Make(t)).Any(a => a.Type == AlertType.TemperatureHigh));

    [Fact]
    public void Analyze_TemperatureWarningThenCritical()
    {
        var warning = Assert.Single(_analyzer.Analyze(TestData.Make(temperature: 30)));
        Assert.Equal(AlertSeverity.Warning, warning.Severity);

        var critical = Assert.Single(_analyzer.Analyze(TestData.Make(temperature: 31)));
        Assert.Equal(AlertType.TemperatureHigh, critical.Type);
        Assert.Equal(AlertSeverity.Critical, critical.Severity);
    }

    [Fact]
    public void Analyze_LowTemperature()
    {
        var alert = Assert.Single(_analyzer.Analyze(TestData.Make(temperature: 15)));
        Assert.Equal(AlertType.TemperatureLow, alert.Type);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void Analyze_HumidityHighWarning_NoMold()
    {
        var alert = Assert.Single(_analyzer.Analyze(TestData.Make(humidity: 65)));
        Assert.Equal(AlertType.HumidityHigh, alert.Type);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
    }

    [Fact]
    public void Analyze_VeryHighHumidity_CriticalAndMoldRisk()
    {
        var alerts = _analyzer.Analyze(TestData.Make(humidity: 80));
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.Type == AlertType.HumidityHigh && a.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, a => a.Type == AlertType.MoldRisk);
    }

    [Fact]
    public void Analyze_LowHumidity()
    {
        var alert = Assert.Single(_analyzer.Analyze(TestData.Make(humidity: 25)));
        Assert.Equal(AlertType.HumidityLow, alert.Type);
    }

    [Theory]
    [InlineData(1000, 0)]
    [InlineData(1500, 1)]
    [InlineData(2500, 1)]
    public void Analyze_Co2(double co2, int expectedAlerts) =>
        Assert.Equal(expectedAlerts, _analyzer.Analyze(TestData.Make(co2: co2)).Count);

    [Fact]
    public void Analyze_InvalidReading_Throws() =>
        Assert.Throws<ArgumentException>(() => _analyzer.Analyze(TestData.Make(temperature: double.NaN)));

    [Fact]
    public void Analyze_CustomThresholds_AreUsed()
    {
        var analyzer = new ComfortAnalyzer(new ComfortThresholds { TemperatureMaxC = 21 });
        Assert.Single(analyzer.Analyze(TestData.Make(temperature: 22)));
    }

    [Fact]
    public void ComfortScore_Ideal_Is100() =>
        Assert.Equal(100, _analyzer.ComfortScore(TestData.Make()), 6);

    [Fact]
    public void ComfortScore_PenalisesTemperatureHumidityAndCo2()
    {
        Assert.Equal(80, _analyzer.ComfortScore(TestData.Make(temperature: 28)), 6);
        Assert.Equal(85, _analyzer.ComfortScore(TestData.Make(humidity: 70)), 6);
        Assert.Equal(90, _analyzer.ComfortScore(TestData.Make(co2: 1500)), 6);
    }

    [Fact]
    public void ComfortScore_NeverBelowZero() =>
        Assert.Equal(0, _analyzer.ComfortScore(TestData.Make(40, 100, 10000)), 6);

    [Fact]
    public void ComfortScore_InvalidReading_Throws() =>
        Assert.Throws<ArgumentException>(() => _analyzer.ComfortScore(TestData.Make(humidity: 150)));

    [Fact]
    public void DewPoint_KnownValue() =>
        Assert.Equal(9.3, ComfortAnalyzer.DewPointC(20, 50), 1);

    [Fact]
    public void DewPoint_At100PercentEqualsTemperature() =>
        Assert.Equal(20, ComfortAnalyzer.DewPointC(20, 100), 6);

    [Fact]
    public void DewPoint_ZeroHumidity_IsFinite() =>
        Assert.True(double.IsFinite(ComfortAnalyzer.DewPointC(20, 0)));

    [Fact]
    public void Constructor_InvalidThresholds_Throws() =>
    Assert.Throws<ArgumentException>(() =>
        new ComfortAnalyzer(new ComfortThresholds { TemperatureMinC = 30 }));
}
