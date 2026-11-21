using System.Globalization;
using Microclimate.Core.Analysis;
using Microclimate.Core.Models;
using Microclimate.Core.Simulation;
using Microclimate.Data;

namespace Microclimate.App;

/// <summary>Консольні команди застосунку. Вивід передається через TextWriter, тому код тестується.</summary>
public sealed class CommandRunner
{
    private const int DefaultSimulationCount = 20;
    private const int MaxSimulationCount = 10_000;
    private const int DefaultAlertLimit = 10;

    private readonly RoomRepository _rooms;
    private readonly ReadingRepository _readings;
    private readonly AlertRepository _alerts;
    private readonly TextWriter _out;
    private readonly TextWriter _err;

    public CommandRunner(SqliteDatabase db, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(db);
        _out = output ?? throw new ArgumentNullException(nameof(output));
        _err = error ?? throw new ArgumentNullException(nameof(error));

        db.Initialize();
        _rooms = new RoomRepository(db);
        _readings = new ReadingRepository(db);
        _alerts = new AlertRepository(db);
    }

    /// <summary>Шлях до БД береться зі змінної MICROCLIMATE_DB (за замовчуванням ./microclimate.db).</summary>
    public static CommandRunner CreateDefault()
    {
        var path = Environment.GetEnvironmentVariable("MICROCLIMATE_DB");
        if (string.IsNullOrWhiteSpace(path))
            path = "microclimate.db";

        return new CommandRunner(SqliteDatabase.ForFile(path), Console.Out, Console.Error);
    }

    /// <returns>Код завершення: 0 — успіх, 1 — помилка.</returns>
    public int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "help" or "--help" or "-h" => PrintHelp(),
                "add-room" => AddRoom(args),
                "rooms" => ListRooms(),
                "simulate" => Simulate(args),
                "report" => Report(args),
                "alerts" => ShowAlerts(args),
                _ => Fail($"Unknown command: {args[0]}")
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Fail(ex.Message);
        }
    }

    private int PrintHelp()
    {
        _out.WriteLine("Microclimate Monitor");
        _out.WriteLine("Usage:");
        _out.WriteLine("  add-room <name> [location]        add a room");
        _out.WriteLine("  rooms                             list rooms");
        _out.WriteLine("  simulate <room> [count] [--anomaly]  generate and analyse readings");
        _out.WriteLine("  report <room>                     statistics and comfort score");
        _out.WriteLine("  alerts <room> [limit]             latest alerts");
        return 0;
    }


    private int AddRoom(string[] args)
    {
        if (args.Length < 2)
            return Fail("Usage: add-room <name> [location]");

        var room = _rooms.Add(args[1], args.Length > 2 ? args[2] : null);
        _out.WriteLine($"Room added: {room.Name} (id {room.Id})");
        return 0;
    }

    private int ListRooms()
    {
        var rooms = _rooms.GetAll();
        if (rooms.Count == 0)
        {
            _out.WriteLine("No rooms yet.");
            return 0;
        }

        foreach (var room in rooms)
            _out.WriteLine($"{room.Id}\t{room.Name}\t{room.Location ?? "-"}");

        return 0;
    }

    private int Simulate(string[] args)
{
    var withAnomaly = args.Contains("--anomaly");
    var positional = args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

    if (positional.Length == 0)
        return Fail("Usage: simulate <room> [count] [--anomaly]");

    var count = DefaultSimulationCount;
    if (positional.Length > 1
        && (!int.TryParse(positional[1], NumberStyles.None, CultureInfo.InvariantCulture, out count)
            || count < 1 || count > MaxSimulationCount))
    {
        return Fail($"Count must be an integer from 1 to {MaxSimulationCount}.");
    }

    // Використання нового методу
    if (!TryGetRoom(positional[0], out var room))
        return 1;

    var step = TimeSpan.FromMinutes(5);
    var start = DateTime.UtcNow - step * count;
    var simulator = new SensorSimulator();
    var pipeline = new MonitoringPipeline();
    var alertTotal = 0;

    foreach (var reading in simulator.Generate(count, start, step, withAnomaly ? count / 2 : -1))
    {
        var alerts = pipeline.Process(reading);
        _readings.SaveWithAlerts(room.Id, reading, alerts);
        alertTotal += alerts.Count;
    }

    _out.WriteLine($"Saved {count} readings, {alertTotal} alerts.");
    return 0;
}

   private int Report(string[] args)
{
    if (args.Length < 2)
        return Fail("Usage: report <room>");

    // Використання нового методу
    if (!TryGetRoom(args[1], out var room))
        return 1;

    var stats = _readings.GetStats(room.Id);
    if (stats is null)
    {
        _out.WriteLine($"No readings for room '{room.Name}'.");
        return 0;
    }

    var latest = _readings.GetLatest(room.Id, 1)[0];
    var analyzer = new ComfortAnalyzer();
    var score = analyzer.ComfortScore(latest);
    var dewPoint = ComfortAnalyzer.DewPointC(latest.TemperatureC, latest.HumidityPercent);

    var inv = CultureInfo.InvariantCulture;
    _out.WriteLine($"Room: {room.Name}");
    _out.WriteLine($"Readings: {stats.Count}");
    _out.WriteLine(string.Create(inv, $"Avg temperature: {stats.AvgTemperatureC:F1} C"));
    _out.WriteLine(string.Create(inv, $"Avg humidity: {stats.AvgHumidityPercent:F1} %"));
    _out.WriteLine(string.Create(inv, $"Avg CO2: {stats.AvgCo2Ppm:F0} ppm (max {stats.MaxCo2Ppm:F0})"));
    _out.WriteLine(string.Create(inv, $"Latest comfort score: {score:F0}/100, dew point {dewPoint:F1} C"));
    return 0;
}

    private int ShowAlerts(string[] args)
{
    if (args.Length < 2)
        return Fail("Usage: alerts <room> [limit]");

    var limit = DefaultAlertLimit;
    if (args.Length > 2
        && (!int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out limit) || limit < 1))
    {
        return Fail("Limit must be a positive integer.");
    }

    // Використання нового методу
    if (!TryGetRoom(args[1], out var room))
        return 1;

    var alerts = _alerts.GetByRoom(room.Id, limit);
    if (alerts.Count == 0)
    {
        _out.WriteLine("No alerts.");
        return 0;
    }

    foreach (var stored in alerts)
    {
        var ts = stored.TimestampUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        _out.WriteLine($"[{stored.Alert.Severity}] {stored.Alert.Type} {ts} - {stored.Alert.Message}");
    }

    return 0;
}
    private bool TryGetRoom(string name, out Room room)
    {
        var found = _rooms.GetByName(name);
        if (found is null)
        {
            Fail($"Room '{name}' not found.");
            room = null!;
            return false;
        }

        room = found;
        return true;
    }
    private int Fail(string message)
    {
        _err.WriteLine(message);
        return 1;
    }
}
