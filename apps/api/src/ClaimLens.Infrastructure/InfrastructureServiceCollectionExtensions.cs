using ClaimLens.Application.Admin;
using ClaimLens.Application.Auth;
using ClaimLens.Application.Investigations;
using ClaimLens.Application.Reviews;
using ClaimLens.Infrastructure.Admin;
using ClaimLens.Infrastructure.Ai;
using ClaimLens.Infrastructure.EvidenceCollection;
using ClaimLens.Infrastructure.Investigations;
using ClaimLens.Infrastructure.Reviews;
using ClaimLens.Infrastructure.Auth;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? configuration["DATABASE_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ClaimLensDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IUserStore, EfUserStore>();
            services.AddScoped<IInvestigationStore, EfInvestigationStore>();
            services.AddScoped<IReviewStore, EfReviewStore>();
            services.AddScoped<IAdminStore, EfAdminStore>();
        }

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        var aiUrl = configuration["AI_SERVICE_URL"] ?? "http://localhost:8000";
        var aiKey = configuration["AI_SERVICE_KEY"];
        services.AddHttpClient<IAiAnalysisClient, HttpAiAnalysisClient>(client =>
        {
            client.BaseAddress = new Uri(aiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            if (!string.IsNullOrWhiteSpace(aiKey))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Key", aiKey);
            }
        });
        services.AddScoped<ClaimExtractionProcessor>();
        services.AddSingleton<IEvidenceProvider, MockEvidenceProvider>();
        services.AddScoped<EvidenceCollectionProcessor>();
        services.AddScoped<EvidenceClassificationProcessor>();
        services.AddScoped<ConclusionProcessor>();
        if (configuration.GetValue("Extraction:Enabled", false))
        {
            services.AddHostedService<ClaimExtractionWorker>();
        }

        if (configuration.GetValue("Evidence:Enabled", false))
        {
            services.AddHostedService<EvidenceCollectionWorker>();
        }

        if (configuration.GetValue("Classification:Enabled", false))
        {
            services.AddHostedService<EvidenceClassificationWorker>();
        }

        if (configuration.GetValue("Verdict:Enabled", false))
        {
            services.AddHostedService<ConclusionWorker>();
        }

        if (configuration.GetValue("Recovery:Enabled", false))
        {
            services.AddHostedService<WorkerRecoveryService>();
        }

        return services;
    }
}
