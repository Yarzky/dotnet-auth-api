using AuthSystem.Application.Interfaces;
using AuthSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSystem.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }
}
