# EntApp.Framework

Enterprise-grade modular monolith framework built with .NET 9.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for infrastructure services)
- PostgreSQL 16 (via Docker or standalone)

## Quick Start

### 1. Start Infrastructure (Docker)

```bash
docker-compose up -d
```

This starts:
| Service    | Port(s)       | Description                    |
|------------|---------------|--------------------------------|
| PostgreSQL | 5432          | Primary database               |
| Redis      | 6379          | Cache & distributed locking    |
| RabbitMQ   | 5672 / 15672  | Message broker (+ management)  |
| Seq        | 5341          | Structured log viewer          |

> **Note:** If you already have PostgreSQL running locally (e.g., `ag-postgres` container), you can skip the `postgres` service and the API will connect to `localhost:5432` as configured in `appsettings.json`.

### 2. Build & Run

```bash
dotnet build EntApp.sln
dotnet run --project src/Host/EntApp.WebAPI
```

### 3. Verify

| Endpoint            | Description              |
|---------------------|--------------------------|
| `http://localhost:5079/`       | API info (version, environment) |
| `http://localhost:5079/health` | Health check (incl. PostgreSQL) |
| `http://localhost:5079/swagger`| Swagger UI (Development only)  |

## Project Structure

```
EntApp.Framework/
├── src/
│   ├── Host/
│   │   └── EntApp.WebAPI/        # Walking Skeleton API host
│   ├── Shared/                   # Shared kernel & contracts (Phase 2+)
│   └── Modules/                  # Business modules (Phase 6+)
├── tests/                        # Test projects (Phase 2+)
├── database/                     # Migrations & scripts
├── frontend/                     # React/Next.js frontend (Phase 5)
├── docs/                         # Architecture documentation
├── docker-compose.yml            # Infrastructure services
├── Directory.Build.props         # Central build properties
├── .editorconfig                 # Code style rules
└── EntApp.sln                    # Solution file
```

## Documentation

See the `docs/` folder for detailed architecture documentation:

- [Enterprise Framework Evaluation](docs/enterprise-framework-evaluation.md)
- [Roadmap](docs/_roadmap.md)
- [Coding Conventions](docs/CODING_CONVENTIONS.md)
- [Dynamic UI Generation](docs/dynamic-ui-generation.md)
- [Business Framework Layer](docs/business-framework-layer.md)

## Technology Stack

| Category       | Technology                              |
|----------------|-----------------------------------------|
| Runtime        | .NET 9 (STS)                            |
| Database       | PostgreSQL 16 + EF Core 9              |
| Messaging      | RabbitMQ 3.13 (Outbox Pattern)          |
| Cache          | Redis 7                                 |
| Logging        | Serilog + Seq                           |
| Frontend       | React 19 + Next.js 15 + shadcn/ui      |
| API            | Minimal API + Swagger + API Versioning  |

## License

Proprietary — All rights reserved.
