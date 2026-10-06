# Interactive Distributed Trace Explorer and Gateway Routing Plan

## Summary

- Keep the direct application -> OpenTelemetry Collector -> OpenSearch pipeline, with no Data Prepper, Seq, or Serilog.
- Reproduce the OpenSearch trace-group -> trace -> selectable span workflow in the committed custom dashboards.
- Route every Angular business REST and GraphQL request through the gateway.
- Preserve the 10-second tail-sampling decision window while preventing refreshes from replacing the selected trace or span.
- Add internal MediatR spans and replace START/END pairs with one structured completion event.

## Implementation Changes

### Gateway and distributed tracing

- Add `Yarp.ReverseProxy` 2.3.0 to the gateway.
- Expose REST through `http://127.0.0.1:5002/api/{**path}`, strip `/api`, and forward to `http://localhost:5004` locally or `http://api:8080` in Compose.
- Keep GraphQL at `http://127.0.0.1:5002/graphql` and change Angular's REST base to the gateway.
- Forward authorization and W3C trace headers, exclude hop-by-hop headers, and run gateway security, CORS, rate limiting, and request logging before proxy dispatch.
- Retain the direct API port for diagnostics while ensuring Angular does not use it.

### Application spans and event noise

- Add a shared `ActivitySource` registered with OpenTelemetry tracing.
- Wrap each MediatR request in an internal span with request/response metadata and error status.
- Replace each `[START]`/`[END]` pair with one completion event containing request type, response type, elapsed milliseconds, outcome, trace ID, and span ID.
- Emit Warning for slow handlers and Error with the actual exception for failures.
- Never capture HTTP bodies, query values, authorization data, cookies, arbitrary headers, or database parameter values.

### Trace exploration

- Add a root-span-only trace-group table with group, service, count, error rate, average duration, p95, and last seen; group clicks filter the trace list, and trace clicks pin `traceId`.
- Clearly label limits of 500 recent groups and 200 recent traces, preserve root-only `traceGroup`, and keep noise filters visible and removable.
- Replace disconnected trace-detail panels with an integrated clickable tree/waterfall and selected-span inspector whose `selectedSpanId` remains stable during refresh.
- Derive useful database labels from `db.query.text` and expose safe span, HTTP, database, event, exception, messaging, and dropped-data details.
- Display span SQL from `db.query.text` and log messages from `body`; never capture HTTP request or response bodies.

### Correlation and refresh

- Provide selected-span and separately labeled trace-wide event views, with trace-wide counts grouped by span, source, and event type.
- Show SQL from database span attributes and do not misrepresent EF logs on enclosing spans as direct database-span correlation.
- Keep Collector tail sampling at 10 seconds and batching at 2 seconds; show the expected 10-12 second ingestion delay.
- Refresh every 5 seconds without replacing a pinned trace or span, and expose last-updated state plus an explicit follow-latest action.
- Import stable Event Explorer and Trace Explorer saved objects into the Global tenant.

## Interfaces and Compatibility

- New gateway route: `/api/{**path}`; downstream API routes remain unchanged.
- Angular business traffic uses gateway port 5002; Keycloak traffic and static assets remain outside the business gateway.
- Add a shared tracing source for MediatR activities.
- Existing `ILogger<T>`, REST, GraphQL, domain-event, database, and audit contracts remain compatible.
- Keep existing OpenTelemetry, Collector, and OpenSearch versions; add YARP 2.3.0.

## Test Plan

- Unit-test MediatR span nesting, completion logging, timing, failure status, exception attachment, and sensitive-data exclusion.
- Integration-test gateway path rewriting, authorization forwarding, response preservation, CORS, rate limiting, and unavailable downstream behavior.
- Verify Angular REST and GraphQL traffic uses port 5002 and a basket-item trace contains gateway, API, handler, nested-query, and PostgreSQL spans.
- Verify the UI distinguishes product lookup, basket lookup, and inserts, with formatted SQL and redacted parameters.
- Verify selected-span and trace-wide event counts, stable selection across refreshes, sampling-delay messaging, group drill-down, raw documents, and Global tenant links.
- Confirm Collector health and drop/rejection telemetry and confirm no Data Prepper, Seq, or Serilog remains.

## Assumptions

- The Global tenant is the authoritative shared tenant.
- Vega saved objects are preferred over a custom Dashboards plugin.
- The existing reference trace's three PostgreSQL spans contain distinct `db.query.text` values.
- Its eight current events are four MediatR START/END records, three EF command records, and one HTTP completion record; the new handler instrumentation intentionally reduces that noise.
- Tail-sampling completeness is preferred over immediate ingestion, so the 10-second decision window remains.
