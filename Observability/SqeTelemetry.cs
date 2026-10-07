using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SQE_Practice.Observability;

public static class SqeTelemetry
{
    public const string ServiceName = "SQE_Practice";

    public static readonly ActivitySource ActivitySource =
        new(ServiceName);

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

    public static readonly Counter<long> HttpRequestsTotal =
        Meter.CreateCounter<long>(
            "sqe.http.requests.total",
            description: "Total HTTP requests handled by the middleware.");

    public static readonly Counter<long> HttpRequestsInfo =
        Meter.CreateCounter<long>(
            "sqe.http.requests.info",
            description: "HTTP requests classified as Info (2xx, fast).");

    public static readonly Counter<long> HttpRequestsWarning =
        Meter.CreateCounter<long>(
            "sqe.http.requests.warning",
            description: "HTTP requests classified as Warning (4xx, 3xx, or slow 2xx).");

    public static readonly Counter<long> HttpRequestsError =
        Meter.CreateCounter<long>(
            "sqe.http.requests.error",
            description: "HTTP requests classified as Error (5xx).");

    public static readonly Counter<long> HttpRequestsCritical =
        Meter.CreateCounter<long>(
            "sqe.http.requests.critical",
            description: "HTTP requests classified as Critical (5xx AND slow).");

    public static readonly Histogram<double> HttpRequestDuration =
        Meter.CreateHistogram<double>(
            "sqe.http.request.duration",
            unit: "ms",
            description: "HTTP request round-trip duration in milliseconds.");
}