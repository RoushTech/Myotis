# Myotis - Claude Code Guide

Myotis is a multi-tenant mail catcher for non-production environments: apps send to one SMTP endpoint with a per-mailbox credential, mail lands in that mailbox, and users granted the mailbox read it in the browser. Nothing is ever delivered. Read [docs/brief.md](docs/brief.md) before changing scope or the data model.

The repo starts empty. Build it to the layout and rules below, which match Procyon, Polecat and Tamias so code and habits carry across.

## Stack

- **Server:** ASP.NET Core (.NET 10), EF Core with SQLite, ASP.NET Identity, SignalR for live mail, Sentry
- **SMTP:** an in-process SMTP listener (library is an open question in the brief), MimeKit for parsing
- **Frontend:** Vue 3 + TypeScript, Vite, Vuetify, Pinia, Pinia Colada
- **Shared:** RoushTech.Meles once it is published; RoushTech.Asio for background task logs

No Hangfire and no Postgres: retention is one hosted service, and SQLite allows a single writer, so the SMTP listener and the web app share one process and one replica.

## Solution Structure

```
RoushTech.Myotis/            # Core: data model, DbContext, migrations, services, SMTP intake, DI
RoushTech.Myotis.Website/    # ASP.NET Core host: API, SignalR hub, MCP endpoint, serves the SPA
RoushTech.Myotis.Tests/      # NUnit tests
RoushTech.Myotis.Vue/        # Vue 3 frontend
```

## Development Setup

```bash
cp RoushTech.Myotis.Website/appsettings.example.json RoushTech.Myotis.Website/appsettings.local.json
dotnet watch --project RoushTech.Myotis.Website
```

No `docker compose` is needed: the database is a SQLite file under `.dev/`. The server runs on http://localhost:5400 and starts Vite through SpaProxy, redirecting to http://localhost:3004. SMTP listens on 2526. Development alone accepts SMTP auth without TLS. The example settings seed `admin@roushtech.net` / `Password1` on an empty database.

To send a test message, create a mailbox and credential in the UI, then point any SMTP client at `localhost:2526` with that credential.

Migrations:

```bash
dotnet ef migrations add <Name> --project RoushTech.Myotis -o Migrations
```

## Model

- `Mailbox` owns `SmtpCredential` rows. The authenticated credential is the only routing key; recipients never choose a mailbox.
- `Message` belongs to a mailbox and stores the envelope (`MAIL FROM`, every `RCPT TO`) plus parsed summary fields for listing. Raw bytes live in `MessageContent`, one row per message, so list queries never load them.
- `MailboxGrant` joins a user to a mailbox. Users hold any number of grants. Administrators see everything without grants.
- `PersonalAccessToken` (`myotis_pat_...`) authenticates `/api/v1` and `/mcp`, never the browser cookie.

## Rules

### Isolation (the reason this exists)
- Every action touching a mailbox or message checks the caller holds a grant for that mailbox, or is an administrator. Answer 404, never 403, so mailbox names and IDs don't leak.
- Bind entities from the route and check access in one global filter, as Tamias does with `CustomerAccessFilter`, rather than in each action.
- `/mcp` skips MVC, so filters and role attributes don't apply there: every tool checks access itself through the same service the controllers use.
- The SignalR hub skips MVC filters too: joining a mailbox's live feed checks access through that same service, and a client only ever receives events for mailboxes it may read.
- Never return a credential, password hash or token after the response that created it. No endpoint returns another user's data.

