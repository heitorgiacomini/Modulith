# Eshop Trace Explorer Dashboard

## Summary

- First execution action: save this finalized plan as `.github/plan/eshop-trace-explorer-dashboard-plan.md`.
- Add an **Eshop Trace Explorer** inside OpenSearch Dashboards 2.19.6 using the existing `otel-v1-apm-span-*` data.
- Provide both trace and span tables plus an interactive parent/child waterfall inspired by the supplied example.
- Keep Collector-only ingestion, existing credentials, and the empty native service-map compatibility index unchanged.

## Dashboard Experience

- Default to the last 30 minutes with 15-second refresh and a removable filter hiding schema, health, readiness, and metrics polling.
- Add summary cards for total traces, failures, slow traces, p95 duration, and active services.
- Show the latest 100 traces, one row per `traceId`, with start time, entry service, operation/route, duration, span/service counts, HTTP status, dependencies, and shortened trace ID.
- Clicking a trace applies an OpenSearch Dashboard `traceId` filter, updating the waterfall and span table. Removing the filter returns to the overview.
- Add clear, non-exclusive trace/span flags:
  - `ERROR` in red for OpenTelemetry error status or HTTP 5xx.
  - `4XX` in yellow for client errors.
  - `SLOW` in orange for durations of at least 2 seconds.
  - `WATCH` in amber for durations from 500 ms to under 2 seconds.
  - `DATA LOSS` in purple when attributes, events, or links were dropped.
  - `ORPHAN` in gray when a displayed span references a missing parent.
  - `ROOT` in blue and `OK` in green when no warning applies.
- Classify spans with colored badges for Agent, LLM, Tool, GraphQL, HTTP/API, PostgreSQL, RabbitMQ/messaging, Redis/cache, and Internal. AI badges should activate automatically when standard `gen_ai.*` attributes appear, even though current Eshop traffic may not contain them.
- Build a tree waterfall showing parent/child indentation, relative start, proportional duration bars, service/category colors, warnings, and hover details. Limit a trace to 500 displayed spans and show a truncation warning when exceeded.
- Add an expandable span table ordered by start time with category, flags, service, operation, relative start, duration, status, HTTP/database/messaging fields, span ID, and parent span ID.

## Implementation Changes

- Store the new dashboard, Vega panels, summary visualizations, and saved span search in a separate `eshop-trace-explorer.ndjson`; update the Dashboards configurator to import every dashboard bundle idempotently with overwrite enabled.
- Use root spans for trace duration and entry information, falling back from `traceGroupFields.durationInNanos` to root `durationInNanos`. Aggregate errors and dependencies across every span in each trace.
- Require a visible root span in the trace aggregation so removing a noisy root through the default filter also removes its child-only bucket.
- Correct existing RED dashboard filters from `SPAN_KIND_SERVER` to the Collector’s actual `Server` value and display durations in milliseconds/seconds instead of raw nanoseconds.
- Keep the current RED and dependency dashboards available; the Trace Explorer becomes the recommended investigation view.
- Document navigation, flag meanings, selecting/clearing a trace, raw-span expansion, noise-filter removal, and the distinction between this trace waterfall and the intentionally empty native service map.
- Do not add application endpoints, new credentials, Data Prepper, stored metrics, proprietary service-map documents, or telemetry schema changes.

## Validation

- Validate every NDJSON line and its embedded Vega, visualization, panel, and search-source JSON.
- Run `docker compose config --quiet`, recreate the Dashboards configurator, and confirm repeated imports exit with code `0` without duplicate saved objects.
- Sign in as `eshop_operator` and verify the dashboard loads without authorization, missing-index, or Vega errors.
- Verify the default view hides polling noise, dashboard filters and time controls affect every panel, and removing the noise filter restores those traces.
- Select representative traces and confirm the trace table, waterfall hierarchy, span counts, span table, durations, parent relationships, dependency badges, and native trace IDs agree with the stored documents.
- Exercise success, HTTP 4xx/5xx, error-status, 500 ms, 2-second, dropped-data, orphan, database, messaging, and `gen_ai.*` fixture spans to verify every flag and category.
- Confirm a selected trace updates both detail panels, clearing `traceId` restores the overview, and traces above 500 spans show truncation safely.
- Run repository tests and scan Dashboard/OpenSearch logs for fatal, authorization, index-not-found, or saved-object import errors.

## Assumptions

- OpenSearch and Dashboards remain pinned to `2.19.6`.
- Vega click filtering uses `opensearchDashboardsAddFilter`, supported by this Dashboards version.
- Slow thresholds default to 500 ms warning and 2 seconds slow/critical, matching the existing two-second tail-sampling policy.
- The dashboard is optimized for operators investigating Eshop traffic; routine internal polling is retained in OpenSearch but hidden initially.
