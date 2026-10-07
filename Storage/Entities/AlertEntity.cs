namespace SQE_Practice.Storage.Entities
{
    public sealed class AlertEntity
    {
        public long Id { get; set; }
        public string QueryName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double Threshold { get; set; }
        public string DimensionsJson { get; set; } = "{}";
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
