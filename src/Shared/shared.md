# Shared architecture

This directory contains the reusable building blocks used by the Accounts, Basket, Catalog, and Ordering modules. Shared provides technical contracts and cross-cutting infrastructure; it does not own business entities, business workflows, or a database schema.

## Projects

| Project | Responsibility | Typical consumers |
| --- | --- | --- |
| `Shared.Contracts` | Minimal CQRS contracts built on MediatR | Commands, queries, and their handlers in every module |
| `Shared` | DDD primitives, auditing, EF Core filters and interceptors, pipeline behaviors, exceptions, pagination, and startup helpers | Bootstrapper and module projects |
| `Shared.Messaging` | Integration-event contracts and MassTransit/RabbitMQ registration | Modules that publish or consume cross-module events |

## Core libraries

| Library | Role |
| --- | --- |
| MediatR | Dispatches commands, queries, domain events, and pipeline behaviors |
| Carter | Discovers and maps module-owned minimal API endpoints |
| FluentValidation | Validates commands in the MediatR pipeline |
| Entity Framework Core and Npgsql | Provide module-owned PostgreSQL persistence, filters, interceptors, and migrations |
| MassTransit | Publishes and consumes integration events through RabbitMQ |
| Hot Chocolate | Hosts the module GraphQL schemas |
| Mapperly | Generates compile-time mappings inside each module; Shared carries the common build dependency but owns no business mapper |
| Serilog | Emits structured application and audit logs from the bootstrapper and shared infrastructure |

The dependency direction should remain toward these projects. Shared must not reference Accounts, Basket, Catalog, Ordering, or Bootstrapper. HTTP-specific implementations belong in Bootstrapper, while business-specific implementations belong in their module.

## DDD entity contracts

The entity hierarchy separates capabilities instead of making every entity automatically audited or soft-deletable.

```text
IEntity                         marker
└── IEntity<TId>                Id

ICreationAuditedObject          CreatedAt, CreatedBy
IModificationAuditedObject      LastModified, LastModifiedBy
└── IAuditedObject              creation + modification

ISoftDelete                     IsDeleted
└── IDeletionAuditedObject      IsDeleted, DeletedAt, DeletedBy

IFullAuditedObject              IAuditedObject + IDeletionAuditedObject
```

Available base classes:

| Base class | Capabilities |
| --- | --- |
| `Entity<TId>` | Identity only |
| `AuditedEntity<TId>` | Identity, creation audit, and modification audit |
| `FullAuditedEntity<TId>` | Identity, full audit, and soft delete |
| `Aggregate<TId>` | Identity and domain events |
| `AuditedAggregate<TId>` | Aggregate with creation and modification audit |
| `FullAuditedAggregate<TId>` | Aggregate with full audit and soft delete |

`TId` must be a value type. Actor fields are nullable `Guid` values and timestamps are `DateTimeOffset`. A null actor represents a background process, migration, seed, message consumer, or unauthenticated execution.

`ISoftDelete` and `IMultiTenant` are independent contracts in their own files. `IMultiTenant` has nullable `Guid? TenantId`, matching ABP-style host compatibility. Business entities configure the column as required; infrastructure entities opt out by not implementing the interface.

Choose the narrowest base that matches the entity:

```csharp
public sealed class Product : FullAuditedAggregate<Guid>
{
  // Product is audited, dispatches domain events, and is soft-deleted.
}

public sealed class OutboxMessage : Entity<Guid>
{
  // Infrastructure record: no automatic auditing or soft-delete filter.
}
```

Interface inheritance is deliberate. For example, `IFullAuditedObject` is assignable to `ISoftDelete` through `IDeletionAuditedObject`, so full-audited entities automatically receive the soft-delete query filter.

## Domain events

`Aggregate<TId>` stores an in-memory list of `IDomainEvent` instances. Aggregates call `AddDomainEvent` while enforcing business behavior. `DispatchDomainEventsInterceptor` runs during `SaveChanges`, finds tracked `IAggregate` instances, copies and clears their events, then publishes each event through MediatR.

Domain events are in-process notifications. They are different from integration events in `Shared.Messaging`, which cross module or process boundaries through RabbitMQ.

## Current user

`ICurrentUser` is the shared, transport-neutral view of the current identity:

```csharp
public interface ICurrentUser
{
  Guid? Id { get; }
  string? UserName { get; }
  bool IsAuthenticated { get; }
  string? TraceId { get; }
}
```

Shared declares only the abstraction. `HttpContextCurrentUser` is implemented and registered in Bootstrapper because Shared must not depend on the current HTTP transport.

