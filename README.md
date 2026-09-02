# Vasl

Vasl is a URL shortener service built on .NET 10, structured as a Clean Architecture solution. It generates short, Base62-encoded codes for long URLs, redirects visitors from a short code to the original destination, and uses Redis as a read-through cache in front of a SQL Server–backed store.

## Architecture

The solution follows Clean Architecture (a.k.a. Onion Architecture), splitting the codebase into four layered projects with dependencies pointing inward toward the domain:

```
Vasl.WebAPI            -> HTTP endpoints, composition root (Program.cs)
      |
      v
Vasl.ApplicationService -> Use cases (CQRS commands/queries via MediatR)
      |
      v
Vasl.Infrastructure     -> EF Core, Redis, distributed locking, code generation
      |
      v
Vasl.Domain             -> Entities, contracts (no external dependencies)
```

- **Vasl.Domain** — The innermost layer. Contains the `Url` entity (an encapsulated aggregate with a private constructor and a `Create` factory method that guards its invariants) and the `ICodeGenerator` abstraction. Has no dependencies on other projects or infrastructure concerns.
- **Vasl.Infrastructure** — Implements the domain's contracts and owns all external integrations: the EF Core `DbContext` and entity configuration, the Base62 code generator, Redis connection setup, and RedLock-based distributed locking. Depends only on `Vasl.Domain`.
- **Vasl.ApplicationService** — The use-case layer. Each operation is modeled as a MediatR command or query with its own request, handler, and response, following a vertical-slice CQRS style (`Urls/Commands/Create`, `Urls/Queries/Get`). Depends on `Vasl.Infrastructure` (handlers use the `DbContext` and cache directly, rather than through a repository abstraction).
- **Vasl.WebAPI** — The composition root and entry point. A minimal-API ASP.NET Core project that wires up dependency injection (`ConfigureApplication`, aggregating `ConfigureInfrastructure` and `ConfigureApplicationService`) and exposes two endpoint groups (`Read`, `Write`).

Each layer exposes a static `DependencyInjection` class with an extension method (`ConfigureInfrastructure`, `ConfigureApplicationService`, `ConfigureApplication`) that registers its own services, so `Program.cs` stays a thin composition root.

## Request flow

**Creating a short URL** (`POST /CreateShortUrl`)
1. The endpoint dispatches a `CreateUrlCommand` through MediatR.
2. `CreateUrlCommandHandler` atomically increments an `url:id` counter in Redis (`INCR`) to obtain a new numeric ID.
3. The ID is passed to `Base62CodeGenerator`, which multiplies it by a fixed large prime modulo 2^40 and encodes the result in Base62 — this scrambles sequential IDs so generated codes aren't easily guessable/enumerable, while remaining deterministic and collision-free for a given ID.
4. A `Url` domain entity is created (with validation) and persisted via EF Core to SQL Server.
5. The response returns the short code and a full redirect URL built from `AppSettings.RedirectUrl`.

**Resolving a short URL** (`GET /{code}`)
1. The endpoint dispatches a `GetUrlQuery` through MediatR.
2. `GetUrlQueryHandler` implements a cache-aside pattern: it first checks Redis for the code.
3. On a cache miss, it acquires a distributed lock (via RedLock.net, backed by the same Redis instance) before hitting the database, to prevent a "thundering herd" of concurrent requests all querying SQL Server for the same missing key.
4. After acquiring the lock, it re-checks the cache (double-checked locking), then falls back to EF Core/SQL Server if still missing.
5. If found and not expired, the result is written back to Redis with a 30-minute TTL, and the caller is redirected (HTTP 302) to the original URL. Missing or expired codes return `404 Not Found`.

## Technology stack

| Concern | Technology |
|---|---|
| Runtime / language | .NET 10, C# (nullable reference types, implicit usings enabled) |
| Web framework | ASP.NET Core Minimal APIs |
| Mediator / CQRS | MediatR |
| Persistence | Entity Framework Core 10 with SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Caching | Redis via `StackExchange.Redis` |
| Distributed locking | RedLock.net (Redis-based Redlock algorithm) |
| Domain guard clauses | Ardalis.GuardClauses |

## Project layout

```
src/
├── Vasl.Domain/
│   ├── Contracts/ICodeGenerator.cs
│   └── Entities/Url.cs
├── Vasl.Infrastructure/
│   ├── Data/VaslDbContext.cs
│   ├── Data/EFConfigurations/UrlConfiguration.cs
│   ├── Services/CodeGenerators/Base62CodeGenerator.cs
│   ├── AppSettings.cs
│   └── DependencyInjection.cs
├── Vasl.ApplicationService/
│   ├── Urls/Commands/Create/  (CreateUrlCommand, Handler, Response)
│   ├── Urls/Queries/Get/      (GetUrlQuery, Handler, Response)
│   └── DependencyInjection.cs
└── Vasl.WebAPI/
    ├── Endpoints/Read.cs   (GET /{code})
    ├── Endpoints/Write.cs  (POST /CreateShortUrl)
    ├── DependencyInjection.cs
    └── Program.cs
```
