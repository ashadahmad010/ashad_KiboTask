using Kibo.TestingFramework.Api;
using Kibo.TestingFramework.Builders;
using Kibo.TestingFramework.Resiliency;

namespace Kibo.LegacyTests;

/// <summary>
/// Refactored order tests using the Kibo.TestingFramework.
/// Demonstrates DRY principles, fluent builders, polling, and test observability.
/// </summary>
public class OrderTests : IDisposable
{
    private readonly KiboApiClient _client;

    public OrderTests()
    {
        _client = new KiboApiClient(new KiboApiClientOptions
        {
            BaseUrl = "http://localhost:5000",
            TenantId = "tenant-abc-123"
        });
    }

    [Fact]
    public async Task CreateOrder_ReturnsSuccess()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("john.doe@example.com")
            .WithItem("SKU-001", 2, 29.99m)
            .Build();

        var response = await _client.CreateOrderAsync(order);

        Assert.True(response.IsSuccess);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal("Pending", response.Data!.Status);
        Assert.Equal("john.doe@example.com", response.Data.CustomerEmail);
        Assert.NotEqual(Guid.Empty, response.Data.Id);
    }

    [Fact]
    public async Task CreateOrder_WithoutTenantHeader_Returns401()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("no-tenant@example.com")
            .WithItem("SKU-999", 1, 9.99m)
            .Build();

        var response = await _client.PostRawAsync(
            "/v1/orders",
            System.Text.Json.JsonSerializer.Serialize(order),
            includeTenantHeader: false);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_AfterCreation_StatusBecomesReadyForFulfillment()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("status-check@example.com")
            .WithItem("SKU-042", 1, 49.99m)
            .Build();

        var createResponse = await _client.CreateOrderAsync(order);
        Assert.True(createResponse.IsSuccess);
        var orderId = createResponse.Data!.Id;

        // Use polling instead of Thread.Sleep - much faster and more reliable
        var readyOrder = await Poller.WaitUntilAsync(
            action: async () =>
            {
                var resp = await _client.GetOrderAsync(orderId);
                return resp;
            },
            condition: resp => resp.Data?.Status == "ReadyForFulfillment",
            interval: TimeSpan.FromMilliseconds(500),
            timeout: TimeSpan.FromSeconds(15)
        );

        Assert.Equal("ReadyForFulfillment", readyOrder.Data!.Status);
        
        // Demonstrates timing capture capability
        Assert.True(readyOrder.ElapsedMs < 5000, $"Request took {readyOrder.ElapsedMs}ms, expected < 5000ms");
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_Returns404()
    {
        var response = await _client.GetOrderAsync(Guid.NewGuid());

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_RecordsTimingAndLogs()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("timing-test@example.com")
            .WithItem("SKU-TIMING", 1, 10.00m)
            .Build();

        var response = await _client.CreateOrderAsync(order);

        // Verify observability data is captured
        Assert.True(response.ElapsedMs > 0, "Timing should be captured");
        Assert.False(string.IsNullOrEmpty(response.RequestLog), "Request log should be captured");
        Assert.False(string.IsNullOrEmpty(response.ResponseLog), "Response log should be captured");
        Assert.Contains("POST", response.RequestLog);
        Assert.Contains("201", response.ResponseLog);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}