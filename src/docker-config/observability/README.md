# Eshop observability stack

The default Compose project uses OpenSearch as its observability platform and
OpenTelemetry Collector as its only telemetry runtime:

```text
API and gateway -> OpenTelemetry Collector -> OpenSearch -> Dashboards
API audit NDJSON -> Collector filelog receiver -----^
```

OpenSearch, Dashboards, and Collector are pinned in `docker-compose.yml`.
No additional ingestion runtime or observability platform is required.
OpenSearch bootstrap owns security roles, index templates, ingest pipelines,
retention policies, alerts, and dashboard imports. Applications receive no
OpenSearch credentials.

## Known observability gap

Collector `0.159.0` can export logs and traces to OpenSearch, but its OpenSearch
exporter cannot export metrics. Application metrics and Collector-generated
`traces_service_graph_*` metrics are accepted through OTLP and sent only to a
rate-limited `debug/metrics` exporter. They are not persisted in OpenSearch.

Consequently, the Eshop .NET runtime-metrics dashboard and graphical service
topology are temporarily unavailable. Application logs, sampled traces, audit
records, native trace and span drill-down, alerts, and the trace-based
dependency dashboard remain available. This gap avoids depending on an
unreleased or locally maintained Collector image.

OpenSearch Dashboards 2.19 enables its native Traces and Spans pages only when
both `otel-v1-apm-span-*` and `otel-v1-apm-service-map*` exist and are readable.
The bootstrap therefore creates an empty, unmanaged
`otel-v1-apm-service-map` compatibility index with the official v1 mapping.
Collector never writes to it, and the ingest account has no permission to do
so. The native Services/Service Map view has no current topology data and must
not be presented as a live service map. Existing indexes are preserved if the
bootstrap encounters one created by an older deployment.

