using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace MultiTenantApi.Auth;

/// <summary>
/// Verifies that the tenant id in the route ({tenantId}) is one the caller
/// is allowed to access according to claims in the JWT.
///
/// Two shapes are accepted:
///   - tenant_id  : single tenant binding (most users / per-tenant tokens).
///   - tenants    : space-separated list of tenants (multi-tenant admin tokens).
///
/// If neither claim matches the route value, authorization fails -- which means
/// a forged or stale URL like /tenants/OTHER_TENANT/orders is rejected before
/// the controller runs, even if the controller author forgot to filter.
/// </summary>
public sealed class TenantAccessHandler : AuthorizationHandler<TenantAccessRequirement>
{
    private readonly IHttpContextAccessor _http;

    public TenantAccessHandler(IHttpContextAccessor http) => _http = http;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var routeTenant = _http.HttpContext?.Request.RouteValues["tenantId"]?.ToString();
        if (string.IsNullOrWhiteSpace(routeTenant))
        {
            // No tenant in the route -- this handler doesn't apply. Don't fail
            // outright (other handlers may succeed); just don't grant.
            return Task.CompletedTask;
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal);

        var single = context.User.FindFirst(AppClaimTypes.TenantId)?.Value;
        if (!string.IsNullOrEmpty(single)) allowed.Add(single);

        foreach (var c in context.User.FindAll(AppClaimTypes.AllowedTenants))
        {
            foreach (var t in c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                allowed.Add(t);
        }

        if (allowed.Contains(routeTenant))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
