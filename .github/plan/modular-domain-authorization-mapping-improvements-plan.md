# Modular Domain, Authorization, Mapping, and Documentation Improvements

## Summary

Implement the improvements as four reviewable stages, keeping the solution buildable and tested after each stage. Preserve all HTTP/GraphQL contracts, routes, status codes, database schema, and integration-event wire formats.

## Implementation Changes

### 1. Domain organization and controlled construction

- Rename module `Models` folders and namespaces to `Domain`, updating imports, EF configurations, tests, GraphQL code, and architecture rules.
- Organize domain concepts beneath their bounded context; keep infrastructure records such as Basket’s `OutboxMessage` under `Data` rather than `Domain`.
- Give aggregate roots and entities private EF-compatible parameterless constructors.
- Add named factories such as `Create(...)` only where creation must enforce invariants. Child entities that belong to an aggregate are created or changed through aggregate behavior rather than public constructors.
- Keep simple constructors for value objects when construction itself is already valid; introduce factories only when validation or normalization is required.
- Do not rewrite historical migrations unnecessarily. Update the current EF snapshot and references only as required for compilation, and verify that the refactor produces no database-schema changes.

### 2. Authorization ownership and enforcement

- Keep authentication, the authenticated tenant-member fallback policy, current-tenant middleware, general organization-role handlers, and `HttpContextCurrentUser` in `Bootstrapper.Api`.
- Keep the existing `ICurrentUser` registration and transport boundary unchanged; do not introduce `Shared.AspNetCore` or move HTTP implementations into `Shared`.
- Move module-specific named policies and requirements into their owning modules and register them through `Add...Module`: Catalog owns its administrator write policy; Ordering continues to own its RPT scope policies and evaluator; other modules add named policies only where they have rules beyond the global authenticated tenant-member fallback.
- Retain `.RequireAuthorization(...)` on endpoints for coarse transport-level protection.
- Enforce resource ownership and business permissions again in application-level permission evaluators or command/query handlers where access depends on the loaded resource. Domain entities remain independent of ASP.NET authorization types and claims.
- Deny access when identity, active tenant, organization membership, audience, scope, or resource ownership cannot be established. Preserve the current cross-tenant 404/403 behavior and Problem Details responses.

### 3. DTO boundaries and Mapperly migration

- Audit REST endpoints, GraphQL types/resolvers, commands, queries, and integration boundaries so no domain entity is serialized or returned as a public transport contract.
- Preserve existing DTO field names, nullability, nesting, collection shapes, pagination, routes, and response status codes.
- Add stable `Riok.Mapperly` 4.3.1 as a build-time shared dependency and remove Mapster after all usages are migrated.
- Create module-owned static partial Mapperly mappers near each module’s DTO/application boundary. Do not create one cross-module mapper or add mapper DI where static generated mappings suffice.
- Replace every `.Adapt<T>()` call and handwritten property-copy conversion, including `AccountMapping`, with generated mappings.
- Keep semantic behavior that is not object conversion—such as the empty account’s default preferences—in an explicitly named DTO/default factory, then compose it with generated mappings.
- Configure explicit Mapperly mappings for differently named, flattened, nested, immutable, or factory-created targets. Treat unmapped required members as build diagnostics rather than silently dropping data.
- Remove Mapster package references, global usings, comments, and obsolete mapping helpers after migrated tests pass.

### 4. Documentation

- Repair formatting/encoding issues in `src/Shared/shared.md` and update it to accurately describe shared project responsibilities and the libraries used, including Mapperly’s build-time role and the rule that HTTP identity implementations remain in the bootstrapper.
- Add `src/Modules/module.md` as a reusable module guide covering bounded-context organization, dependency rules, request flow, authorization, persistence, mapping, tests, and adding a module.
- Update existing documentation examples and architecture tests so they use the new namespaces and conventions.

## Public Interfaces and Compatibility

- Internal CLR namespaces change from `*.Models` to `*.Domain`; callers and tests inside the repository must update accordingly.
- Aggregate/entity public constructors may be replaced by factories or aggregate methods; tests and seeders must use the supported creation APIs.
- No external REST, GraphQL, database, cache-key, Keycloak claim, or integration-event contract may change.
- `ICurrentUser`, `ICurrentTenant`, their lifetimes, and HTTP claim-resolution behavior remain unchanged.
- Authorization policy names remain stable where observable; newly module-owned constants retain existing policy string values.

## Test Plan

- After every stage, build the full solution and run all .NET tests.
- Add or update domain, architecture, authorization, and mapping tests.
- Verify EF reports no pending model changes and no migration is generated by namespace-only refactoring.
- Run the existing Keycloak multi-tenancy verifier and Angular boundary/build checks when infrastructure is available.
- Confirm there are no remaining Mapster references, `.Adapt<T>()` calls, or handwritten property-copy mapping helpers.

## Assumptions

- All improvements are delivered in four reviewable stages.
- Endpoint policies are supplemented by application/resource checks; domain objects do not consume ASP.NET authorization services.
- Granular per-property authorization is excluded.
- Existing HTTP adapters remain in Bootstrapper.Api.
- Full external compatibility takes precedence over cleanup that would change persisted or serialized contracts.
