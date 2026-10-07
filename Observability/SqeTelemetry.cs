using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SQE_Practice.Observability;

public static class SqeTelemetry
{
    public const string ServiceName = "SQE_Practice";

    // Tracing
    public static readonly ActivitySource ActivitySource =
        new(ServiceName);

    // Metrics
    public static readonly Meter Meter =
        new(ServiceName);

    public static readonly Counter<long> EventsReceived =
        Meter.CreateCounter<long>(
            "sqe.events.received",
            description: "Total telemetry events received.");

    public static readonly Counter<long> EventsMatched =
        Meter.CreateCounter<long>(
            "sqe.events.matched",
            description: "Total events matched with standing queries.");

    public static readonly Counter<long> EventsUnmatched =
        Meter.CreateCounter<long>(
            "sqe.events.unmatched",
            description: "Total events that did not match a standing query.");

    public static readonly Counter<long> MetricsGenerated =
        Meter.CreateCounter<long>(
            "sqe.metrics.generated",
            description: "Total SQE metrics generated.");

    public static readonly Counter<long> AlertsTriggered =
        Meter.CreateCounter<long>(
            "sqe.alerts.triggered",
            description: "Total alerts triggered.");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "sqe.event.processing.duration",
            unit: "ms",
            description: "SQE event processing duration.");
}