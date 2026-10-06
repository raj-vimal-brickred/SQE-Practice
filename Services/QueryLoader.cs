using SQE_Practice.Models;
using System.Text.Json;

namespace SQE_Practice.Services
{
    public class QueryLoader
    {
        private readonly IWebHostEnvironment _environment;
        public QueryLoader(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public async Task<List<StandingQuery>> LoadAsync()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Queries", "queries.json");
            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Query Configuration Not Found : {filePath}");
            }
            var json=await File.ReadAllTextAsync(filePath);
            var configuration = JsonSerializer.Deserialize<QueryConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return configuration?.Queries ?? new List<StandingQuery>();
        }

        private class QueryConfiguration
        {
            public List<StandingQuery> Queries { get; set; } = new();
        }

    }
}
