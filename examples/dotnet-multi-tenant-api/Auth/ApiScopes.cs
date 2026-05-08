namespace MultiTenantApi.Auth;

/// <summary>
/// Scope strings the IdP issues into the access token's "scope" claim.
///
/// Convention (OAuth2 best practice):
///   {api}.{resource}.{action}
///
/// "Tenant" and "subscriber" are *not* encoded in the scope name itself --
/// scopes describe *what* the caller may do, claims describe *which*
/// tenant/subscriber they are bound to. Both must match before access is granted.
/// </summary>
public static class ApiScopes
{
    // General (cross-tenant) APIs -- typically only granted to internal/admin clients.
    public const string CatalogRead = "api.catalog.read";
    public const string CatalogWrite = "api.catalog.write";

    // Tenant-scoped APIs (a tenant = a customer organization).
    public const string TenantRead = "api.tenant.read";
    public const string TenantWrite = "api.tenant.write";

    // Subscriber-scoped APIs (a subscriber = an end user inside a tenant).
    public const string SubscriberRead = "api.subscriber.read";
    public const string SubscriberWrite = "api.subscriber.write";
}
