using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQE_Practice.Models;
using SQE_Practice.Storage;
using SQE_Practice.Storage.Entities;

namespace SQE_Practice.Services;

public sealed class MetricStore
{
    private readonly IDbContextFactory<SqeDbContext>
        _dbContextFactory;

    private readonly ILogger<MetricStore> _logger;

    public MetricStore(
        IDbContextFactory<SqeDbContext> dbContextFactory,
        ILogger<MetricStore> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task SaveAsync(
        Metric metric,
        CancellationToken cancellationToken = default)
    {
        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var entity = new MetricEntity
        {
            Name = metric.Name,
            Value = metric.Value,
            WindowSeconds = metric.WindowSeconds,

            Timestamp =
                metric.TimeStamp == default
                    ? DateTime.UtcNow
                    : metric.TimeStamp,

            DimensionsJson =
                JsonSerializer.Serialize(
                    metric.Dimensions),

            CreatedAtUtc = DateTime.UtcNow
        };

        database.Metrics.Add(entity);

        await database.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Metric {MetricName} with value {MetricValue} saved.",
            metric.Name,
            metric.Value);
    }

    public async Task<List<MetricEntity>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);

        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await database.Metrics
            .AsNoTracking()
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}