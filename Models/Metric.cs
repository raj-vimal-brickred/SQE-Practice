namespace SQE_Practice.Models
{
    public class Metric
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime TimeStamp = DateTime.UtcNow;
        public int WindowSeconds { get; set; }
        public Dictionary<string, string> Dimensions { get; set; } = new();
    }
}
