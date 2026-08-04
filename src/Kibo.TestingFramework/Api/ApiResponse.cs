namespace Kibo.TestingFramework.Api;

/// <summary>
/// Represents a response from an API call with observability data.
/// </summary>
public class ApiResponse<T>
{
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }
    public string RawContent { get; init; } = string.Empty;
    public long ElapsedMs { get; init; }
    public string RequestLog { get; init; } = string.Empty;
    public string ResponseLog { get; init; } = string.Empty;
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
}

/// <summary>
/// Non-generic version for responses without a body.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
}