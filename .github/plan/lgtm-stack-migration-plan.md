# Replace OpenSearch with an Alloy-Based Grafana Stack

## Summary

Replace OpenSearch, OpenSearch Dashboards, and the standalone OpenTelemetry Collector with a single-host Grafana observability stack for an early-stage production VPS:

- Grafana for visualization
- Loki for logs
- Tempo monolithic for traces, without Kafka
- Prometheus for metrics
- Grafana Alloy as the only collection agent
- Local persistent volumes with encrypted VPS snapshots/off-host backups

Use [docker-otel-lgtm](https://github.com/grafana/docker-otel-lgtm) for signal-routing and correlation patterns and [OIB](https://github.com/matijazezelj/oib) for Alloy Docker discovery and operational dashboards. Do not copy their demo-only all-in-one or collect-every-container defaults.

## Implementation Changes

### Deployment and storage

- First save this plan as `.github/plan/lgtm-stack-migration-plan.md`.
- Pin Grafana `13.2.0`, Alloy `1.18.0`, Loki `3.7.7`, Tempo `3.0.3`, and Prometheus `3.14.0`.
- Run Loki and Tempo as separate monolithic services with local persistent storage:
  - Application logs: 15 days
  - Audit logs: 60 days through a higher-priority Loki stream rule
  - Traces: 15 days
  - Metrics: 30 days or 10 GB, whichever comes first
- Do not add Kafka, NGINX, MinIO, Garage, Ceph, Node Exporter, Blackbox Exporter, Pyroscope, or OBI.
- Retain Grafana, Loki, Tempo, Prometheus, and Alloy state in separate named volumes. Removing Compose references to OpenSearch must not delete its existing volume.
- Configure bounded Docker logging using the `local` driver with rotation so temporary stdout history cannot fill the VPS disk.
- Establish a production runbook for encrypted VPS snapshots: daily retention for seven days, weekly retention for four weeks, and a quarterly restore test. The repository documents the procedure without embedding provider-specific credentials.
- Apply restart policies, graceful shutdown, health checks, memory limits, read-only configuration mounts, dropped capabilities, and `no-new-privileges`.

### Alloy as the only telemetry agent

- Replace `otel-collector` with an `alloy` service while retaining host OTLP ports `4317` and `4318`.
- Mount the Docker socket read-only, add only the host Docker group, and persist Alloy’s data directory for Docker and file read positions.
- Use an opt-in Docker label such as `observability.logs=true`; initially enable it only for the API and Gateway. Newly deployed containers become observable by adding the same label.
- Configure Alloy pipelines:
  - Docker discovery → label filtering → Docker stdout reader → JSON/plain-text processing → Loki
  - Audit file matching → JSON validation → audit timestamp extraction → Loki
  - OTLP traces → memory limiting → existing error/slow/baseline tail sampling → batching → Tempo
  - OTLP metrics → batching → Prometheus conversion → Prometheus remote write
  - Alloy’s built-in Unix and cAdvisor exporters → Prometheus remote write
- Enable Prometheus’s remote-write receiver and scrape Prometheus, Alloy, Loki, Tempo, and Grafana internal metrics.
- Configure Tempo’s metrics generator for service graphs, span metrics, and exemplars, remote-writing to Prometheus.
- Keep all backends on a private Compose network. Publish only:
  - Grafana on `127.0.0.1:5601`
  - Alloy OTLP on `127.0.0.1:4317` and `4318`
  - Alloy diagnostics on `127.0.0.1:12345`
- Do not add backend authentication or an in-stack reverse proxy. Loki, Tempo, and Prometheus ports remain unpublished. Grafana retains built-in authentication, disables anonymous access and sign-up, and requires environment-provided credentials.

### Application and audit logging

- Continue writing API and Gateway logs to Docker stdout, but replace simple-console formatting with JSON console formatting that preserves structured state, category, severity, trace ID, span ID, timestamp, and scopes.
- Add `OpenTelemetry:ExportLogs`, defaulting to `true` for compatibility outside Compose. Set it to `false` for the API and Gateway containers so each event reaches Loki only through Alloy’s Docker reader.
- Keep OTLP trace and metric export enabled and change the internal endpoint from `otel-collector:4317` to `alloy:4317`.
- Preserve the audit NDJSON contract and shared read-only audit volume.
- In Alloy’s audit pipeline:
  - Drop malformed JSON, missing `eventId`, and invalid `occurredAtUtc`.
  - Use `occurredAtUtc` as the Loki timestamp.
  - Add the low-cardinality `log_type=audit` label.
  - Store `eventId`, user identifiers, and trace identifiers as structured metadata, not stream labels.
- Preserve append-only audit behavior: different events sharing an `eventId` remain visible and dashboards flag repeated IDs rather than overwriting them.

## Grafana and Operational Behavior

- Provision immutable Loki, Tempo, and Prometheus data sources using direct internal service URLs and stable UIDs.
- Configure:
  - Loki trace IDs → Tempo
  - Tempo traces → matching Loki logs
  - Prometheus exemplars → Tempo
  - Tempo service maps and node graphs → Prometheus
- Port the current workflows into provisioned dashboards:
  - Overview and RED metrics
  - Event/Log Explorer
  - Trace Explorer
  - Service Dependencies
  - Audit Explorer
  - Stack Health and VPS/container resources
- Preserve service, environment, severity, route, status, duration, trace, and audit filtering.
- Add Prometheus rules for trace/application errors, unavailable scrape targets, Alloy ingestion failures, container restarts/resource pressure, and disk usage at 70%, 80%, and 85%.
- Surface alerts in Grafana without adding an external notification integration.
- Remove OpenSearch services, security configuration, templates, lifecycle policies, bootstrap jobs, and dashboards. Update environment examples, diagrams, screenshots, runbooks, README content, and generated DocFX output.
- Public application APIs and audit schemas do not change. The only new application configuration interface is `OpenTelemetry:ExportLogs`.

## Test Plan

- Validate Compose, Alloy, Prometheus rules, Loki, Tempo, and Grafana provisioning configurations.
- Verify required Grafana credentials have no production fallback and raw backend ports are inaccessible from the host.
- Start with empty LGTM volumes and confirm all services become healthy without Kafka or NGINX.
- Confirm a single API or Gateway request produces exactly one Loki log entry despite also appearing through `docker logs`.
- Start labeled and unlabeled test containers and verify Alloy collects only the labeled container.
- Restart Alloy and verify persisted Docker/audit positions prevent unwanted replay while continuing from the last offset.
- Verify structured request fields and trace IDs survive JSON console parsing.
- Generate successful, failed, and slow requests and confirm logs, traces, metrics, RED panels, service graphs, and cross-signal links.
- Test valid, malformed, missing-ID, invalid-timestamp, identical-replay, and differing-same-ID audit records.
- Exercise alert rules and validate host/container disk and resource metrics generated by Alloy.
- Restart the complete stack and confirm retained telemetry and dashboards survive.
- Perform and document one snapshot restore into a clean Compose project.
- Confirm active repository configuration no longer references OpenSearch except for an intentional migration note.

## Assumptions

- Production runs on one Linux VPS using Docker Compose.
- High availability, horizontal scaling, Kafka, and multi-host collection are intentionally deferred.
- Local volumes are preferred over MinIO, Garage, or Ceph because all share the same VPS failure domain while local storage has substantially lower operational risk.
- The VPS provider or operator supplies encrypted snapshots and at least one off-host backup; losing the only VPS without those backups can lose observability data.
- Grafana is accessed through an existing external TLS ingress, VPN, or SSH tunnel; implementing that ingress is outside this migration.
