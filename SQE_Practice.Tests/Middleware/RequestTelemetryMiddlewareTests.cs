using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SQE_Practice.Middleware;
using SQE_Practice.Storage;
using SQE_Practice.Tests.Helpers;
using Xunit;

namespace SQE_Practice.Tests.Middleware;

public class RequestTelemetryMiddlewareTests
{
    private (RequestTelemetryMiddleware middleware, RequestLogStore store) CreateMiddleware(
        RequestDelegate next,
        RequestMiddlewareOptions? options = null)
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        var store = new RequestLogStore(factory, Mock.Of<ILogger<RequestLogStore>>());
        var logger = Mock.Of<ILogger<RequestTelemetryMiddleware>>();
        var opt = Options.Create(options ?? new RequestMiddlewareOptions
        {
            SlowRequestThresholdMs = 1000,
            CaptureBody = false,
            IgnoredPaths = ["/swagger", "/health"]
        });

        var middleware = new RequestTelemetryMiddleware(next, logger, opt);
        return (middleware, store);
    }

    [Fact]
    public async Task InvokeAsync_IgnoredPath_DoesNotLogToStore()
    {
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var (middleware, store) = CreateMiddleware(next);

        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/v1/swagger.json";

        await middleware.InvokeAsync(context, store);

        nextCalled.Should().BeTrue();
        var logs = await store.GetRecentAsync();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_NormalFastRequest_ClassifiedAsInfo()
    {
        RequestDelegate next = (ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var (middleware, store) = CreateMiddleware(next);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/items";

        await middleware.InvokeAsync(context, store);

        var logs = await store.GetRecentAsync();
        logs.Should().HaveCount(1);
        logs[0].StatusCode.Should().Be(200);
        logs[0].Severity.Should().Be("Info");
        logs[0].Path.Should().Be("/api/items");
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Warning")]
    [InlineData(StatusCodes.Status404NotFound, "Warning")]
    [InlineData(StatusCodes.Status302Found, "Warning")]
    [InlineData(StatusCodes.Status500InternalServerError, "Error")]
    [InlineData(StatusCodes.Status503ServiceUnavailable, "Error")]
    public async Task InvokeAsync_StatusCodes_ClassifiedCorrectly(int statusCode, string expectedSeverity)
    {
        RequestDelegate next = (ctx) =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };

        var (middleware, store) = CreateMiddleware(next);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/actions";

        await middleware.InvokeAsync(context, store);

        var logs = await store.GetRecentAsync();
        logs.Should().HaveCount(1);
        logs[0].StatusCode.Should().Be(statusCode);
        logs[0].Severity.Should().Be(expectedSeverity);
    }

    [Fact]
    public async Task InvokeAsync_SlowRequest_ClassifiedAsWarningOrCritical()
    {
        // Set low threshold so 100ms is slow
        var options = new RequestMiddlewareOptions
        {
            SlowRequestThresholdMs = 20,
            CaptureBody = false,
            IgnoredPaths = []
        };

        RequestDelegate slow200 = async (ctx) =>
        {
            await Task.Delay(50);
            ctx.Response.StatusCode = StatusCodes.Status200OK;
        };

        var (middleware200, store200) = CreateMiddleware(slow200, options);
        var ctx200 = new DefaultHttpContext();
        ctx200.Request.Path = "/slow-ok";

        await middleware200.InvokeAsync(ctx200, store200);

        var logs200 = await store200.GetRecentAsync();
        logs200[0].Severity.Should().Be("Warning");

        // Slow 500 error -> should be classified as Critical
        RequestDelegate slow500 = async (ctx) =>
        {
            await Task.Delay(50);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        };

        var (middleware500, store500) = CreateMiddleware(slow500, options);
        var ctx500 = new DefaultHttpContext();
        ctx500.Request.Path = "/slow-fail";

        await middleware500.InvokeAsync(ctx500, store500);

        var logs500 = await store500.GetRecentAsync();
        logs500[0].Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task InvokeAsync_PipelineException_Sets500AndRethrows()
    {
        RequestDelegate failingNext = (_) => throw new InvalidOperationException("Fatal database failure");

        var (middleware, store) = CreateMiddleware(failingNext);

        var context = new DefaultHttpContext();
        context.Request.Path = "/crash";

        Func<Task> act = async () => await middleware.InvokeAsync(context, store);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fatal database failure");

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var logs = await store.GetRecentAsync();
        logs.Should().HaveCount(1);
        logs[0].ErrorMessage.Should().Be("Fatal database failure");
        logs[0].ErrorType.Should().Be(typeof(InvalidOperationException).FullName);
        logs[0].StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_CaptureBodyEnabled_StoresRequestAndResponseBody()
    {
        var options = new RequestMiddlewareOptions
        {
            CaptureBody = true,
            MaxBodyBytes = 1024,
            IgnoredPaths = []
        };

        RequestDelegate next = async (ctx) =>
        {
            var responseBytes = Encoding.UTF8.GetBytes("{\"result\":\"success\"}");
            await ctx.Response.Body.WriteAsync(responseBytes);
            ctx.Response.StatusCode = StatusCodes.Status200OK;
        };

        var (middleware, store) = CreateMiddleware(next, options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/payload";
        var requestPayload = "{\"query\":\"items\"}";
        var reqBytes = Encoding.UTF8.GetBytes(requestPayload);
        context.Request.Body = new MemoryStream(reqBytes);
        context.Request.ContentLength = reqBytes.Length;

        await middleware.InvokeAsync(context, store);

        var logs = await store.GetRecentAsync();
        logs.Should().HaveCount(1);
        logs[0].RequestBody.Should().Be(requestPayload);
        logs[0].ResponseBody.Should().Be("{\"result\":\"success\"}");
    }
}

