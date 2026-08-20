# .NET Modular Monolith E-Shop

This repository is a reference e-commerce application built as a **modular monolith on .NET 10**. Catalog, Basket, Ordering, and Accounts are independently structured business modules hosted in one ASP.NET Core process. An Angular client uses a Hot Chocolate Fusion gateway for the composed GraphQL API and calls selected REST endpoints on the API host.


![Modular Monolith Architecture](ModularMonolithArchitecture.png)

![Module Boundaries & Ownership](ModuleBoundaries&Ownership.png)

![Docker Compose runtime and internal .NET modular-monolith architecture](image.png)

The editable source for this diagram is [`architecture.svg`](architecture.svg).

## Architecture at a glance

### Docker Compose runtime

The Compose project in `src/` starts the following topology:

1. The **Angular 21** client authenticates users with Keycloak.
2. GraphQL requests go to the **Hot Chocolate Fusion gateway** at `http://localhost:5002/graphql`.
3. The gateway delegates fields to the Catalog, Basket, and Ordering source schemas hosted by the modular API.
4. REST requests, including the Accounts API, go directly to the API host at `http://localhost:5004`.
5. A short-lived **Fusion Composer** container downloads the three source schemas and generates `gateway.far` in a shared Docker volume before the gateway starts serving requests.
6. The API uses PostgreSQL, Redis, RabbitMQ, Keycloak, and Seq as backing services.

The composer is a startup job, not a long-running request-path service. If a source GraphQL schema changes, recreate the composer and gateway so the Fusion archive is regenerated.

### Internal .NET modules

`Bootstrapper/Api` is the composition root. It registers four modules in one ASP.NET Core host:

| Module | Public surface | Persistence | Responsibilities |
| --- | --- | --- | --- |
| Catalog | Carter REST endpoints and the `catalog` GraphQL schema | `CatalogDbContext` | Products, queries, and price-change events |
| Basket | Carter REST endpoints and the `basket` GraphQL schema | `BasketDbContext` plus Redis cache | Shopping carts, checkout, cache-aside access, and transactional outbox |
| Ordering | Carter REST endpoints and the `ordering` GraphQL schema | `OrderingDbContext` | Order creation, queries, and authorization policies |
| Accounts | Authenticated Carter REST endpoints under `/account/me` | `AccountsDbContext` | Customer preferences, saved addresses, and payment methods |

The contexts share one PostgreSQL server/database in this sample, but ownership remains inside each module. Accounts does not expose a GraphQL source schema and is therefore not included in the Fusion graph.

Shared projects are technical building blocks rather than business modules:

- `Shared.Contracts` contains CQRS abstractions used with MediatR.
- `Shared` contains common DDD types, validation/logging behaviors, EF Core interceptors, exceptions, pagination, and infrastructure extensions.
- `Shared.Messaging` contains integration-event contracts and MassTransit/RabbitMQ setup.
- `Catalog.Contracts` is Catalog's explicit in-process contract consumed by Basket.

Architecture tests enforce that business module assemblies do not directly depend on one another. The explicit Catalog contract is kept in a separate contracts assembly.

## Communication flows

### Synchronous communication

- Basket sends `GetProductByIdQuery`, defined by `Catalog.Contracts`, through the in-process MediatR pipeline before adding a product to a cart.
- The Fusion gateway routes a single client GraphQL operation across the Catalog, Basket, and Ordering source schemas over HTTP.
- Accounts operations use authenticated REST calls directly to the API host.

### Asynchronous communication

- When a Catalog product price changes, Catalog publishes `ProductPriceChangedIntegrationEvent` through MassTransit and RabbitMQ; Basket consumes it and updates matching cart items.
- Basket checkout stores `BasketCheckoutIntegrationEvent` in its outbox in the same database transaction. `OutboxProcessor` later publishes it to RabbitMQ; Ordering consumes it and creates the order.
- Domain events are dispatched in-process by the EF Core save-changes interceptor and MediatR.

## Patterns and technologies

