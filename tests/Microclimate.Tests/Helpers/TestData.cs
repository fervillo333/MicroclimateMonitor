using Microclimate.Core.Models;

namespace Microclimate.Tests.Helpers;

internal static class TestData
{
    public static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static Reading Make(double temperature = 22, double humidity = 45, double co2 = 600, DateTime? at = null) =>
        new(at ?? BaseTime, temperature, humidity, co2);
}
