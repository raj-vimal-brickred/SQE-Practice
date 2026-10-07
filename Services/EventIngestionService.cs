using SQE_Practice.Models;
using SQE_Practice.Observability;
using SQE_Practice.Storage;
using System.Diagnostics;

namespace SQE_Practice.Services
{
    public sealed class EventIngestionService
    {
        private readonly EventStore _eventStore;

        private readonly StandingQueryEngine
        _standingQueryEngine;

        private readonly ILogger<EventIngestionService>
        _logger;

        public EventIngestionService(
        EventStore eventStore,
        StandingQueryEngine standingQueryEngine,
        ILogger<EventIngestionService> logger)
        {
            _eventStore = eventStore;

            _standingQueryEngine =
            standingQueryEngine;

            _logger = logger;
        }

        public async Task<IngestionResult> IngestAsync(
            TelemetryEvent telemetryEvent,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            using var activity =
                SqeTelemetry.ActivitySource.StartActivity(
                    "SQE.IngestEvent");

            activity?.SetTag(
                "sqe.event.name",
                telemetryEvent.EventName);

            activity?.SetTag(
                "sqe.service",
                telemetryEvent.Service);

            activity?.SetTag(
                "sqe.region",
                telemetryEvent.Region);

            activity?.SetTag(
                "sqe.event.id",
                telemetryEvent.EventId);

            try
            {
                SqeTelemetry.EventsReceived.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "event.name",
                        telemetryEvent.EventName));

                if (telemetryEvent.EventId == Guid.Empty)
                {
                    telemetryEvent.EventId =
                        Guid.NewGuid();
                }

                if (telemetryEvent.Timestamp == default)
                {
                    telemetryEvent.Timestamp =
                        DateTime.UtcNow;
                }

                var saved =
                    await _eventStore.SaveAsync(
                        telemetryEvent,
                        cancellationToken);

                if (!saved)
                {
                    activity?.SetTag(
                        "sqe.event.duplicate",
                        true);

                    return new IngestionResult(
                        Accepted: true,
                        Duplicate: true,
                        EventId: telemetryEvent.EventId);
                }

                await _standingQueryEngine.ProcessAsync(
                    telemetryEvent,
                    cancellationToken);

                activity?.SetStatus(
                    ActivityStatusCode.Ok);

                return new IngestionResult(
                    Accepted: true,
                    Duplicate: false,
                    EventId: telemetryEvent.EventId);
            }
            catch (Exception exception)
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error,
                    exception.Message);

                activity?.SetTag(
                    "error.type",
                    exception.GetType().FullName);

                throw;
            }
            finally
            {
                stopwatch.Stop();

                SqeTelemetry.ProcessingDuration.Record(
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    public sealed record IngestionResult(
    bool Accepted,
    bool Duplicate,
    Guid EventId);
}
