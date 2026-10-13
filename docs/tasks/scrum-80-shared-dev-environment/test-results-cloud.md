# SCRUM-80 — DEV cloud acceptance record

Date: 2026-10-08 (Asia/Saigon). Budget: zero; no payment methods or paid resources authorized.

## Scope and user decision
User requested execution of remaining acceptance checks and explicitly waived the second-machine criterion because no second machine is available. Record as WAIVED BY USER, never PASS. LOCAL/DEV switching, restart, backup/restore remain in scope. No merge/PR/Jira action authorized.

## Evidence already received
- Render screenshot: deployed Live from fa78cdb; public hostname creatorflow-dev-api.onrender.com.
- Neon screenshots: creatorflow-dev / production / neondb, role neondb_owner; initial public tables 0; 04/05/06 COMMIT successful; verification returned auth_schema_verified after 01 baseline.
- User reported health ok and readiness ready over HTTPS.
- WinForms invalid-login screenshot and matching Render 21:45:28 status401 establish DEV request path.
- Brevo Free selected without card; SMTP key Active; sender verified. Gmail DKIM/DMARC warning remains; actual successful delivery reported by user.
- User reported registration, real email verification, login and profile persistence across application restart successful. These are operator-reported, not independent agent replay.

## Remaining checks
- Two machines: WAIVED BY USER.
- LOCAL to DEV to LOCAL: NOT RUN.
- Render API restart and subsequent version/health/readiness/profile persistence: NOT RUN.
- Neon idle/wake data persistence: NOT RUN as a recorded drill.
- Cloud pg_dump checksum and local restore/verify/data comparison: NOT RUN.
- Configured SMTP password reset and old-token rejection: NOT RUN.
- Quota readings before next substantial drill: required; current usage not supplied.

## Access observations
PostgreSQL18 tools exist on operator machine. Docker daemon initially unavailable; starting existing Docker Desktop for local drill. Browser control runtime fails initialization twice, so provider dashboard actions cannot currently be automated. No cloud restore, data deletion or additional Neon resources performed.

## Operator restart follow-up
User reported checks still OK after instructed Render restart; supplied WinForms authenticated profile screenshot with previously saved display name preserved. Record operator-reported health/readiness/login/profile persistence PASS after restart. Independent provider restart event and startup version log still not supplied. No secrets retained in this record.

## Operator password-reset follow-up
User reported all instructed reset checks successful: real reset mail received, old password rejected, new password login successful, profile persisted. PASS (operator-reported). Existing bearer-token rejection was not separately exercised and is not claimed.

## Neon quota check before backup
2026-10-08 ~23:07 Asia/Saigon, screenshots supplied by operator: current Free plan USD0/month, 1GB storage, 100 compute hours. Usage: compute0.15 CU-hours, storage33.19MB, history8.1MB, transfer19.74kB. Compute/storage well below80% guardrail. Transfer allowance not shown; backup expected small for current database, stop on quota warnings. No plan upgrade or new cloud resource authorized.

## Cloud backup and isolated local restore drill
Operator helper returned BACKUP_OK, target neondb/neondb_owner, PostgreSQL18.6. Backup outside repository: C:\Users\Lenovo\AppData\Local\CreatorFlowBackups\creatorflow-dev-20261008-235655.dump. SHA256 independently verified: 6C1C38EF706207995BD20CC941A97A8AA1F8ED623189DE696A8BB7C084A30EC1. TLS verify-full with explicit Windows trusted-root PEM; no TLS bypass.
Agent restored archive with pg_restore --exit-on-error --no-owner --no-privileges, exit0, into newly created LOCAL-only database creatorflow_scrum80_cloud_restore_20261009, existing SCRUM80 PostgreSQL container on127.0.0.1:55480. No cloud database writes, no overwrite, no additional cloud resources.
verify_auth_schema.sql returned auth_schema_verified and COMMIT. Restored aggregate count:2 users,1 verified. No identities/hashes/OTP exposed. PASS archive restore and Auth schema/data presence; restored-account login and full source/restored data fingerprint comparison NOT RUN, not claimed.
LOCAL/DEV switch and restart version log remain outstanding; second machine waived by user.

## Approved LOCAL schema correction (2026-10-09)
Target localhost5432/creatorflow/postgres. Inventory confirmed migration06 column/function/trigger all absent. Pre06 custom backup successful, archive readable; file outside repository creatorflow-local-before06-20261009-120143.dump, SHA256 E73665F645677FAB1D7F68CC17EAFB68D5ACDCC2742B9B67484DFCE2567A038A. Applied ONLY06_auth_token_version.sql: ALTER TABLE, CREATE FUNCTION, CREATE TRIGGER, COMMIT. Verification returned auth_schema_verified and COMMIT. No baseline/seed rerun or cloud changes.

## LOCAL / DEV switching acceptance (2026-10-09)
- LOCAL API localhost5080: after approved local-only06 migration, agent observed health200/ok and readiness200/ready. Operator supplied WinForms invalid-login screenshot with matching local API401 log (TraceId suffix JRE:00000002).
- Client source/build and running configuration confirmed DEV base URL after switch. Operator instructed to stop local API, then supplied successful DEV authenticated-profile screenshot ~12:26 Asia/Saigon, prior display name preserved. PASS LOCAL/DEV config switching, based on combined agent checks and operator evidence.
- Intermediate DEV login timeout occurred twice; later explicit retry after health succeeded without source/config timeout change. Cause not established; retain as intermittent observation, not a diagnosed fix.
- Render startup screenshot dated2026-10-09 12:11 shows fa78cdbb8daea07591645ee3f4389b0c28052103, Production, MailConfigured=True. Restart/idle-wake startup version now supported by screenshot; timing cause of wake not independently established.
- Two-machine criterion remains WAIVED BY USER (not PASS). Extended module integration remains BLOCKED until corresponding APIs ready. No PR/merge/Jira Done performed. Restored account login/full data fingerprint and token-revocation drill not exercised; no claim for those additional checks.

## Final acceptance status — 2026-10-09
This final status supersedes earlier chronological NOT RUN entries where follow-up evidence is recorded below.

- PASS: Render HTTPS deployment and version fa78cdb; Neon migration/verification; health/readiness; real registration/email verification/login/password reset; User profile persistence; operator-reported restart smoke supported by later startup SHA log; cloud backup archive/checksum and isolated local restore with schema/data-presence checks; LOCAL/DEV switching.
- WAIVED BY USER: second-machine acceptance, explicitly removed because unavailable; never recorded as PASS.
- Extended integration: BLOCKED pending corresponding module APIs; legacy Board/Workflow direct DB dependencies remain technical debt and must not target shared Neon.
- Evidence limits: several UI/mail/restart outcomes are operator-reported. Full restored-data fingerprint, restored-account login and separate bearer-token revocation drill were not exercised. Intermittent DEV client timeout later cleared without a code fix; cause unconfirmed.
- Budget: retain Free services, no payment methods, no paid workaround. Quota check required before substantial activity. Backup dumps and secret values excluded from Git.
- Git publication of this acceptance record does not imply PR merge or Jira Done. Those actions remain separately authorized.
