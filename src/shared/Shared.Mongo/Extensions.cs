using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Shared.Mongo;

public static class Extensions
{
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

}
