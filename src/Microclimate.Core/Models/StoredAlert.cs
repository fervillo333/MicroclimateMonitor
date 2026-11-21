namespace Microclimate.Core.Models;

/// <summary>Сповіщення, збережене в БД, з часом вимірювання.</summary>
public sealed record StoredAlert(long Id, long ReadingId, DateTime TimestampUtc, Alert Alert);
