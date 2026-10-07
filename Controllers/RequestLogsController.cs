using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Storage;

namespace SQE_Practice.Controllers;

[ApiController]
[Route("api/requestlogs")]
public sealed class RequestLogsController : ControllerBase
{
    private readonly RequestLogStore _store;

    public RequestLogsController(RequestLogStore store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int     limit    = 100,
        [FromQuery] string? severity = null,
        [FromQuery] string? path     = null,
        [FromQuery] string? method   = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _store.GetRecentAsync(
            limit,
            severity,
            path,
            method,
            cancellationToken);

        return Ok(new
        {
            count = rows.Count,
            filters = new { severity, path, method },
            logs = rows.Select(r => new
            {
                r.Id,
                r.RequestId,
                r.Method,
                r.Path,
                r.QueryString,
                r.StatusCode,
                r.Severity,
                r.DurationMs,
                r.ClientIp,
                r.UserAgent,
                r.ErrorMessage,
                r.ErrorType,
                r.Timestamp
            })
        });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        CancellationToken cancellationToken = default)
    {
        var summary = await _store.GetSummaryAsync(cancellationToken);

        return Ok(new
        {
            summary.TotalRequests,
            avgDurationMs   = Math.Round(summary.AvgDurationMs, 2),
            maxDurationMs   = Math.Round(summary.MaxDurationMs, 2),
            bySeverity = new
            {
                info     = summary.InfoCount,
                warning  = summary.WarningCount,
                error    = summary.ErrorCount,
                critical = summary.CriticalCount
            }
        });
    }

    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var errors = await _store.GetRecentAsync(limit, "Error", null, null, cancellationToken);

        var critical = await _store.GetRecentAsync(limit, "Critical", null, null, cancellationToken);

        var combined = errors
            .Concat(critical)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .Select(r => new
            {
                r.RequestId,
                r.Method,
                r.Path,
                r.StatusCode,
                r.Severity,
                r.DurationMs,
                r.ErrorMessage,
                r.ErrorType,
                r.Timestamp
            });

        return Ok(new { count = combined.Count(), errors = combined });
    }

    [HttpGet("warnings")]
    public async Task<IActionResult> GetWarnings(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var rows = await _store.GetRecentAsync(
            limit, "Warning", null, null, cancellationToken);

        return Ok(new
        {
            count    = rows.Count,
            warnings = rows.Select(r => new
            {
                r.RequestId,
                r.Method,
                r.Path,
                r.StatusCode,
                r.Severity,
                r.DurationMs,
                r.Timestamp
            })
        });
    }

    [HttpGet("slow")]
    public async Task<IActionResult> GetSlow([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var warnings = await _store.GetRecentAsync(limit * 2, "Warning", null, null, cancellationToken);

        var critical = await _store.GetRecentAsync(limit * 2, "Critical", null, null, cancellationToken);

        var slow = warnings
            .Concat(critical)
            .OrderByDescending(r => r.DurationMs)
            .Take(limit)
            .Select(r => new
            {
                r.RequestId,
                r.Method,
                r.Path,
                r.StatusCode,
                r.Severity,
                r.DurationMs,
                r.Timestamp
            });

        return Ok(new { count = slow.Count(), slowRequests = slow });
    }
}

