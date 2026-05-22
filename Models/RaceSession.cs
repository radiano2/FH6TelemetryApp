using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FH6TelemetryApp.Models;

public sealed class RaceSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string?            Id          { get; set; }
    public DateTime           StartedAt   { get; init; }
    public DateTime?          EndedAt     { get; set; }
    public List<LapSnapshot>  Laps        { get; init; } = [];
    public List<TuningAdvice> Advice      { get; set; } = [];

    // Derived display helpers (not stored)
    [BsonIgnore] public TimeSpan Duration => (EndedAt ?? DateTime.UtcNow) - StartedAt;
    [BsonIgnore] public float    BestLap  => Laps.Count == 0 ? 0f : Laps.Min(l => l.LapTimeSeconds > 0 ? l.LapTimeSeconds : float.MaxValue);
}
