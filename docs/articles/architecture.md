# Application architecture

## Runtime and request paths

![Runtime architecture](../images/ModularMonolithArchitecture.png)

[SVG companion](../images/ModularMonolithArchitecture.svg)

The Angular browser application runs on port 4200 and authenticates against Keycloak on port 9090. It sends GraphQL to gateway `/graphql` on port 5002 and REST to gateway `/api`. YARP removes the `/api` prefix before forwarding REST to the API. Fusion delegates GraphQL fields to Catalog, Basket, and Ordering schemas over HTTP and forwards caller authorization. Accounts exposes REST only.

The API runs on host port 5004 and registers all four modules in one process. Its direct REST endpoints and `/graphql/catalog`, `/graphql/basket`, and `/graphql/ordering` source endpoints are available for development; they are not separate module deployments.

Compose startup includes three finite jobs: `graphql-gateway-configurator` downloads source schemas and writes the composed `gateway.far` into a shared volume; `keycloak-configurator` configures organizations and authorization; `observability-volume-init` prepares volume permissions. The gateway consumes the archive, and these jobs exit rather than serving application traffic.

## Module ownership and communication

![Module ownership](../images/ModuleBoundaries%26Ownership.png)

[SVG companion](../images/ModuleBoundaries%26Ownership.svg)

Each module owns its EF Core context and PostgreSQL schema (`catalog`, `basket`, `ordering`, or `accounts`) inside the shared `eshopdb` database. Keycloak uses its own `keycloak` schema. Basket additionally owns its tenant-scoped Redis cache and checkout outbox.

- Basket queries Catalog in-process through `Catalog.Contracts` and MediatR's `GetProductByIdQuery`.
- Catalog publishes `ProductPriceChangedIntegrationEvent` through MassTransit/RabbitMQ; Basket updates affected cart prices.
- Basket stores `BasketCheckoutIntegrationEvent` in its transactional outbox; the background processor publishes it through RabbitMQ and Ordering creates the order.
- Domain events stay in-process. Modules share technical building blocks, not implementation assemblies or business entities.

See [module conventions](../../src/Modules/module.md) and [shared building blocks](../../src/Shared/shared.md) for ownership, DTO mapping, persistence, and testing details.

## Identity, tenancy, and quotas

Keycloak organization UUIDs identify tenants. The API resolves the selected organization from validated claims, enforces tenant membership and roles, and makes tenant context available to module handlers and persistence filters. Ownership and operation permissions remain module responsibilities. Background consumers and the checkout outbox restore the event's tenant scope explicitly.

The API and gateway have independent, process-local rate counters: by default, 60 requests per caller per 60 seconds, without a queue. Caller identity uses JWT `sub` or the anonymous connection IP. A composed GraphQL call consumes gateway quota and separate API quota for each downstream request. See [rate limiting](getting-started.md#rate-limiting).

## Observability and auditing

![Runtime and internal module architecture](../images/image.png)

[Editable SVG](../images/architecture.svg)

API and gateway export OTLP traces and metrics to Alloy. Alloy independently reads opt-in Docker JSON application logs and API audit NDJSON files from a shared volume; OTLP log export is disabled in Compose to avoid duplicating those logs.

Loki stores application and audit logs, Tempo stores traces and generates span metrics/service graphs, and Prometheus stores metrics. Grafana queries the stores and provides correlated exploration. Loki, Tempo, and Prometheus expose no host ports; Grafana is available on loopback port 5601. Application logs and traces retain 15 days, audit logs 60 days, and Prometheus metrics 30 days or 10 GB.

The [observability runbook](../../src/docker-config/observability/README.md) covers credentials, Docker group configuration, collection labels, sampling, retention, dashboards, and recovery. See [diagram regeneration](scripts.md#architecture-diagrams) for maintaining these images.
