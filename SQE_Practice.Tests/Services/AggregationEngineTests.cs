using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class AggregationEngineTests
{
    private (AggregrationEngine engine, MetricStore metricStore, AlertStore alertStore) CreateEngine()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var metricLogger = Mock.Of<ILogger<MetricStore>>();
        var alertLogger = Mock.Of<ILogger<AlertStore>>();

        var metricStore = new MetricStore(factory, metricLogger);
        var alertStore = new AlertStore(factory, alertLogger);
        var alertEngine = new AlertEngine(alertStore);
        var engine = new AggregrationEngine(metricStore, alertEngine);

        return (engine, metricStore, alertStore);
    }

    [Fact]
    public async Task ProcessAsync_AggregationCount_ComputesCorrectCount()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "QueryCountTest",
            EventName = "HttpRequest",
            Aggregration = "count",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service" }
        };

        var event1 = new TelemetryEvent
        {
            EventName = "HttpRequest",
            Service = "BillingService",
            Timestamp = DateTime.UtcNow
        };
        var event2 = new TelemetryEvent
        {
            EventName = "HttpRequest",
            Service = "BillingService",
            Timestamp = DateTime.UtcNow
        };

        await engine.ProcessAsync(query, event1);
        await engine.ProcessAsync(query, event2);

        var metrics = await metricStore.GetAllAsync();
        metrics.Should().NotBeEmpty();
        // The latest metric value should be 2
        metrics[0].Value.Should().Be(2);
        metrics[0].Name.Should().Be("QueryCountTest");
    }

    [Fact]
    public async Task ProcessAsync_AggregationSum_ComputesDurationSum()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "QuerySumTest",
            EventName = "DbQuery",
            Aggregration = "sum",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service" }
        };

        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Sql", DurationMs = 120.5, Timestamp = DateTime.UtcNow });
        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Sql", DurationMs = 80.5, Timestamp = DateTime.UtcNow });

        var metrics = await metricStore.GetAllAsync();
        metrics[0].Value.Should().Be(201.0);
    }

    [Fact]
    public async Task ProcessAsync_AggregationAverage_ComputesDurationAverage()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "QueryAvgTest",
            EventName = "DbQuery",
            Aggregration = "average",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service" }
        };

        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Sql", DurationMs = 100, Timestamp = DateTime.UtcNow });
        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Sql", DurationMs = 200, Timestamp = DateTime.UtcNow });

        var metrics = await metricStore.GetAllAsync();
        metrics[0].Value.Should().Be(150.0);
    }

    [Fact]
    public async Task ProcessAsync_AggregationMinAndMax_ComputesCorrectExtremes()
    {
        var (engineMin, metricStoreMin, _) = CreateEngine();
        var (engineMax, metricStoreMax, _) = CreateEngine();

        var queryMin = new StandingQuery { Name = "MinQ", Aggregration = "min", WindowSeconds = 60, Dimensions = new List<string> { "service" } };
        var queryMax = new StandingQuery { Name = "MaxQ", Aggregration = "max", WindowSeconds = 60, Dimensions = new List<string> { "service" } };

        var ev1 = new TelemetryEvent { Service = "App", DurationMs = 50, Timestamp = DateTime.UtcNow };
        var ev2 = new TelemetryEvent { Service = "App", DurationMs = 150, Timestamp = DateTime.UtcNow };

        await engineMin.ProcessAsync(queryMin, ev1);
        await engineMin.ProcessAsync(queryMin, ev2);

        await engineMax.ProcessAsync(queryMax, ev1);
        await engineMax.ProcessAsync(queryMax, ev2);

        var minMetrics = await metricStoreMin.GetAllAsync();
        var maxMetrics = await metricStoreMax.GetAllAsync();

        minMetrics[0].Value.Should().Be(50);
        maxMetrics[0].Value.Should().Be(150);
    }

    [Fact]
    public async Task ProcessAsync_NullDurations_ReturnsZeroForSumAndAverage()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "QueryNullDuration",
            Aggregration = "average",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service" }
        };

        await engine.ProcessAsync(query, new TelemetryEvent { Service = "App", DurationMs = null, Timestamp = DateTime.UtcNow });

        var metrics = await metricStore.GetAllAsync();
        metrics[0].Value.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_UnsupportedAggregation_ThrowsNotSupportedException()
    {
        var (engine, _, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "QueryInvalidAgg",
            Aggregration = "percentile99",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service" }
        };

        Func<Task> act = async () => await engine.ProcessAsync(query, new TelemetryEvent { Service = "App", DurationMs = 10 });

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*percentile99*");
    }

    [Fact]
    public async Task ProcessAsync_DimensionGrouping_CreatesDistinctMetricsPerDimension()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "DimensionGroupTest",
            Aggregration = "count",
            WindowSeconds = 60,
            Dimensions = new List<string> { "service", "region" }
        };

        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Order", Region = "East", Timestamp = DateTime.UtcNow });
        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Order", Region = "East", Timestamp = DateTime.UtcNow });
        await engine.ProcessAsync(query, new TelemetryEvent { Service = "Order", Region = "West", Timestamp = DateTime.UtcNow });

        var metrics = await metricStore.GetAllAsync();
        // There should be metrics saved; the last batch contains both East (count = 2) and West (count = 1)
        metrics.Should().Contain(m => m.DimensionsJson.Contains("East"));
        metrics.Should().Contain(m => m.DimensionsJson.Contains("West"));
    }

    [Fact]
    public async Task ProcessAsync_ExpiredWindowEvents_ArePruned()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "WindowTest",
            Aggregration = "count",
            WindowSeconds = 10,
            Dimensions = new List<string> { "service" }
        };

        // Event from 30 seconds ago (expired)
        var oldEvent = new TelemetryEvent
        {
            Service = "Auth",
            Timestamp = DateTime.UtcNow.AddSeconds(-30)
        };
        // Current event
        var currentEvent = new TelemetryEvent
        {
            Service = "Auth",
            Timestamp = DateTime.UtcNow
        };

        await engine.ProcessAsync(query, oldEvent);
        await engine.ProcessAsync(query, currentEvent);

        var metrics = await metricStore.GetAllAsync();
        // Since oldEvent was outside the 10s window when currentEvent was added,
        // it was pruned, leaving count = 1.
        metrics[0].Value.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_CustomPropertyDimension_ExtractsCorrectlyOrDefaultsToUnknown()
    {
        var (engine, metricStore, _) = CreateEngine();

        var query = new StandingQuery
        {
            Name = "CustomDimTest",
            Aggregration = "count",
            WindowSeconds = 60,
            Dimensions = new List<string> { "customTag" }
        };

        var evtWithTag = new TelemetryEvent
        {
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object> { { "customTag", "BetaUser" } }
        };
        var evtWithoutTag = new TelemetryEvent
        {
            Timestamp = DateTime.UtcNow
        };

        await engine.ProcessAsync(query, evtWithTag);
        await engine.ProcessAsync(query, evtWithoutTag);

        var metrics = await metricStore.GetAllAsync();
        metrics.Should().Contain(m => m.DimensionsJson.Contains("BetaUser"));
        metrics.Should().Contain(m => m.DimensionsJson.Contains("Unknown"));
    }
}

