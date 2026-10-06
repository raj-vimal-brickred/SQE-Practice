using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Models;
using SQE_Practice.Services;

namespace SQE_Practice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly EventIngestionService _eventIngestionService;
        public EventController(EventIngestionService eventIngestionService) 
        { 
            _eventIngestionService= eventIngestionService;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> Ingest([FromBody] TelemetryEvent telemetryEvent)
        {
            if (string.IsNullOrWhiteSpace(telemetryEvent.EventName))
            {
                return BadRequest(new
                {
                    message = "EventName is Required"
                });
            }
            await _eventIngestionService.Ingest(telemetryEvent);
            return Ok(new
            {
                accepted = true,
                eventName = telemetryEvent.EventName,
                message = "Telemetry Event Processed Successfully"
            });
        }
    }
}
