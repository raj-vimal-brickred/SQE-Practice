using SQE_Practice.Models;
using SQE_Practice.Observability;
namespace SQE_Practice.Services;

public sealed class AggregrationEngine
{
    private readonly MetricStore _metricStore;
    private readonly AlertEngine _alertEngine;

    private readonly object _lock = new();

    private readonly Dictionary<
        string,
        List<TelemetryEvent>> _eventsByQuery =
            new(StringComparer.OrdinalIgnoreCase);

    public AggregrationEngine(
        MetricStore metricStore,
        AlertEngine alertEngine)
    {
        _metricStore = metricStore;
        _alertEngine = alertEngine;
    }

    public async Task ProcessAsync(
        StandingQuery query,
        TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            SqeTelemetry.ActivitySource.StartActivity(
                "SQE.AggregateMetric");

        activity?.SetTag(
            "sqe.query.name",
            query.Name);

        activity?.SetTag(
            "sqe.event.name",
            telemetryEvent.EventName);

        activity?.SetTag(
            "sqe.service",
            telemetryEvent.Service);

        activity?.SetTag(
            "sqe.region",
            telemetryEvent.Region);

        List<Metric> metrics;

        lock (_lock)
        {
            if (!_eventsByQuery.TryGetValue(
                    query.Name,
                    out var events))
            {
                events =
                    new List<TelemetryEvent>();

                _eventsByQuery[query.Name] =
                    events;
            }

            events.Add(telemetryEvent);

            var windowStart =
                DateTime.UtcNow.AddSeconds(
                    -query.WindowSeconds);

            events.RemoveAll(
                item =>
                    item.Timestamp < windowStart);

            metrics =
                CreateMetrics(
                    query,
                    events);
        }

        foreach (var metric in metrics)
        {
            activity?.SetTag(
                "sqe.metric.name",
                metric.Name);

            activity?.SetTag(
                "sqe.metric.value",
                metric.Value);

            SqeTelemetry.MetricsGenerated.Add(
                1,
                new KeyValuePair<string, object?>(
                    "metric.name",
                    metric.Name));

            await _metricStore.SaveAsync(
                metric,
                cancellationToken);

            await _alertEngine.EvaluateAsync(
                query,
                metric,
                cancellationToken);
        }

        activity?.SetStatus(
            System.Diagnostics.ActivityStatusCode.Ok);
    }

    private static List<Metric> CreateMetrics(
        StandingQuery query,
        List<TelemetryEvent> events)
    {
        var groupedEvents = events.GroupBy(
            telemetryEvent =>
                CreateDimensionKey(
                    query,
                    telemetryEvent));

        var metrics = new List<Metric>();

        foreach (var group in groupedEvents)
        {
            var groupedItems = group.ToList();

            if (groupedItems.Count == 0)
            {
                continue;
            }

            // Get one event from this dimension group
            var firstEvent = groupedItems[0];

            var metric = new Metric
            {
                Name = query.Name,

                // IMPORTANT:
                // Calculate only this group's value
                Value = CalculateValue(
                    query,
                    groupedItems),

                TimeStamp = DateTime.UtcNow,

                WindowSeconds =
                    query.WindowSeconds,

                Dimensions =
                    CreateDimensions(
                        query,
                        firstEvent)
            };

            metrics.Add(metric);
        }

        return metrics;
    }

    private static double CalculateValue(
        StandingQuery query,
        List<TelemetryEvent> events)
    {
        if (query.Aggregration.Equals(
                "count",
                StringComparison.OrdinalIgnoreCase))
        {
            return events.Count;
        }

        var values = events
            .Where(item => item.DurationMs.HasValue)
            .Select(item => item.DurationMs!.Value)
            .ToArray();

        if (values.Length == 0)
        {
            return 0;
        }

        if (query.Aggregration.Equals(
                "sum",
                StringComparison.OrdinalIgnoreCase))
        {
            return values.Sum();
        }

        if (query.Aggregration.Equals(
                "average",
                StringComparison.OrdinalIgnoreCase))
        {
            return values.Average();
        }

        if (query.Aggregration.Equals(
                "min",
                StringComparison.OrdinalIgnoreCase))
        {
            return values.Min();
        }

        if (query.Aggregration.Equals(
                "max",
                StringComparison.OrdinalIgnoreCase))
        {
            return values.Max();
        }

        throw new NotSupportedException(
            $"Aggregration '{query.Aggregration}' is not supported.");
    }

    private static string CreateDimensionKey(
        StandingQuery query,
        TelemetryEvent telemetryEvent)
    {
        return string.Join(
            "|",
            query.Dimensions.Select(
                dimension =>
                    GetDimensionValue(
                        telemetryEvent,
                        dimension)));
    }

    private static Dictionary<string, string>
        CreateDimensions(
            StandingQuery query,
            TelemetryEvent telemetryEvent)
    {
        var dimensions =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var dimension in query.Dimensions)
        {
            dimensions[dimension] =
                GetDimensionValue(
                    telemetryEvent,
                    dimension);
        }

        return dimensions;
    }

    private static string GetDimensionValue(
        TelemetryEvent telemetryEvent,
        string dimension)
    {
        if (dimension.Equals(
                "service",
                StringComparison.OrdinalIgnoreCase))
        {
            return telemetryEvent.Service;
        }

        if (dimension.Equals(
                "region",
                StringComparison.OrdinalIgnoreCase))
        {
            return telemetryEvent.Region;
        }

        if (dimension.Equals(
                "eventName",
                StringComparison.OrdinalIgnoreCase))
        {
            return telemetryEvent.EventName;
        }

        if (telemetryEvent.Properties.TryGetValue(
                dimension,
                out var value))
        {
            return value?.ToString() ?? "Unknown";
        }

        return "Unknown";
    }
}