namespace Microclimate.Core.Models;

/// <summary>Агрегована статистика по вимірюваннях приміщення.</summary>
public sealed record ReadingStats(
    int Count,
    double AvgTemperatureC,
    double AvgHumidityPercent,
    double AvgCo2Ppm,
    double MaxCo2Ppm,
    double MinTemperatureC,
    double MaxTemperatureC);
