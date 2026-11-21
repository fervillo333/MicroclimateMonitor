using Microclimate.Core.Models;

namespace Microclimate.Core.Simulation;

/// <summary>Детермінований (за seed) імітатор датчиків — для демо та тестів.</summary>
public sealed class SensorSimulator
{
    private const double HoursInDay = 24.0;

    // Константи для генерації температури
    private const double BaseTemperature = 23.0;
    private const double TemperatureAmplitude = 1.5;
    private const double TemperatureNoise = 0.2;
    private const double TemperatureAnomalyOffset = 15.0;

    // Константи для генерації вологості
    private const double BaseHumidity = 45.0;
    private const double HumidityAmplitude = 5.0;
    private const double HumidityNoise = 1.0;

    // Константи для генерації CO2
    private const double BaseCo2 = 625.0;
    private const double Co2Amplitude = 175.0;
    private const double Co2Noise = 30.0;
    private const double Co2AnomalyOffset = 1500.0;

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
        var phase = 2 * Math.PI * timestampUtc.TimeOfDay.TotalHours / HoursInDay;

        var temperature = BaseTemperature + TemperatureAmplitude * Math.Sin(phase) + Noise(TemperatureNoise);
        var humidity = BaseHumidity + HumidityAmplitude * Math.Cos(phase) + Noise(HumidityNoise);
        var co2 = BaseCo2 + Co2Amplitude * Math.Sin(phase) + Noise(Co2Noise);

        if (injectAnomaly)
        {
            temperature += TemperatureAnomalyOffset;
            co2 += Co2AnomalyOffset;
        }

        return new Reading(timestampUtc, temperature, humidity, co2);
    }

    private double Noise(double amplitude) => (_rng.NextDouble() * 2 - 1) * amplitude;
}
