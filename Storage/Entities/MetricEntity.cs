namespace SQE_Practice.Storage.Entities
{
    public sealed class MetricEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public int WindowSeconds { get; set; }
        public string DimensionsJson { get; set; } = "{}";
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
