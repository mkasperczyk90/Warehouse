using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.IdentityBridge;

/// <summary>
/// Polls Keycloak's Admin REST API for the full user list + realm roles, on a fixed interval, and
/// writes one <c>BronzeUser</c> snapshot per user via the shared <see cref="IBronzeExportWriter"/> —
/// the same writer every event-driven Bronze consumer uses, so this data lands in the same
/// <c>bronze/identity-service/UserSnapshot/...</c> shape as everything else (docs/BI_Plan.md §5.4).
/// A poll cycle failure is logged and retried on the next tick rather than crashing the worker.
/// </summary>
public sealed partial class UserSnapshotPollingService(
    KeycloakAdminClient keycloak,
    IBronzeExportWriter writer,
    IOptions<IdentityBridgeOptions> options,
    ILogger<UserSnapshotPollingService> logger) : BackgroundService
{
    private readonly IdentityBridgeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        do
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "IdentityBridge: poll cycle failed; will retry next interval");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var polledAt = DateTimeOffset.UtcNow;
        var accessToken = await keycloak.GetAccessTokenAsync(cancellationToken);
        var users = await keycloak.ListUsersAsync(accessToken, cancellationToken);

        foreach (var user in users)
        {
            var roles = await keycloak.GetRealmRolesAsync(accessToken, user.Id, cancellationToken);
            await writer.WriteAsync(BronzeUserMapper.Map(user, roles, polledAt), cancellationToken);
        }

        LogWroteSnapshots(logger, users.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "IdentityBridge: wrote {Count} user snapshot(s)")]
    private static partial void LogWroteSnapshots(ILogger logger, int count);
}
