using FH6TelemetryApp.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FH6TelemetryApp.Services;

/// <summary>Persists widget layout to MongoDB, replacing the localStorage approach.</summary>
public sealed class LayoutRepository
{
    private readonly IMongoCollection<LayoutDocument> _col;

    private const string SingletonId = "main";

    public LayoutRepository(MongoDbService mongo)
    {
        _col = mongo.Database.GetCollection<LayoutDocument>("widget_layout");
    }

    public async Task<List<WidgetConfig>?> LoadAsync()
    {
        var doc = await _col.Find(d => d.Id == SingletonId).FirstOrDefaultAsync();
        return doc?.Configs;
    }

    public async Task SaveAsync(IEnumerable<WidgetConfig> configs)
    {
        var doc = new LayoutDocument { Id = SingletonId, Configs = configs.ToList() };
        await _col.ReplaceOneAsync(
            d => d.Id == SingletonId,
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    private sealed class LayoutDocument
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string          Id      { get; set; } = "";
        public List<WidgetConfig> Configs { get; set; } = [];
    }
}
