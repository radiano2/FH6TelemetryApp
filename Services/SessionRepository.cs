using FH6TelemetryApp.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FH6TelemetryApp.Services;

public sealed class SessionRepository
{
    private readonly IMongoCollection<RaceSession>     _sessions;
    private readonly IMongoCollection<RawPacketDocument> _packets;

    public SessionRepository(MongoDbService mongo)
    {
        _sessions = mongo.Database.GetCollection<RaceSession>("race_sessions");
        _packets  = mongo.Database.GetCollection<RawPacketDocument>("telemetry_packets");

        // Index for fast per-session packet queries
        var idx = Builders<RawPacketDocument>.IndexKeys
            .Ascending(p => p.SessionId)
            .Ascending(p => p.TimestampMs);
        _packets.Indexes.CreateOne(new CreateIndexModel<RawPacketDocument>(idx));
    }

    // ── Sessions ──────────────────────────────────────────────────────────────
    public async Task<string> InsertSessionAsync(RaceSession session)
    {
        await _sessions.InsertOneAsync(session);
        return session.Id!;
    }

    public async Task UpdateSessionAsync(RaceSession session) =>
        await _sessions.ReplaceOneAsync(s => s.Id == session.Id, session);

    public async Task<List<RaceSession>> GetAllSessionsAsync() =>
        await _sessions
            .Find(_ => true)
            .SortByDescending(s => s.StartedAt)
            .Limit(50)
            .ToListAsync();

    public async Task<RaceSession?> GetSessionAsync(string id) =>
        await _sessions.Find(s => s.Id == id).FirstOrDefaultAsync();

    // ── Raw packets ───────────────────────────────────────────────────────────
    public async Task BulkInsertPacketsAsync(string sessionId, IEnumerable<RawPacketDocument> packets) =>
        await _packets.InsertManyAsync(packets);

    public sealed class RawPacketDocument
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.ObjectId)]
        public string? Id        { get; set; }
        public string  SessionId { get; init; } = "";
        public uint    TimestampMs { get; init; }
        public int     LapNumber { get; init; }

        // Core physics fields stored raw
        public float Speed          { get; init; }
        public float CurrentRpm     { get; init; }
        public float AccelerationX  { get; init; }
        public float AccelerationY  { get; init; }
        public float AccelerationZ  { get; init; }
        public float TireTempFL     { get; init; }
        public float TireTempFR     { get; init; }
        public float TireTempRL     { get; init; }
        public float TireTempRR     { get; init; }
        public float SuspFL         { get; init; }
        public float SuspFR         { get; init; }
        public float SuspRL         { get; init; }
        public float SuspRR         { get; init; }
        public byte  Accel          { get; init; }
        public byte  Brake          { get; init; }
        public byte  Gear           { get; init; }
        public float Fuel           { get; init; }
        public float CurrentLap     { get; init; }
        public float BestLap        { get; init; }
        public float EngineMaxRpm   { get; init; }
    }
}
