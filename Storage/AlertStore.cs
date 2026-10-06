using SQE_Practice.Models;
using System.Globalization;

namespace SQE_Practice.Storage
{
    public class AlertStore
    {
        private readonly List<Alert> _alerts =new();
        private readonly object _lock = new();
        public void Add(Alert alert)
        {
            lock(_lock)
            {
                _alerts.Add(alert);
            }
        }
        public List<Alert> GetAll()
        {
            lock(_lock)
            {
                return _alerts.OrderByDescending(x => x.Timestamp).ToList();
            }
        }
    }
}
