using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Domain.Repositories;
using MongoDB.Driver;

namespace Infrastructure.Persistence.MongoDb;

public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : class
{
    private readonly IMongoCollection<TDocument> _collection;

    public MongoRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<TDocument>();
    }

    public async Task<TDocument?> GetByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TDocument>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<TDocument>> FindAsync(Expression<Func<TDocument, bool>> predicate)
    {
        return await _collection.Find(predicate).ToListAsync();
    }

    public async Task InsertAsync(TDocument document)
    {
        await _collection.InsertOneAsync(document);
    }

    public async Task UpdateAsync(string id, TDocument document)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        await _collection.ReplaceOneAsync(filter, document);
    }

    public async Task DeleteAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        await _collection.DeleteOneAsync(filter);
    }
}
