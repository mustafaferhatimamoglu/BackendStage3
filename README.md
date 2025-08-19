# Backend Stage 3 – Repository Calibration & Pathways

This repo contains a .NET 8 solution set up for microservices development and unit testing. It includes shared messaging blocks, minimal Auth/Product infrastructure for token and caching logic, and unit tests that run entirely in‑memory (no external services).

Use this README to calibrate your environment (configs) and understand key pathways (folders, endpoints, and commands).

## Contents
- Solution: `BackendStage3.sln`
- Building blocks: `buldng-blocks/BuldngBlocks.Messagng`
- Product (domain/application/infrastructure): `product-svc/*`
- Auth (infrastructure-only for tests): `auth-svc/Auth.Infrastructure`
- Tests: `tests/Product.Tests`, `tests/Auth.Tests`

## Quick Start (Local Tests)
- Prereq: .NET 8 SDK
- Commands (run from `backend-stage3/`):
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test -v minimal`

Tests use EF Core InMemory and an in-memory adapter for IDistributedCache. No Docker required for unit tests.

## Calibration: Configuration
Even though unit tests do not require external config, here are the standard environment variables to keep consistent across services (12‑Factor):

- JWT (Auth/Product APIs)
  - `JWT__Issuer`
  - `JWT__Audience`
  - `JWT__Key` (long, random string)
  - `JWT__AccessTokenMinutes` (optional)
  - `JWT__RefreshTokenDays` (optional)
- SQL Server
  - `ConnectionStrings__AuthDb` (e.g., `Server=sql-auth;Database=AuthDb;User Id=sa;Password=<pwd>;Encrypt=False;TrustServerCertificate=True`)
  - `ConnectionStrings__ProductDb` (e.g., `Server=sql-product;Database=ProductDb;User Id=sa;Password=<pwd>;Encrypt=False;TrustServerCertificate=True`)
- RabbitMQ
  - `RABBITMQ__HOST` (default `rabbitmq`)
  - `RABBITMQ__USER` (default `guest`)
  - `RABBITMQ__PASS` (default `guest`)
- Redis (Product query cache)
  - `Redis__Configuration` (default `redis:6379`)

A ready-to-copy template is in `deploy/.env.example`.

## Pathways

- Solution:
  - `BackendStage3.sln`
- Building Blocks:
  - `buldng-blocks/BuldngBlocks.Messagng` – `IMessagePublisher`, `RabbitPublisher`, `RabbitConfig`
- Product:
  - Domain: `product-svc/Product.Doman` (entities and enums)
  - Application: `product-svc/Product.Applcaton` (MediatR commands/queries, validation)
  - Infrastructure: `product-svc/Product.Infrastructure` (EF Core, cache, publisher, saga)
- Auth:
  - Infrastructure: `auth-svc/Auth.Infrastructure` (POCO `AppUser`, `RefreshToken`, `JwtTokenServce`, `AppDbContext` for tests)
- Tests:
  - Product tests: `tests/Product.Tests` – create + update + cache invalidation + event publishing
  - Auth tests: `tests/Auth.Tests` – issue + rotate refresh tokens

## Typical Commands
- Restore/build/tests
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test -v minimal`

## Extending to Full Services (Optional)
To evolve into full microservices (gateway, auth-svc API, product-svc API, log-svc), add:
- Web API hosts for Auth/Product/Log
- YARP gateway with per-route rate limiting
- Docker Compose with Redis, RabbitMQ, SQL Server, and Seq
- Health checks and Swagger

Use `deploy/.env.example` as the baseline for environment variables.

## Conventions
- C#: .NET 8, nullable enabled
- CQRS: MediatR + FluentValidation
- EF: Migrations for DB-backed services (tests use InMemory context)
- 12‑Factor: configuration via env vars

## Troubleshooting
- Ensure .NET 8 SDK is installed (`dotnet --info`)
- If tests fail to compile, run a clean: `dotnet clean` then rebuild
- If using an IDE, load `BackendStage3.sln`

