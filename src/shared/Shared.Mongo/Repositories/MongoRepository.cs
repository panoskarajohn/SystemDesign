using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Shared.Mongo.Repositories;

public interface IMongoRepository<TEntity, in TIdentifiable> where TEntity : IIdentifiable<TIdentifiable> {
    IMongoCollection<TEntity> Collection { get; }
    Task<TEntity> GetAsync(TIdentifiable id);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task UpdateAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate);
    Task DeleteAsync(TIdentifiable id);
}

internal class MongoRepository<TEntity, TIdentifiable> : IMongoRepository<TEntity, TIdentifiable>
    where TEntity : IIdentifiable<TIdentifiable> {
    public MongoRepository(IMongoDatabase database, string collectionName) {
        Collection = database.GetCollection<TEntity>(collectionName);
    }

    public IMongoCollection<TEntity> Collection { get; }

    public Task<TEntity> GetAsync(TIdentifiable id) {
        return Collection.Find(e
                => e.Id != null && e.Id.Equals(id))
            .SingleOrDefaultAsync();
    }

    public Task AddAsync(TEntity entity) => Collection.InsertOneAsync(entity);

    public Task UpdateAsync(TEntity entity)
        => Collection.ReplaceOneAsync(e => e.Id != null && e.Id.Equals(entity.Id), entity);

    public Task UpdateAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        => Collection.ReplaceOneAsync(predicate, entity);

    public Task DeleteAsync(TIdentifiable id)
        => Collection.DeleteOneAsync(e => e.Id != null && e.Id.Equals(id));
}