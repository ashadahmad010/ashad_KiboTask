namespace Kibo.MockApi.Models;

public class Order
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public List<LineItem> LineItems { get; set; } = new();
}

public class LineItem
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
