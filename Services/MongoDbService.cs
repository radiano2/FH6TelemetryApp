using MongoDB.Driver;

namespace FH6TelemetryApp.Services;

public sealed class MongoDbService
{
    public IMongoDatabase Database { get; }

    public MongoDbService(IConfiguration config)
    {
        var connectionString = config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName     = config["MongoDB:DatabaseName"]     ?? "fh6telemetry";
        var client           = new MongoClient(connectionString);
        Database             = client.GetDatabase(databaseName);
    }
}
