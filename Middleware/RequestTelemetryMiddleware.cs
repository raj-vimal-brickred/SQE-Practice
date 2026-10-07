using Microsoft.Extensions.Options;
using SQE_Practice.Observability;
using SQE_Practice.Services;
using SQE_Practice.Storage;
using System.Diagnostics;
using System.Text;

namespace SQE_Practice.Middleware;
public sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;
    private readonly RequestMiddlewareOptions _options;

    public RequestTelemetryMiddleware(
        RequestDelegate next,
        ILogger<RequestTelemetryMiddleware> logger,
        IOptions<RequestMiddlewareOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        RequestLogStore requestLogStore)           
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldIgnore(path))
        {
            await _next(context);
            return;
        }

        string requestBody = string.Empty;

        if (_options.CaptureBody &&
            context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            requestBody = await ReadBodyAsync(
                context.Request.Body,
                _options.MaxBodyBytes);
            context.Request.Body.Position = 0;
        }

        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        using var activity = SqeTelemetry.ActivitySource.StartActivity(
            "SQE.HttpRequest",
            ActivityKind.Server);

        var requestId = Guid.NewGuid();

        activity?.SetTag("http.method",         context.Request.Method);
        activity?.SetTag("http.path",           path);
        activity?.SetTag("http.query",          context.Request.QueryString.Value);
        activity?.SetTag("sqe.request.id",      requestId);
        activity?.SetTag("network.peer.address",
            context.Connection.RemoteIpAddress?.ToString());

        var stopwatch = Stopwatch.StartNew();
        string? responseBody = null;
        Exception? caughtException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
        finally
        {
            stopwatch.Stop();

            if (_options.CaptureBody)
            {
                responseBuffer.Position = 0;
                responseBody = await ReadBodyAsync(
                    responseBuffer,
                    _options.MaxBodyBytes);
            }

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            var statusCode  = context.Response.StatusCode;
            var durationMs  = stopwatch.Elapsed.TotalMilliseconds;
            var isSlow      = durationMs > _options.SlowRequestThresholdMs;
            var severity    = ClassifySeverity(statusCode, isSlow, caughtException);

            activity?.SetTag("http.status_code",    statusCode);
            activity?.SetTag("http.duration_ms",    durationMs);
            activity?.SetTag("sqe.severity",        severity.ToString());
            activity?.SetTag("sqe.slow_request",    isSlow);

            if (caughtException is not null)
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error,
                    caughtException.Message);

                activity?.SetTag("error.type",
                    caughtException.GetType().FullName);
            }
            else if (severity >= RequestSeverity.Error)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {statusCode}");
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            RecordMetrics(
                context.Request.Method,
                path,
                statusCode,
                severity,
                durationMs);

            EmitLog(
                severity,
                context.Request.Method,
                path,
                statusCode,
                durationMs,
                requestId,
                caughtException);

            await PersistLogAsync(
                requestLogStore,
                requestId,
                context,
                statusCode,
                severity,
                durationMs,
                requestBody,
                responseBody,
                caughtException);
        }

        if (caughtException is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo
                .Capture(caughtException)
                .Throw();
    }
    private bool ShouldIgnore(string path)
    {
        foreach (var ignored in _options.IgnoredPaths)
        {
            if (path.StartsWith(ignored,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static RequestSeverity ClassifySeverity(
        int statusCode,
        bool isSlow,
        Exception? exception)
    {
        if (exception is not null || statusCode >= 500)
            return isSlow ? RequestSeverity.Critical : RequestSeverity.Error;

        if (statusCode >= 400 || statusCode >= 300)
            return RequestSeverity.Warning;

        if (isSlow)
            return RequestSeverity.Warning;

        return RequestSeverity.Info;
    }

    private static void RecordMetrics(
        string method,
        string path,
        int statusCode,
        RequestSeverity severity,
        double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("http.method",    method),
            new("http.route",     path),
            new("http.status",    statusCode),
            new("sqe.severity",   severity.ToString()),
        };

        switch (severity)
        {
            case RequestSeverity.Info:
                SqeTelemetry.HttpRequestsInfo.Add(1, tags);
                break;

            case RequestSeverity.Warning:
                SqeTelemetry.HttpRequestsWarning.Add(1, tags);
                break;

            case RequestSeverity.Error:
                SqeTelemetry.HttpRequestsError.Add(1, tags);
                break;

            case RequestSeverity.Critical:
                SqeTelemetry.HttpRequestsCritical.Add(1, tags);
                break;
        }

        SqeTelemetry.HttpRequestsTotal.Add(1, tags);
        SqeTelemetry.HttpRequestDuration.Record(durationMs, tags);
    }

    private void EmitLog(
        RequestSeverity severity,
        string method,
        string path,
        int statusCode,
        double durationMs,
        Guid requestId,
        Exception? exception)
    {
        var message =
            "[{Severity}] {Method} {Path} → {StatusCode} in {DurationMs:F1} ms  (RequestId={RequestId})";

        switch (severity)
        {
            case RequestSeverity.Info:
                _logger.LogInformation(message,
                    severity, method, path, statusCode, durationMs, requestId);
                break;

            case RequestSeverity.Warning:
                _logger.LogWarning(message,
                    severity, method, path, statusCode, durationMs, requestId);
                break;

            case RequestSeverity.Error:
                _logger.LogError(exception, message,
                    severity, method, path, statusCode, durationMs, requestId);
                break;

            case RequestSeverity.Critical:
                _logger.LogCritical(exception, message,
                    severity, method, path, statusCode, durationMs, requestId);
                break;
        }
    }

    private static async Task PersistLogAsync(
        RequestLogStore store,
        Guid requestId,
        HttpContext context,
        int statusCode,
        RequestSeverity severity,
        double durationMs,
        string requestBody,
        string? responseBody,
        Exception? exception)
    {
        try
        {
            var log = new Models.RequestLog
            {
                RequestId  = requestId,
                Method     = context.Request.Method,
                Path       = context.Request.Path.Value ?? string.Empty,
                QueryString= context.Request.QueryString.Value ?? string.Empty,
                StatusCode = statusCode,
                Severity   = severity.ToString(),
                DurationMs = durationMs,
                ClientIp   = context.Connection.RemoteIpAddress?.ToString()
                                 ?? "unknown",
                UserAgent  = context.Request.Headers.UserAgent.ToString(),
                RequestBody  = requestBody,
                ResponseBody = responseBody ?? string.Empty,
                ErrorMessage = exception?.Message ?? string.Empty,
                ErrorType    = exception?.GetType().FullName ?? string.Empty,
                Timestamp    = DateTime.UtcNow
            };

            await store.SaveAsync(log);
        }
        catch
        {
        }
    }

    private static async Task<string> ReadBodyAsync(
        Stream body,
        int maxBytes)
    {
        var buffer = new byte[maxBytes];
        var read   = await body.ReadAsync(buffer, 0, maxBytes);

        return Encoding.UTF8.GetString(buffer, 0, read)
                        .TrimEnd('\0');
    }
}

