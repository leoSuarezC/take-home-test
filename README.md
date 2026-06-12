# Loan Management System

A simple loan management system built as a take-home challenge: a **.NET 8 (C#) REST API** backed by **SQL Server** via **Entity Framework Core**, and a lightweight **Angular 19** frontend that lists the existing loans.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 (ASP.NET Core), Entity Framework Core 8 |
| Database | SQL Server 2022 (Docker) / LocalDB (local dev) |
| Frontend | Angular 19, Angular Material |
| Tests | xUnit, FluentAssertions, SQLite in-memory |
| DevOps | Docker multi-stage build, Docker Compose, GitHub Actions CI |

## Repository Structure

```
backend/
  Dockerfile                      # Multi-stage build for the API
  src/
    Fundo.Applications.WebApi/    # REST API
      Controllers/                # HTTP layer (thin, maps service results to status codes)
      Services/                   # Business logic (loan creation, payment rules)
      Contracts/                  # Request/response DTOs (the public API contract)
      Domain/                     # Loan entity + status enum
      Data/                       # EF Core DbContext + seed data
      Migrations/                 # EF Core migrations (applied automatically at startup)
    Fundo.Services.Tests/
      Unit/                       # Business-rule tests for LoanService
      Integration/                # Full HTTP-pipeline tests via WebApplicationFactory
frontend/                         # Angular app (loans table)
docker-compose.yml                # API + SQL Server
```

## Quick Start (Docker)

Requires Docker. From the repository root:

```sh
docker compose up --build
```

This starts SQL Server 2022 and the API. The API waits for the database healthcheck, applies EF Core migrations and seed data automatically, and listens on:

```
http://localhost:8080/loans
```

To run the frontend against it (requires Node 18+):

```sh
cd frontend
npm install
npm start
```

Then open `http://localhost:4200` — the table shows the seeded loans.

## Running Locally (without Docker)

The backend defaults to **SQL Server LocalDB** (installed with Visual Studio). Requires the .NET 8 SDK.

```sh
cd backend/src
dotnet run --project Fundo.Applications.WebApi
```

The database is created, migrated and seeded automatically on first run. The API listens on `http://localhost:8080` (same port as Docker, so the frontend works unchanged).

To use a different SQL Server instance, override the connection string via `appsettings.json` or the `ConnectionStrings__LoanManagementDb` environment variable.

## Running the Tests

```sh
cd backend/src
dotnet test
```

25 tests: unit tests covering every business rule in `LoanService`, plus integration tests exercising every endpoint (success and error paths) through the real HTTP pipeline. Tests run against in-memory SQLite databases — no external infrastructure needed, fully isolated per test.

## API Reference

| Method | Route | Description | Responses |
|---|---|---|---|
| `GET` | `/loans` | List all loans | `200` |
| `GET` | `/loans/{id}` | Get loan details | `200`, `404` |
| `POST` | `/loans` | Create a new loan | `201`, `400` |
| `POST` | `/loans/{id}/payment` | Deduct a payment from the balance | `200`, `400`, `404` |

**Create a loan** — the server sets the initial balance and status (clients cannot forge them):

```sh
curl -X POST http://localhost:8080/loans \
  -H "Content-Type: application/json" \
  -d '{ "amount": 1500.00, "applicantName": "Maria Silva" }'
```

```json
{ "id": 6, "amount": 1500.00, "currentBalance": 1500.00, "applicantName": "Maria Silva", "status": "active" }
```

**Make a payment** — when the balance reaches exactly zero, the loan automatically becomes `paid`:

```sh
curl -X POST http://localhost:8080/loans/6/payment \
  -H "Content-Type: application/json" \
  -d '{ "amount": 500.00 }'
```

Errors follow RFC 9110 Problem Details, e.g. overpaying returns:

```json
{ "title": "Bad Request", "status": 400, "detail": "Payment amount (5000) exceeds the current balance (1000.00)." }
```

## Implementation Notes & Decisions

- **.NET 8 upgrade** — the template targeted .NET 6, which reached end-of-life in November 2024. Both projects were moved to the .NET 8 LTS while keeping the original `Startup`-based architecture intact.
- **Layered design, single project** — `Controller → ILoanService → DbContext`. Controllers only translate HTTP; all business rules (initial balance, payment validation, automatic `active → paid` transition) live in the service layer, which is what the unit tests target. For a 4-endpoint API, separate solution projects per layer would add ceremony without benefit.
- **DTOs over exposed entities** — `POST /loans` only accepts `amount` and `applicantName`; `currentBalance` and `status` are server-controlled. This prevents over-posting (e.g. a client creating a loan with `"status": "paid"`).
- **Result pattern for payments** — expected outcomes (not found, overpayment, already paid) are modeled as explicit results mapped to status codes, not exceptions.
- **`decimal` with fixed precision (18,2)** for all money values; loan status stored as a human-readable string while remaining a type-safe enum in code.
- **Seed data via `HasData`** — baked into the migration, so every environment gets identical data with no manual steps.
- **Testing strategy** — SQLite in-memory instead of mocking the `DbContext` (tests real queries against a real relational engine) or EF's InMemory provider (not relational, discouraged by Microsoft for testing). Each unit test gets a fresh database; integration tests swap SQL Server for SQLite inside `WebApplicationFactory`.
- **Container security** — multi-stage build (no SDK or source in the final image) running as the non-root user on an unprivileged port. Compose starts the API only after the SQL Server healthcheck passes, so automatic migration never races database startup.
- **CORS** — specific origin whitelist (no wildcard), configurable per environment.
- ✅**Structured logging (Serilog)** — configuration-driven (levels and sinks in `appsettings.json`), with per-request logging (method, path, status, elapsed time) and domain events logged as named properties (`LoanId`, `PaymentAmount`, `CurrentBalance`), queryable in any log aggregator. Unhandled exceptions are logged and returned as a generic RFC 9110 problem response — stack traces are never exposed to clients.
- ✅**CI (GitHub Actions)** — on every push and pull request, the backend is built and tested in Release configuration and the Docker image is built. Because tests run on in-memory SQLite, the pipeline needs no database service and finishes fast.

## Improvements With More Time

- **Authentication/authorization** (e.g. JWT bearer) and rate limiting.
- **Optimistic concurrency** on payments (a `rowversion` token) — currently two simultaneous payments to the same loan could race.
- **Payment history** as its own entity (`Payment` table) instead of only mutating the balance — better auditability for a financial domain.
- **Pagination** on `GET /loans` once the dataset grows.
- **Frontend**: loan creation/payment forms, retry button on the error state, and component tests.