Mapperly follows the same ownership rule: the package version is shared as build infrastructure, while mapper declarations stay in the module that owns the source and target contracts. Mapping is generated at compile time and requires no runtime mapper registration.

The HTTP implementation follows these rules:

- `Id` comes exclusively from the Keycloak `sub` claim and is parsed as `Guid?`.
- A missing or invalid `sub` produces `null`; no other claim replaces it.
- `UserName` comes from `preferred_username`, with `Identity.Name` as a presentation fallback.
- Audit columns use only `Id`; usernames are never persisted as authoritative audit identities.
- `TraceId` uses the active `Activity` identifier, with the HTTP trace identifier as fallback.
- Executions without an HTTP context naturally return a null actor and trace where unavailable.

Bootstrapper registration:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

Endpoints may inject `ICurrentUser` for consistent identity resolution, but each module remains responsible for its own authorization rules.

## Automatic entity auditing

`AuditableEntityInterceptor` is an EF Core `SaveChangesInterceptor`. It processes capabilities independently:

| Entity state/capability | Behavior |
| --- | --- |
| Added + `ICreationAuditedObject` | Sets `CreatedAt` to UTC and `CreatedBy` to `ICurrentUser.Id` |
| Modified + `IModificationAuditedObject` | Sets `LastModified` and `LastModifiedBy` |
| Deleted + `ISoftDelete` | Converts the entry to `Modified` and sets `IsDeleted = true` |
| Deleted + `IDeletionAuditedObject` | Also sets `DeletedAt` and `DeletedBy` on the first deletion |
| Deleted without `ISoftDelete` | Leaves the entry deleted, resulting in a physical delete |
| Changed owned data + `IModificationAuditedObject` owner | Updates the owning entity's modification metadata |

Creation metadata is not overwritten during modification or deletion. Existing deletion metadata is preserved when an already deleted object is encountered.

### Structured audit events

Before saving, the interceptor captures pending changes. It emits the structured log only after EF reports a successful save. Failed or canceled saves discard pending events. Events include:

- module, derived from the DbContext name;
- entity type and ID;
- `Created`, `Modified`, or `Deleted` operation;
- actor GUID;
- active Keycloak organization UUID as `TenantId`;
- UTC timestamp;
- request/activity trace ID;
- old and new values for changed properties.

Only `IAuditedObject` entities emit detailed audit events. Audit metadata fields and primary keys are excluded from the change dictionary. Values are redacted when a property is marked with `[AuditSensitive]` or its name indicates passwords, secrets, tokens, authorization data, payment/card information, email, phone, or address data.

Serilog receives these structured events and sends them to the configured sinks, including Seq. There is no audit-history entity or audit database table.

Each module registers the three EF interceptors and attaches all registered `ISaveChangesInterceptor` instances to its DbContext:

```csharp
services.TryAddEnumerable(
  ServiceDescriptor.Scoped<ISaveChangesInterceptor, AuditableEntityInterceptor>());
services.TryAddEnumerable(
  ServiceDescriptor.Scoped<ISaveChangesInterceptor, MultiTenantEntityInterceptor>());
services.TryAddEnumerable(
  ServiceDescriptor.Scoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>());

services.AddDbContext<ModuleDbContext>((provider, options) =>
{
  options.AddInterceptors(provider.GetServices<ISaveChangesInterceptor>());
  options.UseNpgsql(connectionString);
});
```

## Typed data filters

The data-filter API controls query visibility by interface type, not by a string name.

### Responsibilities

| Type | Purpose |
| --- | --- |
| `IDataFilter` | Non-generic entry point used to enable, disable, or inspect a filter type dynamically |
| `IDataFilter<TFilter>` | State holder for one filter capability |
| `DataFilter` | Singleton resolver that caches the generic filter instance for each filter type |
| `DataFilter<TFilter>` | Uses `AsyncLocal` to hold the current flow's override |
| `DataFilterOptions` | Stores application-wide default states by interface `Type` |
| `IDataFilterContext` | Exposes soft-delete, multi-tenant, and current-tenant parameters to EF query expressions |
| `AddDataFilters()` | Registers filter services and `ICurrentTenant`; enables `ISoftDelete` and `IMultiTenant` by default |

Bootstrapper must register the services once:

```csharp
services.AddDataFilters();
```

`AddDataFilters()` configures both capability filters as enabled by default. Applications can override a default during service registration without changing the public, strongly typed API:

```csharp
services.Configure<DataFilterOptions>(options =>
{
  options.DefaultStates[typeof(ISoftDelete)] = true;
  options.DefaultStates[typeof(IMultiTenant)] = true;
});
```

