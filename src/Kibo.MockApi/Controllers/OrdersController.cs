using Microsoft.AspNetCore.Mvc;
using Kibo.MockApi.Models;
using Kibo.MockApi.Storage;

namespace Kibo.MockApi.Controllers;

[ApiController]
[Route("v1/orders")]
public class OrdersController : ControllerBase
{
    /// <summary>
    /// POST /v1/orders
    /// Creates a new order. Requires the "x-kibo-tenant" header.
    /// The order starts as "Pending" and transitions to "ReadyForFulfillment" after 5 seconds.
    /// </summary>
    [HttpPost]
    public IActionResult CreateOrder([FromBody] Order order)
    {
        // ── Auth gate: tenant header is required ──
        if (!Request.Headers.TryGetValue("x-kibo-tenant", out var tenantHeader)
            || string.IsNullOrWhiteSpace(tenantHeader))
        {
            return Unauthorized(new { error = "Missing required header: x-kibo-tenant" });
        }

        order.Id = Guid.NewGuid();
        order.TenantId = tenantHeader.ToString();
        order.Status = "Pending";

        OrderStore.Add(order);

        // ── THE SDET TRAP ──
        // Fire-and-forget: after exactly 5 seconds the status flips.
        // Naive tests use Thread.Sleep(6000) to handle this — candidates
        // should replace that with a polling/retry strategy.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            OrderStore.UpdateStatus(order.Id, "ReadyForFulfillment");
        });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>
    /// GET /v1/orders/{id}
    /// Returns the current state of an order.
    /// </summary>
    [HttpGet("{id:guid}")]
    public IActionResult GetOrder(Guid id)
    {
        if (!OrderStore.TryGet(id, out var order) || order is null)
        {
            return NotFound(new { error = $"Order {id} not found" });
        }

        return Ok(order);
    }
}
