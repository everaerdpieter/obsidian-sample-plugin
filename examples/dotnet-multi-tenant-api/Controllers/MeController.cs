using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantApi.Auth;

namespace MultiTenantApi.Controllers;

/// <summary>
/// Convenience endpoint -- "tell me about my own token". Useful when debugging
/// scope/claim configuration end-to-end. Requires only authentication; no
/// scope, because it is purely reflective.
/// </summary>
[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var caller = CallerContext.FromPrincipal(User);
        return Ok(new
        {
            caller.TenantId,
            caller.SubscriberId,
            AllowedTenants = caller.AllowedTenants.ToArray(),
            AllowedSubscribers = caller.AllowedSubscribers.ToArray(),
            Scopes = caller.Scopes.ToArray()
        });
    }
}
