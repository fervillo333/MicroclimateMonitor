namespace Microclimate.Core.Analysis;

/// <summary>
/// Виявляє різкі стрибки значення відносно ковзного середнього (z-score).
/// Аномальні значення не потрапляють у вікно, щоб не "отруювати" статистику.
/// </summary>
public sealed class AnomalyDetector
{
    /// <summary>Мінімальне σ — розв'язність датчика; захищає від ділення на ~0.</summary>
    private const double MinStdDev = 0.3;

    private readonly int _window;
    private readonly double _zThreshold;
    private readonly Queue<double> _values = new();

    public AnomalyDetector(int window = 10, double zThreshold = 3.0)
    {
        if (window < 3)
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be at least 3.");
        if (zThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(zThreshold), "Threshold must be positive.");

        _window = window;
        _zThreshold = zThreshold;
    }

    /// <summary>true, якщо значення різко відрізняється від останніх.</summary>
    public bool IsAnomaly(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentException("Value must be finite.", nameof(value));

        var anomaly = false;

        if (_values.Count >= _window)
        {
            var mean = _values.Average();
            var variance = _values.Sum(v => (v - mean) * (v - mean)) / _values.Count;
            var std = Math.Max(Math.Sqrt(variance), MinStdDev);
            anomaly = Math.Abs(value - mean) / std > _zThreshold;
        }

        if (!anomaly)
        {
            _values.Enqueue(value);
            while (_values.Count > _window)
                _values.Dequeue();
        }

        return anomaly;
    }
}
