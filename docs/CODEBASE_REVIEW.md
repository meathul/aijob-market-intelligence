# Codebase Architecture & Code Review Guide

This document provides a comprehensive review of the AI Job Market Intelligence Platform codebase. It details the directory structures, explains the purpose and naming convention of each file, and highlights the architectural design patterns (Clean Architecture, Repository Pattern, Dependency Injection, and Standalone Angular Components) utilized throughout the project.

---

## 📂 System Architecture Overview

The application is split into a **.NET 10 Web API Backend** (located in the `api` folder) and an **Angular 17 SPA Frontend** (located in the `ui` folder).

```
AiJobMarketIntelligence/
├── api/                           # .NET 10 Backend API (Clean Architecture)
│   └── src/
│       ├── Domain/                # Core enterprise entities & rules
│       ├── Application/           # Application-specific business use cases
│       ├── Infrastructure/        # Data access, migrations & external clients
│       └── Api/                   # ASP.NET Core Web Host & Controllers
├── tests/                         # Test Suites
│   └── UnitTests/                 # C# unit tests for services and controllers
├── ui/                            # Angular 17 Standalone UI
│   ├── src/aijob-ui/              # Angular workspace root
│   │   └── src/app/               # App routing, modules, services, and components
│   └── tests/                     # UI integration test scripts
└── docs/                          # Project Documentation
```

---

## 🗄️ 1. Backend Code Review (api/src)

The backend follows the principles of **Clean Architecture** (Onion Architecture). Dependencies flow inwards: `Api` -> `Infrastructure` -> `Application` -> `Domain`.

### 🧬 Domain Layer (`Domain/`)
This is the core of the application. It contains domain models (database schemas) and core logic. It has zero external dependencies (no databases, no third-party APIs).

*   **`Entities/JobRaw.cs`**
    *   *Why it is used*: Represents raw job listings exactly as scraped/fetched from external providers (e.g. title, company, description, raw salary text, URL).
    *   *Why it is called that*: `Job` + `Raw` indicates it stores unfiltered data before parsing.
*   **`Entities/JobProcessed.cs`**
    *   *Why it is used*: Represents normalized job details extracted after running the raw job description through our parsing services (e.g. min/max parsed annual salary, experience level).
    *   *Why it is called that*: Represents the structured data model after processing has occurred.
*   **`Entities/Skill.cs`**
    *   *Why it is used*: Holds master records of unique skill keywords (e.g. "C#", "Angular", "Python").
    *   *Why it is called that*: Describes a singular skill unit in the directory.
*   **`Entities/JobSkill.cs`**
    *   *Why it is used*: Join entity implementing the many-to-many relationship between `JobRaw` and `Skill`.
    *   *Why it is called that*: Combines both entity names (`Job` + `Skill`) to signify its role as a bridge table.
*   **`Entities/UserPreferences/UserJobPreferences.cs`**
    *   *Why it is used*: Defines the database schema for job seeker onboarding options (preferred titles, skills text, location, work modes).
    *   *Why it is called that*: Clearly describes that it stores the specific job preferences of a registered user.

---

### ⚙️ Application Layer (`Application/`)
Defines the business logic, service contracts (interfaces), and data transfer structures (DTOs). It coordinates use cases but doesn't know *how* databases or APIs are implemented.

*   **`Interfaces/Repositories/IJobRepository.cs` (and other `IRepository` files)**
    *   *Why they are used*: Decouples database queries from business use cases, supporting mock testing.
    *   *Why they are called that*: Prefix `I` (Interface) + repository entity target.
*   **`DTOs/Career/CareerChatRequestDto.cs` (and other `Dto` files)**
    *   *Why they are used*: Data Transfer Objects define strict data contracts for endpoint inputs/outputs, avoiding exposure of raw database entities to the client.
    *   *Why they are called that*: Suffixed with `Dto` to distinguish them from database-mapped entity classes.
*   **`Services/JobIngestionService.cs`**
    *   *Why it is used*: Orchestrates job fetching from providers, checks for duplicates using URL indexes, and saves new listings to database repos.
    *   *Why it is called that*: Defines its core function—ingesting job postings.
*   **`Services/Salary/SalaryParserService.cs`**
    *   *Why it is used*: Extracted rate parsing calculations. It annualizes daily/hourly raw inputs into clean numerical bounds for backend operations.
    *   *Why it is called that*: It parses raw salary text.
*   **`Services/Career/CareerChatService.cs`**
    *   *Why it is used*: Integrates with the LLM API, translating user prompts and stateless client-synced chat history arrays into query contexts.
    *   *Why it is called that*: Manages conversation flows for the AI Career Copilot.

---

### 💾 Infrastructure Layer (`Infrastructure/`)
Implements repository interfaces, manages EF Core database contexts, runs migrations, and handles third-party HTTP clients.

*   **`Data/AiJobContext.cs`**
    *   *Why it is used*: The Entity Framework DbContext. It configures table mappings, key relationships (1-to-1, many-to-many), unique indexes (e.g. unique URLs to prevent duplicates), and cascading delete rules.
    *   *Why it is called that*: Context mapping the database name (`AiJob`) to EF Core.
*   **`Repositories/JobRepository.cs`**
    *   *Why it is used*: Implements database reads/writes for job listings using LINQ queries.
    *   *Why it is called that*: Represents concrete database operations for the `JobRaw` entity.
*   **`Services/LlamaChatService.cs`**
    *   *Why it is used*: Concrete client service fetching completions from the Groq/OpenAI endpoints.
    *   *Why it is called that*: Names the specific LLM integration backend provider used.

---

### 🌐 Api Host Layer (`Api/`)
The entry point of the backend HTTP application. Handles authentication, routing, and controller endpoints.

