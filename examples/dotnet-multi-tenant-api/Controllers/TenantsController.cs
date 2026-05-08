using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantApi.Auth;
using MultiTenantApi.Data;

namespace MultiTenantApi.Controllers;

/// <summary>
/// Tenant-scoped API. The route carries {tenantId}; the TenantAccessHandler
/// ensures that {tenantId} is present in the caller's claims before the
/// action runs. Without this, a caller from "acme" could call
/// /tenants/globex/settings just by changing the URL.
/// </summary>
[ApiController]
[Route("tenants/{tenantId}")]
public sealed class TenantsController : ControllerBase
{
    private readonly DemoStore _store;
    public TenantsController(DemoStore store) => _store = store;

    [HttpGet("settings")]
    [Authorize(Policy = AuthPolicies.TenantRead)]
    public IActionResult GetSettings(string tenantId)
    {
        // Defense in depth: even though TenantAccessHandler already verified
        // the route's tenantId is allowed, we still pass tenantId into every
        // store call. The store has no "give me everything" method.
        var settings = _store.GetTenant(tenantId);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut("settings")]
    [Authorize(Policy = AuthPolicies.TenantWrite)]
    public IActionResult UpdateSettings(string tenantId) => StatusCode(501);
}
