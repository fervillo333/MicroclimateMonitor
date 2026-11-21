using Microclimate.Core.Analysis;

namespace Microclimate.Tests.Core;

public class AnomalyDetectorTests
{
    [Fact]
    public void NoAnomalyUntilWindowIsFull()
    {
        var d = new AnomalyDetector(window: 5);
        for (var i = 0; i < 5; i++)
            Assert.False(d.IsAnomaly(i == 4 ? 99 : 22));
    }

    [Fact]
    public void Spike_IsDetected_AndDoesNotPoisonWindow()
    {
        var d = new AnomalyDetector(window: 5);
        for (var i = 0; i < 5; i++)
            Assert.False(d.IsAnomaly(22.0));

        Assert.True(d.IsAnomaly(40.0));
        Assert.False(d.IsAnomaly(22.1));
    }

    [Fact]
    public void SmallNoise_IsNotAnomaly()
    {
        var d = new AnomalyDetector(window: 5);
        foreach (var v in new[] { 22.0, 22.1, 21.9, 22.0, 22.1, 22.2, 21.8 })
            Assert.False(d.IsAnomaly(v));
    }

    [Theory]
    [InlineData(2, 3.0)]
    [InlineData(5, 0.0)]
    [InlineData(5, -1.0)]
    public void InvalidArguments_Throw(int window, double z) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnomalyDetector(window, z));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonFiniteValue_Throws(double value) =>
        Assert.Throws<ArgumentException>(() => new AnomalyDetector().IsAnomaly(value));
}
