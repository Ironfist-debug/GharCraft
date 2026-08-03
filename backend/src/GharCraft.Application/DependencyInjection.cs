using GharCraft.Application.Common.Interfaces;
using GharCraft.Application.Identity.Services;
using GharCraft.Application.Identity.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GharCraft.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
