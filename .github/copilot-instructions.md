# Copilot instructions (EShop modular monolith)

- Always check the current working directory before running commands. The .NET solution root is **`src/`** (not the git repo root) — `src/eshop-modular-monilith.slnx`. `docker compose`, `dotnet run`, and `dotnet ef` all need to be run from `src/`. The Angular client lives in `src/web-client` and its own `npm` commands run from there.
- This is the **single canonical** Copilot instruction file for this repository. Do not create another `.github` instruction tree under `src/`.

## Big picture
- This is a **.NET 10 modular monolith**: the HTTP host lives in `src/Bootstrapper/Api`, feature code lives in `src/Modules/*` (Accounts, Catalog, Basket, Ordering), and cross-cutting building blocks live in `src/Shared/*`.
- Each module exposes two extension methods:
  - `AddXModule(IServiceCollection, IConfiguration)` for DI registration.
  - `UseXModule(IApplicationBuilder)` for app-startup hooks (EF migrations via `UseMigration<TDbContext>()`, seeding, etc.).
- `src/Bootstrapper/Gateway` hosts the Hot Chocolate Fusion gateway, and `src/web-client` is the Angular + PrimeNG client.

## Architecture rules
- Preserve module boundaries. A module must not reference another module implementation assembly.
- Put intentionally shared cross-module contracts in the appropriate contracts project instead of directly referencing another module's domain or persistence code.
- Keep domain behavior and EF Core access inside the owning module. Bootstrapper projects compose services and transports; they do not own business rules.
- Use `Shared.Messaging` integration events for asynchronous cross-module workflows.
- Treat changes under `src/Shared` as high impact and inspect all consumers before changing public contracts or behavior.
- The frontend uses the Fusion gateway for GraphQL reads and the API host for REST commands; verify `src/web-client/src/environments/environment.ts` before changing transport URLs.

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
- **Accounts**: module, data, and project files are flat, while vertical slices are nested:
  - `src/Modules/Accounts/AccountsModule.cs`, `src/Modules/Accounts/Data/*`
  - `src/Modules/Accounts/Accounts/Features/GetMyAccount/*`
- **Shared**: the actual project is double-nested (`Shared/Shared/...`), but `Shared.Messaging` and `Shared.Contracts` are NOT:
  - `src/Shared/Shared/DDD/Aggregate.cs`, `src/Shared/Shared/Behaviors/ValidationBehavior.cs`, `src/Shared/Shared/Data/Interceptors/DispatchDomainEventsInterceptor.cs`, `src/Shared/Shared/Exceptions/Handler/CustomExceptionHandler.cs`
  - `src/Shared/Shared.Messaging/Events/IntegrationEvent.cs`
- When in doubt, glob/search for the file before writing a path in code, commands, or docs.

## HTTP + endpoints
- REST: Minimal APIs + Carter. Endpoints are `ICarterModule` classes colocated per feature (example: `Modules/Catalog/Catalog/Products/Features/GetProducts/GetProductsEndpoint.cs`).
- Carter modules are registered via `AddCarterWithAssemblies(...)` in `Bootstrapper/Api/Program.cs`, passed all module assemblies (same pattern for `AddMediatRWithAssemblies` and `AddMassTransitWithAssemblies`).
- **GraphQL**: Hot Chocolate exposes module-owned named schemas at `/graphql/catalog`, `/graphql/basket`, and `/graphql/ordering`. The API also exposes anonymous schema documents beneath each endpoint's `/schema.graphqls` path for health checks and Fusion composition. Keep resolvers and registration in their owning module.
- There is **no OData** in this codebase — GraphQL replaced it for Catalog querying. Don't reintroduce OData docs/config unless the code actually adds it back.
- Auth (Keycloak) is fully wired: `AddKeycloakWebApiAuthentication` + `AddAuthorization`, and `UseAuthentication()` / `UseAuthorization()` **are enabled** in the pipeline — always check the current `Program.cs` before assuming middleware is disabled/commented out.

## Vertical Slice + CQRS conventions
- Features are organized by slice under `<ModuleProjectRoot>/<Area>/Features/<UseCase>/` (see folder-layout warning above for what `<ModuleProjectRoot>` actually is per module).
- A typical slice has:
  - `*Endpoint.cs` (Carter route definitions, calls `ISender`)
  - `*Handler.cs` that co-locates `record` command/query + result + FluentValidation validator + handler class.
  - Example: `Modules/Catalog/Catalog/Products/Features/CreateProduct/CreateProductHandler.cs`.
