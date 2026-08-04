# Kibo SDET Challenge — Testing Framework

## Architecture

```
Kibo.SDET.Challenge/
├── src/
│   ├── Kibo.MockApi/                    # Mock Fulfillment API (DO NOT MODIFY)
│   │   ├── Controllers/OrdersController.cs
│   │   ├── Models/Order.cs
│   │   └── Storage/OrderStore.cs
│   └── Kibo.TestingFramework/           # Reusable Testing SDK
│       ├── Api/
│       │   ├── KiboApiClient.cs         # HTTP client with observability
│       │   └── Models.cs                # Request/response DTOs
│       ├── Builders/
│       │   └── OrderBuilder.cs          # Fluent test data builder
│       └── Resiliency/
│           └── Poller.cs                # Wait-until polling utility
└── tests/
    └── Kibo.LegacyTests/               # Refactored test suite
        ├── OrderTests.cs                # Core order lifecycle tests
        └── EdgeCaseTests.cs             # Destructive/edge case tests
```

## Key Components

### KiboApiClient

Reusable HTTP client that manages lifecycle, base URL, headers, JSON serialization, and observability.

```csharp
var client = new KiboApiClient(new KiboApiClientOptions
{
    BaseUrl = "http://localhost:5000",
    TenantId = "tenant-abc-123",
    EnableRequestLogging = true  // for CI/CD debugging
});

var response = await client.CreateOrderAsync(order);
Console.WriteLine($"Status: {response.StatusCode}, Took: {response.ElapsedMs}ms");
```

### OrderBuilder

Fluent builder for constructing test data with sensible defaults.

```csharp
var order = new OrderBuilder()
    .WithCustomerEmail("test@kibo.com")
    .WithItems(2)                          // 2 random line items
    .ForTenant("tenant-xyz")
    .Build();
```

### Poller

Generic polling utility that replaces `Thread.Sleep`.

```csharp
var readyOrder = await Poller.WaitUntilAsync(
    action: () => client.GetOrderAsync(orderId),
    condition: order => order.Status == "ReadyForFulfillment",
    interval: TimeSpan.FromMilliseconds(500),
    timeout: TimeSpan.FromSeconds(15)
);
```

## Running

```bash
# Build
dotnet build

# Start Mock API (separate terminal)
dotnet run --project src/Kibo.MockApi

# Run tests
dotnet test
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KIBO_BASE_URL` | `http://localhost:5000` | API base URL |
| `KIBO_TENANT_ID` | `tenant-abc-123` | Default tenant ID |

## Test Results

| Test | Result | Notes |
|------|--------|-------|
| `CreateOrder_ReturnsSuccess` | PASS | Basic order creation |
| `CreateOrder_WithoutTenantHeader_Returns401` | PASS | Auth validation |
| `GetOrder_AfterCreation_StatusBecomesReadyForFulfillment` | PASS | Uses Poller (~5s) |
| `GetOrder_WithInvalidId_Returns404` | PASS | Not found handling |
| `CreateOrder_RecordsTimingAndLogs` | PASS | Observability validation |
| `CreateOrder_WithEmptyLineItems_ReturnsCreated` | PASS | Bug: accepts empty items |
| `CreateOrder_WithNegativePrice_ReturnsCreated` | PASS | Bug: accepts negative price |
| `CreateOrder_WithSqlInjectionInTenant_ReturnsCreated` | PASS | Safe but not validated |
| `CreateOrder_WithExtremelyLongEmail_ReturnsCreated` | PASS | Bug: no length validation |
| `CreateOrder_WithMissingEmail_ReturnsCreated` | PASS | Bug: no required field validation |

## API Bugs Found

| Bug | Severity | Description |
|-----|----------|-------------|
| Empty lineItems accepted | Medium | Orders with no items waste storage |
| Negative pricing accepted | High | Could corrupt financial records |
| No email length validation | Low | Extremely long emails accepted |
| No required field validation | Medium | Missing email accepted |
