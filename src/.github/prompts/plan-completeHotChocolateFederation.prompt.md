## Plan: Complete Hot Chocolate Federation

Keep the runtime entirely on Hot Chocolate: module schemas use Hot Chocolate's Apollo Federation v2 compatibility layer, while the public gateway uses Hot Chocolate Fusion. Replace the invalid text `gateway.fgx` with a real build-time composed Fusion v15 archive, then add actual cross-subgraph relationships only where the domain requires them.

**Steps**
1. Confirm the protocol boundary: retain `HotChocolate.ApolloFederation` v15 in the three module schemas for Federation v2 SDL, and retain `HotChocolate.Fusion` v15 in the gateway. Do not add Apollo Server, Apollo Router, or Apollo Gateway.
2. Preserve the existing named source schemas and routes in the combined API: `catalog` at `/graphql/catalog`, `basket` at `/graphql/basket`, and `ordering` at `/graphql/ordering`.
3. Export each named source schema to a repository-owned composition directory and pair it with a v15 subgraph config containing a unique name and Docker runtime URL (`http://api:8080/graphql/<module>`). This depends on the API schemas building successfully.
4. Pack the three source schemas with `HotChocolate.Fusion.CommandLine` v15 using `fusion subgraph pack`, then compose them with `fusion compose` into a real gateway package/archive. Replace the current plain-text file only after composition succeeds.
5. Keep the v15 gateway loading API, `AddFusionGatewayServer().ConfigureFromFile(...)`, unless package versions are deliberately upgraded together. Ensure the composed artifact is copied/mounted at the configured path.
6. Add a repeatable composition command or build script so schema export, packing, and composition run locally and in CI; fail CI on composition diagnostics.
7. Model cross-subgraph relationships only where useful. For example, Basket and Ordering currently expose scalar `ProductId` values but do not contribute fields to the Catalog `Product` entity. Add entity stubs/lookups only if clients need queries that traverse basket/order items into product fields.
8. After the base gateway works, optionally evaluate migration from Apollo-compatible `[Key]`/`[ReferenceResolver]` APIs to native Fusion lookup APIs. Treat this as a separate version-aligned refactor, not a prerequisite.

**Relevant files**
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Modules\Catalog\Catalog\GraphQL\CatalogGraphQLExtensions.cs` — existing per-schema Apollo Federation registration.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Modules\Basket\Basket\GraphQL\BasketGraphQLExtensions.cs` — existing per-schema Apollo Federation registration.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Modules\Ordering\Orders\GraphQL\OrderingGraphQLExtensions.cs` — existing per-schema Apollo Federation registration.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Bootstrapper\Api\Program.cs` — named schema endpoint mappings.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Bootstrapper\Gateway\Program.cs` — Fusion v15 gateway registration and archive loading.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\Bootstrapper\Gateway\gateway.fgx` — currently invalid plain-text recipe; replace with composed v15 output.
- `c:\_0PROJETOS\.NET\SINTER\samples\MehmedCourse\src\docker-compose.override.yml` — gateway archive mount and API dependency.

**Verification**
1. Build `Bootstrapper/Api/Api.csproj` and `Bootstrapper/Gateway/Gateway.csproj`.
2. Verify each `/graphql/<module>?sdl` endpoint returns HTTP 200 and valid Federation v2 SDL.
3. Run v15 subgraph packing and gateway composition; require zero composition errors.
4. Start Docker Compose and query `{ __typename }` through `http://127.0.0.1:5001/graphql`; require HTTP 200.
5. Query one root field from each module through the single gateway endpoint.
6. If cross-subgraph entity fields are added, run a query that begins in Basket or Ordering and resolves Catalog product fields, confirming the Fusion query plan crosses schemas.
7. Re-run CORS preflight from `http://127.0.0.1:4200` and the architecture tests if shared contracts or module boundaries change.

**Decisions**
- Apollo is used as a schema interoperability specification through a Hot Chocolate package, not as the gateway/runtime product.
- Hot Chocolate Fusion remains the only gateway and query planner.
- The single API container with three named schemas is retained; it provides logical subgraphs but not independent deployment/scaling.
- Cross-subgraph entity traversal is not assumed merely because `[Key]` exists; it must be modeled explicitly.
- The missing `.codemap/skills/federation-graphql.md` documentation target is a separate repository-maintenance issue.
