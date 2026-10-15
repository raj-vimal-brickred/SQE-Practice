using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SQE_Practice.Models;
using SQE_Practice.Services;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class AlertEngineTests
{
    private AlertEngine CreateAlertEngine(out AlertStore alertStore)
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var logger = Mock.Of<ILogger<AlertStore>>();
        alertStore = new AlertStore(factory, logger);
        return new AlertEngine(alertStore);
    }

    [Fact]
    public async Task EvaluateAsync_NullAlertRule_DoesNotTriggerAlert()
    {
        var engine = CreateAlertEngine(out var alertStore);
        var query = new StandingQuery
        {
            Name = "QueryNoAlert",
            Alert = null
        };
        var metric = new Metric { Name = "QueryNoAlert", Value = 100 };

        await engine.EvaluateAsync(query, metric);

        var alerts = await alertStore.GetAllAsync();
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_DisabledAlertRule_DoesNotTriggerAlert()
    {
        var engine = CreateAlertEngine(out var alertStore);
        var query = new StandingQuery
        {
            Name = "QueryDisabledAlert",
            Alert = new AlertRule
            {
                Enabled = false,
                Operator = "gte",
                Threshold = 10
            }
        };
        var metric = new Metric { Name = "QueryDisabledAlert", Value = 50 };

        await engine.EvaluateAsync(query, metric);

        var alerts = await alertStore.GetAllAsync();
        alerts.Should().BeEmpty();
    }

    [Theory]
    [InlineData("gte", 10, 10, true)]
    [InlineData("gte", 9.9, 10, false)]
    [InlineData("gt", 10.1, 10, true)]
    [InlineData("gt", 10, 10, false)]
    [InlineData("lte", 10, 10, true)]
    [InlineData("lte", 10.1, 10, false)]
    [InlineData("lt", 9.9, 10, true)]
    [InlineData("lt", 10, 10, false)]
    [InlineData("eq", 10.0, 10.0, true)]
    [InlineData("eq", 10.1, 10.0, false)]
    [InlineData("ne", 10.1, 10.0, true)]
    [InlineData("ne", 10.0, 10.0, false)]
    public async Task EvaluateAsync_Operators_TriggerAppropriately(
        string op, double value, double threshold, bool shouldTrigger)
    {
        var engine = CreateAlertEngine(out var alertStore);
        var query = new StandingQuery
        {
            Name = $"Query_{op}_{value}_{threshold}",
            Alert = new AlertRule
            {
                Enabled = true,
                Operator = op,
                Threshold = threshold,
                CooldownSeconds = 0
            }
        };

        var metric = new Metric
        {
            Name = query.Name,
            Value = value,
            Dimensions = new Dictionary<string, string> { { "env", "prod" } }
        };

        await engine.EvaluateAsync(query, metric);

        var alerts = await alertStore.GetAllAsync();
        if (shouldTrigger)
        {
            alerts.Should().HaveCount(1);
            alerts[0].CurrentValue.Should().Be(value);
            alerts[0].Threshold.Should().Be(threshold);
            alerts[0].Message.Should().Contain(query.Name);
        }
        else
        {
            alerts.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task EvaluateAsync_UnsupportedOperator_ThrowsNotSupportedException()
    {
        var engine = CreateAlertEngine(out _);
        var query = new StandingQuery
        {
            Name = "QueryInvalidOp",
            Alert = new AlertRule
            {
                Enabled = true,
                Operator = "unknown_operator",
                Threshold = 10
            }
        };
        var metric = new Metric { Name = query.Name, Value = 15 };

        Func<Task> act = async () => await engine.EvaluateAsync(query, metric);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Fact]
    public async Task EvaluateAsync_CooldownSuppressesSubsequentAlertsForSameDimension()
    {
        var engine = CreateAlertEngine(out var alertStore);
        var query = new StandingQuery
        {
            Name = "OrderErrors",
            Alert = new AlertRule
            {
                Enabled = true,
                Operator = "gte",
                Threshold = 5,
                CooldownSeconds = 60 // 60 seconds cooldown
            }
        };

        var metric = new Metric
        {
            Name = "OrderErrors",
            Value = 10,
            Dimensions = new Dictionary<string, string>
            {
                { "service", "Checkout" },
                { "region", "East" }
            }
        };

        // First trigger: should save alert
        await engine.EvaluateAsync(query, metric);
        var alertsFirst = await alertStore.GetAllAsync();
        alertsFirst.Should().HaveCount(1);

        // Immediate second trigger for same query & dimension: should be suppressed
        metric.Value = 15;
        await engine.EvaluateAsync(query, metric);
        var alertsSecond = await alertStore.GetAllAsync();
        alertsSecond.Should().HaveCount(1, "cooldown should suppress consecutive alert for same dimensions");

        // Third trigger with DIFFERENT dimension: should NOT be suppressed
        var metricDifferentRegion = new Metric
        {
            Name = "OrderErrors",
            Value = 20,
            Dimensions = new Dictionary<string, string>
            {
                { "service", "Checkout" },
                { "region", "West" }
            }
        };

        await engine.EvaluateAsync(query, metricDifferentRegion);
        var alertsThird = await alertStore.GetAllAsync();
        alertsThird.Should().HaveCount(2, "different dimension should have its own cooldown tracker");
    }
}

