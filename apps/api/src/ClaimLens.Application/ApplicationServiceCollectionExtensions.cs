using ClaimLens.Application.Admin;
using ClaimLens.Application.Auth;
using ClaimLens.Application.Investigations;
using ClaimLens.Application.Reviews;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<InvestigationService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<AdminService>();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IInvestigationProgress, NoInvestigationProgress>();
        return services;
    }
}
