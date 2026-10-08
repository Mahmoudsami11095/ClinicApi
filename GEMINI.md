# Clinic API Backend — Gemini CLI Project Guidelines

## 1. Technical Stack & Architecture
- **Framework**: .NET 9.0 (C# 13)
- **Pattern**: Clean Architecture
  - `Domain`: Enterprise entities, domain events, and value objects (zero external dependencies).
  - `Application`: Use cases, CQRS commands/queries, interfaces, and business logic.
  - `Infrastructure`: EF Core DbContext, repository implementations, external cloud integrations.
  - `API`: Minimal APIs / Controllers, dependency injection registration, middleware, and SignalR hubs.
- **Database**: Azure SQL via Entity Framework Core with code-first migrations.
- **Real-Time Communication**: SignalR hubs (`/hubs/notifications`) for live queue and chair state updates.

## 2. Testing & Quality Standards
- **Test Framework**: xUnit with FluentAssertions and Moq.
- **Coverage**: Unit tests for Application and Domain layers; integration tests for API endpoints.
- **Run Tests**: `dotnet test ClinicApi.sln`
- **Build Verification**: `dotnet build ClinicApi.sln --configuration Release`

## 3. Mandatory Verification Checklist
Before finishing any backend task:
1. Ensure the solution compiles cleanly with no warnings treated as errors: `dotnet build ClinicApi.sln`.
2. Run all unit and integration tests: `dotnet test ClinicApi.sln`.
3. If database entities were modified, verify that an EF Core migration is generated and applied cleanly.
