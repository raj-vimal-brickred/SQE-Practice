using SQE_Practice.Models;

namespace SQE_Practice.Services
{
    public class EventIngestionService
    {
        private readonly StandingQueryEngine _queryEngine;
        public EventIngestionService(StandingQueryEngine queryEngine)
        {
            _queryEngine = queryEngine;
        }
        public async Task Ingest(TelemetryEvent telemetryEvent)
        {
            Console.WriteLine();
            Console.WriteLine("Telemetry Recieved");
            Console.WriteLine($"Event : {telemetryEvent.EventName}");
            Console.WriteLine($"Service : {telemetryEvent.Service}");
            Console.WriteLine($"Region : {telemetryEvent.Region}");
            Console.WriteLine($"Duration : {telemetryEvent.DurationMs}");
            Console.WriteLine($"Time Stamp : {telemetryEvent.Timestamp}");
            Console.WriteLine();
            await _queryEngine.ProcessAsync(telemetryEvent);
        }
    }
}
