# PC2 Coding Conventions

This document captures repository-wide conventions for consistent code, architecture, and team communication. Follow these guidelines to keep the codebase readable, maintainable, and easy to review.

## Table of Contents

- [IDE](#ide)
- [Member Naming Conventions](#member-naming-conventions)
- [Architecture Guidance](#architecture-guidance)
- [Team Communication (GitHub Discussions & PR workflow)](#team-communication)

---

## IDE
The team uses Visual Studio 2026 to support .NET 11 development and built-in support
for tools like the .editorconfig file, analyzers, and refactoring tools.

## Member Naming Conventions

Follow idiomatic .NET naming unless a specific exception is documented. Enforce with mechanisms like
.editorconfig where possible.

- Types
  - Use `PascalCase` for classes, structs, enums, and record types.
  - Interfaces use the `I` prefix and `PascalCase` (e.g., `IUserRepository`).
  - Generic type parameters use `T` or `TEntity` style (e.g., `T`, `TResult`).

- Methods, Properties, Events
  - Use `PascalCase`. Method names should be verbs or verb phrases (e.g., `GetUser`, `CalculateScore`).
  - Async methods must end with the `Async` suffix (e.g., `LoadAsync`).
  - Event names use `PascalCase` and describe the event (e.g., `UserCreated`). Event handler delegate types end with `EventHandler` when applicable.

- Fields
  - Private instance fields use `_camelCase` with a single leading underscore (e.g., `_cache`, `_logger`).
  - `readonly` private fields follow the same `_camelCase` pattern.
  - Avoid `this.` unless required for clarity or to disambiguate (editorconfig prefers no qualification).

- Constants
  - Use `PascalCase` for `const` and `static readonly` values (e.g., `MaxRetryAttempts`).

- Local Variables
  - Use `camelCase`. Keep names short and descriptive.

- Backing fields
  - Use `_camelCase` for backing fields. Prefer auto-properties where possible.

- Abbreviations and Acronyms
  - Treat acronyms as words: use `XmlReader` instead of `XMLReader` (PascalCase for types, camelCase for locals as appropriate).

---

## Architecture Guidance

This project targets .NET 11 and is an ASP.NET Core MVC application with Identity integration. Use the following structural and naming guidance to organize code consistently.

- Current Project Structure
  - `PC2/` (ASP.NET Core MVC app - single project architecture)
    - `Controllers/` — MVC controllers. Name classes with `Controller` suffix (e.g., `HomeController`, `AgencyController`).
    - `Views/` — Razor views organized by controller name. Shared views live in `Views/Shared/`.
    - `Models/` — view models, domain entities, business logic helpers, and service interfaces.
    - `Data/` — Entity Framework Core `DbContext`, database entity classes (e.g., `AgencyDB`, `CalendarEventDB`), migrations, and data access helpers.
    - `Filters/` — action filters and other cross-cutting concerns (e.g., `ApplicationInsightsPageViewTracker`).
    - `Areas/Identity/Pages/` — Identity-related Razor Pages for authentication and account management.
    - `wwwroot/` — static assets (CSS, JS, images, PDFs).
    - `Properties/` — launch settings and other project configuration.

- Layering & Responsibilities (Single-Project Architecture)
  - `Controllers/` handle HTTP requests, orchestrate services, and return views or results.
  - `Models/` contain domain entities, view models, business logic helpers, and service abstractions.
  - `Data/` owns all database concerns: `ApplicationDbContext`, entity configurations, migrations, and DB entity classes (suffixed with `DB` for database-specific models).
  - `Filters/` implement cross-cutting concerns like telemetry, logging, and request validation.
  - Keep controllers thin; delegate complex logic to models or services registered in `Program.cs`.
  - Use Dependency Injection (constructor injection) for services, DbContext, and other dependencies.

- Naming Conventions for Classes and Components
  - Controllers: `SomethingController` (e.g., `HomeController`, `AgencyController`).
  - Database Entity Classes: suffix with `DB` (e.g., `AgencyDB`, `CalendarEventDB`, `StaffDB`).
  - View Models: suffix with `ViewModel` (e.g., `CalendarEventViewModel`, `ErrorViewModel`).
  - Services/Helpers: descriptive names ending with purpose (e.g., `AzureBlobUploader`, `IdentityHelper`, `TextLinkifier`).
  - Filters: descriptive names describing the filter's purpose (e.g., `ApplicationInsightsPageViewTracker`).
  - Interfaces: prefix with `I` (e.g., `IEmailSender`).

- File and Namespace Organization
  - Keep namespace matching folder structure (__dotnet_style_namespace_match_folder__ is enabled).
  - Inheritance hierarchies and/or interfaces may reside in the same file as any derived or concrete classes.
  - Controllers in the `Controllers` namespace, models in `Models`, data classes in `Data`.

- MVC & Identity Specifics
  - Controllers should be focused: one controller per resource or feature area.
  - Keep action methods concise; complex logic belongs in models, services, or helpers.
  - Views are organized by controller name under `Views/` (e.g., `Views/Home/`, `Views/Agency/`).
  - Identity uses Razor Pages in `Areas/Identity/Pages/` for authentication workflows.
  - Shared layouts and partials live in `Views/Shared/`.

- Database & Migrations
  - Use Entity Framework Core with SQL Server.
  - `ApplicationDbContext` is the single DbContext; it includes Identity and application entities.
  - Database entity classes live in `Data/` and use the `DB` suffix.
  - Keep migrations in `Data/Migrations/`; use descriptive migration names.
  - Use `dotnet ef migrations add` for schema changes.

- Configuration & Services
  - Register services, DbContext, and dependencies in `Program.cs`.
  - Use the built-in DI container; register with appropriate lifetime (Singleton, Scoped, Transient).
  - Store secrets in User Secrets (development) and Azure App Configuration or Key Vault (production).
  - Configuration lives in `appsettings.json` and environment-specific overrides.

- Static Assets & External Storage
  - Static files (images, CSS, JS, PDFs) live in `wwwroot/`.
  - Organize by resource type: `wwwroot/images/`, `wwwroot/PDF/`.
  - Large or user-uploaded files are stored in Azure Blob Storage via `AzureBlobUploader`.

- Documentation and XML Comments
  - Document public types and members with XML documentation comments where it benefits consumers.
  - Use summary comments for service interfaces, public APIs, and complex algorithms.

- Code Quality
  - Favor small, single-responsibility classes and methods.
  - Apply SOLID principles and prefer composition over inheritance.
  - Use meaningful names; prefer clarity over cleverness.
  - Keep controllers thin; move business logic to models or services.

---

## Team Communication

Use GitHub Discussions for high-level planning, questions, and proposals. Keep communication 
discoverable and linked to issues and PRs when possible.

- When to use Discussions
  - High-level design proposals, "how-to" questions, and community Q&A.
  - Start a discussion for design proposals that might impact multiple areas (tag `proposal`).
  - Use discussion categories: Q&A, Ideas, and Announcements as appropriate.

- Proposal Process
  - Create a short proposal in Discussions. Summarize the problem, options considered, recommended solution, and migration/cost.
  - Link the discussion to a tracking issue when a decision leads to work.
  - Use the discussion to gather feedback before opening a PR.

- Issues and Pull Requests
  - Create an issue for work that needs to be tracked, referenced, or prioritized.
  - PRs should reference the issue number (e.g., `Closes #123`).
  - Include a concise description, testing instructions, and any migration notes in the PR body.
  - Assign reviewers and request review through GitHub. Use labels to indicate scope and priority.

- Review Guidelines
  - Reviews should focus on correctness, design, test coverage, readability, and security implications.
  - Keep PRs small and focused; prefer multiple smaller PRs over one large change.
  - Use CI checks and address failing builds before merging (this is already required in PRs).

- Communication Best Practices
  - Use clear titles and descriptive comments in Discussions, Issues, and PRs.
  - When a decision is made in Discussions, summarize it and link to any follow-up issues or PRs.
  - Use code owners and required reviewers where appropriate (automated via GitHub).

- Documentation & Knowledge Base
  - Important decisions should be summarized in the repo’s `README.md` or a dedicated `docs/` area.
  - Keep the CodingConventions and CONTRIBUTING documentation up to date as conventions evolve.

---