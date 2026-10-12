# Production deployment and recovery

The application remains a .NET modular monolith with Angular, Keycloak, Fusion,
RabbitMQ, PostgreSQL, Redis, and the Grafana stack. Production deployment uses
`src/docker-compose.production.yml`; local development continues to use the
base Compose file plus its development override.

## Reliability guarantees

Audited entity changes persist an `AuditRecords` row and an `AuditDeliveries`
checkpoint in the owning module's transaction. Rolling back the transaction
rolls back both the business change and its audit record. Runtime database roles
can insert and read audit records, but cannot update, delete, or truncate them.
Database administrators retain maintenance access; this is not cryptographic
tamper protection. Direct SQL and EF bulk updates bypass SaveChanges auditing
and must explicitly record their business audit or use database-level auditing.

The export workers read committed records and write the existing redacted NDJSON
projection for Alloy/Loki. A failed write leaves the checkpoint pending. A crash
between file writing and checkpoint commit can duplicate a projection event;
deduplicate by `eventId`. PostgreSQL is the authoritative business audit store.
Existing request/security events written through `IAuditTrail` remain best-effort
operational events, distinct from these transactional entity records.

Authoritative audit records and checkout receipts are retained indefinitely by
default. There is no automatic deletion. Monitor database storage and establish
an approved archival/retention policy before adding a maintenance purge. Existing
bounded file rotation and Loki retention apply only to the observability projection.

Catalog price changes and Basket checkout use the same shared PostgreSQL outbox
implementation, with separate tables in their module schemas. Dispatchers select
at most 20 eligible rows with `FOR UPDATE SKIP LOCKED`, publish with a stable
MessageId and a ten-second timeout, and commit delivery state afterward. Failures
retry with exponential delays capped at five minutes. Unsupported contracts remain
pending and generate warnings rather than being silently deleted. Failed batches
must be monitored through the service logs and the database queries below.

Delivery is at-least-once. Ordering records a durable `(TenantId, EventId)` checkout
receipt in the same transaction as the order and uses a transaction-scoped
PostgreSQL advisory lock to serialize competing deliveries. Replays return the
original order ID, including when that order has been soft deleted. Existing Basket
outbox rows survive the additive migration and use their row ID as the stable ID.
Previously delivered messages from the old implementation have no receipt history;
drain the old dispatcher before upgrading if a replay of those messages is possible.

## Configure and deploy

1. Copy `src/.env.production.example` to a private `src/.env.production` and fill
   every blank value. Use different random secrets; hex-encoded passwords avoid
   connection-string delimiters. Keep public URLs as HTTPS origins without trailing
   slashes. The populated file is excluded from Git and Docker build contexts.
2. Configure TLS ingress for the app hostname to `web-client:8080` and the identity
   hostname to `keycloak:9090`. In Dokploy, attach these services to the ingress
   network and configure their domains there. The Compose topology publishes no
   app, database, cache, broker, or identity host ports. Grafana and collector
   diagnostics retain loopback-only bindings from the observability configuration.
3. On the Linux deployment host, run:

   ```bash
   docker compose --env-file src/.env.production -p eshop \
     -f src/docker-compose.production.yml config --quiet
   docker compose --env-file src/.env.production -p eshop \
     -f src/docker-compose.production.yml up --build -d
   ```

   Keep the project name stable across releases to preserve named volumes. For
   release pipelines, set the five image variables to tested immutable registry
   digests rather than rebuilding the `:local` images on the server.
4. Confirm `db-migrator`, `db-permissions`, `keycloak-configurator`,
   `graphql-gateway-configurator`, and `observability-volume-init` exit successfully.
   The API never runs migrations at startup. The migrator runs all module migrations
   once and seeds only when `Database:Seed=true` (development defaults enable it).
5. Create production organizations, organization groups/roles, and user memberships
   in Keycloak. Production setup installs the existing organization claim and
   Ordering permission configuration but creates no sample users or tenants. Grant
   organization roles `customer`/`admin` through groups as in the existing Keycloak
   runbook. Client redirects and the confidential Ordering client secret come from
   the production environment.

The Keycloak image builds its PostgreSQL configuration before `start --optimized`.
Its internal HTTP endpoint must be behind trusted TLS ingress, which supplies and
overwrites forwarded headers. The Angular image serves compiled assets through
NGINX; `/config.json` supplies public runtime endpoints without rebuilding the UI.
No credentials appear in that browser configuration.

### Existing databases

The PostgreSQL initialization script runs only for a new database volume. For an
existing deployment, back up first, create the migration and module login roles,
transfer the existing module schema/table ownership to `eshop_migrator`, and grant
that role access to the existing public migration-history table. Run the migration
job and `runtime-permissions.sql` before switching the API to its four module
connection strings. Keep Keycloak's existing schema and its separate database role.
Do not delete or reset a volume to make the initialization script run.

Stop old API/dispatcher instances during this upgrade: the new audit tables and
outbox state must exist before new instances start. Resume traffic only after the
API and gateway readiness checks succeed. An application image rollback must remain
compatible with the additive schema; do not automatically roll back audit migrations
because their Down methods remove audit history.

## Health and operational checks

`/health/live` checks the process without dependencies. API `/health/ready` checks
all four module audit tables using their runtime credentials and includes registered
MassTransit bus health. Gateway readiness checks the API and the composed archive.
Keycloak, PostgreSQL, Redis, RabbitMQ, the web server, and the observability services
also have Compose health checks.

For each of the `catalog` and `basket` schemas, monitor pending messages and their
age using the following query (shown for Catalog):

```sql
SELECT count(*) AS pending,
       min("OccuredOn") AS oldest,
       max("AttemptCount") AS highest_attempt_count
FROM catalog."OutboxMessages" WHERE "ProcessedOn" IS NULL;
SELECT count(*) AS pending_audit_exports
FROM catalog."AuditDeliveries" WHERE "ExportedAtUtc" IS NULL;
```

Investigate pending messages older than five minutes and continuously increasing
audit checkpoints. Use the existing audit-write-failure alert for projection
failures. Retry a corrected poison message by setting its `NextAttemptAt` to
`now()` using that module's maintenance connection; preserve the event ID.

## Back up and restore

Use a database-aware backup, rather than relying on a snapshot of a running
PostgreSQL volume. On the Linux host, with the production Compose environment:

```bash
docker compose --env-file src/.env.production -p eshop \
  -f src/docker-compose.production.yml exec -T postgres \
  pg_dump -U eshop_admin -Fc eshopdb > eshop.dump
```

Store the dump encrypted outside the host. Back up database role definitions through
a separate protected administrative procedure. Periodically restore the dump into
an isolated PostgreSQL 18 database with `pg_restore --exit-on-error`, verify business
and audit row counts, apply runtime permissions, and exercise authentication and a
checkout. Never test restoration against the active production database.

CI runs PostgreSQL rollback, replay, concurrency, audit-export failure, and role
permission tests. Its backup/restore smoke test restores seeded products and their
transactional audits into a separate scratch database and compares counts. Run the
reliability tests locally by setting `POSTGRES_TEST_CONNECTION` to an isolated
PostgreSQL administrator connection with permission to create test databases.

FullStack Hero remains a pattern reference. Dokploy is a supported deployment
candidate, not an installed platform. A warehouse and Fusion simplification remain
separate architecture decisions; neither is necessary for these reliability fixes.
