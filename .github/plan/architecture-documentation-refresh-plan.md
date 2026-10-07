# Update architecture documentation and diagrams

## Summary

Align architecture documentation and images with the checked-in application and Compose configuration. Preserve the supplied image’s white background, colored headers, recognizable icons, container boundaries, and arrow legends.

## Documentation

- Correct README, introduction, getting started, module/shared architecture guides, and observability documentation. Leave frontend articles and historical changelogs outside this update.
- Describe Angular’s REST calls through gateway `/api` and GraphQL through `/graphql`; distinguish direct API access on port 5004.
- Document four modules in one API process, module-owned PostgreSQL schemas, Basket’s Redis cache, Catalog’s explicit contracts, and the actual integration-event directions.
- Explain Keycloak organization-based tenancy, authorization, independent host rate limits, and API auditing.
- Replace obsolete active-stack descriptions of Seq/OpenSearch with Alloy, Loki, Tempo, Prometheus, and Grafana. Match setup instructions, prerequisites, ports, and links to current configuration.

## Diagrams and preservation

- Refresh three diagrams: runtime architecture, module boundaries and ownership, and observability.
- Runtime: show Angular, Fusion/YARP gateway, modular API, Keycloak, PostgreSQL, Redis, RabbitMQ, and separate startup jobs for schema composition and Keycloak configuration.
- Module boundaries: show Basket’s in-process Catalog lookup, Catalog price events consumed by Basket, and Basket checkout outbox events consumed by Ordering. Accounts exposes REST only.
- Observability: distinguish OTLP traces/metrics, Docker application logs, and audit files; route them through Alloy to their actual stores and show Grafana’s query connections.
- Use consistent vector icons alongside labels, with separate arrow styles for requests, events, authentication, telemetry, and startup artifacts. Provide readable legends and accessible SVG descriptions.
- Maintain editable SVGs and matching PNG exports through the existing diagram generator. Use the same generated assets in README and DocFX documentation.
- Before replacement, rename each affected original with `.old` before its extension, such as `ModularMonolithArchitecture.old.png` and `architecture.old.svg`. Preserve affected copies under `docs/images` as well. Never overwrite an existing archive; use a numbered `.old` basename on collision.
- Correct the generator’s existing `.png.old` backup convention and document regeneration commands.

## Validation

- Check every diagram connection, service, port, schema, and event against source/configuration.
- Render and inspect SVG/PNG outputs for legibility, clipping, overlapping labels, icon consistency, and correct arrow endpoints.
- Verify documentation links, DocFX asset inclusion, and absence of obsolete active-stack claims; retain legitimate historical references.
- Run the available documentation build and report any missing tooling. Confirm archived originals remain byte-identical.

## Assumptions and execution

- This is a documentation and asset change; application behavior and deployment configuration remain unchanged.
- Prefer deterministic vector diagrams and PNG exports for precise labels and reproducible editing.
- Plan Mode prohibits file writes. The first execution action will save this exact plan to `.github/plan/architecture-documentation-refresh-plan.md`, as required by the repository instructions.

## Design correction

Preserve the original illustrated layouts and styling; update information only. Use edited raster masters and SVG companions for the two original illustrations. Restore the original editable two-panel SVG for the combined diagram. The SVG companions embed raster artwork; do not describe them as editable vectors. Keep all original .old assets and archive superseded simplified images without overwriting earlier archives.
