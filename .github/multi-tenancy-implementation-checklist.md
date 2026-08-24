# Multi-tenancy implementation checklist

## Shared contracts and ambient context

- [x] Add `IMultiTenant` with nullable `Guid? TenantId` for ABP-compatible host semantics.
- [x] Add `ICurrentTenant` with ID, organization alias/name, availability, and nested `Change` scopes.
- [x] Implement `CurrentTenant` with `AsyncLocal` state.
- [x] Restore the previous tenant after nested scopes, disposal, and exceptions.
- [x] Propagate tenant state through `async/await` without leaking across concurrent flows.
- [x] Reject `Guid.Empty` as an active tenant identifier.
- [x] Register `ICurrentTenant` once through Shared data-filter registration.
- [x] Keep tenant-specific sample IDs out of Shared infrastructure.
- [x] Extract `ISoftDelete` and `IMultiTenant` into independent DDD contract files.

## Typed EF Core filtering

- [x] Enable `IMultiTenant` in `IDataFilter` by default.
- [x] Extend `IDataFilterContext` with `IsMultiTenantFilterEnabled` and `CurrentTenantId`.
- [x] Generalize model filtering by interface capability with `IsAssignableFrom`.
- [x] Apply soft-delete filtering only to `ISoftDelete` entities.
- [x] Apply tenant filtering only to `IMultiTenant` entities.
- [x] Combine both predicates when an entity implements both interfaces.
- [x] Inject `IDataFilter` and `ICurrentTenant` into all four module DbContexts.
- [x] Apply the combined filters in Accounts, Basket, Catalog, and Ordering.
- [x] Preserve `IDataFilter.Disable<IMultiTenant>()` as a typed visibility-only escape hatch.

## Write ownership and auditing

- [x] Add `MultiTenantEntityInterceptor`.
- [x] Require an active tenant for tenant-owned inserts, updates, and deletes.
- [x] Assign the current tenant to new entities when `TenantId` is unset.
- [x] Reject caller-supplied tenant IDs that differ from the current tenant.
- [x] Reject changes to existing tenant ownership.
- [x] Validate directly queryable child entities independently.
- [x] Leave non-`IMultiTenant` infrastructure entities unchanged.
- [x] Register the tenant interceptor in every module.
- [x] Add `TenantId` to structured audit events.
- [x] Continue using only the Keycloak `sub` GUID in actor columns.

## Module persistence

- [x] Make Accounts aggregates and directly queryable children tenant-owned.
- [x] Make Basket aggregates and directly queryable children tenant-owned.
- [x] Make Catalog aggregates tenant-owned.
- [x] Make Ordering aggregates and directly queryable children tenant-owned.
- [x] Keep `OutboxMessage` outside tenant query filtering.
- [x] Add non-null tenant-routing metadata to `OutboxMessage`.
- [x] Change `CustomerAccount.Id` to a generated aggregate GUID.
- [x] Add `CustomerAccount.UserId` for the Keycloak `sub` GUID.
- [x] Make Account uniqueness tenant-aware with `(TenantId, UserId)`.
- [x] Make Basket uniqueness tenant-aware with `(TenantId, UserName)`.
- [x] Make Ordering name uniqueness tenant-aware with `(TenantId, OrderName)`.
- [x] Configure tenant columns as required in each business table.

## Messaging, outbox, cache, and seeding

- [x] Add `TenantId` to the base integration-event envelope.
- [x] Set tenant context on checkout events.
- [x] Set tenant context on product-price events.
- [x] Store tenant-routing metadata with outbox messages.
- [x] Restore tenant context in the Basket checkout consumer.
- [x] Restore tenant context in the product-price consumer.
- [x] Restore tenant context in the outbox processor.
- [x] Recover legacy pending-event tenant context from the outbox routing column.
- [x] Prefix Basket Redis keys with the organization UUID.
- [x] Run Catalog seeding inside explicit tenant scopes.
- [x] Bind Catalog seed tenant IDs from development configuration.
- [x] Seed separate deterministic UUID-v5 Catalog data for configured tenants.

## Authentication and authorization

- [x] Parse exactly one signed Keycloak `organization` claim.
- [x] Require a non-empty GUID organization ID.
- [x] Establish `ICurrentTenant` after authentication and before authorization.
- [x] Return 403 Problem Details for authenticated business requests without a valid tenant.
- [x] Keep GraphQL schema documents tenant-neutral for composition and health checks.
- [x] Reject tenant selection from headers, routes, query strings, cookies, and DTOs by design.
- [x] Require `customer` or `admin` from the selected organization for normal business APIs.
- [x] Require selected-organization `admin` for Catalog writes.
- [x] Hide Catalog write actions in Angular when the selected organization is not admin.
- [x] Restrict Ordering `*-all` RPT scopes to `admin` inside the selected organization claim.
- [x] Restrict Ordering `*-own` RPT scopes to a selected-organization customer or admin.

