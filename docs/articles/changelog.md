


## [unreleased]








### 2026-01-04




#### ⚙️ Miscellaneous Tasks


- Add initial changelog and git-cliff configuration (Heitor Giacomini) - ([beca2c4](https://github.com/heitorgiacomini/Modulith/commit/beca2c49c652d07f194804e737362ed66c63770a))





### 2026-01-03




#### 🐛 Bug Fixes


- Fix path for artifact upload in GitHub Actions (Heitor Giacomini) - ([7afe23a](https://github.com/heitorgiacomini/Modulith/commit/7afe23a0ed3b1f76798e0ca109044cc23e751add))






#### 🧱 Other


- Change artifact upload path for GitHub Pages (Heitor Giacomini) - ([31319bd](https://github.com/heitorgiacomini/Modulith/commit/31319bda886712dd74ed74e79170ec6ec6557ba4))






- Add PaginationRequest class and related documentation

- Introduced the PaginationRequest class in the Shared.Pagination namespace, implementing IEquatable for equality comparison.
- Added properties PageIndex and PageSize with appropriate accessors.
- Created YAML documentation for PaginationRequest, including constructor and property details.
- Updated Shared.Pagination namespace documentation to include the new PaginationRequest class.
- Modified table of contents to reflect the addition of PaginationRequest and its related items. (Heitor Giacomini) - ([b64a2f7](https://github.com/heitorgiacomini/Modulith/commit/b64a2f799d45dbb39958779ee581eb35a1d17e42))





### 2026-01-02




#### ⛰️  Features


- Implement CQRS pattern with MediatR and add domain event handling (Heitor Giacomini) - ([a6f33f8](https://github.com/heitorgiacomini/Modulith/commit/a6f33f868923ad04b22754030a3d9b477b7bec65))





### 2025-12-25




#### 🚜 Refactor


- Refactor Docker, messaging, and compose for modularity

- Switch Docker images from Alpine to standard .NET, update build to include new modular project references.
- Add Dockerfile.original with multi-stage build and publish.
- Register OutboxProcessor and add messaging references in Ordering.
- Add BasketCheckoutIntegrationEventHandler for event-driven order creation.
- Overhaul docker-compose files: new api service, environment config, ports, and volumes for secrets/certs.
- Improve type safety in CreateBasketEndpoint.
- Overall, enhance modularity, messaging, and production readiness. (web admin) - ([03d0335](https://github.com/heitorgiacomini/Modulith/commit/03d033521e8a1e9a73d183286fb9ebda0cf20c79))





### 2025-12-24




#### 🧱 Other


- Implement transactional outbox pattern in Basket service

Introduce OutboxMessage entity and OutboxMessages table with new migration. Add OutboxProcessor background service to publish events reliably via MassTransit. Refactor checkout basket flow to write integration events to the outbox within a transaction. Add new endpoint and handler for basket checkout. Remove old migrations and update DbContext/model snapshot. Add BasketCheckoutIntegrationEvent and update project structure. Enables reliable, eventual-consistent event publishing. (web admin) - ([58fa2dc](https://github.com/heitorgiacomini/Modulith/commit/58fa2dcd32ce4bbb9b4af3fa3723ac1c3c123bdf))






- Add Ordering module with full CRUD and CQRS support

Introduces a new modular Ordering system with domain models, value objects, and event handling. Implements EF Core data layer, migrations, and DTOs. Adds Carter endpoints and MediatR handlers for create, read (by id and paginated), and delete operations. Integrates with app infrastructure and follows DDD, CQRS, and clean architecture principles. (web admin) - ([d1ba890](https://github.com/heitorgiacomini/Modulith/commit/d1ba8909042bd387937c6f499b567318db7a367c))





### 2025-12-23




#### 🧱 Other


- Require auth for all basket endpoints; refactor handlers

Added .RequireAuthorization() to all basket API endpoints to enforce authentication. In CreateBasketEndpoint, now set UserName from the authenticated user. Refactored route handler signatures, improved code style, and clarified variable types for consistency. (web admin) - ([c4cb547](https://github.com/heitorgiacomini/Modulith/commit/c4cb5479d6ea9b54c1a7b4d832f78e0977f1c031))






- Add Keycloak auth, RabbitMQ config, and Docker services

- Integrated Keycloak authentication and authorization, including config and middleware setup.
- Switched MassTransit to use RabbitMQ with externalized config in appsettings.json.
- Updated Docker Compose to include postgres, redis, seq, rabbitmq, and keycloak services with appropriate environment variables and ports.
- Changed webApp.Run() to RunAsync() and performed minor code cleanup. (web admin) - ([b968bd5](https://github.com/heitorgiacomini/Modulith/commit/b968bd5029fbc17c031be590338dffa9743390f4))





### 2025-12-22




#### 🧱 Other


- Handle product price updates across Catalog and Basket

Implement integration event flow for product price changes:
- Catalog now publishes ProductPriceChangedIntegrationEvent via MassTransit when a product's price is updated.
- Basket service consumes this event and updates relevant basket item prices using a new command and handler.
- Added validation for update commands.
- Ensures basket prices stay in sync with catalog changes. (web admin) - ([d6bb8fd](https://github.com/heitorgiacomini/Modulith/commit/d6bb8fd42937568735d78ea94911e97d2dddd234))






#### 🚜 Refactor


- Extract CQRS contracts & add messaging support (web admin) - ([e6709cf](https://github.com/heitorgiacomini/Modulith/commit/e6709cfe9e8e9413068d08184d46f46f1117559b))






- Introduce Shared.Contracts & split Catalog module (web admin) - ([2bfd5d2](https://github.com/heitorgiacomini/Modulith/commit/2bfd5d2ebf766ba2dd2ad6689ca722caeec46618))





### 2025-12-21




#### 🧱 Other


- Add Redis distributed cache for shopping cart data

Introduces Redis as a distributed cache by adding a Redis service to docker-compose, configuring StackExchangeRedisCache in the API, and updating appsettings.json with a Redis connection string. Implements custom JSON converters for ShoppingCart and ShoppingCartItem to ensure correct (de)serialization when caching. Refactors CachedBasketRepository to use these converters. Cleans up unused code and updates package references. This improves performance and scalability for basket operations. (web admin) - ([e468a13](https://github.com/heitorgiacomini/Modulith/commit/e468a1332f812e306a0d959fbfd781788277ac4d))






- Add distributed cache for basket data with decorator

Introduced CachedBasketRepository to cache basket data per user using IDistributedCache. Updated IBasketRepository and implementations to support cache invalidation via userName parameter in SaveChangesAsync. Refactored DI registration to use Scrutor's Decorate for repository wrapping. Added HasContent string extension for null/empty checks. Updated package versions and command handlers for cache consistency. Improves basket performance and cache management. (web admin) - ([cc46315](https://github.com/heitorgiacomini/Modulith/commit/cc463155edf0cc521186795089b1c14373ea4fb2))





### 2025-12-20




#### 🚜 Refactor


- Refactor basket handlers to use repository abstraction

Replaced direct DbContext access in basket feature handlers with a new IBasketRepository interface and BasketRepository implementation. Updated DI registration, adjusted validation style, and standardized type usage. This improves separation of concerns, testability, and encapsulates basket data access logic. (web admin) - ([8b5763c](https://github.com/heitorgiacomini/Modulith/commit/8b5763cd497f70828ba375cc59f9ce452f7706ff))





### 2025-12-19




#### 🧱 Other


- Implement Basket CRUD API with CQRS, Carter, and Validation

Add full CRUD endpoints and CQRS handlers for Basket using MediatR, Carter, and FluentValidation. Introduce DTOs, custom exceptions, and validators. Refactor Program.cs for modular registration of endpoints and behaviors. Add MediatRExtension for streamlined setup. Update namespaces, global usings, and configuration files for consistency and maintainability. (web admin) - ([5ae7873](https://github.com/heitorgiacomini/Modulith/commit/5ae787378516291f40980142722e849b3ab718f9))





### 2025-12-17




#### 🧱 Other


- Initial Basket data layer with EF Core and migrations

Added BasketDbContext, ShoppingCart models, entity configurations, and initial EF Core migrations. Registered DbContext and interceptors in BasketModule. Updated .gitignore and solution items. Added EF CLI script for migrations. Lays groundwork for basket persistence and integration. (web admin) - ([8339fb9](https://github.com/heitorgiacomini/Modulith/commit/8339fb967662f65fc61a7a7d68e65736027f8add))





### 2025-12-16




#### 🧱 Other


- Integrate Serilog and Seq for structured logging

- Replace default logging with Serilog, reading config from appsettings.json
- Add Serilog.AspNetCore and Serilog.Sinks.Seq packages
- Configure console and Seq sinks; add enrichment and log level overrides
- Add Seq service to docker-compose for centralized log collection
- Enable Serilog request logging middleware in Program.cs
- Switch to CustomExceptionHandler for global error handling
- Add MediatR LoggingBehavior for request/response logging and performance warnings
- Remove ILogger from CreateProductHandler; rely on pipeline logging
- Remove color customizations from settings.json (web admin) - ([c8bee70](https://github.com/heitorgiacomini/Modulith/commit/c8bee702713d87d905d7f11f4f47aea6ba9e10ac))





### 2025-12-13




#### 🧱 Other


- Add centralized exception handling and custom exceptions

Introduced custom exception classes (NotFoundException, BadRequestException, InternalServerException, ProductNotFoundException) for more descriptive error management. Updated product handlers to throw ProductNotFoundException when a product is not found. Added CustomExceptionHandler to log errors and return standardized ProblemDetails responses with appropriate HTTP status codes. Registered new exceptions and handler in global usings. Also included minor code cleanups and parameter corrections. These changes improve error reporting, ensure consistent API error responses, and centralize exception handling logic. (web admin) - ([fcd0099](https://github.com/heitorgiacomini/Modulith/commit/fcd009936ecc9242e03309930216e46e013ccbc4))






- ValidationBehavior (web admin) - ([7bb17a1](https://github.com/heitorgiacomini/Modulith/commit/7bb17a19886ca95608afd36a40b0f99b31be2f3e))





### 2025-12-12




#### 🧱 Other


- Endpoints (web admin) - ([f0a40d3](https://github.com/heitorgiacomini/Modulith/commit/f0a40d3a10a39d7be90b6e841c8b325f5a9399f9))





### 2025-12-11




#### 🧱 Other


- Handlers (web admin) - ([3cf001a](https://github.com/heitorgiacomini/Modulith/commit/3cf001ae37916a7bff2bbb1ed35b970b9e9cf2a6))





### 2025-12-10




#### 🧱 Other


- Adding handlers (web admin) - ([726e1e6](https://github.com/heitorgiacomini/Modulith/commit/726e1e6d1725a50a42b58e957cbd354a9bb233bf))





### 2025-12-09




#### 🧱 Other


- Add CQRS abstractions and restructure Catalog module

Introduced CQRS interfaces (ICommand, IQuery, etc.) in Shared.CQRS to support MediatR-based command/query handling. Added skeletons for product features (CreateProduct, GetProductById) and a ProductDto record. Updated Catalog.csproj to organize feature folders. Reformatted migration, event, seeder, and module files for consistency and clarity. These changes lay the foundation for a CQRS architecture and improve code organization. (web admin) - ([07b92d3](https://github.com/heitorgiacomini/Modulith/commit/07b92d34bcb1db361d57f4b5907aa519da191eb5))





### 2025-12-08




#### 🧱 Other


- Add MediatR and EF Core interceptors to CatalogModule

Refactor `AddCatalogModule` to register MediatR for domain event handling and use dependency injection for EF Core interceptors.

Introduce `DispatchDomainEventsInterceptor` to automatically dispatch domain events during `SaveChanges`. Register `AuditableEntityInterceptor` and `DispatchDomainEventsInterceptor` as scoped services.

Add `IDataSeeder` registration for data seeding. Improve modularity and support for domain-driven design patterns. (web admin) - ([b947763](https://github.com/heitorgiacomini/Modulith/commit/b947763e3cd31ecab3db9c06f205b2ca2d8190e9))






- Add AuditableEntityInterceptor for entity auditing

Introduced `AuditableEntityInterceptor` to handle automatic
auditing of entities during database operations. Updated the
`CatalogModule` to register the interceptor in the `AddDbContext`
configuration.

Refactored the `IEntity` interface to include a placeholder
for future auditing functionality. Removed the unused and
empty `Interceptors` class to improve code clarity.

Added logic to populate auditing fields (`CreatedBy`,
`CreatedAt`, `LastModifiedBy`, `LastModified`) in the
`AuditableEntityInterceptor`. Included an extension method
to detect changes in owned entities.

These changes centralize auditing logic, improve maintainability,
and prepare the codebase for future enhancements. (web admin) - ([0092223](https://github.com/heitorgiacomini/Modulith/commit/0092223daad6ed30394a77107a7a664f5c832a35))






- Readme (web admin) - ([27d3ef1](https://github.com/heitorgiacomini/Modulith/commit/27d3ef17905615ab9a5161a57ef06b1f1dc2da85))





### 2025-12-07




#### 🧱 Other


- Update README title for clarity (web admin) - ([f352ae9](https://github.com/heitorgiacomini/Modulith/commit/f352ae9c6e610e44b959ead4d07c3e8156cd3f35))






- Add Docker support and PostgreSQL integration

Introduced Docker support with `Dockerfile`, `.dockerignore`, and Docker Compose configurations for containerized deployment. Integrated PostgreSQL as the database for the Catalog module, including `CatalogDbContext`, migrations, and seeding.

Refactored `Program.cs` for clarity and updated `CatalogModule` to handle database setup. Enhanced domain models to enforce value-type IDs and added domain events for `Product`.

Improved solution structure with `GlobalUsings.cs`, updated project references, and added initial data seeding. These changes enhance scalability, maintainability, and deployment flexibility. (web admin) - ([f9e0949](https://github.com/heitorgiacomini/Modulith/commit/f9e0949654603e8be2163e161284a0fd45e9597c))





### 2025-12-06




#### 🧱 Other


- Cho satanas (web admin) - ([2b2a458](https://github.com/heitorgiacomini/Modulith/commit/2b2a4580015a7d372fab3e099440cd5e8d220222))






- Pra ficar legal, pagode na cohab no maior astral (web admin) - ([6fba1ef](https://github.com/heitorgiacomini/Modulith/commit/6fba1efcfa6dde316d369bd40b7c26c659e64f2e))





### 2025-12-03




#### 🧱 Other


- Modules (web admin) - ([b337bc7](https://github.com/heitorgiacomini/Modulith/commit/b337bc7ccf62e0844b1b803fe23c1d242ef65ea5))






- First (web admin) - ([bbe093e](https://github.com/heitorgiacomini/Modulith/commit/bbe093ee2164d68b10c0b812f5fe011d9b17bb77))
<!-- generated by git-cliff -->
