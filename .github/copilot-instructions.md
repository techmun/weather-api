# Project Guidelines

## Solution Target
- Build this project as a .NET 10 Web API using the standard Visual Studio 2026 Web API structure unless the user explicitly asks for a different architecture.
- Prefer the default Visual Studio layout first: solution file, one API project, Controllers, Program.cs, appsettings files, Properties, and test project added only when needed.
- Do not introduce Clean Architecture, CQRS, event buses, microservices, or extra projects unless there is a concrete requirement that justifies them.

## Domain Scope
- This repository implements a Weather service using the OpenAPI specs stored in RealtimeWeatherReadingsacrossSingapore/.
- Treat those JSON files as the source contract for upstream Singapore weather data endpoints.
- Design useful REST endpoints around current, historical, forecast-like aggregation when feasible from available data, CSV export by location, and alert subscription use cases.

## Persistence
- Persist weather data in a relational database through EF Core.
- Prefer the simplest provider that keeps local setup easy unless the user chooses another database.
- Keep entities, migrations, and repository or service code straightforward, maintainable, and easy to support.

## Security And Resiliency
- Apply practical security defaults: input validation, authentication or authorization where relevant, secret-free source control, rate-limit awareness, and safe error handling.
- Add resiliency where external weather data is consumed: timeouts, retries with backoff, cancellation tokens, and structured logging.
- Avoid unnecessary security complexity. Choose measures that are realistic to implement, operate, and justify.

## API Standards
- Use RESTful naming, correct status codes, async APIs, DTOs at the boundary, and pagination or filtering only where it helps a clear use case.
- Configure OpenAPI and Swagger as part of the default solution.
- Keep endpoints and models easy to demo.

## Delivery Priorities
- Prioritize a working vertical slice over broad abstraction.
- When choosing between multiple designs, prefer the one that is simpler to explain, test, and ship.
- Include CI/CD by default with build, test, and publish stages.
- Include deployment through CI/CD, preferring Azure unless the user chooses a different target.