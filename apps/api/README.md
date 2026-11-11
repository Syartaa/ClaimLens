# ClaimLens API

ASP.NET Core 10 solution.

```text
ClaimLens.slnx
src/ClaimLens.Domain
src/ClaimLens.Application
src/ClaimLens.Infrastructure
src/ClaimLens.Api
tests/ClaimLens.Domain.Tests
tests/ClaimLens.Api.IntegrationTests
```

`VerdictThresholds` holds the engineering heuristics for the later verdict calculator. They are not probabilities that a claim is true.

```bash
dotnet test ClaimLens.slnx
dotnet run --project src/ClaimLens.Api
```

Health: `GET http://localhost:8080/api/health`. Swagger is available in Development at `/swagger`.

The database connection string is reserved for a later phase. This API does not open PostgreSQL yet.
