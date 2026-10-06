using SQE_Practice.Models;
using SQE_Practice.Storage;

namespace SQE_Practice.Services
{
    public class AlertEngine
    {
        private readonly AlertStore _alertStore;
        public AlertEngine(AlertStore alertStore)
        {
            _alertStore = alertStore;
        }
        public void Evaluate(StandingQuery query,Metric metric)
        {
            if (query.Alert == null)
            {
                return;
            }
            var triggered = query.Alert.Operator.ToLower() switch
            {
                "gte" => metric.Value >= query.Alert.Threshold,
                "gt" => metric.Value >= query.Alert.Threshold,
                "lte" => metric.Value >= query.Alert.Threshold,
                "lt" => metric.Value >= query.Alert.Threshold,
                "eq" => metric.Value >= query.Alert.Threshold,
                _ =>false
            };
            if (!triggered)
            {
                return;
            }
            var alert = new Alert
            {
                QueryName = query.Name,
                Message = $"{query.Name} crossed threshold",
                CurrentValue = metric.Value,
                Timestamp = DateTime.UtcNow,
                Threshold = query.Alert.Threshold,
                Dimensions = new Dictionary<string, string>(metric.Dimensions)
            };
            _alertStore.Add(alert);

            Console.WriteLine("-------------------------------------");
            Console.WriteLine($"Query {alert.QueryName}");
            Console.WriteLine($"Value {alert.CurrentValue}");
            Console.WriteLine($"Threshold {alert.Threshold}");
            foreach(var dimension in alert.Dimensions)
            {
                Console.WriteLine($"{dimension.Key} : {dimension.Value}");
            };
        }
    }
}
