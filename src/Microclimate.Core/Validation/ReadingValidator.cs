using Microclimate.Core.Models;

namespace Microclimate.Core.Validation;

/// <summary>Перевірка того, що показники датчика фізично можливі.</summary>
public static class ReadingValidator
{
    public const double MinTemperatureC = -40;
    public const double MaxTemperatureC = 85;
    public const double MinHumidityPercent = 0;
    public const double MaxHumidityPercent = 100;
    public const double MinCo2Ppm = 0;
    public const double MaxCo2Ppm = 10_000;

    /// <summary>Повертає список помилок; порожній список — вимірювання коректне.</summary>
    public static IReadOnlyList<string> Validate(Reading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var errors = new List<string>();

        if (reading.TimestampUtc == default)
            errors.Add("Timestamp must be set.");

        if (reading.TimestampUtc.Kind == DateTimeKind.Local)
            errors.Add("Timestamp must be in UTC.");

        if (!IsInRange(reading.TemperatureC, MinTemperatureC, MaxTemperatureC))
            errors.Add($"Temperature must be a finite value between {MinTemperatureC} and {MaxTemperatureC} C.");

        if (!IsInRange(reading.HumidityPercent, MinHumidityPercent, MaxHumidityPercent))
            errors.Add($"Humidity must be a finite value between {MinHumidityPercent} and {MaxHumidityPercent} %.");

        if (!IsInRange(reading.Co2Ppm, MinCo2Ppm, MaxCo2Ppm))
            errors.Add($"CO2 must be a finite value between {MinCo2Ppm} and {MaxCo2Ppm} ppm.");

        return errors;
    }

    public static bool IsValid(Reading reading) => Validate(reading).Count == 0;

    private static bool IsInRange(double value, double min, double max) =>
        double.IsFinite(value) && value >= min && value <= max;
}
