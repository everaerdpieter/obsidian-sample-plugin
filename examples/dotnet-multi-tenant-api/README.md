# Multi-tenant Claims & Scopes — .NET 8 Web API example

A minimal-but-realistic ASP.NET Core 8 Web API showing how to authorize:

| API shape | Example route | Authorized by |
|---|---|---|
| **General** (cross-tenant) | `GET /catalog` | scope only |
| **Tenant-scoped** | `GET /tenants/{tenantId}/settings` | scope **+** tenant claim must match `{tenantId}` |
| **Subscriber-scoped** | `GET /tenants/{tenantId}/subscribers/{subscriberId}/orders` | scope **+** tenant claim **+** subscriber claim must match the route |
| **Reflective** | `GET /me` | authenticated user |

## Mental model: scopes vs. claims

- **Scopes** = *what* the caller is allowed to do (verbs on resource families). They are issued by the IdP based on the OAuth2 client and the consent granted. Example: `api.tenant.read`.
- **Claims** = *who/where* the caller is bound to (identity, tenant, subscriber). Example: `tenant_id=acme`.

Both must be satisfied. A token with `api.tenant.read` and `tenant_id=acme` can `GET /tenants/acme/settings` but **not** `GET /tenants/globex/settings`, even though the scope is identical.

## How a request is authorized

```
HTTP request ─▶ JwtBearer authn ─▶ Authorization policy
                                    ├─ ScopeRequirement       (does the token carry the required scope?)
                                    ├─ TenantAccessRequirement (does {tenantId} match a tenant claim?)
                                    └─ SubscriberAccessRequirement (does {subscriberId} + {tenantId} match?)
                                                  │
                                          all succeed ▶ controller runs
                                          any fail    ▶ 401 / 403
```

Every requirement is enforced **before** the controller runs, so a controller bug cannot accidentally bypass authz. The controllers also re-check via `CallerContext` and the data store only exposes tenant-/subscriber-filtered queries — that's defense in depth.

## How tenant/subscriber isolation is enforced

Three layers, top-down:

1. **Token validation** (`Program.cs`). The JWT must be signed by the configured IdP, not expired, audience matches. If not, 401 — controllers never run.
2. **Policy-based authorization** (`Auth/*Handler.cs`).
   - `ScopeHandler` parses the space-delimited `scope` claim and checks for the required scope string.
   - `TenantAccessHandler` reads `{tenantId}` from `HttpContext.Request.RouteValues` and verifies it appears in the caller's `tenant_id` claim or `tenants` list claim.
   - `SubscriberAccessHandler` does the same for `{subscriberId}` **and** also re-checks the tenant — otherwise a caller could keep their own `subscriberId` while swapping `{tenantId}` to a foreign tenant.
3. **Data layer** (`Data/DemoStore.cs`). Every query takes `tenantId` (and `subscriberId` where relevant) as required parameters. There is intentionally no "list all orders" method — a forgetful controller cannot leak rows.

## Token shape (JWT payload)

A regular tenant user:
```json
{
  "iss": "https://auth.example.com/",
  "aud": "multi-tenant-api",
  "sub": "alice",
  "tenant_id": "acme",
  "sub_id": "alice",
  "scope": "api.tenant.read api.subscriber.read"
}
```

A tenant admin who can act on multiple subscribers in their tenant:
```json
{
  "tenant_id": "acme",
  "subscribers": "alice bob carol",
  "scope": "api.tenant.read api.tenant.write api.subscriber.read api.subscriber.write"
}
```

An internal cross-tenant admin (e.g. catalog management):
```json
{
  "tenants": "acme globex initech",
  "scope": "api.catalog.read api.catalog.write api.tenant.read"
}
```

## Files of interest

- `Program.cs` — JWT bearer setup, policy-to-requirement mapping.
- `Auth/ApiScopes.cs` — scope string constants.
- `Auth/AppClaimTypes.cs` — claim name constants.
- `Auth/AuthPolicies.cs` — policy name constants.
- `Auth/ScopeRequirement.cs` — scope check.
- `Auth/TenantAccessHandler.cs` — `{tenantId}` route ↔ claim check.
- `Auth/SubscriberAccessRequirement.cs` — `{subscriberId}` + `{tenantId}` cross-check.
- `Auth/CallerContext.cs` — typed view over claims for use in services and queries.
- `Controllers/CatalogController.cs` — general API.
- `Controllers/TenantsController.cs` — tenant-scoped API.
- `Controllers/SubscribersController.cs` — subscriber-scoped API.
- `Controllers/MeController.cs` — reflective endpoint, useful for debugging.

## Run it

```bash
cd examples/dotnet-multi-tenant-api
dotnet restore
dotnet run
# Open https://localhost:5001/swagger
```

Set `Jwt:Issuer` / `Jwt:Audience` / `Jwt:SigningKey` via `appsettings.Development.json`, environment variables, or user-secrets. For real deployments, point `Authority` at your IdP and remove the symmetric `IssuerSigningKey` block in `Program.cs` — let the JWKS endpoint provide signing keys.

## Practical guidance

- **Don't put tenant ids in scopes.** It blows up the scope vocabulary and you end up minting a new scope per tenant. Keep scopes coarse; gate identity with claims.
- **Always put `{tenantId}` in the URL** for tenant APIs and **enforce it against a claim**. Never trust a header or body field as the tenant identifier.
- **Cross-check tenant on every subscriber endpoint.** A subscriber id alone is not enough — see `SubscriberAccessHandler`.
- **Filter at the data layer too.** Authorization handlers protect the route; query parameters protect the rows. You want both.
- **Prefer short-lived access tokens** plus refresh, so revoking a tenant binding propagates within minutes.
- **Audit logs should record `tenant_id`, `sub_id`, and the route's `{tenantId}`/`{subscriberId}`** so any mismatch is detectable post-hoc.
