namespace SQE_Practice.Storage.Entities
{
    public sealed class TelemetryEventEntity
    {
        public long Id { get; set; }
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double? DurationMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string PropertiesJson { get; set; } = "{}";
        public DateTime CreatedAtUtc { get; set; }
    }
}
