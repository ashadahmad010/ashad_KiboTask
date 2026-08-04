# Kibo.TestingFramework

> **This is your workspace.** Build your reusable Testing SDK here.

## Purpose

This class library is intentionally empty. Your job is to turn it into a **shared testing platform** that eliminates the duplication and fragility found in `Kibo.LegacyTests`.

## What Belongs Here

| Component | Description |
|---|---|
| **API Client Wrapper** | A configured `HttpClient` base class that handles base URL resolution, default headers (`x-kibo-tenant`), serialization, and response unwrapping. |
| **Fluent Data Builder** | `OrderBuilder` (and potentially `LineItemBuilder`) classes that let tests construct valid order payloads in a single line — e.g., `OrderBuilder.Default().WithItems(3).ForTenant("t1").Build()`. |
| **Polling / WaitUntil Utility** | A reusable async helper that polls an endpoint at a configurable interval and returns as soon as a predicate is satisfied (or throws on timeout). This replaces `Thread.Sleep()`. |
| **Models (optional)** | Shared DTOs or response wrappers if you want to deserialize API responses into strongly-typed objects. |

## Getting Started

```bash
# Reference this project from your test project
dotnet add tests/Kibo.LegacyTests/Kibo.LegacyTests.csproj reference src/Kibo.TestingFramework/Kibo.TestingFramework.csproj
```

Then start extracting and building!
