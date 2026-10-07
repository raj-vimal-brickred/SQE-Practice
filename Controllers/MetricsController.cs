using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Services;

namespace SQE_Practice.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private readonly MetricStore _metricStore;

    public MetricsController(
        MetricStore metricStore)
    {
        _metricStore = metricStore;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var rows = await _metricStore.GetAllAsync(
            limit,
            cancellationToken);

        return Ok(new
        {
            count = rows.Count,

            metrics = rows.Select(item => new
            {
                item.Id,
                item.Name,
                item.Value,
                item.WindowSeconds,
                item.Timestamp,

                dimensions =
                    JsonSerializer.Deserialize<
                        Dictionary<string, string>>(
                            item.DimensionsJson)
            })
        });
    }
}