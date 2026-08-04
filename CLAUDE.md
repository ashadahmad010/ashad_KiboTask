# CLAUDE.md

## Project Overview

Kibo SDET Technical Assessment — a mock fulfillment API with a testing framework SDK. The goal is to demonstrate transitioning from bloated manual tests to a scalable testing platform.

## Commands

```bash
dotnet build                    # Build all projects
dotnet test                     # Run all tests (requires MockApi running)
dotnet test tests/Kibo.LegacyTests  # Run specific test project
dotnet run --project src/Kibo.MockApi  # Start Mock API (port 5000)
```

## Architecture

```
src/
├── Kibo.MockApi/              # ASP.NET Core API (DO NOT MODIFY)
│   ├── Controllers/OrdersController.cs
│   ├── Models/Order.cs
│   └── Storage/OrderStore.cs
└── Kibo.TestingFramework/     # Reusable testing SDK
    ├── Api/
    │   ├── KiboApiClient.cs   # HTTP client with observability
    │   ├── ApiResponse.cs     # Response wrapper with timing/logs
    │   └── Models.cs          # DTOs
    ├── Builders/
    │   └── OrderBuilder.cs    # Fluent test data builder
    └── Resiliency/
        └── Poller.cs          # Wait-until polling utility
tests/
└── Kibo.LegacyTests/
    ├── OrderTests.cs          # Core order lifecycle tests
    └── EdgeCaseTests.cs       # Edge case / bug discovery tests
```

## Key Design Decisions

- **KiboApiClient** wraps HttpClient with timing, correlation IDs, and toggleable logging
- **OrderBuilder** uses fluent API with sensible defaults — zero config produces valid orders
- **Poller.WaitUntilAsync** replaces Thread.Sleep with configurable interval/timeout
- **ApiResponse<T>** always captures timing (`ElapsedMs`), request/response logs
- Base URL and Tenant ID configurable via constructor or env vars (`KIBO_BASE_URL`, `KIBO_TENANT_ID`)

## API Behavior

- `POST /v1/orders` — requires `x-kibo-tenant` header, returns 201
- `GET /v1/orders/{id}` — returns 200 or 404
- Order status transitions from `Pending` → `ReadyForFulfillment` after 5 seconds
- API has minimal input validation (bugs documented in EdgeCaseTests.cs)

## Test Conventions

- Test naming: `Method_Condition_ExpectedResult`
- Tests use `IDisposable` for client cleanup
- Edge cases document expected vs actual behavior as bug reports
- Use `PostRawAsync(..., includeTenantHeader: false)` to test auth failures

## Common Pitfalls

- MockApi must be running on port 5000 before `dotnet test`
- The 5-second status transition makes the polling test take ~5s
- `includeTenantHeader: false` is required for 401 auth tests (client defaults add tenant)

## Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `KIBO_BASE_URL` | `http://localhost:5000` | API base URL |
| `KIBO_TENANT_ID` | `tenant-abc-123` | Default tenant header |
