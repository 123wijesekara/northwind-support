# Northwind Support Desk — Candidate Submission

A support ticket management system: raise tickets, search and filter them, assign them to
agents, escalate them, and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## What I built

I forked the repo and completed **Tasks 1, 2, and 3**. Task 4 (Dockerising the API) was
**not completed** — see [Known limitations](#known-limitations).

| Task | Status | Commits |
| --- | --- | --- |
| 1 — Fix the list-filter defect, add server-side filtering | ✅ Complete | 3 |
| 2 — Triage on create, escalation with history, SLA status, UI | ✅ Complete | 6 |
| 3 — Sign-in with JWT, protected API, login UI | ✅ Complete | 3 |
| 4 — Dockerise the API | ❌ Not done   | — |

**Rough time spent:** ~13 hours. See [Time](#time-spent) at the bottom.

---

## Quick start (how I ran it)

Prerequisites: **.NET SDK 10**, **Node.js 20.19+ or 22** (24 also works), **SQL Server 2022**.

On Windows with SQL Server installed natively (no Docker):

```powershell
# 1. Enable TCP/IP on SQL Server (once): SQL Server Configuration Manager → Protocols for MSSQLSERVER → enable TCP/IP → set IPAll port 1433 → restart service.

# 2. From the repo root, set the connection string for the local Windows-auth SQL Server.
$env:ConnectionStrings__Default="Server=localhost;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"

# 3. Start the API. On first run it creates the database, applies all migrations, and seeds demo data.
npm run api

# 4. In a second terminal, start the web app.
npm run web