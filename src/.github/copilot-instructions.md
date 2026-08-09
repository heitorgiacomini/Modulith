# Repository Instructions

## Project Overview

This repository is an e-commerce modular monolith built with .NET 10 and an Angular 21 client.

- `Modules/Catalog`, `Modules/Basket`, and `Modules/Ordering` own their domain logic and persistence.
- `Shared/Shared.Contracts` contains shared CQRS contracts; `Shared/Shared` contains cross-cutting infrastructure; `Shared/Shared.Messaging` contains MassTransit/RabbitMQ integration.
- `Bootstrapper/*Subgraph` hosts the Hot Chocolate GraphQL subgraphs. `Bootstrapper/Gateway` hosts the Fusion gateway. `Bootstrapper/Api` is the combined API host.
- `Tests/ArchitectureTests` enforces module independence with ArchUnitNET.
- `web-client` is the Angular application and uses PrimeNG and Keycloak.

## Architecture Rules

- Preserve module boundaries. A module must not reference another module implementation assembly.
- Put intentionally shared cross-module types in a contracts project instead of referencing another module directly.
- Keep domain behavior inside its owning module. Bootstrapper projects should compose services and expose transport endpoints, not implement business rules.
- Use integration events through `Shared.Messaging` for asynchronous cross-module workflows.
- Keep database access module-local through the module's EF Core `DbContext`.
- Treat changes under `Shared` as high impact and check all module consumers before changing public contracts or behavior.

## Implementation Conventions

- Follow the existing vertical-slice structure under `Features/<UseCase>`.
- Implement application operations as MediatR `ICommand`/`IQuery` records with matching handlers.
- Add FluentValidation validators beside commands when input has business or data constraints.
- Pass `CancellationToken` through asynchronous handlers and EF Core calls.
- Create and mutate aggregates through their domain methods; do not bypass invariants with transport-layer logic.
- Register module services through the module marker class or the relevant bootstrapper host.
- Keep nullable reference types enabled and follow `.editorconfig` formatting. Do not reformat unrelated code.
- Never edit generated output under `bin`, `obj`, or `node_modules`.

## GraphQL And Federation

- Define GraphQL schema behavior in the owning module and compose it in the corresponding subgraph host.
- Preserve entity keys and reference resolvers when changing federated types.
- When a subgraph schema changes, verify the matching subgraph and the Fusion gateway composition.
- Treat `Bootstrapper/Gateway/gateway.fgx` as a generated composition artifact unless the task explicitly requires updating it.

## Native Skills

Load the applicable repository skill before implementing domain-specific work:

- `cqrs-patterns` for commands, queries, handlers, validators, and MediatR behavior.
- `federation-graphql` for Hot Chocolate, subgraphs, entity federation, and gateway composition.
- `modular-monolith` for module boundaries, contracts, shared code, and cross-module dependencies.

## Validation

Run the narrowest relevant check first, then broaden when the change crosses boundaries:

```powershell
dotnet build eshop-modular-monilith.slnx
dotnet test Tests/ArchitectureTests/ArchitectureTests.csproj
npm --prefix web-client run build
npm --prefix web-client test
docker compose config
```

- Add or update focused tests for changed behavior.
- Always run the architecture tests after changing project references, contracts, module boundaries, or shared infrastructure.
- For full-stack configuration changes, validate Docker Compose and the affected host projects.
