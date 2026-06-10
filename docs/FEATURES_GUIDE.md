# Platform Features & Admin Control Panel Guide

This guide provides a detailed overview of the architectural changes, database controls, user experience updates, and security features recently introduced to the AI Job Market Intelligence Platform.

---

## Table of Contents
1. [Career Bot Chat Memory (AI Copilot)](#1-career-bot-chat-memory-ai-copilot)
2. [Jobs Filter & Salary Annualizing Parser](#2-jobs-filter--salary-annualizing-parser)
3. [Premium Layout Redesign](#3-premium-layout-redesign)
4. [Authentication & Session Security](#4-authentication--session-security)
5. [Administrative Access Restrictions](#5-administrative-access-restrictions)
6. [Interactive Admin Control Panel](#6-interactive-admin-control-panel)

---

## 1. Career Bot Chat Memory (AI Copilot)

### The Problem
Previously, the backend chat service relied on a transient in-memory dictionary to hold user chat sessions. Whenever the C# backend was re-compiled or restarted during local development, this cache was reset, causing the Llama-3 model to lose context (e.g. forgetting the user's name).

### The Solution
We transitioned the chat session tracking to a **stateless, client-synchronized memory model**:
- **Stateless DTOs**: Added a `History` payload array (`List<ChatMessageDto>`) to the C# request contract.
- **Frontend Sync**: The Angular client keeps track of active message histories in the chat component state and transmits the whole history alongside each new prompt.
- **LLM Context Retention**: The `CareerChatService` dynamically populates the prompt history from the incoming HTTP payload, ensuring the LLM never forgets context, regardless of server compilation states.

---

## 2. Jobs Filter & Salary Annualizing Parser

### Premium Filter Controls
The search and filter section on the Jobs page has been upgraded to a modern, slate-dark design featuring:
- A responsive search input with inline icons and reset buttons.
- A **Remote Only** toggle with active status indicators.
- A **Salary Range Slider** (Minimum to Maximum up to $250k/yr) rendered dynamically for manual queries under the "All Jobs" view.

### Annualizing Salary Parser
Since salaries scraped from job boards are unstructured text strings, we implemented a robust parsing engine in the frontend to support numeric range filtering:
- **Numerical Extraction**: Regular expressions extract numerical bounds from currency characters ($, £, €, USD, etc.) and shorthand numbers (e.g., "120k" -> 120,000).
- **Time-Period Normalization**: Multiplies raw rates into annual equivalents:
  - *Hourly rates* (detected via "hour", "hr", "/h") are multiplied by **2,000** (average annual working hours).
  - *Daily rates* (detected via "day", "/d") are multiplied by **260** (average annual working days).
- **Overlapping Range Overlays**: Checks that the job's salary range intersects with the user's Min-Max slider bounds. Undisclosed salary postings are preserved.

---

## 3. Premium Layout Redesign

The user portal aesthetics have been redesigned to feel premium, utilizing vibrant HSL-tailored colors, slate-950 borders, and subtle animations.

### Sidebar (Navigation)
- **Categorized Groups**: Navigation links are grouped into dedicated sections (`Core`, `Analytics`, `AI Copilot`).
- **Inline SVGs**: Plain text headers are replaced with custom vector icons with hover transition states.
- **API Status Indicator**: Displays a health badge ("Connected to Groq (Llama-3)") with a pulsing green online indicator.
- **User Initials Avatar Card**: Shows user initials, email, role, and actions side-by-side.

### Header (Topbar)
- **Breadcrumb Paths**: Displays a navigation trail (e.g., `Portal / Salary Analytics`).
- **Icon Actions**: Upgrade buttons (like Export and Refresh) to use matching inline vector icons.
- **Notifications Hub**: Added a stylized notification bell with a red active notification badge.

---

## 4. Authentication & Session Security

### Logged-Out Screen
Created a dedicated landing page at `/auth/logged-out`. When a user requests a logout, session tokens are cleared, and they are navigated to this screen showing a secure lock shield. Clicking **Sign In Again** returns them to the login screen.

### Switch Account Safeguard
To support rapid user changes, the **Switch** button in the sidebar triggers a method that clears authentication credentials before redirecting to `/auth/login`. This prevents the Angular `guestGuard` from causing redirect loops.

### Preferences Isolation (Cache Protection)
To prevent browser caches from leaking preferences between different users on the same machine:
- **Response Cache Prevention**: The backend `UserPreferencesController.Get()` method is decorated with `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` to output strict `no-store, no-cache` headers.
- **Cache-Busting Queries**: The frontend requests append a dynamic timestamp parameter `_t=Date.now()` on every preferences GET query to bypass intermediate caches.

---

## 5. Administrative Access Restrictions

Administrative accounts represent operators rather than Job Seekers, so all application-tracking features are restricted for the Admin role:
- **No Apply Actions**: The "Apply" and "Applied" buttons are hidden on job cards.
- **No Recommendation Mode**: The "Recommended" tab, onboarding banners, and the "Applied Jobs" list are hidden on the Jobs page, defaulting them straight to the "All Jobs" view.

---

## 6. Interactive Admin Control Panel

The **Reports** page (guarded by `adminGuard`) has been redesigned as a full **Admin Control Panel**:

- **Manual Sync Engine**: A card containing a "Sync Jobs Now" trigger button. Clicking it hits `POST /api/admin/trigger-fetch`, animating a spinner, and outputting success metrics (number of jobs added) or rose-colored error banners.
- **Recent Operations Log**: A local storage-persisted log ledger table recording sync status (SUCCESS/FAILED badges), timestamps, and ingestion counts.
- **Database KPIs**: Statistics showing connected model states, active providers, and ingestion volume totals over the last 30 days.
- **Daily Ingestion Trends Chart**: An interactive Area Chart showing ingestion volumes day-by-day.
- **Distribution Breakdowns**: Glowing CSS bars illustrating the percentage concentration of jobs by data source, location, and seniority.
