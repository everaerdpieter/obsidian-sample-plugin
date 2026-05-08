using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantApi.Auth;
using MultiTenantApi.Data;

namespace MultiTenantApi.Controllers;

/// <summary>
/// "General" API -- not bound to any tenant or subscriber.
/// Authorized purely by scope. Typically only internal services or admin
/// clients are granted these scopes by the IdP.
/// </summary>
[ApiController]
[Route("catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly DemoStore _store;
    public CatalogController(DemoStore store) => _store = store;

    [HttpGet]
    [Authorize(Policy = AuthPolicies.CatalogRead)]
    public IActionResult List() => Ok(_store.ListCatalog());

    [HttpPost]
    [Authorize(Policy = AuthPolicies.CatalogWrite)]
    public IActionResult Add() => StatusCode(501);
}
