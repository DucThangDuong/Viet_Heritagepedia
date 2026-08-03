using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Domain.Repositories;

public interface IMongoRepository<TDocument> where TDocument : class
{
    Task<TDocument?> GetByIdAsync(string id);
    Task<IEnumerable<TDocument>> GetAllAsync();
    Task<IEnumerable<TDocument>> FindAsync(Expression<Func<TDocument, bool>> predicate);
    Task InsertAsync(TDocument document);
    Task UpdateAsync(string id, TDocument document);
    Task DeleteAsync(string id);
}
