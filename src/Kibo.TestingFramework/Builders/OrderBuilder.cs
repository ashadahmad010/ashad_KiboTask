using Kibo.TestingFramework.Api;

namespace Kibo.TestingFramework.Builders;

/// <summary>
/// Fluent builder for constructing CreateOrderRequest objects with sensible defaults.
/// Supports chainable methods and random data generation.
/// </summary>
public class OrderBuilder
{
    private string? _customerEmail;
    private string? _tenantId;
    private readonly List<CreateLineItemRequest> _lineItems = new();
    private static readonly Random _random = new();

    private static readonly string[] Domains = { "example.com", "test.com", "kibo.com", "mail.com" };
    private static readonly string[] FirstNames = { "john", "jane", "alex", "sam", "taylor", "morgan" };
    private static readonly string[] LastNames = { "doe", "smith", "jones", "brown", "wilson", "davis" };
    private static readonly string[] SkuPrefixes = { "SKU", "PROD", "ITEM", "PART" };

    /// <summary>
    /// Creates a new OrderBuilder with sensible defaults.
    /// Produces a valid order with zero configuration.
    /// </summary>
    public OrderBuilder()
    {
    }

    /// <summary>
    /// Sets the customer email.
    /// </summary>
    public OrderBuilder WithCustomerEmail(string email)
    {
        _customerEmail = email;
        return this;
    }

    /// <summary>
    /// Sets a random customer email.
    /// </summary>
    public OrderBuilder WithRandomCustomerEmail()
    {
        var first = FirstNames[_random.Next(FirstNames.Length)];
        var last = LastNames[_random.Next(LastNames.Length)];
        var domain = Domains[_random.Next(Domains.Length)];
        _customerEmail = $"{first}.{last}{_random.Next(100, 999)}@{domain}";
        return this;
    }

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    public OrderBuilder ForTenant(string tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Adds a specific line item.
    /// </summary>
    public OrderBuilder WithItem(string productCode, int quantity, decimal unitPrice)
    {
        _lineItems.Add(new CreateLineItemRequest
        {
            ProductCode = productCode,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
        return this;
    }

    /// <summary>
    /// Adds a line item with random values.
    /// </summary>
    public OrderBuilder WithRandomItem()
    {
        var prefix = SkuPrefixes[_random.Next(SkuPrefixes.Length)];
        _lineItems.Add(new CreateLineItemRequest
        {
            ProductCode = $"{prefix}-{_random.Next(100, 999)}",
            Quantity = _random.Next(1, 10),
            UnitPrice = Math.Round((decimal)(_random.NextDouble() * 100 + 1), 2)
        });
        return this;
    }

    /// <summary>
    /// Adds N random line items.
    /// </summary>
    public OrderBuilder WithItems(int count)
    {
        for (int i = 0; i < count; i++)
        {
            WithRandomItem();
        }
        return this;
    }

    /// <summary>
    /// Builds the CreateOrderRequest with sensible defaults.
    /// </summary>
    public CreateOrderRequest Build()
    {
        var domain = _random.Next(Domains.Length) == 0 ? "example.com" : Domains[_random.Next(Domains.Length)];
        var email = _customerEmail ?? $"test.user{_random.Next(1000, 9999)}@{domain}";
        
        var lineItems = _lineItems.Count > 0
            ? _lineItems
            : new List<CreateLineItemRequest>
            {
                new CreateLineItemRequest
                {
                    ProductCode = $"{SkuPrefixes[_random.Next(SkuPrefixes.Length)]}-{_random.Next(100, 999)}",
                    Quantity = _random.Next(1, 5),
                    UnitPrice = Math.Round((decimal)(_random.NextDouble() * 50 + 5), 2)
                }
            };

        return new CreateOrderRequest
        {
            CustomerEmail = email,
            LineItems = lineItems
        };
    }

    /// <summary>
    /// Builds the request and returns it along with the tenant ID.
    /// Useful when you need both pieces together.
    /// </summary>
    public (CreateOrderRequest Request, string? TenantId) BuildWithTenant()
    {
        return (Build(), _tenantId);
    }
}