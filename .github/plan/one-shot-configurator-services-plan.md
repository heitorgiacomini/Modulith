# Collector-only Trace UI and release-gated topology

## Summary

- First execution action: replace `.github/plan/one-shot-configurator-services-plan.md` with this finalized revision.
- Unlock OpenSearch’s native Traces and Spans pages now without Data Prepper by creating an empty legacy compatibility index.
- Keep the native service map empty and clearly marked unavailable.
- After an official Collector release contains #49570, persist standard service-graph metrics and build an Eshop topology dashboard from them.

## Immediate trace UI compatibility

- Bootstrap an exact `otel-v1-apm-service-map` index using the official v1 mapping when it does not already exist.
- Keep the compatibility index empty: no Collector exporter, ingest pipeline, ISM policy, synthetic relationship documents, or Data Prepper service.
- Grant `eshop_operator` read, search, index-existence, mapping, settings, and alias access to `otel-v1-apm-service-map*`; do not grant the ingest account write access.
- Make Dashboards wait for `opensearch-configurator` to finish so the compatibility index and permissions exist before the UI starts.
- Preserve any pre-existing service-map index and documents; never delete historical data automatically.
- Keep `otel-v1-apm-span-*` as the source for trace lists, span lists, timelines, and drill-down.

## Documentation and future upgrade

- Correct the known-gap documentation:
  - Native Traces and Spans pages are available.
  - The native Services/Service Map view has no current topology data.
  - The empty compatibility index exists only because Dashboards 2.19 requires both legacy index patterns.
- Keep the existing trace-based Eshop dependency dashboard active.
- Gate the future upgrade on an official numeric Collector image whose changelog contains #49570:
  - Update the pinned Collector version.
  - Replace `debug/metrics` with the authenticated OpenSearch metrics exporter targeting `eshop-app-metrics`.
  - Persist application metrics and `traces_service_graph_request_total`, `traces_service_graph_request_failed_total`, client/server latency histograms, unpaired spans, and dropped spans.
  - Restore the .NET runtime dashboard and add an Eshop Vega topology dashboard grouped by `client`, `server`, and `connection_type`.
  - Show request count, failure count/rate, and client/server latency on topology edges.
- Do not translate service-graph metrics into legacy Data Prepper documents. Retain the compatibility index until a tested Dashboards release removes that requirement.

## Validation

- Run Compose configuration validation and confirm Data Prepper remains absent.
- Test bootstrap against both a fresh OpenSearch volume and the current preserved volume; reruns must leave the compatibility index unchanged.
- Sign in as `eshop_operator` and verify:
  - Trace Analytics setup succeeds.
  - Traces and Spans pages display `otel-v1-apm-span-*` records.
  - Trace selection opens its span timeline and details.
  - The service-map view is empty and is not presented as current topology.
  - The operator cannot write to the compatibility index.
- Confirm all five configurators exit with code `0`, runtime services remain healthy, and no authorization or index-not-found errors appear.
- For the future release, validate all supported metric types and every service-graph metric, then verify topology counts, failures, and latency against generated API, gateway, database, and RabbitMQ traffic.

## Assumptions

- OpenSearch and Dashboards remain pinned to `2.19.6`.
- The selected strategy is “unlock now, upgrade later.”
- No application API, telemetry instrumentation, trace schema, or credential interface changes.
- Historical indexes and named volumes remain preserved.
- A future Collector metrics release enables a custom standards-based topology dashboard, but does not by itself populate OpenSearch’s proprietary native service-map index.
