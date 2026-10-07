namespace SQE_Practice.Models;

/// <summary>
/// Domain model for a single HTTP request log entry captured by
/// the RequestTelemetryMiddleware.
/// </summary>
public sealed class RequestLog
{
    public Guid   RequestId   { get; set; } = Guid.NewGuid();

    /// <summary>HTTP method: GET, POST, PUT, DELETE, PATCH …</summary>
    public string Method      { get; set; } = string.Empty;

    /// <summary>Request path (without query string).</summary>
    public string Path        { get; set; } = string.Empty;

    /// <summary>Raw query string (e.g. ?limit=10).</summary>
    public string QueryString { get; set; } = string.Empty;

    /// <summary>HTTP response status code.</summary>
    public int    StatusCode  { get; set; }

    /// <summary>Classified severity: Info | Warning | Error | Critical.</summary>
    public string Severity    { get; set; } = "Info";

    /// <summary>Total round-trip time in milliseconds.</summary>
    public double DurationMs  { get; set; }

    /// <summary>Remote client IP address.</summary>
    public string ClientIp    { get; set; } = string.Empty;

    /// <summary>User-Agent header value.</summary>
    public string UserAgent   { get; set; } = string.Empty;

    /// <summary>Captured request body (empty if CaptureBody=false).</summary>
    public string RequestBody  { get; set; } = string.Empty;

    /// <summary>Captured response body (empty if CaptureBody=false).</summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>Exception message (empty if no exception).</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Exception type full name (empty if no exception).</summary>
    public string ErrorType    { get; set; } = string.Empty;

    public DateTime Timestamp  { get; set; } = DateTime.UtcNow;
}

