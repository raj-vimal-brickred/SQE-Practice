using SQE_Practice.Models;
using SQE_Practice.Observability;

namespace SQE_Practice.Services;

public sealed class StandingQueryEngine
{
    private readonly QueryLoader _queryLoader;
    private readonly AggregrationEngine _AggregrationEngine;
    private readonly FilterEngine _filterEngine;
    private readonly ILogger<StandingQueryEngine> _logger;

    public StandingQueryEngine(QueryLoader queryLoader, AggregrationEngine AggregrationEngine, FilterEngine filterEngine, ILogger<StandingQueryEngine> logger)
    {
        _queryLoader = queryLoader;
        _AggregrationEngine = AggregrationEngine;
        _filterEngine = filterEngine;
        _logger = logger;
    }

    public async Task ProcessAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
    {
        using var activity = SqeTelemetry.ActivitySource.StartActivity("SQE.ProcessStandingQueries");

        var queries = await _queryLoader.LoadAsync();
        var matched = false;

        foreach (var query in queries)
        {
            if (!string.Equals(query.EventName?.Trim(), telemetryEvent.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_filterEngine.IsMatch(telemetryEvent, query.Filters))
            {
                continue;
            }

            matched = true;

            SqeTelemetry.EventsMatched.Add(1, new KeyValuePair<string, object?>("query.name", query.Name));
            activity?.SetTag("sqe.query.matched", query.Name);

            await _AggregrationEngine.ProcessAsync(query, telemetryEvent, cancellationToken);
        }

        if (!matched)
        {
            SqeTelemetry.EventsUnmatched.Add(1, new KeyValuePair<string, object?>("event.name", telemetryEvent.EventName));
            activity?.SetTag("sqe.query.matched", false);
        }
    }
}