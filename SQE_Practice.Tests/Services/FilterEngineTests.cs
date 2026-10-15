using System.Text.Json;
using FluentAssertions;
using SQE_Practice.Models;
using SQE_Practice.Services;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class FilterEngineTests
{
    private readonly FilterEngine _engine = new();

    [Fact]
    public void IsMatch_NullOrEmptyFilters_ReturnsTrue()
    {
        var evt = new TelemetryEvent
        {
            EventName = "TestEvent",
            Properties = new Dictionary<string, object> { { "key", "value" } }
        };

        _engine.IsMatch(evt, null).Should().BeTrue();
        _engine.IsMatch(evt, new List<QueryFilter>()).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_NullProperties_ReturnsFalseWhenFiltersPresent()
    {
        var evt = new TelemetryEvent
        {
            EventName = "TestEvent",
            Properties = null!
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "code", Operator = "eq", Value = "500" }
        };

        _engine.IsMatch(evt, filters).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_MissingProperty_ReturnsFalse()
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object>
            {
                { "otherKey", "val" }
            }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "errorCode", Operator = "eq", Value = "500" }
        };

        _engine.IsMatch(evt, filters).Should().BeFalse();
    }

    [Theory]
    [InlineData("500", "500", true)]
    [InlineData("500", "400", false)]
    [InlineData("error", "ERROR", true)] // case-insensitive check
    public void IsMatch_OperatorEq_ValidatesCorrectly(string actual, string expected, bool shouldMatch)
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object> { { "errorCode", actual } }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "errorCode", Operator = "eq", Value = expected }
        };

        _engine.IsMatch(evt, filters).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("500", "400", true)]
    [InlineData("500", "500", false)]
    [InlineData("warning", "WARNING", false)]
    public void IsMatch_OperatorNeq_ValidatesCorrectly(string actual, string expected, bool shouldMatch)
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object> { { "status", actual } }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "status", Operator = "neq", Value = expected }
        };

        _engine.IsMatch(evt, filters).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("DatabaseTimeoutException", "timeout", true)]
    [InlineData("DatabaseTimeoutException", "TIMEOUT", true)]
    [InlineData("DatabaseTimeoutException", "connection", false)]
    public void IsMatch_OperatorContains_ValidatesCorrectly(string actual, string expected, bool shouldMatch)
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object> { { "reason", actual } }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "reason", Operator = "contains", Value = expected }
        };

        _engine.IsMatch(evt, filters).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("SQE.Order.Failed", "sqe.order", true)]
    [InlineData("SQE.Order.Failed", "order", false)]
    public void IsMatch_OperatorStartsWith_ValidatesCorrectly(string actual, string expected, bool shouldMatch)
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object> { { "action", actual } }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "action", Operator = "startswith", Value = expected }
        };

        _engine.IsMatch(evt, filters).Should().Be(shouldMatch);
    }

    [Fact]
    public void IsMatch_UnsupportedOperator_ReturnsFalse()
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object> { { "key", "value" } }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "key", Operator = "regex_match", Value = "val.*" }
        };

        _engine.IsMatch(evt, filters).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_JsonElementPropertyValues_HandledCorrectly()
    {
        using var jsonDoc = JsonDocument.Parse(@"{
            ""numProp"": 500,
            ""boolProp"": true,
            ""falseProp"": false,
            ""strProp"": ""active""
        }");

        var root = jsonDoc.RootElement;
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object>
            {
                { "numProp", root.GetProperty("numProp") },
                { "boolProp", root.GetProperty("boolProp") },
                { "falseProp", root.GetProperty("falseProp") },
                { "strProp", root.GetProperty("strProp") }
            }
        };

        _engine.IsMatch(evt, new List<QueryFilter> { new() { Property = "numProp", Operator = "eq", Value = "500" } }).Should().BeTrue();
        _engine.IsMatch(evt, new List<QueryFilter> { new() { Property = "boolProp", Operator = "eq", Value = "true" } }).Should().BeTrue();
        _engine.IsMatch(evt, new List<QueryFilter> { new() { Property = "falseProp", Operator = "eq", Value = "false" } }).Should().BeTrue();
        _engine.IsMatch(evt, new List<QueryFilter> { new() { Property = "strProp", Operator = "eq", Value = "active" } }).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_MultipleFilters_AllMustMatch()
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object>
            {
                { "serviceTier", "Premium" },
                { "statusCode", 500 },
                { "retryCount", 3 }
            }
        };

        var allMatchingFilters = new List<QueryFilter>
        {
            new() { Property = "serviceTier", Operator = "eq", Value = "premium" },
            new() { Property = "statusCode", Operator = "eq", Value = "500" },
            new() { Property = "retryCount", Operator = "eq", Value = "3" }
        };

        var oneFailingFilter = new List<QueryFilter>
        {
            new() { Property = "serviceTier", Operator = "eq", Value = "premium" },
            new() { Property = "statusCode", Operator = "eq", Value = "200" } // Fails
        };

        _engine.IsMatch(evt, allMatchingFilters).Should().BeTrue();
        _engine.IsMatch(evt, oneFailingFilter).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_NullFilterValue_DefaultsToEmptyString()
    {
        var evt = new TelemetryEvent
        {
            Properties = new Dictionary<string, object>
            {
                { "emptyProp", "" }
            }
        };

        var filters = new List<QueryFilter>
        {
            new() { Property = "emptyProp", Operator = "eq", Value = null! }
        };

        _engine.IsMatch(evt, filters).Should().BeTrue();
    }
}

