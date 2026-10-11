\# Approach



How I thought about each task. The README says what was built and why; this file covers

the path, the judgment calls, and what went wrong along the way.



The repo is a layered .NET solution (Domain / Application / Infrastructure / Presentation)

with a React frontend. Every new piece of code went into the layer it belonged to — pure

rules in Domain, orchestration in Application, EF Core and HTTP in the outer layers.



\---



\## Task 1 — the defect and server-side filtering



\*\*The defect.\*\* I reproduced it in the browser first, with the DevTools Network tab open.

Changing the Status dropdown fired no request. Typing in search later fired a request that

carried the earlier status. Reading `useTicketList.ts`, the effect's dependency array was

`\[filters.search, filters.page, filters.pageSize]` — nine other fields updated state

silently.



\*\*The fix.\*\* I split the effect in two: a debounced one for `search`, an immediate one for

everything else. The immediate effect uses `JSON.stringify` of the filters minus `search`

as its key, so a \*\*future\*\* filter — `slaStatus`, which arrived in Task 2 — is picked up

with no code change.



\*\*What I deliberately did not do.\*\* I did not add `key={...}`, filter in memory, or force a

reload.What I did instead. I fixed the useEffect in useTicketList.ts. The effect was only watching search, page, and pageSize, so changing any other filter updated the state but never fired a new request. I split it in two — one debounced effect for the search box, one immediate effect for every other filter — and added a ref so the fetch always reads the latest filter values.



\*\*Proving it.\*\* A regression test fires `updateFilters({ status: 'InProgress' })` and

asserts `getTickets` is called immediately, without advancing timers. It fails on the

original code and passes on the fix.



\*\*Server-side filtering.\*\* The API bound filter values but never used them. I moved the

logic into a single `ApplyFilters` helper, applied \*\*before\*\* the `CountAsync` and the

`Skip`/`Take`, so `totalCount` and the pager reflect the filtered set. I checked the

generated SQL in the API console (the log level is `Information` in Development).



\---



\## Task 2 — triage, escalation, SLA



\*\*Why sub-steps.\*\* This is the largest task, and I did it in five small commits: pure

rules first, then config plus agent selection, then triage on create, then escalation and

history, then the SLA filter. Each sub-step was built, tested and committed before the next.



\*\*Why pure rules first.\*\* `PriorityResolver`, `SlaCalculator`, and `SlaEvaluator` are pure

functions — no database, no HTTP, no config framework. That made them fast to test and

easy to share between create and escalate. `SlaEvaluator` gained a new overload so

existing callers keep working; the new one adds the `AtRisk` state (BR-8) at a configurable

threshold.



\*\*Where `AgentSelector` lives.\*\* It started in Infrastructure, which broke the layering —

`TicketTriageService` (Application) then had to reference Infrastructure. I moved it to

Application. It is a pure rule; it has no business in Infrastructure.



\*\*Escalation.\*\* A new `TicketEscalation` entity, immutable, with before and after values.

The handler keeps the current owner if they are still eligible; otherwise it picks a new

one with the same selector as create. Every escalation writes a history row.



\*\*The SLA filter — a bug I did not catch with tests.\*\* My first version of the filter used

`EF.Functions.DateDiffMillisecond` and multiplied by 100 in the comparison. Tests passed

because their windows were short. On real tickets the multiplication overflowed `int` and

the API returned 500. I switched to `DateDiffSecond` — 1000× smaller values, still an

`int`, no overflow. That is exactly the kind of thing tests should catch, and here they

did not; running the real system caught it.



\*\*Frontend.\*\* One `<SlaIndicator />` used on both the list and the detail page, with icon

\*\*and\*\* text so the state is not conveyed by colour alone. Plus an SLA dropdown in the

filter bar, an escalation panel with the same validation as the server, and a triage

summary on the create page.



\*\*A Windows-only hiccup.\*\* Vitest's default process pool repeatedly timed out on my

machine. Switching to `pool: 'threads'` with `maxWorkers: 1` made the tests reliable.



\---



\## Task 3 — authentication



\*\*Three batches.\*\* Credentials and login (backend); actor from the token (backend);

login page and protected routes (frontend).



\*\*Judgment calls.\*\*

\- Credentials live as a `PasswordHash` column on `Agent`, not a separate `Users` table.

&#x20; `Agent` already carries `Email` and `IsActive` — the other two things sign-in needs — so

&#x20; a second table would only duplicate data.

\- The JWT is stored in `sessionStorage`, not `localStorage`. It is cleared when the tab

&#x20; closes; `localStorage` survives browser restarts and is a larger exposure. This is a

&#x20; trade-off, documented in the README's limitations.

\- The signing key comes from configuration, never a literal. Length is validated at

&#x20; startup with a clear error.



\*\*Two DI and API surprises.\*\*



1\. I registered `AuthOptions` in `Program.cs` as a raw singleton and had `AuthService`

&#x20;  take `IOptions<AuthOptions>`. These do not interoperate — the container does not wrap a

&#x20;  raw instance in `IOptions<>`. The fix was to also call

&#x20;  `services.Configure<AuthOptions>(...)`.

2\. Swashbuckle 10 changed the OpenAPI security scheme shape.

&#x20;  `AddSecurityRequirement` now takes a `Func<OpenApiDocument, OpenApiSecurityRequirement>`

&#x20;  and the requirement uses `OpenApiSecuritySchemeReference("Bearer", document)`. Two

&#x20;  iterations to get right.



\*\*The 401 handler is central.\*\* `client.ts` reads the token from `sessionStorage` on every

request; on any 401 it clears the session and dispatches an event that `main.tsx` reacts

to by signing out and redirecting to `/login`. Callers do not have to handle it.



\---



\## What is not done and why



\*\*Task 4 (Docker) was not attempted. 



\---



&#x20;

&#x20;



\## In one line



Tasks 1, 2 and 3 are complete, tested and committed; Task 4 is not .

