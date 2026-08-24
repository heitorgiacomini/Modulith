# Keycloak development provisioning

The Compose stack runs Keycloak 26.7.0 with the `eshoprealm` realm. Keycloak is the sole source of organization metadata, memberships, and organization-specific roles; the application stores the selected Keycloak organization UUID directly as `TenantId`.

## Provisioning flow

1. `eshoprealm-realm.json` is imported when Keycloak starts with an empty `keycloak` PostgreSQL schema.
2. The import creates the realm, clients, development user, roles, and the fixed Acme and Contoso Organizations.
3. `configure-organizations.mjs` uses the Admin REST API to reconcile organization groups, memberships, role mappings, client scopes, protocol mappers, and Ordering authorization policies.
4. The optional `keycloak-verifier` Compose tool obtains real organization-selected tokens and RPTs and exercises tenant isolation through the API after startup.

The realm JSON is authoritative for development organization IDs:

| Organization | Alias | UUID |
| --- | --- | --- |
| Acme | `acme` | `11111111-1111-1111-1111-111111111111` |
| Contoso | `contoso` | `22222222-2222-2222-2222-222222222222` |

The reconciler deliberately fails if either organization is missing or has another ID. Admin REST organization creation generates an ID, so it must not be used to repair fixed development IDs. Reset only the local `keycloak` schema and let the realm import recreate identity data; do not delete the shared PostgreSQL volume or the Accounts, Basket, Catalog, and Ordering schemas.

## Users, memberships, groups, and roles

`shopper` is one Keycloak user with membership in both organizations. Membership and authorization are organization-specific:

- Acme `Customers` grants `customer`; Acme `Admins` grants `admin`; `shopper` belongs to both.
- Contoso `Customers` grants `customer`; Contoso `Admins` grants `admin`; `shopper` belongs only to `Customers`.
- `customer` is not assigned directly to `shopper` as a global realm role.

Authentication selects one organization. The resulting access token contains exactly one `organization` entry with its UUID and effective group-derived roles. Switching organizations means obtaining a new token; the application never accepts a tenant header or another client-provided tenant override.

`myclient` uses Keycloak's built-in protocol mappers for the GUID `sub` and `preferred_username`. The API persists only `sub`; the username exists solely for the current Basket presentation/compatibility contract.

## JavaScript provider JAR

Keycloak Authorization Services cannot express the nested selected-organization role checks used by Ordering with the built-in group policy alone. The Dockerfile packages three server scripts and `META-INF/keycloak-scripts.json` into a provider JAR and enables Keycloak's `scripts` feature:

- `selected-organization-customer.js` grants Ordering own-record scopes when the single selected organization contains `customer`.
- `selected-organization-admin.js` grants Ordering all-record scopes when that organization contains `admin`.
- `source-organization-claim.js` copies the signed source access token's organization claim into the Ordering RPT.

The policy source lives in the image, not in the realm database. The reconciler attaches the registered provider types to `ordering-api` and removes obsolete policies.

The resource server also uses Keycloak's built-in `oidc-sub-mapper` so an Ordering RPT retains the source user's GUID `sub`. This is a standard protocol mapper, not a fourth JavaScript provider, and preserves the application's rule that no username can substitute for `sub`.

## Tokens and RPTs

The Angular client requests `openid organization:<alias>`. Ordering exchanges that access token for a UMA RPT scoped to `Orders`. Both tokens must retain the same single organization UUID. Acme can receive own and all scopes; Contoso can receive only own scopes for the seeded user.

Run the live verifier from `src` after the stack is healthy:

```powershell
docker compose run --rm --no-deps keycloak-verifier node /verify-multitenancy.mjs
```

## Production provisioning

The fixed organizations and `shopper` are development bootstrap data only. In production, provision organizations and users through controlled Keycloak administration or automation and allow Keycloak to generate organization UUIDs. Use those UUIDs directly as application tenant IDs; do not add a mapping table. Keep credentials and client secrets outside realm import files, disable direct-access grants unless explicitly required, use TLS and production hostname settings, and back up the Keycloak schema independently from application business data.
