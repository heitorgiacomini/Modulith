# Persist and implement the Keycloak and Shared refactor

## Plan persistence and agent instructions

- Create `.github/plan/keycloak-shared-refactor-plan.md` containing this complete finalized plan without the `<proposed_plan>` wrapper.
- Preserve the existing `.github/plan/plan.md` raw-request file.
- Create a root `AGENTS.md`; leave `.github/copilot-instructions.md` unchanged.
- Add these planning rules to `AGENTS.md`:
  - every finalized Plan Mode plan must be saved under `.github/plan/`;
  - use a descriptive kebab-case filename ending in `-plan.md`;
  - save the decision-complete plan, not the raw request;
  - revisions update the existing plan file instead of creating duplicates;
  - never overwrite an unrelated plan;
  - when the active Plan Mode prohibits file writes, preserve the exact plan in the response and make saving it the first action when execution begins.

## Keycloak and tenant provisioning

- Rename the realm import to `eshoprealm-realm.json` and replace `myrealm` with `eshoprealm` in issuers, Angular configuration, Compose, API/Gateway settings, scripts, documentation, checklists, and architecture diagrams.
- Change the seeded default role reference to `default-roles-eshoprealm`.
- Preserve the identity model:
  - one Keycloak user can belong to multiple Organizations;
  - `Customers` and `Admins` are organization-specific groups;
  - roles are assigned through organization groups, not directly to `shopper`;
  - `shopper` is a customer in Acme and Contoso and an admin only in Acme;
  - organization switching requires a newly issued token.
- Keep and document the three Keycloak server scripts:
  - customer policy grants Ordering own scopes from the selected organization’s `customer` role;
  - admin policy grants Ordering all scopes from its `admin` role;
  - RPT mapper copies the signed source organization claim into the Ordering RPT.
- Add a Keycloak README covering realm import, the Node reconciler, JavaScript provider JAR, organizations, users, groups, role mappings, tokens, RPTs, and production provisioning.
- Make the realm JSON authoritative for fixed development organizations. The configurator must fail when Acme or Contoso is missing or has the wrong ID, while continuing to reconcile memberships, groups, mappers, and policies.
- Remove `canonicalize-organization-ids.sql` and the `keycloak-tenant-id-migrator` Compose service.
- Reset only the local `keycloak` PostgreSQL schema during rollout; preserve Accounts, Basket, Catalog, and Ordering data and the shared PostgreSQL volume.
- In production, allow Keycloak to generate organization UUIDs and use those UUIDs directly as application `TenantId` values. Keep tenant metadata and membership exclusively in Keycloak.

## Shared, Catalog, and Accounts refactoring

- Extract `ISoftDelete` and `IMultiTenant` from `AuditingInterfaces.cs` into independent `DDD/ISoftDelete.cs` and `DDD/IMultiTenant.cs` files while retaining the `Shared.DDD` namespace.
- Rename `SoftDeleteModelBuilderExtensions` to `DataFilterModelBuilderExtensions`.
- Keep `ApplyDataFilters(IDataFilterContext)` as the only model-builder API and remove `ApplySoftDeleteQueryFilters`.
- Continue combining `ISoftDelete` and `IMultiTenant` predicates through `IsAssignableFrom`, following ABP’s generalized filter design. [ABP DbContext implementation](https://github.com/abpframework/abp/blob/dev/framework/src/Volo.Abp.EntityFrameworkCore/Volo/Abp/EntityFrameworkCore/AbpDbContext.cs)
- Delete `SampleTenants`; Shared must contain only generic tenancy infrastructure.
- Add Catalog-owned options bound from the development configuration section `Catalog:SeedTenantIds`.
- Generate new seed product IDs deterministically with UUID v5 from `(TenantId, product key)`. Existing products remain unchanged and require no migration.
- Update the live verifier to discover product IDs through GraphQL and assert non-empty, disjoint tenant result sets instead of using fixed product IDs.
- Replace `AccountFeatures.cs` and the monolithic Accounts endpoint class with vertical slices for:
  - `GetMyAccount`
  - `UpdatePreferences`
  - `AddAddress`
  - `UpdateAddress`
  - `DeleteAddress`
  - `AddPaymentMethod`
  - `SetDefaultPaymentMethod`
  - `DeletePaymentMethod`
- Give every Accounts slice its own endpoint and handler file, with commands, queries, results, and validators colocated. Move shared conversion logic into `AccountMapping.cs`.
- Preserve all REST routes, authorization policies, status codes, request bodies, GraphQL contracts, and database schemas.

## Verification

- Confirm runtime source, configuration, and documentation no longer reference `myrealm`; do not modify the raw user request under `.github/plan/plan.md`.
- Validate the new realm JSON, provider registration, and Compose configuration.
- After resetting only the Keycloak schema, verify:
  - only `eshoprealm` exists;
  - Acme and Contoso have their fixed development IDs;
  - `shopper` has the expected organization memberships and roles;
  - access tokens and RPTs contain exactly one selected organization;
  - Acme receives Ordering own/all scopes and Contoso receives only own scopes.
- Test Catalog option binding, UUID-v5 seed stability, idempotency, empty production configuration, and tenant isolation.
- Run the live REST/GraphQL verifier, all .NET and architecture tests, Angular boundary checks, Angular production build, and complete Docker Compose startup.
- Run `git diff --check` and confirm no EF migration or model snapshot changes were introduced.
- Confirm `AGENTS.md` contains the plan-persistence rule and the finalized plan exists at the chosen path.

## Assumptions

- Losing current local Keycloak users, credentials, sessions, and realm configuration is acceptable; application business data must remain.
- Custom Keycloak script providers remain required while Ordering uses organization-specific Keycloak Authorization Services scopes.
- Interface namespaces remain backward-compatible.
- Acme, Contoso, and `shopper` are development bootstrap data only.
- Repository-level `AGENTS.md` is the canonical instruction file for Codex-style agents; GitHub Copilot instructions remain unchanged.
