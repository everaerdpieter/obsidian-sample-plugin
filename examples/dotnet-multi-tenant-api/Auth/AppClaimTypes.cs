namespace MultiTenantApi.Auth;

/// <summary>
/// Custom claim names emitted by the identity provider (IdP) inside the JWT.
/// Use stable, namespaced names so they do not collide with standard OIDC claims.
/// </summary>
public static class AppClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string SubscriberId = "sub_id";
    public const string AllowedTenants = "tenants";
    public const string AllowedSubscribers = "subscribers";
    public const string Scope = "scope";
}
