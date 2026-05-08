using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantApi.Auth;
using MultiTenantApi.Data;

namespace MultiTenantApi.Controllers;

/// <summary>
/// Subscriber-scoped API. The route carries both {tenantId} and {subscriberId};
/// SubscriberAccessHandler verifies both against the caller's claims.
///
/// This controller demonstrates the "and" combination: an endpoint that
/// belongs to a subscriber, inside a tenant, and the caller must be allowed
/// to access that exact pair.
/// </summary>
[ApiController]
[Route("tenants/{tenantId}/subscribers/{subscriberId}")]
public sealed class SubscribersController : ControllerBase
{
    private readonly DemoStore _store;
    public SubscribersController(DemoStore store) => _store = store;

    [HttpGet("orders")]
    [Authorize(Policy = AuthPolicies.SubscriberRead)]
    public IActionResult ListOrders(string tenantId, string subscriberId)
    {
        var caller = CallerContext.FromPrincipal(User);

        // Belt-and-braces: even if the policy is misconfigured, this
        // imperative check would still block cross-tenant/subscriber access.
        if (!caller.CanAccessSubscriber(tenantId, subscriberId)) return Forbid();

        return Ok(_store.ListOrders(tenantId, subscriberId));
    }

    [HttpPost("orders")]
    [Authorize(Policy = AuthPolicies.SubscriberWrite)]
    public IActionResult CreateOrder(string tenantId, string subscriberId) => StatusCode(501);
}
