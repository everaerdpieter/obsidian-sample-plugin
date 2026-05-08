using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MultiTenantApi.Auth;
using MultiTenantApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// ---- AuthN: validate JWTs from the IdP -------------------------------------
//
// The IdP (Entra ID, Auth0, IdentityServer, Keycloak, ...) signs the token.
// We validate signature, issuer, audience and lifetime *before* any handler runs.
// If validation fails the request is rejected with 401 -- the controller never
// executes, so no claim-based check below can be bypassed.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.Authority = jwt["Issuer"];          // discovery for production
        options.Audience = jwt["Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // For local/dev only -- production should use the IdP's JWKS
            // (handled automatically when Authority is set).
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            NameClaimType = "sub",
            RoleClaimType = "roles"
        };
    });

// ---- AuthZ: scope checks + tenant/subscriber claim checks -------------------
builder.Services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, TenantAccessHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SubscriberAccessHandler>();

builder.Services.AddAuthorizationBuilder()
    // General APIs: scope-only. No tenant binding -- granted to internal admins.
    .AddPolicy(AuthPolicies.CatalogRead, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new ScopeRequirement(ApiScopes.CatalogRead)))
    .AddPolicy(AuthPolicies.CatalogWrite, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new ScopeRequirement(ApiScopes.CatalogWrite)))

    // Tenant-scoped APIs: scope + the route's tenant must match the caller's claims.
    .AddPolicy(AuthPolicies.TenantRead, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(
            new ScopeRequirement(ApiScopes.TenantRead),
            new TenantAccessRequirement()))
    .AddPolicy(AuthPolicies.TenantWrite, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(
            new ScopeRequirement(ApiScopes.TenantWrite),
            new TenantAccessRequirement()))

    // Subscriber-scoped APIs: scope + tenant *and* subscriber must match.
    .AddPolicy(AuthPolicies.SubscriberRead, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(
            new ScopeRequirement(ApiScopes.SubscriberRead),
            new SubscriberAccessRequirement()))
    .AddPolicy(AuthPolicies.SubscriberWrite, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(
            new ScopeRequirement(ApiScopes.SubscriberWrite),
            new SubscriberAccessRequirement()));

// In-memory demo "database" so the example runs end-to-end.
builder.Services.AddSingleton<DemoStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