- Modular monolith with enforced module boundaries
- Vertical Slice Architecture and feature folders
- Domain-Driven Design for aggregate and domain-event modeling
- CQRS with MediatR, FluentValidation, and pipeline behaviors
- EF Core code-first migrations with PostgreSQL
- Transactional outbox for reliable basket-checkout messaging
- Cache-aside repository decorator with Redis
- Event-driven integration with MassTransit and RabbitMQ
- REST endpoints with Carter and GraphQL with Hot Chocolate/Fusion
- OAuth 2.0/OpenID Connect and JWT bearer authentication with Keycloak
- Structured logging with Serilog and Seq
- Angular and PrimeNG client organized by bounded context

## Prerequisites

For the recommended container workflow:

- Docker Desktop or another Docker Engine with Docker Compose v2

For running components directly:

- .NET 10 SDK
- Node.js compatible with Angular 21 and npm 11
- PostgreSQL, Redis, RabbitMQ, and Keycloak, or their Compose containers

## Run with Docker Compose

Run these commands from the repository root:

```bash
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up --build
```

To stop the application while retaining named-volume data:

```bash
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml down
```

### Local service URLs

| Service | URL or port | Purpose |
| --- | --- | --- |
| Angular client | <http://localhost:4200> | Browser application |
| Fusion GraphQL gateway | <http://localhost:5002/graphql> | Composed Catalog, Basket, and Ordering graph |
| Modular API | <http://localhost:5004> | REST API and source GraphQL schemas |
| Catalog source schema | <http://localhost:5004/graphql/catalog> | Catalog GraphQL endpoint |
| Basket source schema | <http://localhost:5004/graphql/basket> | Basket GraphQL endpoint |
| Ordering source schema | <http://localhost:5004/graphql/ordering> | Ordering GraphQL endpoint |
| Keycloak | <http://localhost:9090> | Identity provider (`myrealm`) |
| Seq | <http://localhost:9091> | Structured-log UI |
| RabbitMQ management | <http://localhost:15672> | Broker administration UI |
| PostgreSQL | `localhost:5434` | Development database |
| Redis | `localhost:6379` | Distributed basket cache |

The sample Compose credentials for PostgreSQL and RabbitMQ are `eshopdb` / `eshopdb`. They are development-only values and must not be reused in production.

## Run components locally

Start the backing services with Compose, then run the API from the repository root:

```bash
dotnet run --project src/Bootstrapper/Api/Api.csproj
```

The Fusion gateway needs a generated `gateway.far`. The complete Compose workflow generates it automatically and is the simplest way to run the gateway. When the archive is available at `src/Bootstrapper/Gateway/gateway.far`, run:

```bash
dotnet run --project src/Bootstrapper/Gateway/Gateway.csproj
```

Run the Angular client in another terminal:

```bash
cd src/web-client
npm install
npm start
```

The client configuration in `src/web-client/src/environments/environment.ts` targets the default API, gateway, and Keycloak ports shown above.

## Authentication and authorization

Compose imports `src/docker-config/keycloak/myrealm-realm.json` into Keycloak. Both the API and Fusion gateway validate Keycloak JWT bearer tokens, and the gateway forwards the caller's `Authorization` header to source schemas. Ordering applies scope-based authorization policies, while Accounts and user-owned Basket operations derive the customer identity from token claims.

## Project structure

```text
src/
|-- Bootstrapper/
|   |-- Api/                  # Modular-monolith composition root
|   `-- Gateway/              # Hot Chocolate Fusion gateway
|-- Modules/
|   |-- Accounts/
|   |-- Basket/
|   |-- Catalog/
|   `-- Ordering/
|-- Shared/                   # Shared contracts and infrastructure
|-- Tests/                    # Unit and architecture tests
|-- web-client/               # Angular/PrimeNG application
|-- docker-config/            # PostgreSQL, Keycloak, and Fusion setup
`-- docker-compose*.yml
```

## Build and test

From the repository root:

```bash
dotnet build src/eshop-modular-monilith.slnx
dotnet test src/eshop-modular-monilith.slnx --no-build
```

To verify the Angular bounded-context import rules and production build:

```bash
cd src/web-client
npm run check:boundaries
npm run build
```
