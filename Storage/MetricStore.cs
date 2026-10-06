using SQE_Practice.Models;

namespace SQE_Practice.Storage
{
    public class MetricStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, Metric> _metric = new();
        public void Save(Metric metric)
        {
            var key = CreateKey(metric);
            lock( _lock )
            {
                _metric[key]=metric;
            }
        }
        public List<Metric> GetAll()
        {
            lock (_lock)
            {
                return _metric.Values.OrderBy(metric => metric.Name).ThenBy(metric => metric.TimeStamp).ToList();
            }
        }

        private static string CreateKey(Metric metric)
        {
            var dimensions = string.Join("|", metric.Dimensions.OrderBy(item => item.Key).Select(item => $"{item.Key}: {item.Value}"));
            return $"{metric.Name}|{dimensions}";
        }
    }
}
