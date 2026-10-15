using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class StandingQueryEngineTests : IDisposable
{
    private readonly string _tempDir;

    public StandingQueryEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SQETests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "Queries"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private void SetupQueries(List<StandingQuery> queries)
    {
        var json = JsonSerializer.Serialize(new { Queries = queries });
        File.WriteAllText(Path.Combine(_tempDir, "Queries", "queries.json"), json);
    }

    [Fact]
    public async Task ProcessAsync_EventNameMismatch_DoesNotInvokeAggregationEngine()
    {
        SetupQueries(new List<StandingQuery>
        {
            new() { Name = "PaymentFailureQuery", EventName = "PaymentFailed", Aggregration = "count" }
        });

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var queryLoader = new QueryLoader(envMock.Object);

        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        var alertEngine = new AlertEngine(alertStore);
        var aggregationEngine = new AggregrationEngine(metricStore, alertEngine);
        var filterEngine = new FilterEngine();
        var logger = Mock.Of<ILogger<StandingQueryEngine>>();

        var engine = new StandingQueryEngine(queryLoader, aggregationEngine, filterEngine, logger);

        // Sending an event with a different name
        var evt = new TelemetryEvent { EventName = "OrderCreated", Service = "OrderService" };
        await engine.ProcessAsync(evt);

        var metrics = await metricStore.GetAllAsync();
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_EventNameMatches_FiltersMatch_InvokesAggregationEngine()
    {
        SetupQueries(new List<StandingQuery>
        {
            new()
            {
                Name = "OrderFailureQuery",
                EventName = "OrderFailed",
                Aggregration = "count",
                WindowSeconds = 60,
                Dimensions = new List<string> { "service" },
                Filters = new List<QueryFilter>
                {
                    new() { Property = "errorCode", Operator = "eq", Value = "500" }
                }
            }
        });

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var queryLoader = new QueryLoader(envMock.Object);

        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        var alertEngine = new AlertEngine(alertStore);
        var aggregationEngine = new AggregrationEngine(metricStore, alertEngine);
        var filterEngine = new FilterEngine();
        var logger = Mock.Of<ILogger<StandingQueryEngine>>();

        var engine = new StandingQueryEngine(queryLoader, aggregationEngine, filterEngine, logger);

        var matchingEvt = new TelemetryEvent
        {
            EventName = "orderfailed ", // Notice casing and trailing whitespace - should still match
            Service = "Checkout",
            Properties = new Dictionary<string, object> { { "errorCode", "500" } }
        };

        await engine.ProcessAsync(matchingEvt);

        var metrics = await metricStore.GetAllAsync();
        metrics.Should().HaveCount(1);
        metrics[0].Name.Should().Be("OrderFailureQuery");
        metrics[0].Value.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_EventNameMatches_FilterMismatches_SkipsQuery()
    {
        SetupQueries(new List<StandingQuery>
        {
            new()
            {
                Name = "OrderFailureQuery",
                EventName = "OrderFailed",
                Aggregration = "count",
                Filters = new List<QueryFilter>
                {
                    new() { Property = "errorCode", Operator = "eq", Value = "500" }
                }
            }
        });

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var queryLoader = new QueryLoader(envMock.Object);

        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        var alertEngine = new AlertEngine(alertStore);
        var aggregationEngine = new AggregrationEngine(metricStore, alertEngine);
        var filterEngine = new FilterEngine();
        var logger = Mock.Of<ILogger<StandingQueryEngine>>();

        var engine = new StandingQueryEngine(queryLoader, aggregationEngine, filterEngine, logger);

        var nonMatchingFilterEvt = new TelemetryEvent
        {
            EventName = "OrderFailed",
            Service = "Checkout",
            Properties = new Dictionary<string, object> { { "errorCode", "404" } }
        };

        await engine.ProcessAsync(nonMatchingFilterEvt);

        var metrics = await metricStore.GetAllAsync();
        metrics.Should().BeEmpty();
    }
}

