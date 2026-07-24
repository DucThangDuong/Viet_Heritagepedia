using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Persistence.MongoDb;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<TDocument> GetCollection<TDocument>(string? name = null)
    {
        return _database.GetCollection<TDocument>(name ?? typeof(TDocument).Name);
    }
}
