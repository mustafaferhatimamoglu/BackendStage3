# Pathways

This document summarizes the important paths and flows used by this repository.

## Repository Paths
- Solution: `BackendStage3.sln`
- Building Blocks: `buldng-blocks/BuldngBlocks.Messagng`
- Product:
  - Domain: `product-svc/Product.Doman`
  - Application: `product-svc/Product.Applcaton`
  - Infrastructure: `product-svc/Product.Infrastructure`
- Auth:
  - Infrastructure: `auth-svc/Auth.Infrastructure`
- Tests:
  - Product: `tests/Product.Tests`
  - Auth: `tests/Auth.Tests`
- Calibration env template: `deploy/.env.example`

## Data Flow (Tests)
- Product Create
  1. Command handler inserts Product (Pending)
  2. Saga publishes `product.created` via `RabbtPublsher`
  3. Saga simulates ack and marks Product Active (or Failed on error)
  4. Cache invalidation for `products:all` and `products:{id}`

- Product Update
  1. Command handler updates Product and saves
  2. Invalidates caches
  3. Publishes `product.updated`

- Auth Tokens
  1. `JwtTokenServce.IssueAsync` creates access token and refresh token entry
  2. Rotation: old refresh token is revoked and a new one is stored

## Configuration Pathways
- JWT: `JWT__Issuer`, `JWT__Audience`, `JWT__Key`, `JWT__AccessTokenMinutes`, `JWT__RefreshTokenDays`
- ConnectionStrings: `ConnectionStrings__AuthDb`, `ConnectionStrings__ProductDb`
- Redis: `Redis__Configuration`
- RabbitMQ: `RABBITMQ__HOST`, `RABBITMQ__USER`, `RABBITMQ__PASS`

Use `deploy/.env.example` to calibrate env vars during local development.

