namespace SQE_Practice.Storage.Entities;

/// <summary>
/// EF Core entity that maps to the RequestLogs SQL Server table.
/// </summary>
public sealed class RequestLogEntity
{
    public long   Id          { get; set; }

    public Guid   RequestId   { get; set; }

    public string Method      { get; set; } = string.Empty;

    public string Path        { get; set; } = string.Empty;

    public string QueryString { get; set; } = string.Empty;

    public int    StatusCode  { get; set; }

    /// <summary>Info | Warning | Error | Critical</summary>
    public string Severity    { get; set; } = "Info";

    public double DurationMs  { get; set; }

    public string ClientIp    { get; set; } = string.Empty;

    public string UserAgent   { get; set; } = string.Empty;

    public string RequestBody  { get; set; } = string.Empty;

    public string ResponseBody { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string ErrorType    { get; set; } = string.Empty;

    public DateTime Timestamp  { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

