using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace MultiTenantApi.Auth;

public sealed class SubscriberAccessRequirement : IAuthorizationRequirement;

/// <summary>
/// Verifies that the {subscriberId} in the route belongs to the caller -- and,
/// crucially, that the subscriber lives inside a tenant the caller may access.
///
/// Without the tenant check, a malicious caller could swap {tenantId} for a
/// foreign tenant while keeping their own {subscriberId} and read another
/// tenant's data. So this handler explicitly cross-checks both.
/// </summary>
public sealed class SubscriberAccessHandler : AuthorizationHandler<SubscriberAccessRequirement>
{
    private readonly IHttpContextAccessor _http;

    public SubscriberAccessHandler(IHttpContextAccessor http) => _http = http;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SubscriberAccessRequirement requirement)
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return Task.CompletedTask;

        var routeSub = ctx.Request.RouteValues["subscriberId"]?.ToString();
        var routeTenant = ctx.Request.RouteValues["tenantId"]?.ToString();
        if (string.IsNullOrWhiteSpace(routeSub) || string.IsNullOrWhiteSpace(routeTenant))
        {
            return Task.CompletedTask;
        }

        // Subscriber must match the bound subscriber claim, OR appear in an
        // explicit "subscribers" list claim (e.g. an admin operating on
        // multiple subscribers within their tenant).
        var subscriberOk = false;
        var boundSub = context.User.FindFirst(AppClaimTypes.SubscriberId)?.Value;
        if (boundSub == routeSub) subscriberOk = true;

        if (!subscriberOk)
        {
            foreach (var c in context.User.FindAll(AppClaimTypes.AllowedSubscribers))
            {
                foreach (var s in c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (s == routeSub) { subscriberOk = true; break; }
                }
                if (subscriberOk) break;
            }
        }

        if (!subscriberOk) return Task.CompletedTask;

        // Tenant cross-check: the route's tenant must be one the caller may access.
        var tenantOk = false;
        var boundTenant = context.User.FindFirst(AppClaimTypes.TenantId)?.Value;
        if (boundTenant == routeTenant) tenantOk = true;

        if (!tenantOk)
        {
            foreach (var c in context.User.FindAll(AppClaimTypes.AllowedTenants))
            {
                foreach (var t in c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (t == routeTenant) { tenantOk = true; break; }
                }
                if (tenantOk) break;
            }
        }

        if (subscriberOk && tenantOk)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
