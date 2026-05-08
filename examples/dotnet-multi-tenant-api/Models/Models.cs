namespace MultiTenantApi.Models;

public sealed record Product(string Sku, string Name, decimal Price);

public sealed record TenantSettings(string TenantId, string DisplayName, string Timezone);

public sealed record Order(string OrderId, string TenantId, string SubscriberId, decimal Total);
