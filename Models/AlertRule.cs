namespace SQE_Practice.Models
{
    public class AlertRule
    {
        public string Operator { get; set; } = "gte";

        public double Threshold { get; set; }

        public int CooldownSeconds { get; set; } = 300;

        public bool Enabled { get; set; } = true;
    }
}