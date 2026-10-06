namespace SQE_Practice.Models
{
    public class Alert
    {
        public string QueryName { get; set; } =string.Empty;
        public string Message { get; set; }=string.Empty;
        public double CurrentValue { get; set; }
        public double Threshold { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Dimensions { get; set; } = new();

    }
}