*   **`Program.cs`**
    *   *Why it is used*: Initializes the ASP.NET Core host, registers dependencies (DI container), binds database connection strings, configures JWT token authentication middleware, and registers CORS headers.
    *   *Why it is called that*: The entrypoint script containing the main execution thread.
*   **`Controllers/JobsController.cs`**
    *   *Why it is used*: Exposes RESTful endpoints (`/api/jobs`) supporting search queries, remote filters, salary filters, and paginated outputs.
    *   *Why it is called that*: Controllers route incoming HTTP requests.
*   **`Controllers/AdminController.cs`**
    *   *Why it is used*: Exposes administrative endpoints (`/api/admin/trigger-fetch`) allowing authorized users to trigger manual synchronization runs.
    *   *Why it is called that*: Dedicated to admin-only actions.
*   **`Controllers/User/UserPreferencesController.cs`**
    *   *Why it is used*: Exposes preferences endpoints (`/api/user/preferences`). Contains the `ResponseCache(NoStore = true)` attribute protecting against user session caching.
    *   *Why it is called that*: Dedicated to managing user preferences.

---

## 🧪 2. Backend Tests Review (tests/UnitTests)

Contains standard xUnit and Moq testing scripts verifying backend business logic.

*   **`CareerChatMemoryTests.cs`**
    *   *Why it is used*: Asserts that the LLM chat service remembers user context under both stateless (DTO-supplied) and dictionary-fallback memory models.
*   **`SalaryParserServiceTests.cs`**
    *   *Why it is used*: Tests regex extraction rules for standard yearly, daily, and hourly salary structures.
*   **`AdminControllerTests.cs`**
    *   *Why it is used*: Verifies that the sync trigger endpoints handle service successes and system failures with correct HTTP status codes.
*   **`UserPreferencesControllerTests.cs`**
    *   *Why it is used*: Asserts validation rules (min/max salary limits, empty preference safeguards, unauthorized blocks) and validates the presence of caching headers.

---

## 💻 3. Frontend Code Review (ui/src/aijob-ui)

The frontend is an Angular 17 application configured using **standalone components** (eliminating the need for complex `NgModule` imports).

### 🛠️ Core Services (`src/app/core/` & `services/`)
*   **`core/auth/auth.service.ts`**
    *   *Why it is used*: Holds user session tokens in local memory, exposes computed signals for roles (e.g. `isAdmin`), and handles logging out/switching accounts.
    *   *Why it is called that*: Manages authentication state globally.
*   **`core/http/api-client.ts`**
    *   *Why it is used*: A custom wrapper around Angular's `HttpClient` that automatically appends JWT Authorization tokens to all requests.
    *   *Why it is called that*: The unified client interface for all API queries.
*   **`services/user-preferences-api.service.ts`**
    *   *Why it is used*: Connects to `/api/user/preferences`. Appends cache-busting timestamp tokens (`_t=Date.now()`) to bypass intermediate proxy caches.
    *   *Why it is called that*: Wraps preferences API queries.
*   **`services/admin-api.service.ts`**
    *   *Why it is used*: Hits the manual sync ingestion endpoint `/api/admin/trigger-fetch`.
    *   *Why it is called that*: Wraps admin API queries.

---

### 🎨 Views & Components (`src/app/`)
*   **`app.component.html` & `app.component.ts`**
    *   *Why they are used*: The root element of the entire application. It contains the `<om-dark-veil>` element full-screen to draw the animated background, wrapped with a dark gradient overlay.
    *   *Why they are called that*: The entrypoint component of all Angular apps.
*   **`layout/shell/shell.component.html`**
    *   *Why it is used*: Defines the main portal grid layout (sidebar on the left, topbar on the top, main page router-outlet in the middle). Uses transparent background bounds with a blurred sidebar panel.
    *   *Why it is called that*: Serves as the "shell" layout wrapper.
*   **`auth/pages/login-page/` & `register-page/`**
    *   *Why they are used*: Handle user account login and registration.
    *   *Why they are called that*: Clear page indicators.
*   **`onboarding/pages/profile-setup-page/`**
    *   *Why it is used*: The onboarding preferences editor. It pre-populates fields on load and renders a **Cancel** button if the user is editing existing choices.
    *   *Why it is called that*: Where profile setup occurs.
*   **`jobs/pages/jobs-page/`**
    *   *Why it is used*: Renders search results. Automatically filters salary text using an annualized parser logic. Restricts recommendation tabs and application actions if the active user is an `Admin`.
    *   *Why it is called that*: The main jobs list page view.
*   **`jobs/components/jobs-filters/`**
    *   *Why it is used*: Renders the search input, remote toggle network badge, and min-max salary sliders.
    *   *Why it is called that*: Isolated filter controls.
*   **`reports/pages/reports-page/`**
    *   *Why it is used*: Serves as the Admin Control Panel. Contains database KPI metrics, ApexCharts daily ingestion graphs, concentrations bars, and localStorage-persisted sync logs.
    *   *Why it is called that*: Where system statistics are compiled.
*   **`insights/pages/insights-page/`**
    *   *Why it is used*: The chat portal for the AI Copilot. It syncs the active message array to the backend on each user prompt.
    *   *Why it is called that*: Renders job market insights.

---

### 🧪 4. Frontend Tests Review (ui/tests)
*   **`ui-features.test.js`**
    *   *Why it is used*: Node-based static layout script asserting that template anchors, route protections, and button strings are present across templates.
    *   *Why it is called that*: Test script for UI features.
*   **`ui-features.spec.ts`**
    *   *Why it is used*: Jasmine specification file documenting mock component expectations.
    *   *Why it is called that*: Jasmine spec file for frontend layout features.
