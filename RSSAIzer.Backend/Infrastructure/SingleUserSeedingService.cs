using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RSSAIzer.Backend.Infrastructure;

/// <summary>
/// Ensures the single user exists in the database on startup if single-user mode is enabled.
/// This service runs early to ensure the user exists before other services try to use it.
/// </summary>
internal sealed class SingleUserSeedingService(
    BackendAuthConfiguration authConfig,
    IServiceProvider serviceProvider,
    ILogger<SingleUserSeedingService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Only run in single-user mode
        if (!authConfig.IsSingleUserMode)
        {
            logger.LogDebug("Not in single-user mode, skipping user seeding");
            return;
        }

        try
        {
            logger.LogInformation(
                "Single-user mode enabled. Ensuring user {UserId} exists in database",
                authConfig.SingleUserId!.Value
            );

            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var userPersistenceService =
                scope.ServiceProvider.GetRequiredService<IUserPersistenceService>();

            await userPersistenceService.EnsureUserExistsAsync(
                authConfig.SingleUserId.Value,
                authConfig.SingleUserEmail!,
                cancellationToken
            );

            logger.LogInformation(
                "Successfully ensured single user {UserId} exists in database",
                authConfig.SingleUserId.Value
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to seed single user {UserId} in database",
                authConfig.SingleUserId!.Value
            );
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Nothing to clean up
        return Task.CompletedTask;
    }
}