### Mail handling
- Never relay, deliver or make outbound mail connections. There is no code path that sends a caught message anywhere.
- Refuse unauthenticated submission at `MAIL FROM`. Offer `PLAIN` and `LOGIN` only, and only over TLS outside Development (Myotis's own TLS, or a listener configured as behind a TLS-terminating proxy). Never assume a proxy is present. Store credential passwords hashed.
- Message HTML is untrusted. Serve it only from the body endpoint with the strict CSP in the brief, shown in a sandboxed iframe with no `allow-scripts` or `allow-same-origin`. Never render message content with `v-html` in the app itself.
- Remote images load only when the reader asks, per message.
- Attachments download with `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff`.
- Enforce the message size cap during the `DATA` phase, not after the message is buffered.

### API
- The UI's routes live under `/api/v0` and can change with the UI. `/api/v1` is for tests and scripts: stable, token-only, and fields are only added.
- REST: `GET /` list, `GET /{id}` single (no side effects), `POST /` create, `PUT /{id}` replace, `PATCH /{id}` partial, `DELETE /{id}` (404 if missing). Dependents route as `/{parent}/{child}`.
- Business logic in services, not controllers. Bind models directly instead of passing IDs. Always put the route in the method attribute.

### C#
- Always `async`/`await`; never block async calls.
- Nullable reference types always on. JSON models use `required`; EF models initialize with `null!` if needed.
- File-scoped namespaces; `using` directives inside the namespace (enforced by `.editorconfig`).
- Always use GUIDs for primary keys.
- Store times as UTC `DateTime` from the injected `TimeProvider`.
- Fix warnings; never suppress them.

### Vue and TypeScript
- TypeScript only, no `any`, avoid `!` null assertions. `<script setup lang="ts">` with type-based `defineProps` and `defineEmits`; never the options API.
- Axios only inside `api/`. Server state through Pinia Colada queries in `queries/`, one module per domain with its key factory.
- SCSS only, never `scoped` styles.
- Views are thin; components hold the UI.

### Settings files
- `appsettings.json`: production settings with `#{token}` replacements
- `appsettings.local.json`: local dev settings, never commit it
- `appsettings.example.json`: example values to copy

Any new setting goes in all three.

### Writing
- No em dashes or en dashes anywhere: code, comments, UI copy, docs, commit messages. Use a comma, a colon, a full stop, or parentheses, or split the sentence.
- Default to no comment. A comment explains only a non-obvious why, in one line.
- UI copy is sentence case.

### Git
- Commit messages are one short sentence. No `Co-Authored-By` or other Claude attribution.
- Never commit `appsettings.local.json` or anything under `.dev/`.

## Proposing and Reviewing UI Changes

- Before implementing a UI change, produce an interactive HTML artifact mocking it: approximate the Vuetify styling, support light and dark themes, and show key states (default, empty, error) and before/after where relevant. No static mock-up images.
- When verifying an implemented UI change, prefer a screenshot of the real component: ask William to start the server, or render it headlessly with mocked data.

## Deployment

Polecat deploys Myotis from a `myotis/` folder in the deployments repo (`ssh://git@source.roushtech.net:22224/roushtech/deployments.git`), as it does Tamias: `stack.yml` maps Polecat variables to the app's settings, and the values live on the `myotis` app in Polecat. One replica with a pinned volume for the SQLite file. Traefik routes the web UI by host and terminates implicit TLS on 465 with a TCP router, forwarding plaintext SMTP to the app.

**Always:** when a change adds or renames a setting or secret, add it to `myotis/stack.yml` and set its variables on the `myotis` app in Polecat for every environment (`CHANGEME` when the value is unknown, and say so), in the same piece of work.

## Mandatory: After Every Task

```bash
# From root
dotnet format
dotnet test

# From RoushTech.Myotis.Vue/
npm run lint
```

If `npm run lint` reports formatting issues, run `npx prettier --write .` and lint again.

## Related systems

| System | Where | Relationship |
|---|---|---|
| Polecat | `../roushtech.polecat` | Deploys Myotis; also the source of the settings registry, SignalR and MCP patterns to lift |
| Tamias | `../RoushTech.Tamias` | Source of the entity-binding access filter and PAT/MCP patterns; possible future identity provider |
| Procyon | `../roushtech.procyon` | Same stack and conventions |
