using Microclimate.Core.Models;

namespace Microclimate.Core.Simulation;

/// <summary>Детермінований (за seed) імітатор датчиків — для демо та тестів.</summary>
public sealed class SensorSimulator
{
    private readonly Random _rng;

    public SensorSimulator(int seed = 42)
    {
        _rng = new Random(seed);
    }

    /// <summary>Генерує <paramref name="count"/> вимірювань; на позиції anomalyAt — аномалія.</summary>
    public IEnumerable<Reading> Generate(int count, DateTime startUtc, TimeSpan step, int anomalyAt = -1)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");
        if (step <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be positive.");

        return GenerateIterator(count, startUtc, step, anomalyAt);
    }

    private IEnumerable<Reading> GenerateIterator(int count, DateTime startUtc, TimeSpan step, int anomalyAt)
    {
        for (var i = 0; i < count; i++)
        {
            var ts = startUtc + step * i;
            yield return Next(ts, i == anomalyAt);
        }
    }

    public Reading Next(DateTime timestampUtc, bool injectAnomaly = false)
    {
        var phase = 2 * Math.PI * timestampUtc.TimeOfDay.TotalHours / 24.0;

        var temperature = 23 + 1.5 * Math.Sin(phase) + Noise(0.2);
        var humidity = 45 + 5 * Math.Cos(phase) + Noise(1.0);
        var co2 = 625 + 175 * Math.Sin(phase) + Noise(30);

        if (injectAnomaly)
        {
            temperature += 15;
            co2 += 1500;
        }

        return new Reading(timestampUtc, temperature, humidity, co2);
    }

    private double Noise(double amplitude) => (_rng.NextDouble() * 2 - 1) * amplitude;
}
