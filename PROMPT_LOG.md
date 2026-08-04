# PROMPT_LOG.md — AI Usage Documentation

## Prompt 1 — Task 1 (Platform Shift)
**Tool:** Opencode (Claude)
**Prompt:** "Analyze the legacy test class OrderTests.cs and identify all anti-patterns related to HttpClient usage, URL hardcoding, header duplication, JSON construction, and test isolation."
**Outcome:** Identified 6 anti-patterns including HttpClient-per-test, hardcoded URLs, duplicated tenant headers, inline JSON strings, Thread.Sleep, and no shared setup. Used this as a checklist while designing the KiboApiClient class.

## Prompt 2 — Task 2 (Fluent Builder)
**Tool:** Opencode (Claude)
**Prompt:** "Design a fluent builder pattern for constructing CreateOrderRequest objects with sensible defaults, random data generation, and chainable methods for an API testing framework."
**Outcome:** Generated the OrderBuilder class with WithCustomerEmail(), WithRandomCustomerEmail(), WithItem(), WithRandomItem(), WithItems(count), and ForTenant() methods. Modified the random data generation to use more realistic SKU prefixes and domain names. Kept the Build() and BuildWithTenant() methods for flexibility.

## Prompt 3 — Task 3 (Polling Utility)
**Tool:** Opencode (Claude)
**Prompt:** "Create a generic polling/wait-until utility that replaces Thread.Sleep, with configurable interval and timeout, clear timeout exceptions with last observed state, and async support."
**Outcome:** Created the Poller class with WaitUntilAsync<T>() and WaitUntilAsync() methods. The implementation uses Stopwatch for accurate timing and includes a custom PollTimeoutException with LastResult property. Used in the status transition test to replace the brittle Thread.Sleep(6000).

## Prompt 4 — Task 4 (Edge Case Analysis)
**Tool:** Opencode (Claude)
**Prompt:** "Analyze the Kibo Mock API endpoints (POST /v1/orders, GET /v1/orders/{id}) and generate 5 destructive or edge-case test scenarios that could reveal validation gaps or security issues."
**Outcome:** Generated 5 edge cases: (1) empty lineItems, (2) negative pricing, (3) SQL injection in tenant header, (4) extremely long email, (5) missing required fields. Implemented all using the framework. Found that 3 of 5 cases revealed API bugs (no validation for empty items, negative prices, or long emails). Documented bugs as code comments.

## Prompt 5 — Task 6 (Observability)
**Tool:** Opencode (Claude)
**Prompt:** "Design request/response logging and timing capture for an API test client using DelegatingHandler or wrapper pattern, with toggleable logging and always-active timing."
**Outcome:** Integrated observability directly into KiboApiClient rather than using DelegatingHandler. Created ApiResponse<T> with ElapsedMs, RequestLog, and ResponseLog properties. Added X-Correlation-Id header for tracing. Made logging toggleable via EnableRequestLogging option. Timing is always captured via Stopwatch.

## Prompt 6 — Task 1 (Refactoring)
**Tool:** Opencode (Claude)
**Prompt:** "Refactor the legacy OrderTests to use the new Kibo.TestingFramework, replacing all anti-patterns with clean, concise test code."
**Outcome:** Reduced test code by ~60% while improving readability. Each test is now self-contained with the builder pattern. The status transition test uses Poller instead of Thread.Sleep. Added IDisposable for proper client cleanup. All tests use the shared KiboApiClient instance.

## Prompt 7 — Code Quality (Edge Case Tests)
**Tool:** Opencode (Claude)
**Prompt:** "The edge case tests are failing. Analyze the MockApi controller behavior and fix the tests to accurately document actual API behavior vs expected behavior."
**Outcome:** Read the OrdersController.cs to understand the API accepts any non-empty tenant string. Fixed the SQL injection test to expect 201 (safe but not validated). Fixed empty lineItems test to expect 201 (bug documented). Fixed missing email test to expect 201 (bug documented). Added clear bug report comments documenting expected vs actual behavior.

## Prompt 8 — Test Fix (Tenant Header)
**Tool:** Opencode (Claude)
**Prompt:** "The CreateOrder_WithoutTenantHeader_Returns401 test is failing because PostRawAsync always adds the default tenant header. How do I test without the tenant header?"
**Outcome:** Added an `includeTenantHeader` parameter to PostRawAsync with a sentinel value "__SKIP_TENANT__" to conditionally skip adding the tenant header. This allows testing auth scenarios while keeping the default behavior intact.

---

## Summary

| # | Task | AI Tool Used | Key Decisions |
|---|------|-------------|---------------|
| 1 | Task 1 | Opencode (Claude) | Designed API client with observability built-in |
| 2 | Task 2 | Opencode (Claude) | Fluent builder with random data generation |
| 3 | Task 3 | Opencode (Claude) | Generic polling with custom exception |
| 4 | Task 4 | Opencode (Claude) | 5 edge cases, 4 bugs found |
| 5 | Task 6 | Opencode (Claude) | Integrated into API client design |
| 6 | Task 1 | Opencode (Claude) | Reduced test code by 60% |
| 7 | Task 4 | Opencode (Claude) | Fixed tests to match actual API behavior |
| 8 | Task 1 | Opencode (Claude) | Added includeTenantHeader parameter for auth testing |

## AI Judgment Calls

1. **Builder pattern** - Rejected suggestion to use records; kept mutable DTOs for API compatibility
2. **Polling** - Used Task.Delay instead of Thread.Sleep for async compatibility
3. **Observability** - Integrated into client rather than separate DelegatingHandler for simplicity
4. **Edge cases** - Documented expected vs actual behavior to highlight API bugs
5. **Testing** - Added timing assertion test to demonstrate observability capability