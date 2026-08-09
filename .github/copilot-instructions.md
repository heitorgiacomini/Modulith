# Copilot instructions (MehmedCourse — EShop modular monolith)

- Always check the current working directory before running commands. The .NET solution root is **`src/`** (not the git repo root) — `src/eshop-modular-monilith.slnx`. `docker compose`, `dotnet run`, and `dotnet ef` all need to be run from `src/`. The Angular client lives in `src/web-client` and its own `npm` commands run from there.
- This is the **single canonical** copilot-instructions file for this repo (a duplicate previously existed at `src/.github/copilot-instructions.md` and has been removed).

## Big picture
- This is a **modular monolith**: the HTTP host lives in `src/Bootstrapper/Api`, feature code lives in `src/Modules/*` (Catalog, Basket, Ordering), and cross-cutting building blocks live in `src/Shared/*`.
- Each module exposes two extension methods:
  - `AddXModule(IServiceCollection, IConfiguration)` for DI registration.
  - `UseXModule(IApplicationBuilder)` for app-startup hooks (EF migrations via `UseMigration<TDbContext>()`, seeding, etc.).
- The frontend (`src/web-client`, Angular + PrimeNG) talks to the API's **GraphQL** endpoint for Catalog data — see the "Frontend" section below.

## ⚠️ Module folder layout is NOT uniform — verify before assuming a path
Don't assume every module mirrors the same nesting. Actual layout:
- **Catalog**: double-nested project folder + a separate contracts project:
  - `src/Modules/Catalog/Catalog/CatalogModule.cs` (module root)
  - `src/Modules/Catalog/Catalog/Products/Features/CreateProduct/CreateProductHandler.cs` (feature slice)
  - `src/Modules/Catalog/Catalog.Contracts/*` — DTOs/contracts shared with the API host's GraphQL layer.
- **Basket**: module root files are flat, but feature/domain code is one level deeper:
  - `src/Modules/Basket/BasketModule.cs`, `src/Modules/Basket/Data/*` (flat)
  - `src/Modules/Basket/Basket/Features/CheckoutBasket/CheckoutBasketHandler.cs` (nested)
- **Ordering**: fully flat, no double nesting:
  - `src/Modules/Ordering/OrderingModule.cs`, `src/Modules/Ordering/Orders/Features/CreateOrder/CreateOrderHandler.cs`
- **Shared**: the actual project is double-nested (`Shared/Shared/...`), but `Shared.Messaging` and `Shared.Contracts` are NOT:
  - `src/Shared/Shared/DDD/Aggregate.cs`, `src/Shared/Shared/Behaviors/ValidationBehavior.cs`, `src/Shared/Shared/Data/Interceptors/DispatchDomainEventsInterceptor.cs`, `src/Shared/Shared/Exceptions/Handler/CustomExceptionHandler.cs`
  - `src/Shared/Shared.Messaging/Events/IntegrationEvent.cs`
- When in doubt, glob/search for the file before writing a path in code, commands, or docs.

## HTTP + endpoints
- REST: Minimal APIs + Carter. Endpoints are `ICarterModule` classes colocated per feature (example: `Modules/Catalog/Catalog/Products/Features/GetProducts/GetProductsEndpoint.cs`).
- Carter modules are registered via `AddCarterWithAssemblies(...)` in `Bootstrapper/Api/Program.cs`, passed all module assemblies (same pattern for `AddMediatRWithAssemblies` and `AddMassTransitWithAssemblies`).
- **GraphQL**: HotChocolate is exposed at `/graphql` via `MapGraphQL()` in `Bootstrapper/Api/Program.cs`, but each module owns its registration and resolvers: `AddCatalogModule`, `AddBasketModule`, and `AddOrderingModule` call `AddGraphQLServer()` and their `AddXGraphQL()` extension. Query fields are module-owned extensions of the shared root Query type (`Modules/<Module>/.../GraphQL/*`). List resolvers expose `AsNoTracking()` read DTO projections with `[UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]`, filtering, and sorting.
- There is **no OData** in this codebase — GraphQL replaced it for Catalog querying. Don't reintroduce OData docs/config unless the code actually adds it back.
- Auth (Keycloak) is fully wired: `AddKeycloakWebApiAuthentication` + `AddAuthorization`, and `UseAuthentication()` / `UseAuthorization()` **are enabled** in the pipeline — always check the current `Program.cs` before assuming middleware is disabled/commented out.

## Vertical Slice + CQRS conventions
- Features are organized by slice under `<ModuleProjectRoot>/<Area>/Features/<UseCase>/` (see folder-layout warning above for what `<ModuleProjectRoot>` actually is per module).
- A typical slice has:
  - `*Endpoint.cs` (Carter route definitions, calls `ISender`)
  - `*Handler.cs` that co-locates `record` command/query + result + FluentValidation validator + handler class.
  - Example: `Modules/Catalog/Catalog/Products/Features/CreateProduct/CreateProductHandler.cs`.
- CQRS marker interfaces live in `Shared/Shared/*`:
  - Commands implement `ICommand<TResponse>` and handlers implement `ICommandHandler<TCommand, TResponse>`.
  - Queries implement `IQuery<TResponse>` and handlers implement `IQueryHandler<TQuery, TResponse>`.
- Mapping between request/result/response DTOs commonly uses **Mapster** (`request.Adapt<...>()`).

