using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQE_Practice.Models;
using SQE_Practice.Storage;
using SQE_Practice.Storage.Entities;

namespace SQE_Practice.Services;

public sealed class AlertStore
{
    private readonly IDbContextFactory<SqeDbContext>
        _dbContextFactory;

    private readonly ILogger<AlertStore> _logger;

    public AlertStore(
        IDbContextFactory<SqeDbContext> dbContextFactory,
        ILogger<AlertStore> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task SaveAsync(
        Alert alert,
        CancellationToken cancellationToken = default)
    {
        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var entity = new AlertEntity
        {
            QueryName = alert.QueryName,
            Message = alert.Message,
            CurrentValue = alert.CurrentValue,
            Threshold = alert.Threshold,

            Timestamp =
                alert.Timestamp == default
                    ? DateTime.UtcNow
                    : alert.Timestamp,

            DimensionsJson =
                JsonSerializer.Serialize(
                    alert.Dimensions),

            CreatedAtUtc = DateTime.UtcNow
        };

        database.Alerts.Add(entity);

        await database.SaveChangesAsync(
            cancellationToken);

        _logger.LogWarning(
            "Alert for {QueryName} saved to SQL Server.",
            alert.QueryName);
    }

    public async Task<List<AlertEntity>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);

        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await database.Alerts
            .AsNoTracking()
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}