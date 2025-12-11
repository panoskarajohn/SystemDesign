using Shared.Common;
using Microsoft.Extensions.DependencyInjection;

public static class InitializerExtensions
{
    public static IServiceCollection AddInitializer<T>(this IServiceCollection services) where T : class, IInitializer
           => services.AddTransient<IInitializer, T>();
}