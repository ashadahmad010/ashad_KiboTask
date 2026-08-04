using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Kibo.TestingFramework.Api;

/// <summary>
/// Configuration options for the Kibo API client.
/// Supports environment-based and constructor-based configuration.
/// </summary>
public class KiboApiClientOptions
{
    /// <summary>Base URL for the Kibo API. Override via KIBO_BASE_URL env var.</summary>
    public string BaseUrl { get; set; } =
        Environment.GetEnvironmentVariable("KIBO_BASE_URL") ?? "http://localhost:5000";

    /// <summary>Default tenant ID for requests. Override via KIBO_TENANT_ID env var.</summary>
    public string TenantId { get; set; } =
        Environment.GetEnvironmentVariable("KIBO_TENANT_ID") ?? "tenant-abc-123";

    /// <summary>HTTP request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When true, full request/response details are logged to Console.
    /// Useful for debugging in CI/CD pipelines.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;
}

/// <summary>
/// Reusable API client for the Kibo Mock Fulfillment API.
/// 
/// Responsibilities:
///   - HttpClient lifecycle management (create, configure, dispose)
///   - Base URL and default header injection (x-kibo-tenant)
///   - JSON serialization/deserialization with consistent settings
///   - Request/response observability (timing, logging, correlation IDs)
/// 
/// Usage:
///   var client = new KiboApiClient(new KiboApiClientOptions { TenantId = "my-tenant" });
///   var response = await client.CreateOrderAsync(order);
///   Console.WriteLine($"Took {response.ElapsedMs}ms");
/// </summary>
public sealed class KiboApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly KiboApiClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public KiboApiClient(KiboApiClientOptions? options = null)
    {
        _options = options ?? new KiboApiClientOptions();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = _options.DefaultTimeout
        };

        _jsonOptions = DefaultJsonOptions;
    }

    /// <summary>
    /// Creates a new order with full observability (timing, logging, correlation).
    /// </summary>
    /// <param name="request">The order to create.</param>
    /// <param name="tenantId">Optional tenant override. Uses default if null.</param>
    /// <returns>ApiResponse with order data, timing, and diagnostic logs.</returns>
    public async Task<ApiResponse<OrderResponse>> CreateOrderAsync(
        CreateOrderRequest request, string? tenantId = null)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        return await SendTrackedAsync<OrderResponse>(
            HttpMethod.Post,
            "/v1/orders",
            json,
            tenant: tenantId);
    }

    /// <summary>
    /// Gets an order by ID with full observability.
    /// </summary>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="tenantId">Optional tenant override. Uses default if null.</param>
    /// <returns>ApiResponse with order data, timing, and diagnostic logs.</returns>
    public async Task<ApiResponse<OrderResponse>> GetOrderAsync(
        Guid orderId, string? tenantId = null)
    {
        return await SendTrackedAsync<OrderResponse>(
            HttpMethod.Get,
            $"/v1/orders/{orderId}",
            body: null,
            tenant: tenantId);
    }

    /// <summary>
    /// Sends a raw POST request with custom headers.
    /// Useful for edge case testing where you need full control over the request.
    /// </summary>
    /// <param name="path">The API endpoint path.</param>
    /// <param name="json">The JSON request body.</param>
    /// <param name="headers">Optional custom headers to add.</param>
    /// <param name="includeTenantHeader">When false, skips the x-kibo-tenant header (for auth testing).</param>
    public async Task<ApiResponse<object>> PostRawAsync(
        string path, string json, Dictionary<string, string>? headers = null,
        bool includeTenantHeader = true)
    {
        return await SendTrackedAsync<object>(
            HttpMethod.Post,
            path,
            json,
            tenant: includeTenantHeader ? null : "__SKIP_TENANT__",
            extraHeaders: headers);
    }

    /// <summary>
    /// Enables or disables request/response logging at runtime.
    /// </summary>
    public void SetLoggingEnabled(bool enabled)
    {
        _options.EnableRequestLogging = enabled;
    }

    #region Core HTTP with Observability

    /// <summary>
    /// Core HTTP method that adds correlation IDs, timing, and logging to every request.
    /// All public API methods delegate here to avoid code duplication.
    /// </summary>
    private async Task<ApiResponse<T>> SendTrackedAsync<T>(
        HttpMethod method,
        string path,
        string? body = null,
        string? tenant = null,
        Dictionary<string, string>? extraHeaders = null)
    {
        var requestMessage = new HttpRequestMessage(method, path);

        // Set body if present (POST/PUT/PATCH)
        if (body != null)
        {
            requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        // Add tenant header (required for auth)
        // Use special sentinel value "__SKIP_TENANT__" to test without tenant header
        if (tenant != "__SKIP_TENANT__")
        {
            var tenantId = tenant ?? _options.TenantId;
            requestMessage.Headers.Add("x-kibo-tenant", tenantId);
        }

        // Add correlation ID for distributed tracing
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        requestMessage.Headers.Add("X-Correlation-Id", correlationId);

        // Add any extra headers (for edge case testing)
        if (extraHeaders != null)
        {
            foreach (var header in extraHeaders)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Capture request details for observability
        var requestLog = FormatRequestLog(requestMessage, body);

        // Execute with timing
        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.SendAsync(requestMessage);
        stopwatch.Stop();

        // Capture response details for observability
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseLog = FormatResponseLog(response, responseBody);

        // Log if enabled (useful for CI/CD debugging)
        if (_options.EnableRequestLogging)
        {
            Console.WriteLine($"[{correlationId}] {requestLog}");
            Console.WriteLine($"[{correlationId}] {responseLog}");
            Console.WriteLine($"[{correlationId}] Elapsed: {stopwatch.ElapsedMilliseconds}ms");
        }

        // Deserialize response if successful
        T? data = default;
        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(responseBody))
        {
            data = JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
        }

        return new ApiResponse<T>
        {
            StatusCode = response.StatusCode,
            Data = data,
            RawContent = responseBody,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            RequestLog = requestLog,
            ResponseLog = responseLog
        };
    }

    #endregion

    #region Log Formatting

    private static string FormatRequestLog(HttpRequestMessage request, string? body = null)
    {
        var sb = new StringBuilder();
        sb.Append($"{request.Method} {request.RequestUri}");

        if (body != null)
        {
            sb.AppendLine();
            sb.Append(body);
        }

        return sb.ToString();
    }

    private static string FormatResponseLog(HttpResponseMessage response, string body)
    {
        return $"{(int)response.StatusCode} {response.StatusCode}\n{body}";
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}