using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Controllers;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Storage;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Controllers;

public class ControllersTests : IDisposable
{
    private readonly string _tempDir;

    public ControllersTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SQETests_Controllers_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "Queries"));
        var json = JsonSerializer.Serialize(new { Queries = new List<StandingQuery>() });
        File.WriteAllText(Path.Combine(_tempDir, "Queries", "queries.json"), json);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private EventIngestionService CreateIngestionService(TestDbContextFactory factory)
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var queryLoader = new QueryLoader(envMock.Object);

        var eventStore = new EventStore(factory, Mock.Of<ILogger<EventStore>>());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        var alertEngine = new AlertEngine(alertStore);
        var aggEngine = new AggregrationEngine(metricStore, alertEngine);
        var filterEngine = new FilterEngine();
        var standingEngine = new StandingQueryEngine(queryLoader, aggEngine, filterEngine, Mock.Of<ILogger<StandingQueryEngine>>());

        return new EventIngestionService(eventStore, standingEngine, Mock.Of<ILogger<EventIngestionService>>());
    }

    [Theory]
    [InlineData(null, "OrderService", "EventName is required.")]
    [InlineData("", "OrderService", "EventName is required.")]
    [InlineData("   ", "OrderService", "EventName is required.")]
    [InlineData("OrderFailed", null, "Service is required.")]
    [InlineData("OrderFailed", "", "Service is required.")]
    [InlineData("OrderFailed", "   ", "Service is required.")]
    public async Task EventsController_Validation_ReturnsBadRequest(string? eventName, string? service, string expectedError)
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var ingestionService = CreateIngestionService(factory);
        var controller = new EventsController(ingestionService);

        var evt = new TelemetryEvent
        {
            EventName = eventName!,
            Service = service!
        };

        var actionResult = await controller.Ingest(evt, default);

        var badRequest = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);

        var messageProp = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value) as string;
        messageProp.Should().Be(expectedError);
    }

    [Fact]
    public async Task EventsController_ValidEvent_ReturnsAccepted()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var ingestionService = CreateIngestionService(factory);
        var controller = new EventsController(ingestionService);

        var evt = new TelemetryEvent
        {
            EventName = "PaymentAuthorized",
            Service = "CheckoutService",
            Region = "US-West"
        };

        var actionResult = await controller.Ingest(evt, default);

        var acceptedResult = actionResult.Should().BeOfType<AcceptedResult>().Subject;
        acceptedResult.StatusCode.Should().Be(202);
    }

    [Fact]
    public async Task EventsController_DuplicateEvent_ReturnsOk()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var ingestionService = CreateIngestionService(factory);
        var controller = new EventsController(ingestionService);

        var evt = new TelemetryEvent
        {
            EventId = Guid.NewGuid(),
            EventName = "PaymentAuthorized",
            Service = "CheckoutService"
        };

        // First attempt -> Accepted (202)
        var first = await controller.Ingest(evt, default);
        first.Should().BeOfType<AcceptedResult>();

        // Second attempt -> Ok (200)
        var second = await controller.Ingest(evt, default);
        var okResult = second.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AlertsController_GetAlerts_ReturnsOkWithDeserializedDimensions()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var alertStore = new AlertStore(factory, Mock.Of<ILogger<AlertStore>>());
        await alertStore.SaveAsync(new Alert
        {
            QueryName = "HighTraffic",
            Message = "Traffic spike",
            CurrentValue = 100,
            Threshold = 50,
            Dimensions = new Dictionary<string, string> { { "region", "APAC" } }
        });

        var controller = new AlertsController(alertStore);
        var result = await controller.GetAlerts(limit: 10);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var countProp = (int)okResult.Value!.GetType().GetProperty("count")!.GetValue(okResult.Value)!;
        countProp.Should().Be(1);
    }

    [Fact]
    public async Task MetricsController_GetMetrics_ReturnsOkWithDeserializedDimensions()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var metricStore = new MetricStore(factory, Mock.Of<ILogger<MetricStore>>());
        await metricStore.SaveAsync(new Metric
        {
            Name = "RequestRate",
            Value = 42,
            WindowSeconds = 60,
            Dimensions = new Dictionary<string, string> { { "service", "API Gateway" } }
        });

        var controller = new MetricsController(metricStore);
        var result = await controller.GetMetrics(limit: 10);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var countProp = (int)okResult.Value!.GetType().GetProperty("count")!.GetValue(okResult.Value)!;
        countProp.Should().Be(1);
    }

    [Fact]
    public async Task RequestLogsController_Endpoints_ReturnCorrectData()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var store = new RequestLogStore(factory, Mock.Of<ILogger<RequestLogStore>>());

        await store.SaveAsync(new RequestLog { Method = "GET", Path = "/api/test", StatusCode = 200, Severity = "Info", DurationMs = 20 });
        await store.SaveAsync(new RequestLog { Method = "GET", Path = "/api/warn", StatusCode = 404, Severity = "Warning", DurationMs = 150 });
        await store.SaveAsync(new RequestLog { Method = "POST", Path = "/api/err", StatusCode = 500, Severity = "Error", DurationMs = 50 });
        await store.SaveAsync(new RequestLog { Method = "POST", Path = "/api/crit", StatusCode = 500, Severity = "Critical", DurationMs = 1500 });

        var controller = new RequestLogsController(store);

        // GetLogs
        var logsResult = await controller.GetLogs(limit: 10);
        logsResult.Should().BeOfType<OkObjectResult>();

        // GetSummary
        var summaryResult = await controller.GetSummary();
        summaryResult.Should().BeOfType<OkObjectResult>();

        // GetErrors (should contain Error and Critical)
        var errorsResult = await controller.GetErrors(limit: 10);
        var okErrors = errorsResult.Should().BeOfType<OkObjectResult>().Subject;
        var errCount = (int)okErrors.Value!.GetType().GetProperty("count")!.GetValue(okErrors.Value)!;
        errCount.Should().Be(2);

        // GetWarnings
        var warningsResult = await controller.GetWarnings(limit: 10);
        var okWarnings = warningsResult.Should().BeOfType<OkObjectResult>().Subject;
        var warnCount = (int)okWarnings.Value!.GetType().GetProperty("count")!.GetValue(okWarnings.Value)!;
        warnCount.Should().Be(1);

        // GetSlow
        var slowResult = await controller.GetSlow(limit: 10);
        var okSlow = slowResult.Should().BeOfType<OkObjectResult>().Subject;
        var slowCount = (int)okSlow.Value!.GetType().GetProperty("count")!.GetValue(okSlow.Value)!;
        slowCount.Should().Be(2); // Warning (150ms) and Critical (1500ms)
    }
}

