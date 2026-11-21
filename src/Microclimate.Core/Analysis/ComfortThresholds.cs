namespace Microclimate.Core.Analysis;

/// <summary>Пороги комфорту. Значення за замовчуванням — типові санітарні норми для офісу.</summary>
public sealed record ComfortThresholds
{
    public double TemperatureMinC { get; init; } = 20;
    public double TemperatureMaxC { get; init; } = 26;
    public double TemperatureCriticalMinC { get; init; } = 16;
    public double TemperatureCriticalMaxC { get; init; } = 30;

    public double HumidityMin { get; init; } = 30;
    public double HumidityMax { get; init; } = 60;
    public double HumidityCriticalMin { get; init; } = 20;
    public double HumidityCriticalMax { get; init; } = 75;

    /// <summary>Від цієї вологості зростає ризик цвілі.</summary>
    public double MoldRiskHumidity { get; init; } = 70;

    public double Co2Warning { get; init; } = 1000;
    public double Co2Critical { get; init; } = 2000;

    public static ComfortThresholds Default { get; } = new();

    public void EnsureValid()
    {
        if (!(TemperatureCriticalMinC <= TemperatureMinC && TemperatureMinC < TemperatureMaxC
              && TemperatureMaxC <= TemperatureCriticalMaxC))
            throw new ArgumentException("Temperature thresholds must satisfy criticalMin <= min < max <= criticalMax.");

        if (!(HumidityCriticalMin <= HumidityMin && HumidityMin < HumidityMax
              && HumidityMax <= HumidityCriticalMax))
            throw new ArgumentException("Humidity thresholds must satisfy criticalMin <= min < max <= criticalMax.");

        if (Co2Warning > Co2Critical)
            throw new ArgumentException("CO2 warning threshold must not exceed critical threshold.");
    }
}
