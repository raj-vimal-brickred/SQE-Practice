using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SQE_Practice.Models;
using SQE_Practice.Storage;

namespace SQE_Practice.Services
{
    public class AggregationEngine
    {
        private readonly MetricStore _metricStore;
        private readonly object _lock = new();
        private readonly Dictionary<string, List<TelemetryEvent>> _eventsByQuery = new();
        private readonly AlertEngine _alertEngine;
        public AggregationEngine(MetricStore metricStore, AlertEngine alertEngine)
        {
            _alertEngine=alertEngine;
            _metricStore = metricStore;
        }
        public void Process(StandingQuery query, TelemetryEvent telemetryEvent)
        {
            lock (_lock)
            {
                if (!_eventsByQuery.ContainsKey(query.Name))
                {
                    _eventsByQuery[query.Name] = new List<TelemetryEvent>();
                }
                var events=_eventsByQuery[query.Name];
                events.Add(telemetryEvent);
                var windowStart =DateTime.UtcNow.AddSeconds(-query.WindowSeconds);
                events.RemoveAll(item => item.Timestamp < windowStart);
                CreateMetrices(query, events);
            }
        }
        private void CreateMetrices(StandingQuery query,List<TelemetryEvent> events)
        {
            if (!string.Equals(query.Aggregration, "count", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Aggregation '{query.Aggregration}' is not supported yet.");
                return;
            }
            var groupedEvents = events.GroupBy(telemetryEvent => CreateDimensionKey(query, telemetryEvent));
            foreach(var group in groupedEvents)
            {
                var firstEvent = group.First();
                var metric = new Metric
                {
                    Name = query.Name,
                    Value = group.Count(),
                    TimeStamp = DateTime.UtcNow,
                    WindowSeconds = query.WindowSeconds,
                    Dimensions = CreateDimensions(query, firstEvent)
                };
                _metricStore.Save(metric);
                _alertEngine.Evaluate(query, metric);
                Console.WriteLine("====Metric Created====");
                Console.WriteLine($"Metric : {metric.Name}");
                Console.WriteLine($"Value : {metric.Value}");

                foreach(var dimension in metric.Dimensions)
                {
                    Console.WriteLine($"{dimension.Key} : {dimension.Value}");
                }
                Console.WriteLine("============================");
            }
        }

        private static Dictionary<string,string> CreateDimensions(StandingQuery query, TelemetryEvent telemetryEvent)
        {
            var dimensions= new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(var dimension in query.Dimensions)
            {
                dimensions[dimension] = GetDimensionValue(telemetryEvent, dimension);
            }
            return dimensions;
        }
        private static string GetDimensionValue(TelemetryEvent telemetryEvent, string dimension)
        {
            if (dimension.Equals("service", StringComparison.OrdinalIgnoreCase))
            {
                return telemetryEvent.Service;
            }
            if (dimension.Equals("region", StringComparison.OrdinalIgnoreCase))
            {
                return telemetryEvent.Region;
            }
            if(telemetryEvent.Properties.TryGetValue(dimension, out var propertyValue))
            {
                return propertyValue?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        public static string CreateDimensionKey(StandingQuery query,TelemetryEvent telemetryEvent)
        {
            var dimensionValues=query.Dimensions.Select(dimension=> GetDimensionValue(telemetryEvent, dimension));
            return string.Join("|", dimensionValues);
        }
    }
}
