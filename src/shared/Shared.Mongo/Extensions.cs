using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Shared.Mongo.Factory;
using Shared.Mongo.Initializer;
using Shared.Mongo.Repositories;
using Shared.Common;

namespace Shared.Mongo;

public static class Extensions {
    private const string SectionName = "mongo";
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration) {
        var options = configuration
            .GetRequiredSection(SectionName)
            .Get<MongoOptions>()!;
        _ = services.AddSingleton(options);

        services.AddSingleton<IMongoClient>(sp => {
            return new MongoClient(options.ConnectionString);
        });

        services.AddTransient(sp => {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddInitializer<MongoInitializer>();
        services.AddTransient<IMongoSessionFactory, MongoSessionFactory>();

        registerConventions();

        return services;
    }

    public static IServiceCollection AddMongoRepository<TEntity, TIdentifiable>(this IServiceCollection services,
        string collectionName)
        where TEntity : IIdentifiable<TIdentifiable> {
        services.AddTransient<IMongoRepository<TEntity, TIdentifiable>>(sp => {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<TEntity, TIdentifiable>(database, collectionName);
        });

        return services;
    }

    private static void registerConventions() {
        BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(typeof(decimal?),
            new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
        ConventionRegistry.Register("mongo", new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String),
        }, _ => true);
    }

}
