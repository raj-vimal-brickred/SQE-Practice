using SQE_Practice.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SQE_Practice.Models
{
    public sealed class AlertRule : IValidatableObject
    {
        [JsonPropertyName("operator")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AlertOperator Operator { get; init; }

        [JsonPropertyName("threshold")]
        public double Threshold { get; init; }

        [JsonPropertyName("cooldownSeconds")]
        [Range(0, 86400)]
        public int CooldownSeconds { get; init; } = 300;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; } = true;

        public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
        {
            if (double.IsNaN(Threshold) ||
            double.IsInfinity(Threshold))
            {
                yield return new ValidationResult(
                "Alert threshold must be a finite number.",
                new[] { nameof(Threshold) });
            }
        }
    }
}
