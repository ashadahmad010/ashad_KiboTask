namespace Kibo.TestingFramework.Api;

/// <summary>
/// Request payload for creating an order.
/// </summary>
public class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public List<CreateLineItemRequest> LineItems { get; set; } = new();
}

/// <summary>
/// A single line item in an order creation request.
/// </summary>
public class CreateLineItemRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Response model for an order.
/// </summary>
public class OrderResponse
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<LineItemResponse> LineItems { get; set; } = new();
}

/// <summary>
/// A single line item in an order response.
/// </summary>
public class LineItemResponse
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}