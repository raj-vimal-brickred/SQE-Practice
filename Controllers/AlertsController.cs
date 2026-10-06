using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Storage;

namespace SQE_Practice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AlertStore _alertStore;
        public AlertsController(AlertStore alertStore)
        {
            _alertStore=alertStore;
        }
        [HttpGet("alerts")]
        public IActionResult GetAlerts()
        {
            var alerts = _alertStore.GetAll();
            return Ok(new
            {
                count = alerts.Count,
                alerts
            });
        }
    }
}
