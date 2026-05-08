using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MultiTenantApi.Auth;

/// <summary>
/// Requires that the access token carries a specific scope.
/// The "scope" claim is space-delimited per RFC 6749, so we split before comparing.
/// </summary>
public sealed class ScopeRequirement : IAuthorizationRequirement
{
    public ScopeRequirement(string requiredScope) => RequiredScope = requiredScope;
    public string RequiredScope { get; }
}

public sealed class ScopeHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        var scopes = context.User.FindAll(AppClaimTypes.Scope)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        if (scopes.Contains(requirement.RequiredScope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
