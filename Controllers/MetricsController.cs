using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE_Practice.Storage;

namespace SQE_Practice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly MetricStore _metricStore;
        public MetricsController(MetricStore metricStore)
        {
            _metricStore = metricStore;
        }
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            var metrics = _metricStore.GetAll();
            return Ok(new
            {
                count = metrics.Count,
                metrics
            });
        }
    }
}
