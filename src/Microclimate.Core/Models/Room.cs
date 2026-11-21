namespace Microclimate.Core.Models;

/// <summary>Приміщення, в якому ведеться моніторинг.</summary>
public sealed record Room(int Id, string Name, string? Location);
