using MultiTenantApi.Models;

namespace MultiTenantApi.Data;

/// <summary>
/// In-memory placeholder. The point of the example is the auth pipeline,
/// not persistence -- but every read path takes tenantId/subscriberId so
/// the data layer cannot serve cross-tenant rows even if a controller
/// forgets to filter.
/// </summary>
public sealed class DemoStore
{
    private readonly List<Product> _catalog = new()
    {
        new("SKU-1", "Widget", 9.99m),
        new("SKU-2", "Gizmo", 19.99m),
    };

    private readonly Dictionary<string, TenantSettings> _tenants = new()
    {
        ["acme"] = new("acme", "Acme Corp", "Europe/Brussels"),
        ["globex"] = new("globex", "Globex", "America/New_York"),
    };

    private readonly List<Order> _orders = new()
    {
        new("o-1001", "acme",   "alice", 42.00m),
        new("o-1002", "acme",   "bob",   17.50m),
        new("o-2001", "globex", "carol", 99.00m),
    };

    public IReadOnlyList<Product> ListCatalog() => _catalog;

    public TenantSettings? GetTenant(string tenantId) =>
        _tenants.TryGetValue(tenantId, out var t) ? t : null;

    /// <summary>
    /// Returns orders for a specific subscriber within a tenant.
    /// Both filters are mandatory parameters -- there is no "give me all orders"
    /// overload, so a controller bug cannot accidentally cross tenant lines.
    /// </summary>
    public IReadOnlyList<Order> ListOrders(string tenantId, string subscriberId) =>
        _orders.Where(o => o.TenantId == tenantId && o.SubscriberId == subscriberId).ToList();
}
