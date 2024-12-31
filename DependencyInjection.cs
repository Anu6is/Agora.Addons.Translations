using Microsoft.Extensions.DependencyInjection;

namespace Agora.Addons.Translations;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        services.AddScoped<GlobalizationResources>();
        return services;
    }
}
