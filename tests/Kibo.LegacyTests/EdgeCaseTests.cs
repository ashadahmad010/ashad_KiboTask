using Kibo.TestingFramework.Api;
using Kibo.TestingFramework.Builders;

namespace Kibo.LegacyTests;

/// <summary>
/// Edge case and destructive tests for the Kibo Mock API.
/// 
/// These tests probe the API's input validation, error handling, and security behavior.
/// Each test documents the expected behavior, actual behavior, and classifies the result
/// as a bug report or a positive finding.
/// 
/// FINDINGS SUMMARY:
/// ┌────────────────────────────────────────────┬──────────────────────────────────────────┐
/// │ Scenario                                   │ Result                                   │
/// ├────────────────────────────────────────────┼──────────────────────────────────────────┤
/// │ Empty lineItems                            │ BUG: 201 Created (should reject)         │
/// │ Negative pricing                           │ BUG: 201 Created (should reject)         │
/// │ SQL injection in tenant header             │ Safe: 201 (any string accepted)          │
/// │ Extremely long email (1000 chars)          │ BUG: 201 Created (should reject)         │
/// │ Missing customerEmail                      │ BUG: 201 Created (should reject)         │
/// └────────────────────────────────────────────┴──────────────────────────────────────────┘
/// </summary>
public class EdgeCaseTests : IDisposable
{
    private readonly KiboApiClient _client;
    private const string ValidTenant = "tenant-abc-123";

    public EdgeCaseTests()
    {
        _client = new KiboApiClient(new KiboApiClientOptions
        {
            BaseUrl = "http://localhost:5000",
            TenantId = ValidTenant
        });
    }

    /// <summary>
    /// BUG REPORT #1: Empty lineItems array
    /// 
    /// EXPECTED: 400 Bad Request — an order without line items has no fulfillment purpose.
    /// ACTUAL:   201 Created — the API accepts and stores the order.
    /// IMPACT:   Orphaned orders with no items waste storage and confuse downstream systems.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithEmptyLineItems_ReturnsCreated()
    {
        var json = """
        {
            "customerEmail": "empty-items@example.com",
            "lineItems": []
        }
        """;

        var response = await _client.PostRawAsync(
            "/v1/orders",
            json,
            new Dictionary<string, string> { ["x-kibo-tenant"] = ValidTenant });

        // API accepts empty lineItems — this is a bug (should return 400)
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// BUG REPORT #2: Negative unit pricing
    /// 
    /// EXPECTED: 400 Bad Request — negative prices allow credit manipulation.
    /// ACTUAL:   201 Created — the API stores the negative price as-is.
    /// IMPACT:   Financial records will be corrupted; potential for negative order totals.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithNegativePrice_ReturnsCreated()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("negative-price@example.com")
            .WithItem("SKU-NEG", 1, -50.00m)
            .Build();

        var response = await _client.CreateOrderAsync(order);

        // API accepts negative pricing — this is a bug (should return 400)
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(-50.00m, response.Data!.LineItems.First().UnitPrice);
    }

    /// <summary>
    /// POSITIVE FINDING: SQL injection in tenant header
    /// 
    /// EXPECTED: 401 Unauthorized — injection attempt should not succeed.
    /// ACTUAL:   201 Created — but the injection payload is stored as a literal string,
    ///           not executed as SQL. The tenant header is validated only for presence,
    ///           not content, so this is safe by design (parameterized storage).
    /// IMPACT:   No security risk — the payload is inert. However, tenant validation
    ///           could be stricter (e.g., reject special characters).
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithSqlInjectionInTenant_ReturnsCreated()
    {
        var order = new OrderBuilder()
            .WithCustomerEmail("sql-test@example.com")
            .WithItem("SKU-SQL", 1, 10.00m)
            .Build();

        var response = await _client.PostRawAsync(
            "/v1/orders",
            System.Text.Json.JsonSerializer.Serialize(order),
            new Dictionary<string, string>
            {
                ["x-kibo-tenant"] = "'; DROP TABLE Orders; --"
            });

        // API accepts any non-empty string as tenant — safe but not validated
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// BUG REPORT #3: Extremely long customerEmail
    /// 
    /// EXPECTED: 400 Bad Request — emails have practical length limits (~254 chars RFC 5321).
    /// ACTUAL:   201 Created — the API accepts a 1000+ character email.
    /// IMPACT:   Storage bloat, potential UI rendering issues, downstream validation failures.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithExtremelyLongEmail_ReturnsCreated()
    {
        var longEmail = new string('a', 1000) + "@example.com";

        var order = new OrderBuilder()
            .WithCustomerEmail(longEmail)
            .WithItem("SKU-LONG", 1, 5.00m)
            .Build();

        var response = await _client.CreateOrderAsync(order);

        // API accepts extremely long email — this is a bug (should return 400)
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(longEmail, response.Data!.CustomerEmail);
    }

    /// <summary>
    /// BUG REPORT #4: Missing required customerEmail field
    /// 
    /// EXPECTED: 400 Bad Request — customerEmail is required for order fulfillment.
    /// ACTUAL:   201 Created — the API accepts null/missing email.
    /// IMPACT:   Orders without contact info cannot be fulfilled or communicated about.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithMissingEmail_ReturnsCreated()
    {
        // Explicitly send JSON without customerEmail
        var json = """
        {
            "lineItems": [
                {
                    "productCode": "SKU-MISS",
                    "quantity": 1,
                    "unitPrice": 15.00
                }
            ]
        }
        """;

        var response = await _client.PostRawAsync(
            "/v1/orders",
            json,
            new Dictionary<string, string> { ["x-kibo-tenant"] = ValidTenant });

        // API accepts missing email — this is a bug (should return 400)
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}