## Persistence + domain events
- Modules use EF Core + PostgreSQL with schemas: Catalog ("catalog"), Basket ("basket"), Ordering ("ordering").
- On app startup, `UseXModule()` calls `UseMigration<XDbContext>()` which runs EF migrations and seeds data.
- Domain events:
  - Aggregates inherit `Shared.DDD.Aggregate<TId>` (`Shared/Shared/DDD/Aggregate.cs`) and call `AddDomainEvent(...)` (see `Modules/Catalog/Catalog/Products/Models/Product.cs`).
  - Domain events are dispatched via EF `SaveChangesInterceptor` (`Shared/Shared/Data/Interceptors/DispatchDomainEventsInterceptor.cs`) using MediatR `Publish`.

## Messaging + integration events
- Async communication between modules uses MassTransit + RabbitMQ.
- Integration events inherit from `Shared.Messaging.Events.IntegrationEvent` (`Shared/Shared.Messaging/Events/IntegrationEvent.cs`, e.g. `BasketCheckoutIntegrationEvent`).
- Outbox pattern for reliable messaging: events stored in outbox table, published by `OutboxProcessor` background service (see `Modules/Basket/Data/Processors/OutboxProcessor.cs`).
- Consumers registered via `AddMassTransitWithAssemblies` in `Program.cs`.

## Caching
- Basket module uses Redis for distributed caching with `IDistributedCache`.
- Repository decorated with `CachedBasketRepository` using Decorator pattern (see `Modules/Basket/Data/Repository/CachedBasketRepository.cs`, wired in `Modules/Basket/BasketModule.cs`).

## Validation + exceptions
- FluentValidation runs through a MediatR pipeline behavior for commands (`Shared/Shared/Behaviors/ValidationBehavior.cs`). Modules wire it in `XModule.AddXModule()` with `cfg.AddOpenBehavior(typeof(ValidationBehavior<,>))`.
- Global exceptions are formatted as `ProblemDetails` via `Shared/Shared/Exceptions/Handler/CustomExceptionHandler.cs` (registered in `Bootstrapper/Api/Program.cs`).

## Logging
- Serilog is the default logging stack; config is read from `appsettings.json` / environment variables and includes a Seq sink.

## Frontend (Angular web-client)
- `src/web-client` is Angular (standalone components) + PrimeNG (Aura theme) + primeicons. It talks to the API's GraphQL endpoint (see `src/web-client/src/environments/environment.ts` → `graphqlUrl`).
- Data tables use PrimeNG `p-table` in lazy mode. PrimeNG's `LazyLoadEvent`/`TableLazyLoadEvent` (pagination, sort, per-column filters) are translated into GraphQL `skip/take/where/order` variables by a **generic, reusable** builder service — not entity-specific:
  - `src/web-client/src/app/shared/graphql/graphql-query-builder.service.ts` (`GraphqlQueryBuilderService`).
  - Reuse this service for any new entity's GraphQL list query instead of re-implementing PrimeNG filter/sort mapping.
- Follow the pattern in `src/app/catalog/catalog.service.ts` (query built once via `GraphqlQueryBuilderService.buildQuery(...)`, variables via `buildCollectionVariables(event, defaultPageSize)`) and `src/app/pages/products/products-page.component.ts` (a `p-table` with `[lazy]="true"` + `(onLazyLoad)`) when adding new feature pages.

## Dev workflows (Windows)
- Run all commands from `src/` unless noted otherwise.
- Run dependencies: `docker compose up -d` (Postgres, Redis, Seq, RabbitMQ, Keycloak defined in `docker-compose.yml` / `docker-compose.override.yml`).
- Run API host: `dotnet run --project Bootstrapper/Api/Api.csproj`.
- Run Angular client: `cd web-client && npm start` (serves on `http://localhost:4200`).
- Add a migration for a module — note the project path differs per module (see folder-layout warning above):
  - Catalog: `dotnet ef migrations add <Name> -p Modules/Catalog/Catalog/Catalog.csproj -s Bootstrapper/Api/Api.csproj`
  - Basket: `dotnet ef migrations add <Name> -p Modules/Basket/Basket.csproj -s Bootstrapper/Api/Api.csproj`
  - Ordering: `dotnet ef migrations add <Name> -p Modules/Ordering/Ordering.csproj -s Bootstrapper/Api/Api.csproj`
- Local ports (see `docker-compose.override.yml`): API `http://localhost:5001` / `https://localhost:6060` (container ports 8080/8081), web-client `http://localhost:4200`, Postgres `5434`, Redis `6379`, Seq ingestion `5341` / UI `9091`, RabbitMQ amqp `5672` / mgmt UI `15672`, Keycloak `9090`.

## Conventions to follow when adding code
- Keep module code inside its module; put shared primitives in `Shared/`. Always verify the actual folder nesting for the target module first (see warning above) instead of assuming symmetry.
- Prefer adding new endpoints as `ICarterModule` in the relevant feature folder, and call MediatR via `ISender`. For read-heavy/flexible querying use-cases (filtering, sorting, paging from a UI grid), consider exposing a GraphQL query in `Bootstrapper/Api/GraphQL/*` instead of a bespoke REST endpoint.
- Prefer DDD creation/update methods and raise domain events via `AddDomainEvent(...)` when behavior changes. For cross-module async communication, use outbox to publish integration events.
- On the Angular side, keep GraphQL query building generic via `GraphqlQueryBuilderService` — don't hardcode query strings or duplicate PrimeNG filter/sort mapping per feature/service.

