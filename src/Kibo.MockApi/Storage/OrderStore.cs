using System.Collections.Concurrent;
using Kibo.MockApi.Models;

namespace Kibo.MockApi.Storage;

/// <summary>
/// Static in-memory store for orders. Data persists only for the lifetime of the process.
/// </summary>
public static class OrderStore
{
    private static readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public static void Add(Order order)
    {
        _orders[order.Id] = order;
    }

    public static bool TryGet(Guid id, out Order? order)
    {
        return _orders.TryGetValue(id, out order);
    }

    public static void UpdateStatus(Guid id, string newStatus)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            order.Status = newStatus;
        }
    }

    /// <summary>
    /// Clears all orders. Useful for test isolation if needed.
    /// </summary>
    public static void Clear() => _orders.Clear();
}
