namespace MultiTenantApi.Auth;

/// <summary>
/// Authorization policy names. A policy = (required scope) + (required claims/handlers).
/// Controllers reference these names instead of hard-coding scope strings.
/// </summary>
public static class AuthPolicies
{
    public const string CatalogRead = nameof(CatalogRead);
    public const string CatalogWrite = nameof(CatalogWrite);

    public const string TenantRead = nameof(TenantRead);
    public const string TenantWrite = nameof(TenantWrite);

    public const string SubscriberRead = nameof(SubscriberRead);
    public const string SubscriberWrite = nameof(SubscriberWrite);
}