An interface type is the filter key. No string filter names are exposed or compared at runtime.

Every business DbContext injects `IDataFilter` and `ICurrentTenant`, implements `IDataFilterContext`, and applies the model filters:

```csharp
public sealed class ModuleDbContext(
  DbContextOptions<ModuleDbContext> options,
  IDataFilter dataFilter,
  ICurrentTenant currentTenant) : DbContext(options), IDataFilterContext
{
  public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();
  public bool IsMultiTenantFilterEnabled => dataFilter.IsEnabled<IMultiTenant>();
  public Guid? CurrentTenantId => currentTenant.Id;

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.ApplyConfigurationsFromAssembly(typeof(ModuleDbContext).Assembly);
    builder.ApplyDataFilters(this);
    base.OnModelCreating(builder);
  }
}
```

`ApplyDataFilters` scans the EF model with both capability checks:

```csharp
typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType)
typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType)
```

It combines the applicable predicates on each entity type:

```csharp
!IsSoftDeleteFilterEnabled || !entity.IsDeleted
!IsMultiTenantFilterEnabled || entity.TenantId == CurrentTenantId
```

Normal queries hide deleted rows and rows owned by other organizations. `OutboxMessage` implements neither capability, so it receives no query filter.

### Temporarily disabling soft delete

Use a scoped disable operation for trusted administration or restoration code:

```csharp
public async Task<IReadOnlyList<Product>> GetIncludingDeleted(
  IDataFilter dataFilter,
  CatalogDbContext dbContext,
  CancellationToken cancellationToken)
{
  using (dataFilter.Disable<ISoftDelete>())
  {
    return await dbContext.Products.ToListAsync(cancellationToken);
  }
}
```

Disposing the returned object restores the exact previous state. This works with nested scopes, exceptions, and `async/await`. State follows the asynchronous execution context and does not globally disable filtering for concurrent requests.

`IgnoreQueryFilters()` remains EF Core's global escape hatch and bypasses every EF query filter. Prefer `IDataFilter.Disable<ISoftDelete>()` when only the soft-delete capability should be controlled.

The filter affects reads only. It does not delete, restore, mutate, or authorize an entity; the audit interceptor and module use cases handle those concerns.

### Trusted host visibility

An unavailable current tenant means `ICurrentTenant.Id` is `null`. With the tenant filter enabled, that produces `entity.TenantId == null`; because module business columns are required, ordinary host-context queries return no tenant-owned business rows.

Trusted infrastructure that must inspect every organization can disable only the tenant capability:

```csharp
using (dataFilter.Disable<IMultiTenant>())
{
  IReadOnlyList<Product> allOrganizations =
    await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);
}
```

This expands query visibility only. It does not grant authorization and it does not bypass `MultiTenantEntityInterceptor`: a tenant-owned write still requires an active matching tenant. `IgnoreQueryFilters()` is broader because it disables tenant and soft-delete predicates together.

## Keycloak Organizations and current tenant

Keycloak is the only source of tenant metadata, membership, and organization-specific roles. The application uses the organization UUID directly as `TenantId`; there is no local tenant registry or ID-mapping table. The `eshoprealm` development import defines Acme (`11111111-1111-1111-1111-111111111111`) and Contoso (`22222222-2222-2222-2222-222222222222`). Shared contains no sample-tenant constants; Catalog receives development seed tenant IDs through `Catalog:SeedTenantIds` configuration and derives stable UUID-v5 product IDs from each `(TenantId, product key)` pair.

The Angular client requests the parameterized `organization` scope. A user with multiple memberships selects exactly one organization during authorization and receives a new token when switching. The API never accepts a tenant from a header, route, query string, cookie, command, or DTO.

A selected organization claim has one keyed entry. The key is presentation metadata; the nested UUID is authoritative:

```json
{
  "acme": {
    "id": "11111111-1111-1111-1111-111111111111",
    "realm_access": {
      "roles": ["admin", "customer"]
    }
  }
}
```

`CurrentTenantMiddleware` runs after authentication and before authorization. It accepts only a signed `organization` claim with exactly one entry and a non-empty GUID `id`. The alias is presentation metadata; the UUID is authoritative. Authenticated business requests with missing, invalid, or multiple organizations receive a 403 Problem Details response. GraphQL schema documents used by health and composition remain tenant-neutral.

`ICurrentTenant` is transport-neutral:

```csharp
public interface ICurrentTenant
{
  Guid? Id { get; }
  string? Name { get; }
  bool IsAvailable { get; }
  IDisposable Change(Guid? id, string? name = null);
}
```