- CQRS marker interfaces live in `src/Shared/Shared.Contracts/CQRS/*`:
  - Commands implement `ICommand<TResponse>` and handlers implement `ICommandHandler<TCommand, TResponse>`.
  - Queries implement `IQuery<TResponse>` and handlers implement `IQueryHandler<TQuery, TResponse>`.
- Mapping between request/result/response DTOs commonly uses **Mapster** (`request.Adapt<...>()`).

## Persistence + domain events
- Modules use EF Core + PostgreSQL with schemas: Accounts (`accounts`), Catalog (`catalog`), Basket (`basket`), and Ordering (`ordering`).
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
- Data tables use PrimeNG `p-table` in lazy mode. PrimeNG's `LazyLoadEvent`/`TableLazyLoadEvent` (pagination, sort, per-column filters) are translated into GraphQL `skip/take/where/order` variables by a **generic, reusable** builder service, not entity-specific code:
  - `src/web-client/src/app/core/graphql/graphql-query-builder.service.ts` (`GraphqlQueryBuilderService`).
  - Reuse this service for any new entity's GraphQL list query instead of re-implementing PrimeNG filter/sort mapping.
- Follow `src/web-client/src/app/contexts/catalog/data-access/catalog.api.ts` and the context-owned feature pages under `src/web-client/src/app/contexts/*/features/` when adding functionality.

## Dev workflows (Windows)
- Run all commands from `src/` unless noted otherwise.
- Run dependencies: `docker compose up -d` (Postgres, Redis, Seq, RabbitMQ, Keycloak defined in `docker-compose.yml` / `docker-compose.override.yml`).
- Run API host: `dotnet run --project Bootstrapper/Api/Api.csproj`.
- Run Angular client: `cd web-client && npm start` (serves on `http://localhost:4200`).
- Add a migration for a module — note the project path differs per module (see folder-layout warning above):
  - Accounts: `dotnet ef migrations add <Name> -p Modules/Accounts/Accounts.csproj -s Bootstrapper/Api/Api.csproj`
  - Catalog: `dotnet ef migrations add <Name> -p Modules/Catalog/Catalog/Catalog.csproj -s Bootstrapper/Api/Api.csproj`
  - Basket: `dotnet ef migrations add <Name> -p Modules/Basket/Basket.csproj -s Bootstrapper/Api/Api.csproj`
  - Ordering: `dotnet ef migrations add <Name> -p Modules/Ordering/Ordering.csproj -s Bootstrapper/Api/Api.csproj`
- Local ports (see `docker-compose.override.yml`): API `http://localhost:5004` / HTTPS `6060`, Fusion gateway `5002`, web-client `4200`, Postgres `5434`, Redis `6379`, Seq ingestion `5341` / UI `9091`, RabbitMQ AMQP `5672` / management `15672`, and Keycloak `9090`.

## Skills
- Inspect `.github/skills/` before implementation and follow every applicable repository skill. The currently checked-in `concise-responses` skill controls chat response length.
- If the active agent environment provides `cqrs-patterns`, `federation-graphql`, or `modular-monolith`, load the relevant skill before changing those areas; do not assume unavailable skills exist locally.

## Validation
- Run the narrowest relevant check first, then broaden when changes cross module or infrastructure boundaries.
- From the repository root, use `dotnet build src/eshop-modular-monilith.slnx` and `dotnet test src/eshop-modular-monilith.slnx`.
- After project-reference, contract, Shared, or module-boundary changes, run `dotnet test src/Tests/ArchitectureTests/ArchitectureTests.csproj`.
- For Angular changes, run `npm --prefix src/web-client run check:boundaries` and `npm --prefix src/web-client run build`.
- For full-stack configuration changes, run `docker compose config` from `src/` and validate affected hosts or the complete Compose stack.

## Conventions to follow when adding code
- Keep module code inside its module; put shared primitives in `Shared/`. Always verify the actual folder nesting for the target module first (see warning above) instead of assuming symmetry.
- Prefer adding new endpoints as `ICarterModule` in the relevant feature folder, and call MediatR via `ISender`. For read-heavy or flexible queries, add a module-owned Hot Chocolate resolver instead of transport logic in Bootstrapper or a bespoke REST list endpoint.
- Prefer DDD creation/update methods and raise domain events via `AddDomainEvent(...)` when behavior changes. For cross-module async communication, use outbox to publish integration events.
- On the Angular side, keep GraphQL query building generic via `GraphqlQueryBuilderService` — don't hardcode query strings or duplicate PrimeNG filter/sort mapping per feature/service.

