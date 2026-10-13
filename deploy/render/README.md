# Render Free + Neon Free — CreatorFlow DEV

This is a deployment recipe, not evidence of a running environment. Provision/deploy,
database operations, mail, tests and Git actions require their separate approvals.

Follow [ZERO-COST GUARDRAIL and runbook](../../docs/shared-dev-test-runbook.md).
No payment method on any provider. If a free tier requires a card, may auto-charge,
or cannot meet a prerequisite at zero cost, stop for review and record BLOCKED.

## Service settings after approval

| Setting | Value |
|---|---|
| Workspace/service plan | Free; no paid additions, no payment method |
| Source | Approved Git repository and release commit containing SCRUM-80 |
| Language/runtime | Docker |
| Root/build context | Repository root (`.`); leave Root Directory blank |
| Dockerfile | `src/CreatorFlow.Api/Dockerfile` |
| Final stage | `runtime` (last Dockerfile stage); never the `tests` target |
| Start command | Leave blank, use Docker ENTRYPOINT |
| Port | `PORT=8080`, `ASPNETCORE_HTTP_PORTS=8080`, listen on all interfaces |
| AllowedHosts | Exact assigned `service-name.onrender.com` hostname |
| Provider health path | `/api/health` (liveness); operator checks readiness separately |
| Auto-deploy | Off; manual deploy of reviewed commit only |
| Storage/DB | No Render persistent disk; no Render Postgres; Neon Free only |
| URL | Assigned HTTPS `onrender.com` URL; no purchased domain/DNS changes |

Use [environment.example](environment.example) as a list of keys. Enter secrets only
in the provider's server configuration, never in Git/client/chat. On Render, environment
values are exposed as build arguments: the Dockerfile declares only public revision
arguments, never credentials. Avoid secret files in the build context.

Render creation may trigger an initial build/deploy: obtaining permission to view an
account does not authorize clicking Create/Deploy. Before that action, release source,
free quota, secrets, target DB and deployment approval must already be confirmed.

## Commit provenance

Render provides `RENDER_GIT_COMMIT`; Docker uses it as the default `SOURCE_REVISION`.
The image's OCI revision label and API assembly informational version carry that SHA.
Startup logs show Version, Environment and MailConfigured without config values.
Do not override SOURCE_REVISION or RENDER_GIT_COMMIT on Render. For an approved local
image build, pass `--build-arg SOURCE_REVISION=<full-clean-commit-sha>` instead.
Local builds without either value are labeled `local` and cannot serve as release evidence.

Record dashboard deployment commit, startup version and image digest if exposed.
They must agree with the approved release SHA. An unavailable image-label/digest
inspection is recorded as a provider limitation; never invent its value. If Render
cannot supply revision at build time, stop for review. A separate registry is only
a last-resort fallback needing its own review, even when free.

## Neon and SMTP

Create only the minimum DEV resource needed; no spare projects/branches/databases for
experiments. Use server-side direct Npgsql configuration with TLS VerifyFull and a
small pool; the client gets only the API URL/timeout. Schema/migrations are manual,
approved operations, never a Docker build/startup step. Backup outside Git using
existing resources; prefer local disposable PostgreSQL for restore/upgrade drills.

Prefer Brevo Free port 2525 with StartTls and a free approved sender. Verify account
eligibility, outbound connectivity, TLS and actual delivery only after permission.
If signup requires a card or sender verification needs paid services/domain, stop and
record BLOCKED. Do not route Gmail SMTP through 2525, disable TLS or mark emails verified
in SQL. Missing mail configuration permits process startup but does not pass Auth flows.

## Quota and acceptance

Before any large deploy/test batch, record Render build/bandwidth and Neon compute/storage
usage, available free allowance, reset time (if any), and expected workload. Near the
limit, pause unnecessary tests and wait; do not upgrade to attain PASS. Free cold start
is expected: wait for health once, retry user actions explicitly; no keep-awake pinger.

Two actual Windows machines must exercise Auth/User via the same HTTPS API and switch
LOCAL → DEV → LOCAL using configuration. Project/Board/workflow integration remains
BLOCKED until API-ready; legacy WinForms must never connect to the Neon DEV database.

Primary references (reviewed 07/10/2026): [Render Docker](https://render.com/docs/docker),
[default environment variables](https://render.com/docs/environment-variables),
[Free limits](https://render.com/docs/free). Recheck account-specific free limits before use.
