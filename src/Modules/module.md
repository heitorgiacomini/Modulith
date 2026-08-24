# Module architecture

Each directory below `Modules` is an independently owned bounded context. A module owns its domain model, use cases, transport adapters, persistence, authorization rules, and tests. Modules may depend on the Shared projects and explicit contract projects, but they must not reference another module's implementation assembly.

## Standard layout

```text
Module
├── <Feature Area>
│   ├── Domain          aggregates, entities, value objects, and domain events
│   ├── Dtos            transport/application data contracts
│   ├── Features        Carter endpoints plus CQRS commands, queries, and handlers
│   ├── Mapping         module-owned Mapperly declarations
│   ├── Authorization   module policies and resource permission evaluators
│   └── GraphQL         schema-specific query and result types
├── Data                DbContext, EF configurations, migrations, repositories, outbox
├── <Module>Module.cs   service and application-pipeline registration
└── GlobalUsings.cs
```

Infrastructure records, including outbox messages, belong under `Data`; they are not domain entities. Historical migrations retain the CLR names captured when they were generated.

## Request and data flow

1. A Carter endpoint or GraphQL resolver validates the transport boundary and requires the module's coarse authorization policy.
2. The endpoint sends a DTO-based command or query through MediatR.
3. The handler re-evaluates resource ownership or business permissions when access depends on the requested data.
4. The handler invokes aggregate behavior and persists through the module DbContext or repository.
5. Domain events are dispatched in-process; integration events use contracts from `Shared.Messaging`.
6. Mapperly converts domain/application results to DTOs before a REST or GraphQL response crosses the module boundary.

Domain code does not depend on `HttpContext`, claims, ASP.NET authorization services, another module, or transport DTOs. Aggregate roots create and mutate their child entities. Private parameterless constructors support EF materialization; factories enforce creation invariants.

## Authorization

Bootstrapper owns authentication, tenant middleware, the authenticated tenant-member fallback, and transport-specific request identity. A module owns policies that describe its operations—for example Catalog administration or Ordering RPT scopes—and registers them in `Add<Module>Module`.

Endpoint authorization is the first gate. It does not replace application checks: handlers and permission evaluators enforce ownership and resource-sensitive scopes again so commands and queries remain safe when invoked outside HTTP endpoints. Data filters isolate tenants but are not authorization grants.

## Persistence and tenancy

Every business DbContext applies module configurations and the shared soft-delete and multi-tenant filters. Register audit, multi-tenant ownership, and domain-event interceptors with the DbContext. Tenant-owned business entities implement `IMultiTenant`; infrastructure records opt in only when they are genuinely tenant-filtered.

Integration events and background work carry a tenant identifier explicitly and restore `ICurrentTenant` for the processing scope. Cache keys include the tenant identifier. Database schema changes require migrations; folder or namespace refactors must not create schema changes.

## DTOs and mapping

REST responses, GraphQL result types, commands that cross a transport boundary, and integration events must not serialize domain entities. Preserve DTO wire names, nullability, nesting, and collection shapes.

Each module declares static partial Mapperly mappers near its DTO boundary. Generated mapping is preferred for ordinary member copying, collections, and wrapper responses. Explicit attributes document renamed, nested, constant, or generated values. Small user mapping methods are reserved for transformations Mapperly cannot express directly; semantic defaults belong in named default factories rather than mapping code.

## Testing expectations

- Domain tests cover valid construction, rejected invariants, and aggregate-controlled child behavior.
- Mapping tests cover nested collections, immutable records, nullability, and semantic defaults.
- Authorization tests cover missing identity/tenant context, member and admin roles, own/all scopes, ownership, and cross-tenant denial.
- Persistence tests cover tenant filters, ownership interception, auditing, EF materialization, and migrations.
- Architecture tests prevent implementation dependencies between modules and prevent domain-to-ASP.NET dependencies.
- Contract checks verify REST, GraphQL, integration-event, and database compatibility.

## Adding a module

- Create the standard layout and a module registration class.
- Reference only the Shared projects and explicit contract assemblies that are required.
- Register Carter, MediatR, validation, authorization, DbContext interceptors, GraphQL, messaging, and seeding as applicable.
- Model business state under `Domain`; keep transport and infrastructure types outside it.
- Add module-owned Mapperly mappings and return DTOs at every external boundary.
- Add endpoint policies and application-level ownership checks.
- Add unit, persistence, authorization, mapping, architecture, and contract tests.
- Register the assembly and startup/migration hooks in Bootstrapper.
