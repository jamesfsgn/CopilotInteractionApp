# Microsoft 365 Copilot Interaction Viewer

A Windows desktop app (.NET 10 / WinForms) that pulls Microsoft 365 Copilot prompts and
responses out of Microsoft Graph, groups them into readable conversations, and exports them
to a formatted Excel workbook.

It is built for compliance, adoption and eDiscovery scenarios where someone needs to answer
"what are people actually asking Copilot?" without writing a script for every request.

---

## Table of contents

- [What it does](#what-it-does)
- [Requirements](#requirements)
- [Setting up the Entra app registration](#setting-up-the-entra-app-registration)
- [Running the app](#running-the-app)
- [The user interface](#the-user-interface)
- [How a pull works](#how-a-pull-works)
- [Excel export](#excel-export)
- [Project structure](#project-structure)
- [Key design decisions](#key-design-decisions)
- [Settings and privacy](#settings-and-privacy)
- [Troubleshooting](#troubleshooting)
- [Known limitations](#known-limitations)
- [Building from source](#building-from-source)
- [Reference](#reference)

---

## What it does

- Authenticates to Microsoft Graph with **app-only** (client credentials) auth.
- Calls `aiInteractionHistory: getAllEnterpriseInteractions` for one user, or for every
  Copilot-licensed user in the tenant.
- Automatically discovers which users hold a Microsoft 365 Copilot license so unlicensed
  users are not queried pointlessly.
- Displays results as a **master–detail** view: a list of conversations on the left, the
  messages of the selected conversation on the right, and a full detail pane underneath.
- Filters by app (Copilot in Teams, Word, Outlook, ...), date range, interaction type, and
  free-text search.
- Exports everything to a four-sheet `.xlsx` workbook laid out around prompt/response pairs.

---

## Requirements

| Requirement | Detail |
|---|---|
| OS | Windows (WinForms desktop app) |
| Runtime | .NET 10 Desktop Runtime (or the SDK to build) |
| Tenant | Microsoft 365 tenant with Copilot licenses assigned |
| Permissions | An Entra app registration with the permissions below |

NuGet dependencies (restored automatically):

- **Azure.Identity** 1.21.0 — `ClientSecretCredential` for app-only token acquisition
- **ClosedXML** 0.105.1 — Excel workbook generation

There is no Graph SDK dependency; requests are issued with `HttpClient` and deserialized
with `System.Text.Json`.

---

## Setting up the Entra app registration

1. In the Microsoft Entra admin center, go to **App registrations → New registration**.
   Give it a name (e.g. *Copilot Interaction Viewer*) and register it. No redirect URI is
   needed — this app never uses interactive sign-in.
2. Under **Certificates & secrets**, create a **client secret** and copy the *value*
   immediately (it is only shown once).
3. Under **API permissions → Add a permission → Microsoft Graph → Application permissions**,
   add:

   | Permission | Why it is needed |
   |---|---|
   | `AiEnterpriseInteraction.Read.All` | **Required.** Reads Copilot interaction history. This is the only permission the interaction API supports. |
   | `User.Read.All` | Required for tenant-wide pulls: enumerates users and reads `assignedPlans` to detect Copilot licenses. Not needed if you only ever query one named user. |

4. Click **Grant admin consent**. Application permissions do not work without it.
5. Copy the **Directory (tenant) ID** and **Application (client) ID** from the app's
   Overview page.

> **Delegated permissions are not supported by this API.** There is no way to run this as a
> signed-in user; it is app-only by design. Treat the client secret as a highly privileged
> credential — it grants read access to every Copilot conversation in the tenant.

---

## Running the app

1. Launch `CopilotInteractionApp.exe`.
2. Fill in **Tenant ID**, **Client ID** and **Client secret**.
3. Optionally set a scope and filters (see below).
4. Click **Get interactions**.
5. Review the results, then click **Export to Excel...** if you need a file.

Tenant ID, client ID, and the non-secret filter preferences are remembered between runs.
The client secret is never saved and must be re-entered each session.

---

## The user interface

### Connection panel (top)

| Control | Meaning |
|---|---|
| **Tenant ID** | Directory ID or domain (`contoso.onmicrosoft.com`). |
| **Client ID** | Application (client) ID of the app registration. |
| **Client secret** | Secret value. Masked, and never written to disk. |
| **User (blank = all)** | A UPN or object ID to query a single user. Leave blank for a tenant-wide pull. |
| **App filter** | Friendly product names ("Microsoft 365 Copilot Chat", "Copilot in Word", ...) mapped to raw `appClass` values. |
| **Page size ($top)** | Graph page size. 100 is the documented optimum. |
| **Only users with a Copilot license** | When checked (default), a tenant-wide pull first enumerates users and keeps only those with an enabled Copilot service plan. |
| **Date range** | Optional `createdDateTime` filter. Both boundaries are always sent because the API requires it. |
| **Use /beta endpoint** | Toggles between `/beta` (default) and `/v1.0`. |
| **Max items** | Hard cap on interactions collected across all users, so a large tenant cannot run away. |
| **Get interactions / Cancel / Export to Excel...** | Run, abort, and export. |

### Filter bar

**Search** (matches message text, user, author, app name, raw app class, session ID and
request ID), an **interaction type** filter (All / Prompts only / Responses only), a
**Group by session** toggle, and a live result count. All filtering here is client-side and
instant — it does not re-query Graph.

### Grids

- **Sessions** (left, shown when *Group by session* is on): one row per conversation —
  Started, User, Msgs, Prompts, First prompt, App, Session ID. Newest first.
- **Messages** (right): the messages of the selected conversation in chronological order —
  Timestamp, User, Type, From, App, Conversation, Prompt / Response, Att., Links,
  Session ID, Request ID, Interaction ID.

Unchecking *Group by session* collapses the sessions grid and shows a single flat list of
all matching interactions.

### Bottom tabs

- **Details** — everything about the selected message: full metadata, the raw body, plus
  contexts, attachments and links (the resources Copilot grounded its answer on).
- **Issues (N)** — non-fatal problems from the last pull, so nothing interrupts a long run
  with a dialog. Reports per-user failures, the count of users skipped for having no Copilot
  license, and a warning if the server-side `$filter` had to be applied locally.

---

## How a pull works

1. **Acquire a token** via `ClientSecretCredential` for scope
   `https://graph.microsoft.com/.default`. The credential is cached and reused until the
   tenant/client/secret changes.
2. **Resolve the scope:**
   - A value in the *User* box → that single user.
   - Otherwise → `GET /v1.0/users?$select=id,displayName,userPrincipalName,accountEnabled,assignedPlans&$top=999`,
	 paged through `@odata.nextLink`. Disabled accounts are dropped. If *Only users with a
	 Copilot license* is on, users are further filtered to those with an **enabled** Copilot
	 service plan (see `CopilotLicenseCatalog`).
3. **Query each user** at
   `GET /{version}/copilot/users/{id}/interactionHistory/getAllEnterpriseInteractions`
   with `$top` and, when set, a `$filter` combining `appClass` and the `createdDateTime`
   boundaries. Results are paged until exhausted or the *Max items* cap is hit.
4. **Handle failures per user.** A user that fails (no license, access denied, ...) is
   recorded in the Issues tab and the run continues.
5. **Project and sort.** Interactions are flattened to `InteractionRow`, sorted newest
   first, and grouped into `SessionGroup` conversations for display.

### Resilience

- **Throttling and transient errors** — 429 and 5xx responses are retried up to four times,
  honoring the `Retry-After` header and falling back to exponential backoff.
- **`$filter` rejection** — some tenants return `400 Bad Request` for a server-side filter
  on this function. When that happens the request is retried **unfiltered** and the date/app
  filter is applied client-side, capped at 50 pages per user so it cannot run forever. A
  warning appears in the Issues tab because results may be truncated for those users.
- **Date boundaries** — the API filter is exclusive on both ends
  (`createdDateTime gt X and createdDateTime lt Y`), so the app widens the selected range by
  one second at the start and one day at the end. A "from" and "to" of the same day
  therefore captures that whole day. The two pickers also auto-correct each other so an
  inverted range can never be submitted.

---

## Excel export

**Export to Excel...** writes the *currently filtered* rows to a `.xlsx` workbook with four
sheets. Every data sheet has a frozen header row and an autofilter.

### Summary

Run metadata (generated timestamp, scope, endpoint, date range, active filters), headline
counts (users with activity, conversations, exchanges, interactions), an **Activity by user**
table (conversations, prompts, responses, average reply time, first/last activity — sorted by
prompt volume), and the list of issues if any.

### Conversations

**One row per exchange** — the readable view. Prompt and response sit side by side:

`# · User · Conversation started · Turn · Prompt time · Prompt · Response time · Response ·
Reply (sec) · App · Chat type · Attachments · Links · Session ID · App class (raw)`

Prompt cells are tinted blue, response cells green, and each conversation is separated by a
top border with alternating banding so exchanges never blur together.

### Messages

**One row per interaction** — the audit view, kept in conversation and turn order with a
running sequence number, colour-coded by prompt/response, and including the request and
interaction IDs for correlation with other Graph data.

### Sessions

**One row per conversation** — the analytics view: start, end, duration in minutes, message
/ prompt / response counts, first prompt, app and session ID.

---

## Project structure

```
CopilotInteractionApp/
├── CopilotInteractionApp.csproj    Project file (net10.0-windows, WinForms, nullable on)
├── Program.cs                      Entry point + global exception handlers
├── Form1.cs                        All application logic
├── Form1.Designer.cs               Hand-written UI layout
├── Models/
│   ├── AiInteraction.cs            DTOs for the Graph aiInteraction resource
│   └── GraphUser.cs                User + assignedPlans projection
└── Services/
	├── GraphInteractionClient.cs   All Graph I/O: auth, paging, retry, filtering
	├── CopilotLicenseCatalog.cs    Copilot service plan IDs + license detection
	├── AppClassCatalog.cs          appClass → friendly product name mapping
	├── InteractionRow.cs           Flattened, grid-friendly projection
	├── SessionGroup.cs             Groups interactions into conversations
	├── ConversationTurn.cs         Pairs a prompt with its response(s)
	└── ExcelExporter.cs            Four-sheet formatted workbook writer
```

### Type reference

| Type | Responsibility |
|---|---|
| `InteractionQueryOptions` | Everything needed for a pull: credentials, scope, filters, caps. |
| `InteractionFetchResult` | Items plus run diagnostics — per-user errors, users scanned/queried, unlicensed count, limit-reached and filter-fallback flags. |
| `UserInteraction` | An `AiInteraction` paired with the `GraphUser` it belongs to (the API response does not name the user). |
| `GraphRequestException` | A non-success Graph response carrying its `HttpStatusCode`, so the client can react specifically to a 400 on `$filter`. |
| `GraphInteractionClient` | Token acquisition and caching, user enumeration, per-user interaction retrieval, paging, retry, and the client-side filter fallback. |
| `CopilotLicenseCatalog` | The six `M365_COPILOT_*` service plan GUIDs and `HasCopilotLicense`. |
| `AppClassOption` / `AppClassCatalog` | Friendly names for `IPM.SkypeTeams.Message.Copilot.*` values, with a graceful fallback for unknown apps. |
| `InteractionRow` | Grid projection; also strips HTML/attachment markup to plain text. |
| `SessionGroup` | Conversation grouping (`Build`) and turn pairing (`BuildTurns`). |
| `ConversationTurn` | One prompt and the response(s) it produced, with latency and attachment rollups. |
| `ExportContext` / `ExportSummary` / `ExcelExporter` | Export inputs, result counts, and workbook generation. |

---

## Key design decisions

**Raw HTTP instead of the Graph SDK.** The interaction export API is preview-era and its
shape shifts; hand-rolled DTOs plus `HttpClient` avoid SDK version churn and keep the
dependency surface to two packages.

**Conversations are keyed on `(userPrincipalName, sessionId)`.** Session IDs are not
guaranteed unique across users, so the user is part of the key. Interactions with no session
ID collapse into a single "(no session)" group per user rather than being dropped.

**Turn pairing rules.** Every prompt starts a new turn. Consecutive responses attach to the
current turn (Copilot sometimes emits several message parts for one answer) and are joined
with a space in the transcript. A response with no preceding prompt becomes a prompt-less
turn instead of being discarded.

**Failures are surfaced, not thrown.** In a tenant of thousands, some users will always fail.
Per-user errors are collected into the Issues tab so one bad user cannot end a 20-minute run.

**Friendly names never replace raw values.** The UI shows "Copilot in Word", but the raw
`appClass` is preserved on every row, is still what gets sent in the `$filter`, is searchable,
and appears in an "App class (raw)" column on every Excel data sheet.

**The UI is hand-written, not designer-generated.** `Form1.Designer.cs` is maintained by hand.
Control **add order matters** for docking — fill-docked controls must be added before
edge-docked ones.

---

## Settings and privacy

Non-secret preferences are stored as JSON at:

```
%AppData%\CopilotInteractionApp\settings.json
```

Persisted: tenant ID, client ID, user box contents, the Copilot-licensed-only toggle, the
selected app class (raw value), the beta toggle, page size and max items.

**Never persisted:** the client secret, and any interaction content. Interaction data lives
only in memory for the life of the process, unless you explicitly export it.

Anything you export is unencrypted Copilot conversation content. Store and share it
accordingly.

### Before you commit

This repository contains no credentials, tenant identifiers or captured conversation data,
and it should stay that way:

- `settings.json` lives in `%AppData%`, **outside** the repository, so your tenant and client
  IDs are never staged by accident. Do not copy it into the project folder.
- The client secret is only ever held in memory and in the (masked) text box.
- `.gitignore` excludes `bin/`, `obj/`, `.vs/` and `*.user`. Those contain absolute paths that
  embed your Windows user name, along with indexed source and machine-specific caches.
- `.gitignore` also excludes `*.xlsx` so an exported workbook of real Copilot conversations
  cannot be committed by mistake.
- If you add screenshots to the README, redact tenant IDs, user principal names and message
  content first.

---

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `401 Unauthorized` on the token request | Wrong tenant/client ID, or an expired secret. Regenerate the secret. |
| `403 Forbidden` on every user | `AiEnterpriseInteraction.Read.All` is missing, or admin consent was never granted. |
| `403` while enumerating users | Tenant-wide pulls also need `User.Read.All`. Add it, or enter a specific user instead. |
| Zero users found with a license | The app matches enabled `M365_COPILOT_*` service plans. Uncheck **Only users with a Copilot license** to query everyone and confirm. |
| A specific user fails with 403/404 | Usually no Copilot license or no interaction history. Check the Issues tab for the exact Graph message. |
| Issues tab reports the `$filter` fallback | Graph rejected the server-side filter; results were filtered locally and may be capped at 50 pages per user. Narrow the date range or drop the app filter. |
| Nothing returned despite known activity | Interaction history is retained for a limited window and only covers experiences that write to the interaction history service. Agents built in Copilot Studio are excluded. |
| Run is very slow | It is one HTTP round trip per user, plus paging. Set a date range, lower **Max items**, or query a single user. |
| Export fails with a file-in-use error | The workbook is open in Excel. Close it and export again. |

---

## Known limitations

- **App-only auth only.** The API does not support delegated permissions.
- **No delta queries.** Every run is a full pull for the selected scope; there is no
  incremental sync.
- **Copilot Studio agents are excluded** by the API itself.
- **`/beta` is the default endpoint.** Beta APIs can change without notice and are not
  supported for production use. Switch to `/v1.0` for stability.
- **Tenant-wide pulls do not parallelize.** Users are queried sequentially to stay well
  clear of Graph throttling.
- **The app class list is partly inferred.** BizChat, Teams, Word, Excel, PowerPoint and
  Outlook are confirmed by Microsoft's documentation; the remaining entries (Stream,
  Designer, SharePoint, Planner, OneDrive, Loop, OneNote, Whiteboard) are inferred from the
  naming pattern. Unrecognized values degrade to "Copilot in {suffix}" and the raw value is
  always preserved.
- **Excel cell limit.** Text longer than 32,000 characters is truncated in the export.
- **Never validated against a live tenant.** All logic has been verified against synthetic
  data. Confirm behavior in your own environment before relying on it for compliance work.

---

## Building from source

```powershell
git clone https://github.com/<your-org>/CopilotInteractionApp.git
cd CopilotInteractionApp
dotnet build
dotnet run
```

Or open `CopilotInteractionApp.slnx` in Visual Studio 2026 and press F5.

> If a build fails on the copy step with a file-lock error, the app is still running under
> the debugger. Stop it first. To compile without the copy step:
> `dotnet msbuild -t:CoreCompile`.

---

## Reference

- [aiInteractionHistory: getAllEnterpriseInteractions](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/interaction-export/aiinteractionhistory-getallenterpriseinteractions)
- [aiInteraction resource type](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/interaction-export/resources/aiinteraction)
- [Microsoft Graph permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
- [Product names and service plan identifiers for licensing](https://learn.microsoft.com/en-us/entra/identity/users/licensing-service-plan-reference)
