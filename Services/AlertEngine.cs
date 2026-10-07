using System.Collections.Concurrent;
using SQE_Practice.Models;
using SQE_Practice.Observability;

namespace SQE_Practice.Services;

public sealed class AlertEngine
{
    private readonly AlertStore _alertStore;
    private readonly ConcurrentDictionary<string, DateTime> _lastAlertTimes = new();

    public AlertEngine(AlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    public async Task EvaluateAsync(
        StandingQuery query,
        Metric metric,
        CancellationToken cancellationToken = default)
    {
        if (query.Alert is null || !query.Alert.Enabled)
        {
            return;
        }

        var triggered = IsTriggered(
            metric.Value,
            query.Alert.Operator,
            query.Alert.Threshold);

        if (!triggered)
        {
            return;
        }

        var dimensionKey = string.Join("|", 
            metric.Dimensions.OrderBy(k => k.Key).Select(k => $"{k.Key}:{k.Value}"));
        
        var cooldownKey = $"{query.Name}|{dimensionKey}";
        var now = DateTime.UtcNow;

        if (_lastAlertTimes.TryGetValue(cooldownKey, out var lastAlertTime))
        {
            if ((now - lastAlertTime).TotalSeconds < query.Alert.CooldownSeconds)
            {
                return;
            }
        }

        _lastAlertTimes[cooldownKey] = now;

        var alert = new Alert
        {
            QueryName = query.Name,
            Message = $"{query.Name} crossed its configured threshold. Value: {metric.Value} (Threshold: {query.Alert.Threshold})",
            CurrentValue = metric.Value,
            Threshold = query.Alert.Threshold,
            Timestamp = now,
            Dimensions = new Dictionary<string, string>(metric.Dimensions)
        };

        await _alertStore.SaveAsync(alert, cancellationToken);
        
        SqeTelemetry.AlertsTriggered.Add(1, new KeyValuePair<string, object?>("query", query.Name));
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
            "eq" => Math.Abs(value - threshold) < 0.000001,
            "ne" => Math.Abs(value - threshold) >= 0.000001,

            _ => throw new NotSupportedException(
                $"Alert operator '{alertOperator}' is not supported.")
        };
    }
}