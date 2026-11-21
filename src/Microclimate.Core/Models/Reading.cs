namespace Microclimate.Core.Models;

/// <summary>Одне вимірювання мікроклімату.</summary>
public sealed record Reading(
    DateTime TimestampUtc,
    double TemperatureC,
    double HumidityPercent,
    double Co2Ppm)
{
    /// <summary>Id запису в БД (0 — ще не збережено).</summary>
    public long Id { get; init; }

    /// <summary>Id приміщення (0 — ще не прив'язано).</summary>
    public int RoomId { get; init; }
}
