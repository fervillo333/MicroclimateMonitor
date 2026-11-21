using System.Globalization;
using Microclimate.Core.Models;
using Microclimate.Core.Validation;

namespace Microclimate.Core.Analysis;

/// <summary>Оцінює вимірювання відносно порогів комфорту.</summary>
public sealed class ComfortAnalyzer
{
    private readonly ComfortThresholds _t;

    public ComfortAnalyzer(ComfortThresholds? thresholds = null)
    {
        _t = thresholds ?? ComfortThresholds.Default;
        _t.EnsureValid();
    }

    /// <summary>Повертає список сповіщень (порожній, якщо все в нормі).</summary>
    /// <exception cref="ArgumentException">Вимірювання некоректне.</exception>
    public IReadOnlyList<Alert> Analyze(Reading reading)
    {
        EnsureValid(reading);
        var alerts = new List<Alert>();

        CheckRange(alerts, reading.TemperatureC, _t.TemperatureMinC, _t.TemperatureMaxC,
            _t.TemperatureCriticalMinC, _t.TemperatureCriticalMaxC,
            AlertType.TemperatureLow, AlertType.TemperatureHigh, "Temperature", "C");

        CheckRange(alerts, reading.HumidityPercent, _t.HumidityMin, _t.HumidityMax,
            _t.HumidityCriticalMin, _t.HumidityCriticalMax,
            AlertType.HumidityLow, AlertType.HumidityHigh, "Humidity", "%");

        if (reading.Co2Ppm > _t.Co2Critical)
            alerts.Add(new Alert(AlertType.Co2High, AlertSeverity.Critical,
                $"CO2 critically high: {Fmt(reading.Co2Ppm)} ppm"));
        else if (reading.Co2Ppm > _t.Co2Warning)
            alerts.Add(new Alert(AlertType.Co2High, AlertSeverity.Warning,
                $"CO2 high: {Fmt(reading.Co2Ppm)} ppm"));

        if (reading.HumidityPercent >= _t.MoldRiskHumidity)
            alerts.Add(new Alert(AlertType.MoldRisk, AlertSeverity.Warning,
                $"Mold risk: humidity {Fmt(reading.HumidityPercent)} %"));

        return alerts;
    }

    /// <summary>Індекс комфорту 0..100 (100 — ідеально).</summary>
    public double ComfortScore(Reading reading)
    {
        EnsureValid(reading);

        var penalty =
            Deviation(reading.TemperatureC, _t.TemperatureMinC, _t.TemperatureMaxC) * 10
            + Deviation(reading.HumidityPercent, _t.HumidityMin, _t.HumidityMax) * 1.5
            + Math.Max(0, reading.Co2Ppm - _t.Co2Warning) / 50;

        return Math.Clamp(100 - penalty, 0, 100);
    }

    /// <summary>Точка роси за формулою Магнуса, °C.</summary>
    public static double DewPointC(double temperatureC, double humidityPercent)
    {
        const double a = 17.62;
        const double b = 243.12;
        var rh = Math.Max(humidityPercent, 0.01) / 100.0;
        var gamma = Math.Log(rh) + a * temperatureC / (b + temperatureC);
        return b * gamma / (a - gamma);
    }

    private static void EnsureValid(Reading reading)
    {
        var errors = ReadingValidator.Validate(reading);
        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(reading));
    }

    private static double Deviation(double value, double min, double max) =>
        value < min ? min - value : value > max ? value - max : 0;

    private static void CheckRange(
        List<Alert> alerts, double value, double min, double max,
        double criticalMin, double criticalMax,
        AlertType low, AlertType high, string name, string unit)
    {
        if (value < criticalMin)
            alerts.Add(new Alert(low, AlertSeverity.Critical, $"{name} critically low: {Fmt(value)} {unit}"));
        else if (value < min)
            alerts.Add(new Alert(low, AlertSeverity.Warning, $"{name} low: {Fmt(value)} {unit}"));
        else if (value > criticalMax)
            alerts.Add(new Alert(high, AlertSeverity.Critical, $"{name} critically high: {Fmt(value)} {unit}"));
        else if (value > max)
            alerts.Add(new Alert(high, AlertSeverity.Warning, $"{name} high: {Fmt(value)} {unit}"));
    }

    private static string Fmt(double v) => v.ToString("F1", CultureInfo.InvariantCulture);
}
