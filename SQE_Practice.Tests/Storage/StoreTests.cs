using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Storage;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Storage;

public class StoreTests
{
    private readonly TestDbContextFactory _factory = new(Guid.NewGuid().ToString());

    [Fact]
    public async Task EventStore_SaveAsync_SavesAndDetectsDuplicates()
    {
        var store = new EventStore(_factory, Mock.Of<ILogger<EventStore>>());
        var id = Guid.NewGuid();
        var evt = new TelemetryEvent
        {
            EventId = id,
            EventName = "PaymentProcessed",
            Service = "Billing",
            Region = "US-East",
            DurationMs = 150.0,
            Properties = new Dictionary<string, object> { { "accountType", "retail" } }
        };

        var firstSave = await store.SaveAsync(evt);
        firstSave.Should().BeTrue();

        var secondSave = await store.SaveAsync(evt);
        secondSave.Should().BeFalse();

        var recent = await store.GetRecentAsync(10);
        recent.Should().HaveCount(1);
        recent[0].EventId.Should().Be(id);
        recent[0].EventName.Should().Be("PaymentProcessed");
    }

    [Fact]
    public async Task EventStore_GetRecentAsync_ClampsLimit()
    {
        var store = new EventStore(_factory, Mock.Of<ILogger<EventStore>>());
        for (int i = 0; i < 5; i++)
        {
            await store.SaveAsync(new TelemetryEvent
            {
                EventId = Guid.NewGuid(),
                EventName = $"Event_{i}",
                Service = "TestService",
                Region = "EU"
            });
        }

        // Limit < 1 should be clamped to 1
        var clampedMin = await store.GetRecentAsync(0);
        clampedMin.Should().HaveCount(1);

        // Limit = 3
        var clampedThree = await store.GetRecentAsync(3);
        clampedThree.Should().HaveCount(3);
    }

    [Fact]
    public async Task MetricStore_SaveAndGetAllAsync_PersistsCorrectly()
    {
        var store = new MetricStore(_factory, Mock.Of<ILogger<MetricStore>>());
        var metric = new Metric
        {
            Name = "CpuUsage",
            Value = 88.5,
            WindowSeconds = 60,
            Dimensions = new Dictionary<string, string> { { "host", "srv-1" } }
        };

        await store.SaveAsync(metric);

        var all = await store.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("CpuUsage");
        all[0].Value.Should().Be(88.5);
        all[0].DimensionsJson.Should().Contain("srv-1");
    }

    [Fact]
    public async Task AlertStore_SaveAndGetAllAsync_PersistsCorrectly()
    {
        var store = new AlertStore(_factory, Mock.Of<ILogger<AlertStore>>());
        var alert = new Alert
        {
            QueryName = "HighMemoryAlert",
            Message = "Memory exceeded threshold",
            CurrentValue = 95.0,
            Threshold = 90.0,
            Dimensions = new Dictionary<string, string> { { "node", "k8s-worker-1" } }
        };

        await store.SaveAsync(alert);

        var all = await store.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].QueryName.Should().Be("HighMemoryAlert");
        all[0].Threshold.Should().Be(90.0);
        all[0].CurrentValue.Should().Be(95.0);
        all[0].DimensionsJson.Should().Contain("k8s-worker-1");
    }

    [Fact]
    public async Task RequestLogStore_SaveAndGetRecentAsync_FiltersAppliedCorrectly()
    {
        var store = new RequestLogStore(_factory, Mock.Of<ILogger<RequestLogStore>>());

        var log1 = new RequestLog
        {
            Method = "GET",
            Path = "/api/orders",
            StatusCode = 200,
            Severity = "Info",
            DurationMs = 25.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-3)
        };

        var log2 = new RequestLog
        {
            Method = "POST",
            Path = "/api/orders",
            StatusCode = 500,
            Severity = "Error",
            DurationMs = 500.0,
            ErrorMessage = "Database timeout",
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        };

        var log3 = new RequestLog
        {
            Method = "GET",
            Path = "/api/users",
            StatusCode = 404,
            Severity = "Warning",
            DurationMs = 15.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        };

        await store.SaveAsync(log1);
        await store.SaveAsync(log2);
        await store.SaveAsync(log3);

        // Filter by Severity
        var errors = await store.GetRecentAsync(10, severityFilter: "Error");
        errors.Should().HaveCount(1);
        errors[0].ErrorMessage.Should().Be("Database timeout");

        // Filter by Path
        var orderLogs = await store.GetRecentAsync(10, pathFilter: "/api/orders");
        orderLogs.Should().HaveCount(2);

        // Filter by Method
        var getLogs = await store.GetRecentAsync(10, methodFilter: "GET");
        getLogs.Should().HaveCount(2);

        // Filter by Method & Severity
        var infoGets = await store.GetRecentAsync(10, severityFilter: "Info", methodFilter: "GET");
        infoGets.Should().HaveCount(1);
    }

    [Fact]
    public async Task RequestLogStore_GetSummaryAsync_AggregatesAccurately()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var store = new RequestLogStore(factory, Mock.Of<ILogger<RequestLogStore>>());

        await store.SaveAsync(new RequestLog { Method = "GET", Path = "/1", StatusCode = 200, Severity = "Info", DurationMs = 100.0 });
        await store.SaveAsync(new RequestLog { Method = "GET", Path = "/2", StatusCode = 400, Severity = "Warning", DurationMs = 200.0 });
        await store.SaveAsync(new RequestLog { Method = "POST", Path = "/3", StatusCode = 500, Severity = "Error", DurationMs = 300.0 });
        await store.SaveAsync(new RequestLog { Method = "POST", Path = "/4", StatusCode = 500, Severity = "Critical", DurationMs = 1200.0 });

        var summary = await store.GetSummaryAsync();

        summary.TotalRequests.Should().Be(4);
        summary.InfoCount.Should().Be(1);
        summary.WarningCount.Should().Be(1);
        summary.ErrorCount.Should().Be(1);
        summary.CriticalCount.Should().Be(1);
        summary.AvgDurationMs.Should().Be(450.0); // (100 + 200 + 300 + 1200) / 4 = 450
        summary.MaxDurationMs.Should().Be(1200.0);
    }
}

