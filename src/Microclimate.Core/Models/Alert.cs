namespace Microclimate.Core.Models;

public enum AlertType
{
    TemperatureHigh,
    TemperatureLow,
    HumidityHigh,
    HumidityLow,
    Co2High,
    MoldRisk,
    Anomaly
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>Сповіщення, яке сформував аналізатор.</summary>
public sealed record Alert(AlertType Type, AlertSeverity Severity, string Message);
