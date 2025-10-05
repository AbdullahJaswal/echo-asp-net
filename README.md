# Echo (ASP.NET 9 chat rooms app)

A small, straight‑forward chat app built on ASP.NET Core 9. It exposes a REST API for auth and users, uses JWT bearer
tokens to secure calls, and talks to a SQL Server database. Real‑time chat is done with SignalR so people in the same
room can see messages instantly.

## What’s inside

- ASP.NET Core 9 (C#)
- REST API (Controllers) for auth and basic user info
- JWT authentication (access + refresh tokens)
- SQL Server 2022 (containerized)
- Swagger/OpenAPI in Development
- SignalR for real‑time chat rooms (clients join a room, send/receive messages)

## Quick start (Docker)

You’ll need Docker Desktop (or Docker Engine + Compose).

1) Copy or tweak the .env at the repo root if you want different credentials/ports. The defaults are fine for local
   work.
2) From the project root, start the stack:

```bash
docker compose -f dev.compose.yaml up --build
```

That spins up:

- echo-api (ASP.NET app with hot reload)
- echo-mssql (SQL Server 2022)

When it’s up, the API is at: http://localhost:5160

Useful URLs (Development):

- Swagger UI: http://localhost:5160/swagger
- OpenAPI JSON: http://localhost:5160/openapi/v1.json

To stop everything:

```bash
docker compose -f dev.compose.yaml down
```

To also drop the local DB data volume:

```bash
docker compose -f dev.compose.yaml down -v
```

## Environment

Everything is wired with env vars (see .env):

- Database__ConnectionString → connection string to the mssql container
- Jwt__Issuer / Jwt__Audience / Jwt__Key → JWT settings (use a long, random key)
- Jwt__AccessTokenMinutes / Jwt__RefreshTokenDays → token lifetimes
- ASPNETCORE_ENVIRONMENT=Development → enables Swagger UI
- ASPNETCORE_URLS=http://+:5160 → app listens on 5160 in the container (published to localhost:5160)

SQL Server runs on localhost:1433 (inside the container as well). Default SA password is set in .env for dev. Change it
for anything beyond local use.

## Real‑time chat (SignalR)

The plan is simple: clients connect to a SignalR hub, join a room, and exchange messages with everyone in that room. The
hub will accept the JWT access token so only authenticated users can join/send. If you’re building a client, you’ll
typically:

- Open a WebSocket connection to the hub
- Send a "join room" command with the room id/name
- Broadcast messages and listen for incoming ones

## Dev notes

- Hot reload is enabled via `dotnet watch` in the container. Your local file changes trigger a rebuild.
- EF Core migrations are applied on startup so the DB schema stays in sync.
- There’s an Echo.http file in the repo if you like sending requests from Rider/IDE.

## Production

There’s a prod.compose.yaml to start from. Don’t ship the dev secrets. Rotate JWT keys and use proper SSL/TLS,
observability, and backups.
