namespace SQE_Practice.Middleware;

public sealed class RequestMiddlewareOptions
{
    public const string SectionName = "RequestMiddleware";
    public double SlowRequestThresholdMs { get; set; } = 1000;
    public string[] IgnoredPaths { get; set; } =
    [
        "/swagger",
        "/health",
        "/favicon.ico"
    ];
    public bool CaptureBody { get; set; } = false;

    public int MaxBodyBytes { get; set; } = 4096;
}

