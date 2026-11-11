using ClaimLens.Application.Investigations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaimLens.Infrastructure.Investigations;

public sealed class WorkerRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<WorkerRecoveryService> _logger;
    private readonly TimeProvider _clock;

    public WorkerRecoveryService(IServiceScopeFactory scopes, ILogger<WorkerRecoveryService> logger, TimeProvider clock)
    {
        _scopes = scopes;
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IInvestigationStore>();
                var recovered = await store.RecoverStaleAsync(_clock.GetUtcNow(), stoppingToken);
                if (recovered > 0)
                {
                    _logger.LogInformation("Recovered {Count} stale investigations", recovered);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Worker recovery failed");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
