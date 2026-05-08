using Microsoft.AspNetCore.Authorization;

namespace MultiTenantApi.Auth;

/// <summary>
/// Marker requirement used by route-bound authorization. The actual tenant id
/// is not on the requirement -- it comes from the route value <c>{tenantId}</c>
/// at request time, which is then matched against the caller's claims.
/// </summary>
public sealed class TenantAccessRequirement : IAuthorizationRequirement;
