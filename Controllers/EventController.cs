using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Models;
using SQE_Practice.Services;

namespace SQE_Practice.Controllers
{
    [ApiController]
    [Route("api/events")]
    public sealed class EventsController : ControllerBase
    {
        private readonly EventIngestionService
        _eventIngestionService;

        public EventsController(
        EventIngestionService eventIngestionService)
        {
            _eventIngestionService =
            eventIngestionService;
        }

        [HttpPost]
        public async Task<IActionResult> Ingest(
        [FromBody] TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
            telemetryEvent.EventName))
            {
                return BadRequest(new
                {
                    message = "EventName is required."
                });
            }

            if (string.IsNullOrWhiteSpace(
            telemetryEvent.Service))
            {
                return BadRequest(new
                {
                    message = "Service is required."
                });
            }

            var result =
            await _eventIngestionService.IngestAsync(
            telemetryEvent,
            cancellationToken);

            return result.Duplicate
            ? Ok(new
            {
                accepted = true,
                duplicate = true,
                eventId = result.EventId
            })
            : Accepted(new
            {
                accepted = true,
                duplicate = false,
                eventId = result.EventId
            });
        }
    }
}