`CurrentTenant` stores state with `AsyncLocal`. `Change` supports nested scopes, flows through `async/await`, restores the previous value after disposal or exceptions, and isolates concurrent execution flows. `Change(null)` enters host context; `Guid.Empty` is rejected.

### Write ownership enforcement

`MultiTenantEntityInterceptor` applies to tracked `IMultiTenant` entities:

- an active tenant is required for added, modified, and deleted business rows;
- an unset `TenantId` is assigned from `ICurrentTenant` on insert;
- a supplied tenant must match the active tenant;
- changing ownership after insertion is rejected;
- records loaded with filtering disabled still cannot be changed under another tenant;
- infrastructure entities without `IMultiTenant` remain unchanged.

This is the write-side complement to the EF query filter. Neither is an authorization grant. `IDataFilter.Disable<IMultiTenant>()` is a trusted host-only visibility escape hatch; ownership validation remains active.

### Background work, messaging, and caches

Background work has no HTTP middleware, so tenant context travels with the work:

- every `IntegrationEvent` carries a required `TenantId`;
- `OutboxMessage` stays outside tenant filtering but carries a non-null routing `TenantId`;
- publishers copy `ICurrentTenant.Id` into the event and outbox metadata;
- consumers and the outbox processor wrap database work in `using (currentTenant.Change(message.TenantId))`;
- disposal prevents tenant context leaking to the next message;
- legacy pending outbox payloads recover the tenant from the backfilled routing column;
- Redis keys are organization-prefixed, for example `{tenantId}:basket:{username}`;
- seeders enter explicit tenant scopes; Catalog seeds distinct data for Acme and Contoso.

### Organization-specific authorization

The Organization Membership mapper includes the organization ID. The Organization Group Membership mapper includes group paths and group-derived roles inside the selected organization. Tenant member policies accept `customer` or `admin`; Catalog writes require `admin`.

Ordering keeps Keycloak Authorization Services and RPT scopes. The API also intersects granted scopes with roles nested in the selected organization: `*-own` requires `customer` or `admin`, and `*-all` requires `admin`. A role held in Acme therefore cannot authorize Contoso data. "All" always means all records in the active tenant.

The custom Keycloak image packages two JavaScript authorization-policy providers. They evaluate `customer` and `admin` only inside the single organization entry from the signed source access token. A protocol-mapper provider copies that same organization claim into the issued Ordering RPT, so the API can establish the identical tenant context after the entitlement exchange. These providers use Keycloak's `scripts` feature and are built into the image as a provider JAR; no policy source is stored in the realm database. See the [Keycloak script-provider documentation](https://www.keycloak.org/docs/latest/server_development/index.html#_script_providers).

`verify-multitenancy.mjs` runs inside the Compose network and exercises real Acme and Contoso access tokens and RPTs. It verifies GraphQL isolation for Catalog, Basket, and Ordering; cross-tenant REST 404 responses; rejected cross-tenant and non-admin deletes; ignored `X-Tenant-Id` values; and 403 Problem Details when the signed token has no organization.

### Tenant switching

Tenant switching is token switching. Angular starts a new Keycloak authorization request for the selected organization, clears tenant-specific UI state and cached Ordering permission tokens, and rebuilds its auth state from the newly issued signed claims. The API deliberately has no runtime tenant-switch endpoint and does not accept `X-Tenant-Id`.

An Ordering RPT is tenant-bound as well. The custom mapper copies the signed source token's single organization entry into the RPT, while the authorization policies grant `*-own` or `*-all` only from the roles effective in that same entry.

## CQRS and MediatR pipeline

`Shared.Contracts` provides:

- `ICommand` and `ICommand<TResponse>`;
- `ICommandHandler<TCommand>` and `ICommandHandler<TCommand, TResponse>`;
- `IQuery<TResponse>`;
- `IQueryHandler<TQuery, TResponse>`.

`AddMediatRWithAssemblies` registers handlers from the supplied module assemblies, discovers FluentValidation validators, and adds two open pipeline behaviors:

1. `ValidationBehavior` runs validators for commands and throws `ValidationException` when any rule fails.
2. `LoggingBehavior` logs request start/end and warns when handling exceeds three seconds.

Queries do not pass through `ValidationBehavior` because it is constrained to `ICommand<TResponse>`.

## HTTP endpoint discovery and errors

`AddCarterWithAssemblies` finds concrete `ICarterModule` implementations in the supplied module assemblies and registers them. Bootstrapper later calls `MapCarter()` to expose their routes.

`CustomExceptionHandler` maps known exceptions to Problem Details responses:

