---
name: backend-development
description: >-
  Apply C#/.NET backend rules for AI Document Analysis: FastEndpoints, EF Core
  only (no raw SQL), explicit types, naming/formatting, and finish
  service/endpoint/repository code then ask before test changes. Use when
  writing, reviewing, or changing APIs, services, repositories, endpoints,
  HTTP clients, or .NET/C# test code.
---

# Backend Development (AI Document Analysis)

Read this skill fully before changing C#/.NET backend code. Apply every rule below. No exceptions.

This repo is **net8.0**, FastEndpoints, Serilog, EF Core + PostgreSQL (`AidaServiceContext`), Azure Document Intelligence, JWT auth. Route prefix is `api/aida-core`.

## Workflow order (must follow)

1. Complete ALL service/endpoint/repository/configuration code. Finish fully before moving on.
2. Present a summary of what changed.
3. STOP and ASK: "Service changes are complete. Would you like me to proceed with test case changes?" WAIT for explicit yes. Do NOT start test changes until the user says yes.
4. Only after confirmation, add/update tests.

NEVER auto-start test case changes after service changes.

## Critical rules

C1: Do not break existing functionality. Verify current behavior before and after changes.
C2: Never modify service code only to improve test coverage. Tests adapt to the service.
C3: Check framework first. MUST read `.csproj` for `TargetFramework` (this app is `net8.0`) before using language features or APIs.
C4: Data access is EF Core only. Use `AidaServiceContext` / LINQ. Do not add raw SQL, `FromSqlRaw`, `FromSqlInterpolated`, or ADO.NET.

## Project structure (match existing)

- Endpoints: `src/AIDocumentAnalysis/Endpoints/{Feature}/` — FastEndpoints `Endpoint<TRequest, TResponse>` with `Configure()` and `HandleAsync`.
- Services: `src/AIDocumentAnalysis/Services/` + `Services/Interfaces/I{Name}.cs`. Register interface + implementation in `RootStartup.ConfigureServices`.
- Data access: `src/AIDocumentAnalysis/DataAccess/` with EF Core (`AidaServiceContext`).
- Config: `src/AIDocumentAnalysis/Configurations/` with `IOptions<T>` and a `SectionName` constant. Do not add raw `IConfiguration["Key"]` lookups for new settings.
- Tests: `tests/AIDocumentAnalysis.E2E.Tests/` — follow existing endpoint test patterns.

New HTTP APIs use FastEndpoints, not Controllers, unless extending an existing controller.

## Code structure

S2: Use existing regions. Public methods in `#region Public Methods`, private in `#region Private Methods`. Do not create feature-specific regions. Tests may use descriptive regions.
S3: No XML doc comments. Do not add `<summary>`, `<param>`, or `<returns>`.
S4: No static classes or methods. Use instance classes with instance methods.
S5: Match existing patterns: naming, repository/service layout, FastEndpoints `Configure`/`HandleAsync`.
S6: Reuse shared utilities, constants, and error messages before creating new ones.
S7: Trace callers before changing a method signature or behavior.
S8: Decompose services that exceed ~2000 lines into focused services.
S9: Validation at the endpoint boundary only. Do not re-validate in services what the endpoint already checked.
S10: Do not null-check trusted internal calls that never return null.
S12: DTO inheritance only when subclasses add members. Empty subclasses = duplicate properties. Prefer passing a request DTO over 4+ 1:1 parameters.
S13: No nested/inner classes. Top-level classes only. Related types may share a file.
S14: No nested ternary chains. Extract a private method with a switch expression.
S15: No unread private fields. Do not inject unused dependencies.
S16: Implementation parameter names must match the interface exactly.
S17: O(1) or O(n) only — never O(n²). Use dictionaries/hash sets for lookups.
S18: Avoid nested loops. Flatten with dictionaries, hash sets, LINQ, or extracted methods.
S19: Explicit types — no `var` (IDE0008). Tuple deconstruction uses explicit types.
S20: Pass `StringComparison.OrdinalIgnoreCase` to string methods that accept it.
S21: Use range/index syntax instead of `Substring`.
S22: New private methods go at the end of `#region Private Methods`.
S23: Guard nullable ValueTuple members with `?? string.Empty`.

## Naming and formatting

N1: Meaningful names that describe what the value holds.
N2: `Get` prefix for new methods. Existing `Load` methods stay as-is.
N3: Singular vs plural must match cardinality.
N4: 5 or fewer params = single-line call.
N5: More than 5 params = wrap; two lines max per call.
N6: Chained async calls and simple ternaries stay on one line.
N7: Method declarations with more than 5 params: all params on the second line.

## Logging

L1: Prefer one line. Max 2 for long messages. NEVER exceed 3.

## Data access and integration

D1: Use EF Core LINQ for reads and writes. No row-by-row processing when a set-based LINQ query works.
D2: Invalidate cache on writes that change cached data.
D3: Strongly typed config via `IOptions<T>` with validation.
D4: Throw domain-specific exceptions. NEVER `Exception` or `ApplicationException`.
D5: Azure Document Intelligence: use the injected `DocumentIntelligenceClient`. Never construct a client with hardcoded keys.
D6: External HTTP: use existing Flurl/`HttpClient` wrappers. Never `new HttpClient()` for integrations.
D7: Auth: do not weaken JWT/authorization defaults. New endpoints are authenticated unless there is an explicit product reason (document like existing `AllowAnonymous()`).

## Testing

T1: Minimal tests for line coverage. No duplicate coverage.
T2: Each test covers a distinct path or edge case.
T3: Follow `AIDocumentAnalysis.E2E.Tests` naming and structure.
T4: Reuse test helpers; do not duplicate setup.
T5: When moving a method, move its tests with it.
T6: New features need integration/E2E coverage, not only unit tests.
T7: `// Arrange`, `// Act`, `// Assert` in every test method.
T8: Mock `.Setup()` / `.ReturnsAsync()` at most 2 lines.
T9: Extract repeated request/response/mock setup into private helpers.
T10: DataAccess tests: `CreateEntity()` helper instead of inline `new Entity { ... }` blocks.

## General discipline

G1: Efficient code. No unnecessary allocations or over-engineering.
G2: If a prior approach was rejected, understand why before retrying.
G3: New endpoints use FastEndpoints request/response types + validators where the project already uses them.
G4: Interface-based DI for all new services.
G5: Check `RootStartup` registration after adding a service, client, or options type.
