namespace SQE_Practice.Models
{
    public class TelemetryEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventName { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double? DurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