| Exception | HTTP status |
| --- | --- |
| `ValidationException` | 400 |
| `BadRequestException` | 400 |
| `NotFoundException` | 404 |
| `InternalServerException` | 500 |
| Any unrecognized exception | 500 |

Responses include the request path and trace ID. Validation responses also include validation failures.

## Database migrations and seeding

`UseMigration<TContext>()` creates a scope, runs `Database.MigrateAsync()` for the requested DbContext, and then resolves and runs all registered `IDataSeeder` implementations.

Each module exposes a `Use...Module()` startup method that invokes this helper for its DbContext. Bootstrapper calls these methods when `Database:RunMigrations` is enabled.

Because all registered seeders are resolved each time `UseMigration<TContext>()` runs, seed implementations must be idempotent.

## Integration messaging

`Shared.Messaging` contains contracts intended to cross module boundaries:

- `BasketCheckoutIntegrationEvent` carries checkout customer, address, payment, and item data.
- `ProductPriceChangedIntegrationEvent` carries the updated product information.
- `IntegrationEvent` supplies event metadata, including the canonical organization `TenantId`.

`AddMassTransitWithAssemblies`:

- uses kebab-case endpoint names;
- discovers consumers, sagas, state machines, and activities from module assemblies;
- configures RabbitMQ from `MessageBroker:Host`, `MessageBroker:UserName`, and `MessageBroker:Password`;
- configures discovered endpoints automatically.

Integration contracts should remain backward-compatible because publishers and consumers can be deployed or executed independently.

## Verification workflow

Run the application and infrastructure from the `src` directory so Compose can resolve its files and volumes:

```powershell
cd src
docker compose up --build -d
docker compose ps --all
```

Run the real-token multi-tenancy acceptance verifier inside the Compose network:

```powershell
docker compose run --rm --no-deps keycloak-verifier node /verify-multitenancy.mjs
```

Run the automated .NET and Angular checks from the repository root and the running web container:

```powershell
dotnet test src/eshop-modular-monilith.slnx --no-restore --nologo -v:minimal
cd src
docker compose exec -T web-client npm run check:boundaries
docker compose exec -T web-client npm run build
```

The verifier must show one canonical organization per access token and RPT, non-empty disjoint Catalog result sets for the configured development tenants, rejected cross-tenant reads and deletes, ignored tenant headers, and 403 Problem Details for a token without organization context.

## Pagination and utility types

`PaginationRequest` provides zero-based `PageIndex` and a default `PageSize` of 10. `PaginatedResult<TEntity>` returns the page index, page size, total count, and current data.

`StringExtension.HasContent()` is a null/empty check. It does not treat whitespace-only strings as empty.

## Adding a new module

Use this checklist when integrating another module:

- [ ] Reference only the Shared projects the module needs.
- [ ] Choose entity bases by capability; do not make infrastructure records full-audited by default.
- [ ] Register the module assembly with Carter and MediatR in Bootstrapper.
- [ ] Register MassTransit discovery only when the module publishes or consumes integration messages.
- [ ] Inject `IDataFilter` and implement `IDataFilterContext` in the module DbContext.
- [ ] Inject `ICurrentTenant` and expose `CurrentTenantId` from the module DbContext.
- [ ] Call `ApplyDataFilters(this)` during model creation.
- [ ] Register and attach the audit, multi-tenant, and domain-event EF interceptors.
- [ ] Implement `IMultiTenant` on every tenant-owned aggregate and directly queryable child.
- [ ] Carry and restore `TenantId` in integration events, outbox processing, and cache keys.
- [ ] Add an idempotent `IDataSeeder` only when seed data is required.
- [ ] Expose module-specific service and startup extension methods.
- [ ] Keep HTTP identity implementation in Bootstrapper and authorization policy in the owning module.
- [ ] Add tests for auditing, filtering, domain events, migrations, and module boundaries.

## Important invariants

- Shared contains no domain-specific aggregate or audit-history table.
- Keycloak `sub` parsed as a GUID is the only persisted user identity for auditing.
- Audit timestamps use UTC.
- Soft deletion is selected by `ISoftDelete`, including through inherited interfaces.
- Tenant ownership is selected by `IMultiTenant`, including through inherited interfaces.
- The Keycloak organization UUID is canonical and is never remapped or accepted from client input.
- Every business read is scoped by the active tenant and every business write validates ownership.
- Outbox rows are host-visible infrastructure records but always carry tenant routing context.
- Data-filter state is scoped to the asynchronous flow and restored on disposal.
- Disabling a data filter changes visibility, not authorization.
- Detailed audit logs are emitted only after a successful save and redact sensitive values.
- Physical deletion remains available to entities that do not implement `ISoftDelete`.
