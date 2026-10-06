using SQE_Practice.Models;

namespace SQE_Practice.Services
{
    public class StandingQueryEngine
    {
        private readonly QueryLoader _queryLoader;
        private readonly AggregationEngine _aggregationEngine;
        public StandingQueryEngine(QueryLoader queryLoader,AggregationEngine aggregationEngine)
        {
            _queryLoader = queryLoader;
            _aggregationEngine = aggregationEngine;
        }
        public async Task ProcessAsync(TelemetryEvent telemetryEvent)
        {
            var queries = await _queryLoader.LoadAsync();
            Console.WriteLine($"Queries Loaded : {queries.Count}");
            Console.WriteLine($"Checking Event : {telemetryEvent.EventName}");
            var matched = false;
            foreach(var query in queries)
            {
                if (!string.Equals(query.EventName, telemetryEvent.EventName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                matched = true;
                Console.WriteLine("Standing Query Matched");
                Console.WriteLine($"Query : {query.Name}");
                Console.WriteLine($"Event : {telemetryEvent.EventName}");
                Console.WriteLine($"Aggregation : '{query.Aggregration}");
                _aggregationEngine.Process(query, telemetryEvent);
            }
            if (!matched)
            {
                Console.WriteLine($"No query Matched '{telemetryEvent.EventName}'");
            }
        }
    }
}
