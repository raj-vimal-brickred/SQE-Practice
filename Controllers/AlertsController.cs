using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Services;

namespace SQE_Practice.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly AlertStore _alertStore;

    public AlertsController(
        AlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var rows = await _alertStore.GetAllAsync(
            limit,
            cancellationToken);

        return Ok(new
        {
            count = rows.Count,

            alerts = rows.Select(item => new
            {
                item.Id,
                item.QueryName,
                item.Message,
                item.CurrentValue,
                item.Threshold,
                item.Timestamp,

                dimensions =
                    JsonSerializer.Deserialize<
                        Dictionary<string, string>>(
                            item.DimensionsJson)
            })
        });
    }
}