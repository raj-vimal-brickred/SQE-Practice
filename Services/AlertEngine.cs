using SQE_Practice.Models;

namespace SQE_Practice.Services;

public sealed class AlertEngine
{
    private readonly AlertStore _alertStore;

    public AlertEngine(AlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    public async Task EvaluateAsync(
        StandingQuery query,
        Metric metric,
        CancellationToken cancellationToken = default)
    {
        if (query.Alert is null ||
            !query.Alert.Enabled)
        {
            return;
        }

        var triggered =
            IsTriggered(
                metric.Value,
                query.Alert.Operator,
                query.Alert.Threshold);

        if (!triggered)
        {
            return;
        }

        var alert = new Alert
        {
            QueryName = query.Name,

            Message =
                $"{query.Name} crossed its configured threshold.",

            CurrentValue = metric.Value,

            Threshold = query.Alert.Threshold,

            Timestamp = DateTime.UtcNow,

            Dimensions =
                new Dictionary<string, string>(
                    metric.Dimensions)
        };

        await _alertStore.SaveAsync(
            alert,
            cancellationToken);
    }

    private static bool IsTriggered(
        double value,
        string alertOperator,
        double threshold)
    {
        return alertOperator
            .Trim()
            .ToLowerInvariant() switch
        {
            "gte" => value >= threshold,
            "gt" => value > threshold,
            "lte" => value <= threshold,
            "lt" => value < threshold,
            "eq" => Math.Abs(
                value - threshold) < 0.000001,
            "ne" => Math.Abs(
                value - threshold) >= 0.000001,

            _ => throw new NotSupportedException(
                $"Alert operator '{alertOperator}' is not supported.")
        };
    }
}