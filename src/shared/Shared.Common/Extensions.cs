using Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Common;

public static class InitializerExtensions
{
    public static IServiceCollection AddInitializer<T>(this IServiceCollection services) where T : class, IInitializer
           => services.AddTransient<IInitializer, T>();
}