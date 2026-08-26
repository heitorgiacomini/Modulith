# ABP-Inspired Basket Read-Cache Refactor

## Summary

- The current converters are valid infrastructure code, but caching rich domain entities directly is not the preferred enterprise design here. ABP likewise recommends a dedicated cache-item/DTO when entities are not naturally JSON-serializable and treats entity caches as read-only.
- Adopt those ABP principles using native Microsoft caching; do not add `Volo.Abp.*` dependencies.
- Make the database authoritative, cache only the Basket query projection, and ensure every successful mutation invalidates affected entries.

## Implementation Changes

### Typed, tenant-aware cache

- Replace the custom JSON converters with internal, serializer-friendly `BasketReadModel` and `BasketItemReadModel` records containing only the fields required by `ShoppingCartDto`.
- Add a module-local `IBasketCache` implementation responsible for typed serialization, key construction, expiration, logging, and `Get`, `Set`, `Remove`, and `RemoveMany` operations.
- Use `JsonSerializerDefaults.Web`; remove `ShoppingCartConverter`, `ShoppingCartItemConverter`, and all serialization knowledge from domain entities.
- Introduce validated `Basket:Cache` configuration:
  - `KeyPrefix`: default `eshop:`
  - `AbsoluteExpiration`: default `00:10:00`
- Use versioned keys formatted as `{prefix}basket:v1:{tenantId:N}:{userName}`. Require an active tenant and preserve username casing because database lookup is currently case-sensitive.
- Fail open when Redis reads, writes, or removals fail: log a structured warning without payload contents and fall back to PostgreSQL. Re-throw caller cancellation. Treat malformed cache JSON as a miss and best-effort remove it.
- Use absolute rather than sliding expiration so missed invalidation or a process crash cannot preserve stale baskets indefinitely.

### Read/write separation

- Introduce `IBasketReadRepository.GetBasketAsync(userName, cancellationToken)` returning `BasketReadModel`.
- Implement the database reader with a direct EF Core `AsNoTracking()` projection, avoiding aggregate construction and fixing the existing ineffective `AsNoTracking()` call.
- Decorate only the read repository with `CachedBasketReadRepository`: return a cache hit, otherwise load PostgreSQL, cache the successful result, and return it. Do not negative-cache missing baskets.
- Update `GetBasketHandler` to use the read repository and map the read model to the unchanged `ShoppingCartDto`.
- Keep GraphQL basket-list queries database-backed; this cache remains specific to the authenticated single-basket REST query.

### Command repository and invalidation

- Remove `CachedBasketRepository`. Make `IBasketRepository` write-oriented:
  - `GetBasketAsync(userName, cancellationToken)` always returns a tracked aggregate.
  - Remove the `asNoTracking` argument.
  - Remove the username argument from `SaveChangesAsync`.
- Commands must never restore or mutate aggregates obtained from Redis.
- After successful persistence, invalidate rather than warm the read cache:
  - create basket;
  - add or remove an item;
  - delete basket;
  - checkout, only after its database transaction commits;
  - product-price updates, for every distinct affected basket username after `SaveChangesAsync` succeeds.
- Cache failures must not turn an already-committed database operation into an apparent command failure. The ten-minute absolute expiration bounds possible stale data if invalidation cannot reach Redis.
- Preserve the existing REST, GraphQL, database, and integration-event contracts. No EF migration is required.

## Tests and Acceptance

- Add `BasketTests`, register it in the solution, and expose Basket internals only to that test assembly.
- Verify cache miss/load/set, cache hit without database access, tenant isolation, versioned key formatting, configured expiration, malformed-entry recovery, Redis outage fallback, and cancellation propagation.
- Verify the read-model JSON round trip retains IDs, quantities, prices, colors, names, and item ordering without custom converters.
- Verify tracked command reads never consult Redis.
- Verify invalidation after create, add, remove, delete, successful checkout commit, and multi-basket price updates.
- Verify failed database writes and rolled-back checkout do not invalidate; cache-removal failure after commit does not change a successful command result.
- Run the complete solution build and all .NET tests. When Redis/PostgreSQL Compose infrastructure is available, smoke-test cache hits, tenant isolation, expiration, checkout deletion, and price-update invalidation.

## Rollout and Assumptions

- Treat Redis as disposable derived state. Deploy all API instances together and begin with a cold `v1` namespace.
- Document one-time deletion of legacy `{tenantId}:basket:{userName}` keys after deployment; do not flush unrelated Redis data.
- Configure a distinct `KeyPrefix` per application/environment when Redis is shared.
- Do not put `TenantId`, auditing fields, or domain behavior in the cached payload; tenant isolation belongs to the cache key, and cached data is a read projection only.
- Do not build a reusable Shared caching framework yet. Promote the module-local typed cache pattern only after a second module demonstrates the same need.
