# Getting Started

Run the reference application from the repository's `src` directory:

```powershell
docker compose up --build -d
docker compose ps --all
```

Copy `src/.env.observability.example` to `src/.env`, provide the required
Grafana credentials, and set the Linux Docker group ID before deployment.
Grafana is available at `http://localhost:5601`; see the
[observability runbook](../../src/docker-config/observability/README.md) for
Alloy Docker discovery, retention, audit guarantees, production sampling,
encrypted snapshots, and restore testing.

The Angular client is available at `http://localhost:4200`. It sends composed GraphQL operations through the Fusion gateway at `http://localhost:5002/graphql` and REST requests directly to the modular API at `http://localhost:5004`.

## Rate limiting

Both HTTP hosts enforce independent, process-local fixed-window quotas. The checked-in configuration permits 60 requests per caller every 60 seconds with no queue. Authenticated callers are identified by the JWT `sub` claim and anonymous callers by their connection IP address.

Override the defaults independently for either container with standard ASP.NET Core environment variables:

```yaml
environment:
  - RateLimiting__PermitLimit=60
  - RateLimiting__WindowSeconds=60
```

Every request consumes quota, including CORS preflight, GraphQL schema downloads, health checks, authorization failures, and Fusion's downstream source requests. When a quota is exhausted, the host returns HTTP `429 Too Many Requests` as Problem Details and includes `Retry-After` when available.

See the [main README](../../README.md#rate-limiting) for the complete topology and operational notes.
