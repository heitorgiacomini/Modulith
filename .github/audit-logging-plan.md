# Audit Logging Implementation Plan

## Goal

- [x] Create `.github/audit-logging-plan.md`.
- [x] Use checkboxes for every implementation, migration, and validation action.
- [x] Follow ABP's [`IFullAuditedObject`](https://abp.io/docs/api/abp/3.2/Volo.Abp.Auditing.IFullAuditedObject.html) approach.
- [x] Keep `Entity<T>` as the identity-only base and use opt-in audited bases.
- [x] Do not introduce an audit entity, shared audit model, or audit database table.
- [x] Use Serilog and Seq for searchable historical change events.

## Entity Auditing

- [x] Keep `IEntity` as a marker and `IEntity<T>` as the identity contract.
- [x] Separate creation, modification, deletion, and soft-delete capability interfaces.
- [x] Use `DateTimeOffset` and `Guid?` in the audited capability contracts.
- [x] Use non-nullable `bool IsDeleted` with `false` as its default.
- [x] Add opt-in audited entity and aggregate base classes.
- [x] Store all audit timestamps in UTC.
- [x] Preserve creation metadata when entities are modified or deleted.
- [x] Preserve original deletion metadata if an entity is already deleted.

## Current User Resolution

- [x] Add a scoped current-user provider to Shared infrastructure.
- [x] Resolve the authenticated user from `IHttpContextAccessor`.
- [x] Read the stable Keycloak `sub` claim.
- [x] Parse the `sub` claim as `Guid?`.
- [x] Return `null` when the claim is absent or invalid.
- [x] Return `null` for migrations, seeding, message consumers, outbox processing, and other background operations.
- [x] Do not store usernames as the authoritative audit identity.
- [x] Remove the hard-coded `"Melchior"` actor.

## EF Core Interceptor

- [x] Inject the current-user provider and structured logger into `AuditableEntityInterceptor`.
- [x] Set creation metadata for entities in the `Added` state.
- [x] Set modification metadata for entities in the `Modified` state.
- [x] Convert entities in the `Deleted` state into soft-deleted `Modified` entities.
- [x] Set `IsDeleted`, `DeletedAt`, and `DeletedBy` during soft deletion.
- [x] Detect changes to owned entities and update their aggregate's modification metadata.
- [x] Capture pending structured audit events during `SavingChanges`.
- [x] Emit captured events only after `SavedChanges` succeeds.
- [x] Discard captured events when `SaveChangesFailed` executes.
- [x] Prevent duplicate events when `SaveChanges` is retried.
- [x] Support synchronous and asynchronous save operations.

## Structured Seq Events

- [x] Log the module name.
- [x] Log the entity type.
- [x] Log the entity ID.
- [x] Log the operation as `Created`, `Modified`, or `Deleted`.
- [x] Log the actor ID.
- [x] Log the UTC timestamp.
- [x] Log the request trace or correlation ID.
- [x] Log only properties that changed.
- [x] Record redacted old and new values where safe.
- [x] Exclude passwords and secrets.
- [x] Exclude access and refresh tokens.
- [x] Exclude authorization headers.
- [x] Exclude complete payment information.
- [x] Exclude any property explicitly marked as sensitive.
- [x] Add an attribute or configuration mechanism for excluding sensitive properties.
- [x] Do not recursively audit audit-log emission.

## Soft-Delete Queries

- [x] Add a reusable EF Core soft-delete filter for entities implementing `ISoftDelete`.
- [x] Apply the filter to `AccountsDbContext`.
- [x] Apply the filter to `BasketDbContext`.
- [x] Apply the filter to `CatalogDbContext`.
- [x] Apply the filter to `OrderingDbContext`.
- [x] Ensure normal REST queries exclude soft-deleted records.
- [x] Ensure normal GraphQL queries exclude soft-deleted records.
- [x] Use `IgnoreQueryFilters()` only in trusted administrative or restoration flows.
- [x] Review aggregate children and owned entities for consistent deletion behavior.
- [x] Preserve current endpoint response contracts after converting deletes to soft deletes.

## Database Migrations

- [x] Backfill missing creation timestamps before making `CreatedAt` non-nullable.
- [x] Backfill null deletion flags with `false` before applying `NOT NULL`.
- [x] Change actor columns from strings to nullable UUID columns.
- [x] Define a safe conversion strategy for existing non-Guid actor values such as `"Melchior"`.
- [x] Generate the Accounts migration.
- [x] Generate the Basket migration.
- [x] Generate the Catalog migration.
- [x] Generate the Ordering migration.
- [x] Remove audit and soft-delete columns from `OutboxMessages`.
- [x] Verify migration ordering across all module schemas.
- [x] Verify migrations run through Docker Compose on an empty database.
- [x] Verify migrations run against a database containing existing records.

## Keycloak Authorization

- [x] Add an `audit:read` permission to the existing `myrealm` configuration.
- [x] Keep audit authorization in the existing realm.
- [x] Do not create another Keycloak realm.
- [x] Do not reuse Ordering permissions for auditing.
- [x] Reserve `audit:read` for future protected audit access.
- [x] Do not add an audit API or Angular audit screen in this implementation.

## Automated Tests

- [x] Test creation metadata for an authenticated user.
- [x] Test creation metadata for a background operation.
- [x] Test modification metadata.
- [x] Test preservation of creation metadata after modification.
- [x] Test soft-deletion metadata.
- [x] Test repeated soft deletion.
- [x] Test missing and invalid `sub` claims.
- [x] Test default filtering of soft-deleted records.
- [x] Test explicit access with `IgnoreQueryFilters()`.
- [x] Test owned-entity changes.
- [x] Test transitive `ISoftDelete` interface detection.
- [x] Test physical deletion and absence of filters for identity-only entities.
- [x] Test that `OutboxMessage` is neither audited nor soft-deletable.
- [x] Test successful structured audit-event emission.
- [x] Test that failed saves do not emit successful audit events.
- [x] Test sensitive-value redaction.
- [x] Test that existing REST contracts remain unchanged.
- [x] Test that existing GraphQL contracts remain unchanged.
- [x] Run .NET tests through the documented Docker workflow.
- [x] Run Angular boundary checks and the production build through Docker Compose.
- [x] Run architecture tests and verify module boundaries remain valid.

## Completion Criteria

- [x] Confirm auditing and soft deletion are opt-in capability contracts.
- [x] Confirm no audit-history entity or audit table was added.
- [x] Confirm all entity audit timestamps use UTC.
- [x] Confirm all actor columns use nullable `Guid`.
- [x] Confirm deletes use soft deletion only for entities implementing `ISoftDelete`.
- [x] Confirm Seq contains redacted historical change events.
- [x] Confirm unsuccessful transactions do not appear as successful audit changes.
- [x] Confirm the complete application starts and migrates successfully with Docker Compose.
