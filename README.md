Modular Monolith Architecture Example

![alt text](image.png)


## Whats Including In This Repository
We have implemented below **architectural patterns in this repository**.
* Modular Monoliths (Modulith) Architecture
* Vertical Slice Architecture (VSA)
* Domain-Driven Design (DDD)
* Command Query Responsibility Segregation (CQRS)
* Outbox Pattern for Reliable Messaging

#### Catalog module which includes; 
* ASP.NET Core Minimal APIs and latest features of .NET8 and C# 12
* **Vertical Slice Architecture** implementation with Feature folders and single .cs file includes different classes in one file
* CQRS implementation using MediatR library
* CQRS Validation Pipeline Behaviors with MediatR and FluentValidation
* Use Entity Framework Core Code-First Approach and Migrations on PostgreSQL Database
* Use Carter for Minimal API endpoint definition
* Cross-cutting concerns Logging, Global Exception Handling and Health Checks

#### Basket module which includes; 
* Using Redis as a Distributed Cache over PostgreSQL database
* Implements Proxy, Decorator and Cache-aside patterns
* Publish BasketCheckoutEvent to RabbitMQ via MassTransit library
* Implement Outbox Pattern For Reliable Messaging w/ BasketCheckout Use Case

#### Module Communications; 
* Sync Communications between Catalog and Basket Modules with In-process Method Calls (Public APIs)
* Async Communications between Modules w/ RabbitMQ & MassTransit for UpdatePrice Between Catalog-Basket Modules

#### Identity module which includes; 
* OAuth2 + OpenID Connect Flows with Keycloak
* Setup Keycloak into Docker-compose file for Identity Provider as a Backing Service
* JwtBearer token for OpenID Connect with Keycloak Identity

#### Ordering module which includes; 
* Implementing DDD, CQRS, and Clean Architecture with using Best Practices
* Implement Outbox Pattern For Reliable Messaging w/ BasketCheckout Use Case