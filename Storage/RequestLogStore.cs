using Microsoft.EntityFrameworkCore;
using SQE_Practice.Models;
using SQE_Practice.Storage.Entities;

namespace SQE_Practice.Storage;

/// <summary>
/// Saves and retrieves <see cref="RequestLog"/> records
/// from the SQL Server RequestLogs table.
/// </summary>
public sealed class RequestLogStore
{
    private readonly IDbContextFactory<SqeDbContext>
        _dbContextFactory;

    private readonly ILogger<RequestLogStore> _logger;

    public RequestLogStore(
        IDbContextFactory<SqeDbContext> dbContextFactory,
        ILogger<RequestLogStore> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger           = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Write
    // ──────────────────────────────────────────────────────────────────────

    public async Task SaveAsync(
        RequestLog log,
        CancellationToken cancellationToken = default)
    {
        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var entity = new RequestLogEntity
        {
            RequestId    = log.RequestId,
            Method       = log.Method,
            Path         = log.Path,
            QueryString  = log.QueryString,
            StatusCode   = log.StatusCode,
            Severity     = log.Severity,
            DurationMs   = log.DurationMs,
            ClientIp     = log.ClientIp,
            UserAgent    = log.UserAgent,
            RequestBody  = log.RequestBody,
            ResponseBody = log.ResponseBody,
            ErrorMessage = log.ErrorMessage,
            ErrorType    = log.ErrorType,
            Timestamp    = log.Timestamp == default
                               ? DateTime.UtcNow
                               : log.Timestamp,
            CreatedAtUtc = DateTime.UtcNow
        };

        database.RequestLogs.Add(entity);

        await database.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "RequestLog saved: {Method} {Path} → {StatusCode} [{Severity}] in {DurationMs:F1} ms",
            log.Method,
            log.Path,
            log.StatusCode,
            log.Severity,
            log.DurationMs);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Read – recent logs (optionally filtered by severity)
    // ──────────────────────────────────────────────────────────────────────

    public async Task<List<RequestLogEntity>> GetRecentAsync(
        int    limit             = 100,
        string? severityFilter   = null,
        string? pathFilter       = null,
        string? methodFilter     = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);

        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var query = database.RequestLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(severityFilter))
        {
            query = query.Where(
                x => x.Severity == severityFilter);
        }

        if (!string.IsNullOrWhiteSpace(pathFilter))
        {
            query = query.Where(
                x => x.Path.Contains(pathFilter));
        }

        if (!string.IsNullOrWhiteSpace(methodFilter))
        {
            query = query.Where(
                x => x.Method == methodFilter.ToUpperInvariant());
        }

        return await query
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Summary stats – used by the dashboard endpoint
    // ──────────────────────────────────────────────────────────────────────

    public async Task<RequestLogSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var database =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var groups = await database.RequestLogs
            .AsNoTracking()
            .GroupBy(x => x.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var stats = await database.RequestLogs
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                AvgDurationMs = g.Average(x => x.DurationMs),
                MaxDurationMs = g.Max(x => x.DurationMs),
                TotalRequests = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new RequestLogSummary
        {
            TotalRequests    = stats?.TotalRequests  ?? 0,
            AvgDurationMs    = stats?.AvgDurationMs  ?? 0,
            MaxDurationMs    = stats?.MaxDurationMs  ?? 0,
            InfoCount        = groups.FirstOrDefault(g => g.Severity == "Info")?.Count    ?? 0,
            WarningCount     = groups.FirstOrDefault(g => g.Severity == "Warning")?.Count ?? 0,
            ErrorCount       = groups.FirstOrDefault(g => g.Severity == "Error")?.Count   ?? 0,
            CriticalCount    = groups.FirstOrDefault(g => g.Severity == "Critical")?.Count ?? 0,
        };
    }
}

/// <summary>
/// Aggregated statistics returned by GET /api/requestlogs/summary.
/// </summary>
public sealed class RequestLogSummary
{
    public int    TotalRequests  { get; set; }
    public double AvgDurationMs  { get; set; }
    public double MaxDurationMs  { get; set; }
    public int    InfoCount      { get; set; }
    public int    WarningCount   { get; set; }
    public int    ErrorCount     { get; set; }
    public int    CriticalCount  { get; set; }
}

