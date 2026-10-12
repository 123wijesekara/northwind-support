# Northwind Support Desk — Candidate Submission

A support ticket management system: raise tickets, search and filter them, assign them to
agents, escalate them, and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## What I built

I forked the repo and completed **Tasks 1, 2, and 3**. Task 4 (Dockerising the API) was
**not completed** — see Known limitations.

| Task | Status | Commits |
| --- | --- | --- |
| 1 — Fix the list-filter defect, add server-side filtering | Complete | 3 |
| 2 — Triage on create, escalation with history, SLA status, UI | Complete | 6 |
| 3 — Sign-in with JWT, protected API, login UI | Complete | 3 |
| 4 — Dockerise the API | Not done | — |

**Rough time spent:** ~13 hours.

---

## Quick start

Prerequisites: **.NET SDK 10**, **Node.js 20.19+ or 22** (24 works too), **Docker Desktop**
(for SQL Server, and required for Task 4), **Git**.

```bash
cp .env.example .env          # every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

Trust the HTTPS development certificate once if the browser warns:

```powershell
dotnet dev-certs https --trust
```

### How I ran it locally

I developed on Windows without Docker Desktop, using a native SQL Server 2022 instance and
a per-session environment override:

```powershell
$env:ConnectionStrings__Default="Server=localhost;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
npm run api
```

Nothing about that is committed. `appsettings.Development.json` and `docker-compose.yml`
still use the Docker `sa` connection string, so the flow above works unchanged.

---

## Development login credentials

 

| Field | Value |
| --- | --- |
| Email | `alex.turner@northwind-support.example` |
| Password | `Passw0rd!Dev` |

 

---

## What was implemented

### Task 1 — Defect fix and server-side filtering

- **DEFECT-117** — fixed at the cause. In
  `apps/web/src/features/tickets/hooks/useTicketList.ts`, the `useEffect` that triggers the
  fetch listed only `[filters.search, filters.page, filters.pageSize]` in its dependency
  array. Every other filter (`status`, `priority`, `categoryId`, `customerId`,
  `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`) updated React state but
  never triggered a re-fetch. The effect is split in two: a debounced one for `search`, an
  immediate one for everything else. A regression test fails on the original code and
  passes on the fix.
- **Server-side filtering (FL-1 through FL-6)** — every filter is now applied in SQL inside
  one `ApplyFilters` helper in `TicketQueries.cs`, **before** `CountAsync` and
  `Skip`/`Take`. `totalCount`, `totalPages` and the pager all reflect the filtered set.
  Filters cover `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId` (exact
  match), `unassignedOnly`, and a trimmed `search` on title, reference, or customer name.
  They combine with AND. Adding another filter is one `Where` — which is exactly what the
  Task 2 SLA filter did, with no restructuring of the query, sort, or projection.

### Task 2 — Triage, escalation, SLA

- **Triage on create (BR-1 through BR-6)** — `POST /api/tickets` resolves the effective
  priority (default Medium; forced Critical for Security and Outage), computes the SLA due
  date (base window per priority, halved for Premium, minimum 1 hour), picks an eligible
  agent (active, specialist where required, strictly below their limit, fewest open,
  deterministic tie-break), and returns a `triage` object with human-readable reasons. If
  nobody is eligible, the ticket is created unassigned — that is not an error.
- **Escalation (BR-7)** — `POST /api/tickets/{id}/escalate` raises priority one step,
  recomputes the due date from now, keeps the current owner if still eligible, and writes
  an immutable history row. Critical, Resolved and Closed tickets return 409.
- **Escalation history** — `GET /api/tickets/{id}/escalations`, newest first.
- **SLA status (BR-8)** — always derived, never stored.
- **SLA filter** — `GET /api/tickets?slaStatus=...`, with filtering, counting and paging
  all in SQL.
- **UI** — one reusable `<SlaIndicator />` used on both list and detail (icon + text, so
  the state is not conveyed by colour alone); SLA dropdown in the filter bar; escalation
  panel with the same client-side validation as the server; escalation history; and a
  triage summary shown after create.

### Task 3 — Login and authentication

- **Credentials on `Agent`** — a `PasswordHash` column added by migration
  `AddAgentCredentials`.
- **Password hashing** — `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2 with
  HMAC-SHA256, per-password salt).
- **`POST /api/auth/login`** — returns a JWT, its expiry, and user data. Wrong email and
  wrong password return the same 401 and message. Inactive agents cannot sign in.
- **`GET /api/auth/me`** — the current user or 401.
- **Global authorization** — a fallback policy means every endpoint needs a valid token;
  only login stays open.
- **`escalatedBy` from the token** — no longer from the request body.
- **UI** — `/login` page with labelled fields, client validation, pending state, and a
  clear error on rejected credentials. The token is kept in `sessionStorage` and attached
  to every API call. Protected routes redirect unauthenticated visitors to login and
  return them to where they were heading. The header shows the signed-in agent's name and
  a Sign out button. Any 401 clears the session and redirects to login.

---

 

## Known limitations

- Task 4 (Docker) is not done.
 

---
## AI usage

I used AI tools across Tasks 1, 2, and 3 to help catch errors and check code
correctness — reviewing   files before saving, working through failing test
output, and diagnosing build and runtime errors. One real bug the AI introduced was an
SLA filter that overflowed on real tickets and returned 500; tests passed but the
running system failed. I fixed it and started testing every feature end-to-end
## Time spent

| Work | Time |
| --- | --- |
| Environment setup | ~2 h |
| Task 1 — defect fix and server-side filtering | ~2.5 h |
| Task 2 — triage, escalation, SLA, frontend | ~6 h |
| Task 3 — auth backend and frontend | ~3 h |
| README and APPROACH.md | ~1 h |
| Task 4 — Docker | 0 (not started) |