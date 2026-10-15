using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Storage;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class EventIngestionServiceTests : IDisposable
{
    private readonly string _tempDir;

    public EventIngestionServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SQETests_Ingest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "Queries"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private (EventIngestionService service, EventStore eventStore, MetricStore metricStore) CreateServices()
    {
        // Write a test query
        var json = JsonSerializer.Serialize(new
        {
            Queries = new List<StandingQuery>
            {
                new()
                {
                    Name = "TestIngestQuery",
                    EventName = "AppCrash",
                    Aggregration = "count",
                    Dimensions = new List<string> { "service" }
                }
            }
        });
        File.WriteAllText(Path.Combine(_tempDir, "Queries", "queries.json"), json);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var queryLoader = new QueryLoader(envMock.Object);

        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var eventStore = new EventStore(factory, Mock.Of<ILogger<EventStore>>());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        var alertEngine = new AlertEngine(alertStore);
        var aggregationEngine = new AggregrationEngine(metricStore, alertEngine);
        var filterEngine = new FilterEngine();
        var standingEngine = new StandingQueryEngine(queryLoader, aggregationEngine, filterEngine, Mock.Of<ILogger<StandingQueryEngine>>());

        var ingestService = new EventIngestionService(eventStore, standingEngine, Mock.Of<ILogger<EventIngestionService>>());

        return (ingestService, eventStore, metricStore);
    }

    [Fact]
    public async Task IngestAsync_EmptyGuidAndDefaultTimestamp_PopulatesDefaults()
    {
        var (service, eventStore, _) = CreateServices();
        var evt = new TelemetryEvent
        {
            EventId = Guid.Empty,
            Timestamp = default,
            EventName = "AppCrash",
            Service = "MobileApi"
        };

        var result = await service.IngestAsync(evt);

        result.Accepted.Should().BeTrue();
        result.Duplicate.Should().BeFalse();
        result.EventId.Should().NotBeEmpty();
        evt.EventId.Should().NotBeEmpty();
        evt.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var stored = await eventStore.GetRecentAsync(10);
        stored.Should().ContainSingle(x => x.EventId == result.EventId);
    }

    [Fact]
    public async Task IngestAsync_DuplicateEvent_ReturnsDuplicateTrueAndSkipsStandingQueries()
    {
        var (service, eventStore, metricStore) = CreateServices();
        var eventId = Guid.NewGuid();
        var evt = new TelemetryEvent
        {
            EventId = eventId,
            EventName = "AppCrash",
            Service = "PaymentService",
            Timestamp = DateTime.UtcNow
        };

        // First ingestion: success, not duplicate
        var result1 = await service.IngestAsync(evt);
        result1.Accepted.Should().BeTrue();
        result1.Duplicate.Should().BeFalse();
        result1.EventId.Should().Be(eventId);

        var metricsAfterFirst = await metricStore.GetAllAsync();
        metricsAfterFirst.Should().HaveCount(1);
        metricsAfterFirst[0].Value.Should().Be(1);

        // Second ingestion with the same EventId: duplicate detected
        var result2 = await service.IngestAsync(evt);
        result2.Accepted.Should().BeTrue();
        result2.Duplicate.Should().BeTrue();
        result2.EventId.Should().Be(eventId);

        // Metric store should NOT have received a second event evaluation
        var metricsAfterSecond = await metricStore.GetAllAsync();
        metricsAfterSecond.Should().HaveCount(1);
        metricsAfterSecond[0].Value.Should().Be(1);
    }
}

