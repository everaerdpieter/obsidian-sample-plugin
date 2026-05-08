using System.Security.Claims;

namespace MultiTenantApi.Auth;

/// <summary>
/// Strongly-typed view over the caller's claims. Inject this into services or
/// build it from <see cref="ClaimsPrincipal"/> when filtering data queries.
///
/// Authorization handlers gate the *route*; this type makes it easy for the
/// data layer to *also* filter every query by tenant/subscriber so a bug in
/// a controller can never accidentally leak rows.
/// </summary>
public sealed record CallerContext(
    string? TenantId,
    string? SubscriberId,
    IReadOnlySet<string> AllowedTenants,
    IReadOnlySet<string> AllowedSubscribers,
    IReadOnlySet<string> Scopes)
{
    public static CallerContext FromPrincipal(ClaimsPrincipal user)
    {
        var allowedTenants = user.FindAll(AppClaimTypes.AllowedTenants)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        var allowedSubs = user.FindAll(AppClaimTypes.AllowedSubscribers)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        var scopes = user.FindAll(AppClaimTypes.Scope)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        return new CallerContext(
            TenantId: user.FindFirst(AppClaimTypes.TenantId)?.Value,
            SubscriberId: user.FindFirst(AppClaimTypes.SubscriberId)?.Value,
            AllowedTenants: allowedTenants,
            AllowedSubscribers: allowedSubs,
            Scopes: scopes);
    }

    public bool CanAccessTenant(string tenantId) =>
        TenantId == tenantId || AllowedTenants.Contains(tenantId);

    public bool CanAccessSubscriber(string tenantId, string subscriberId) =>
        CanAccessTenant(tenantId) &&
        (SubscriberId == subscriberId || AllowedSubscribers.Contains(subscriberId));
}
