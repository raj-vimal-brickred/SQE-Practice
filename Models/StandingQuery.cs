namespace SQE_Practice.Models
{
    public class StandingQuery
    {
        public string Name { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string Aggregration { get; set; } = "count";
        public int WindowSeconds { get; set; } = 60;
        public List<string> Dimensions { get; set; } = new();
        public AlertRule? Alert{ get; set; }
    }
}