## Angular tenant experience

- [x] Request the `organization` scope during login.
- [x] Store tenant ID, alias/name, and organization roles in auth state.
- [x] Require a valid tenant in the Angular auth guard.
- [x] Display the active organization in the application shell.
- [x] Add a "Switch organization" action that starts a new authorization request.
- [x] Clear permission-token caches during tenant switching and logout.
- [x] Namespace Ordering RPT cache entries by tenant ID.
- [x] Upgrade `keycloak-js` to the current `26.2.4` release and update the lockfile.

## Keycloak 26.7 Organizations

- [x] Upgrade Docker Compose from Keycloak `24.0.3` to `26.7.0`.
- [x] Reset only the local Keycloak schema for the realm rename while preserving application schemas.
- [x] Enable Organizations in the existing `eshoprealm`.
- [x] Configure Organization Membership to include organization IDs.
- [x] Configure Organization Group Membership to include groups and group role mappings.
- [x] Attach the `organization` scope to `myclient` and `ordering-api`.
- [x] Create Acme with canonical ID `11111111-1111-1111-1111-111111111111`.
- [x] Create Contoso with canonical ID `22222222-2222-2222-2222-222222222222`.
- [x] Create `Customers` and `Admins` groups in both organizations.
- [x] Assign `customer` and `admin` roles through organization groups.
- [x] Make `shopper` a customer in both organizations.
- [x] Make `shopper` an administrator only in Acme.
- [x] Remove the direct global `customer` assignment from `shopper`.
- [x] Make the realm import authoritative for fixed development organization IDs.
- [x] Fail reconciliation when Acme or Contoso is missing or has a noncanonical ID.
- [x] Remove the organization-ID SQL canonicalizer and its Compose service.
- [x] Verify real Acme and Contoso access tokens contain exactly one canonical organization and the expected roles.
- [x] Package selected-organization customer/admin authorization policies as Keycloak script providers.
- [x] Add an RPT protocol mapper that copies the signed source organization claim.
- [x] Verify Acme receives own/all Ordering permissions and Contoso receives only own permissions.
- [x] Verify an Ordering RPT retains exactly one organization claim and selected-organization roles.

## Database migrations

- [x] Generate one EF Core migration for Accounts.
- [x] Generate one EF Core migration for Basket.
- [x] Generate one EF Core migration for Catalog.
- [x] Generate one EF Core migration for Ordering.
- [x] Backfill existing business rows to Acme.
- [x] Backfill existing Accounts IDs into `UserId`.
- [x] Backfill pending outbox tenant metadata to Acme.
- [x] Replace global unique indexes with tenant-aware indexes.
- [x] Preserve audit, soft-delete, and foreign-key configuration in model snapshots.
- [x] Apply and verify all four migrations against the existing populated Docker database.
- [x] Verify all four migrations against a fresh empty PostgreSQL database.

## Automated verification

- [x] Test current-tenant default, nested scope, exception restoration, async propagation, and concurrency isolation.
- [x] Test valid, missing, malformed, empty, and multiple organization claims.
- [x] Test default `IMultiTenant` filter state.
- [x] Test combined soft-delete and tenant filtering.
- [x] Test independent typed filter disable scopes.
- [x] Test automatic tenant assignment.
- [x] Test missing, mismatched, and changed tenant ownership rejection.
- [x] Test transitive soft-delete capability inheritance.
- [x] Update Ordering permission evaluator tests for organization context.
- [x] Pass Shared tests: 48 tests, including Catalog seed configuration and UUID-v5 isolation.
- [x] Pass Ordering tests: 5 tests.
- [x] Test that structured audit events contain the current organization UUID.
- [x] Run all .NET tests and architecture tests (57 tests passed).
- [x] Run Angular boundary checks and production build.
- [x] Rebuild and start the complete Docker stack after final changes.
- [x] Add a repeatable Compose-network multi-tenancy acceptance verifier.
- [x] Verify tenant isolation through live REST and GraphQL requests.
- [x] Verify unsuccessful cross-tenant writes are rejected in live requests.
- [x] Verify tenant headers cannot override the signed organization claim.
- [x] Verify missing organization context returns 403 Problem Details.

## Documentation

- [x] Extend `src/Shared/shared.md` with organization resolution and security invariants.
- [x] Add Keycloak provisioning and JavaScript-provider documentation.
- [x] Document typed tenant filtering and write ownership validation.
- [x] Document background processing, outbox routing, caching, and tenant-aware authorization.
- [x] Keep this checklist synchronized with completed validation work.
