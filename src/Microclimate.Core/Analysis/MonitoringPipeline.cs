using System.Globalization;
using Microclimate.Core.Models;

namespace Microclimate.Core.Analysis;

/// <summary>Об'єднує перевірку порогів та пошук аномалій температури.</summary>
public sealed class MonitoringPipeline
{
    private readonly ComfortAnalyzer _analyzer;
    private readonly AnomalyDetector _detector;

    public MonitoringPipeline(ComfortAnalyzer? analyzer = null, AnomalyDetector? detector = null)
    {
        _analyzer = analyzer ?? new ComfortAnalyzer();
        _detector = detector ?? new AnomalyDetector();
    }

    public IReadOnlyList<Alert> Process(Reading reading)
    {
        var alerts = new List<Alert>(_analyzer.Analyze(reading));

        if (_detector.IsAnomaly(reading.TemperatureC))
        {
            var t = reading.TemperatureC.ToString("F1", CultureInfo.InvariantCulture);
            alerts.Add(new Alert(AlertType.Anomaly, AlertSeverity.Warning,
                $"Sudden temperature change detected: {t} C"));
        }

        return alerts;
    }
}
