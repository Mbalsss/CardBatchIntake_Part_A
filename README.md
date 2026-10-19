# Card Batch Intake API

A small ASP.NET Core Web API for receiving card production batches. The code is kept intentionally straightforward so each requirement is easy to follow.

## How It Works
A bank sends the API a batch of card records. The API checks the batch details and each record, then either returns a list of problems or saves the complete valid batch as **Accepted**. An operator can find saved batches and release an accepted batch to production. The sample application keeps its data in memory, so saved batches are cleared when the application stops.

Cardholder information is not written to logs. Only batches that pass all checks are saved.

## Requirements
- .NET 8 SDK
- No external database setup

## Run
From the repository root:

```bash
dotnet run --project src/CardBatchIntake.Api
```

The API listens on the local URL printed by `dotnet run`.

Run tests:

```bash
dotnet test
```

## Project Structure
- `Models/` — describes the information sent to and returned by the API.
- `Controllers/` — receives requests and sends back results such as `created`, `not found`, or `needs correction`.
- `Services/` — applies the batch rules and carries out submission, search, and release actions.
- `Repositories/` — stores and retrieves batches; this version stores them in memory.
- `tests/` — checks the validation rules, batch actions, and controller responses.

In short: a controller receives the request, a service decides what should happen, and the repository keeps track of accepted batches.

## Endpoints
- `GET /` — confirm the API is running.
- `POST /api/batches` — validate and accept a batch; returns 201 on success, 422 for validation/security errors, and 409 for a duplicate file name.
- `GET /api/batches/{id}` — retrieve one batch.
- `GET /api/batches?clientCode=KAYA&status=Accepted&page=1&pageSize=20` — filter and paginate batches.
- `POST /api/batches/{id}/release` — release an Accepted batch; returns 409 if it is not Accepted.

## Design decisions and assumptions
- I used an in-memory repository behind `IBatchRepository` to keep the assessment easy to clone and run without database configuration. Data is lost when the process stops. A persistent EF Core SQLite repository would be the next improvement.
- Validation returns a collection of field-level errors so a client can fix multiple problems in one request.
- Expiry is treated as valid through the last day of the stated month.
- Pagination defaults to page 1 and page size 20; page size is limited to 100.
- A duplicate file name is checked case-insensitively.
- Only valid batches are stored. The API does not log request bodies or cardholder/card data.
- The in-memory implementation is suitable for demonstrating the interface but would need a database-level unique constraint and transactional release handling for production use.

## AI tool use
I used an AI coding assistant to help plan the project structure, draft parts of the implementation and tests, and review the requirements. I reviewed and adjusted the generated code. I will make sure I can explain the validation, repository interface, endpoint responses and tests during the assessment discussion.

## What I would improve with more time
- Add integration tests for HTTP status codes and endpoint behaviour.
- Use EF Core with SQLite and enforce file-name uniqueness in the database.
- Add structured operational logging with sensitive fields excluded, plus a health check.
- Add concurrency handling and authentication/authorisation for operators who release batches.
- Add stronger request-size limits and explicit handling for malformed JSON.