PR [open-telemetry/opentelemetry-collector-contrib#49570](https://github.com/open-telemetry/opentelemetry-collector-contrib/pull/49570)
adds the missing functionality but was merged after `0.159.0` was published.
Do not enable the OpenSearch metrics exporter until an official numeric release
contains that PR.

### Follow-up after the official metrics release

1. Confirm the release changelog includes PR #49570 and validate a metrics
   pipeline with the released image.
2. Update the exact Collector image version in `docker-compose.yml`.
3. Replace `debug/metrics` with the authenticated OpenSearch exporter using
   `mapping.mode: otel-v1` and `metrics_index: eshop-app-metrics`.
4. Persist application metrics plus
   `traces_service_graph_request_total`,
   `traces_service_graph_request_failed_total`, client/server latency
   histograms, unpaired spans, and dropped spans.
5. Restore the Eshop .NET runtime dashboard and add an Eshop Vega topology
   dashboard grouped by `client`, `server`, and `connection_type`, with request
   count, failure count/rate, and client/server latency on each edge.
6. Validate gauge, sum, histogram, exponential histogram, summary, and every
   service-graph document; then remove the metrics gap and record the resolving
   Collector version.

Do not transform standard service-graph metrics into proprietary Data Prepper
service-map documents. Retain the empty compatibility index until a tested
Dashboards release no longer requires it.

## Start and sign in

Copy `.env.observability.example` to `.env`, change every password, and start
Compose from `src`:

```powershell
Copy-Item .env.observability.example .env
docker compose up --build -d
docker compose ps --all
```

Open Dashboards at `http://localhost:5601` and sign in with
`OPENSEARCH_OPERATOR_USERNAME` and `OPENSEARCH_OPERATOR_PASSWORD`.

The checked-in bootstrap imports the `Eshop Event Explorer`, `Eshop Trace
Explorer`, `Eshop RED`, and `Eshop dependencies` dashboards, plus data views for:

- `eshop-app-logs-*`;
- `eshop-app-metrics-*`, reserved for the future metrics exporter;
- `otel-v1-apm-span-*`;
- `eshop-audit-*`.

### Eshop Trace Explorer

Open **Dashboards > Eshop Trace Explorer** for the recommended trace
investigation view. It starts on the last 30 minutes and refreshes every 15
seconds. Routine schema, health, readiness, and metrics polling is hidden by
the removable **Hide routine polling** filter.

The summary cards report trace volume, failures, traces lasting at least two
seconds, p95 duration, and active services. Click a trace-table row to add a
`traceId` filter. The parent/child waterfall and span table then show that
trace; remove the filter pill to return to the latest-traces overview. Trace
and span tables are paginated, waterfall bars have hover details, and raw span
rows can be expanded in the final panel.

Flags use these meanings: `ERROR` is an OpenTelemetry error or HTTP 5xx, `4XX`
is a client error, `SLOW` is at least two seconds, `WATCH` is at least 500 ms,
`DATA LOSS` indicates dropped attributes/events/links, `ORPHAN` indicates a
missing displayed parent, `ROOT` marks entry spans, and `OK` means no warning
was detected. Category badges distinguish Agent, LLM, Tool, GraphQL, HTTP/API,
PostgreSQL, RabbitMQ, Redis, and Internal spans. AI categories appear
automatically when standard `gen_ai.*` attributes are present.

Use native Trace Analytics as an additional trace/span drill-down and Discover
for application or audit logs. The native service-map view remains empty by
design. The Eshop dependency dashboard derives its current panels from trace
spans.

### Eshop Event Explorer

Open **Dashboards > Eshop Event Explorer** for the primary log workflow. It
starts on the last 30 minutes, refreshes every 10 seconds, and presents severity,
service, rendered message, HTTP status, elapsed time, source category, and trace
ID. Expand a row for all structured attributes and exception fields. The
application-first query is visible and removable; clearing it restores framework,
schema, health, readiness, and metrics events. Use the trace ID column to pivot
to the Trace Explorer for the same operation.

## Retention, sizing, and alerts

Index State Management deletes application logs and traces after 15 days,
future metrics after 30 days, and audit records after 60 days. All managed
indexes use one primary shard and zero replicas for this single-node deployment.
Protected plugin system indexes may retain plugin-defined replicas, so the
cluster can report yellow while every telemetry primary remains assigned.

The bootstrap installs an application-error monitor and a cluster disk monitor
with 70%, 80%, and 85% triggers. The same values are OpenSearch allocation
watermarks. Configure notification channels in Dashboards; notification secrets
are intentionally not stored in the repository.

Keep at least 25% of the OpenSearch disk free. The initial sizing target is
8 vCPU, 16 GiB RAM, and 250 GB SSD. Container caps allocate 6 GiB to OpenSearch,
1 GiB to Dashboards, and 512 MiB to Collector.

## Trace sampling

`OTEL_TAIL_SAMPLING_PERCENTAGE=100` retains every development trace. In
production, use 10 or less. Collector always retains traces with error status
and traces lasting at least two seconds in addition to the baseline sample.

## Audit guarantees

`AuditEventV1` is written only after a successful EF Core save. The dedicated
file writer emits one JSON object per line, rotates at 128 MiB, and retains eight
files, capping local audit files at 1 GiB. Collector uses persistent file
checkpoints and export queues. The `eshop-audit-event-id` OpenSearch ingest
pipeline sets `_id` from `eventId`, so replay overwrites the original document.
The audit event timestamp also selects its daily index.

This is a best-effort operational audit trail. A process failure between the
database commit and file flush can lose an event, and platform administrators
can mutate the OpenSearch copy. It is not a compliance archive, transactional
ledger, or temporal row history. Do not expose it to tenant users.

Call `IAuditTrail.Write` for explicit business actions, sensitive reads,
security decisions, and administrative actions. Never include passwords,
tokens, authorization headers, request bodies, raw GraphQL documents, or
sensitive entity values in audit metadata.

## Validation

```powershell
docker compose config --quiet
dotnet test eshop-modular-monilith.slnx
```

After startup, these five one-shot services must exit with code `0`:

- `keycloak-configurator`;
- `opensearch-configurator`;
- `opensearch-dashboards-configurator`;
- `otel-collector-configurator`;
- `graphql-gateway-configurator`.

Generate API and gateway requests plus an audited mutation. Confirm logs and
traces in OpenSearch, metric summaries in Collector logs, and the audit document
whose `_id` equals its `eventId`. Collector state is held in
`otel_collector_state`; audit files are held in `audit_logs`. Malformed audit
lines and records without an event ID or valid event timestamp are dropped.

Collector self-metrics are available only on the loopback-bound
`http://localhost:8888/metrics` endpoint. During validation, confirm the current
values for `otelcol_receiver_accepted_log_records`,
`otelcol_receiver_refused_log_records`,
`otelcol_exporter_enqueue_failed_log_records`, and
`otelcol_exporter_send_failed_log_records`. Refused and enqueue-failed
application logs must remain zero; send failures require checking the persistent
queue and must stop increasing after OpenSearch is healthy.

Sign in as the observability operator and confirm Trace Analytics shows trace
and span records and opens the selected trace timeline. Confirm
`otel-v1-apm-service-map` has zero documents, the native service-map view is not
described as current topology, and the operator cannot write to that index.
Also confirm `Eshop Trace Explorer` loads without Vega errors, its polling
filter is removable, selecting a trace updates both detail panels, and clearing
the `traceId` filter restores the overview.
