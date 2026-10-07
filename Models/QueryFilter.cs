namespace SQE_Practice.Models
{
    public class QueryFilter
    {
        public string Property { get; set; } = string.Empty;
        
        public string Operator { get; set; } = "eq";
        
        public string Value { get; set; } = string.Empty;
    }
}
