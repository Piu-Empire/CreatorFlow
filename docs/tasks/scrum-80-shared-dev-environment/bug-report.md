# SCRUM-80 — Điều tra cảnh báo Linux runtime sau đợt 2

Trạng thái: ĐÃ ĐIỀU TRA, CHƯA SỬA, CHỜ REVIEW nếu muốn thay cấu hình. Các kiểm tra chức năng
đợt2 pass; đây là runtime warnings, không failure kết nối hoặc migration.

## 1. Thiếu GSS/Kerberos library

Hiện tượng khi API Linux mở PostgreSQL connection: `Cannot load library libgssapi_krb5.so.2`
và thông báo library không tìm thấy. Readiness sau migrated schema vẫn200, DB tests23/23 pass.

Nguyên nhân: source pin Npgsql10.0.3; connection test không đặt GSS Encryption Mode. Theo
[Npgsql security docs](https://www.npgsql.org/doc/security), mặc định Prefer trong10 thử GSSAPI;
Linux thiếu Kerberos phát cảnh báo, Npgsql xử lý và fallback kết nối không GSSAPI. Log và kết quả
runtime khớp hành vi được tài liệu mô tả; không phải thiếu SkiaSharp/native avatar hoặc credential sai.

Đề xuất optional để log gọn: thêm `GSS Encryption Mode=Disable` vào Npgsql configuration **của
môi trường DEV dùng password/TLS**, ghi environment reference/runbook tương ứng; không thêm
package Kerberos/Docker image hoặc đổi Auth. Giữ `SSL Mode=VerifyFull` cho Neon. Disable GSS
không phải disable TLS; trước khi áp dụng phải review nếu có môi trường thực dùng Kerberos.

Phạm vi nếu duyệt: `deploy/render/environment.example`, `docs/shared-dev-test-runbook.md`;
configuration server mới khi được phép, không toàn bộ client/local legacy. Chưa áp dụng thay đổi
trong test hoặc sửa connection để che log. Retest connection/readiness + check stderr sau config
được duyệt, không dùng cloud/SMTP nếu chưa có permission.

## 2. Data Protection key warning

Log ASP.NET FileSystemXmlRepository[60]: keys tại `/home/app/.aspnet/DataProtection-Keys`
không persisted bên ngoài container; XmlKeyManager[35]: không XML encryptor, key có thể lưu
không mã hóa. Không xuất XML/key contents trong điều tra.

Root cause ở mức đã chứng minh: runtime dùng default filesystem key repository không mount
volume/không cấu hình XML encryption. Dockerfile non-root không persistent disk; code API không
có AddDataProtection/custom repository. [Microsoft default settings](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings)
giải thích key management khi host container.

Source `JwtTokenIssuer` dùng SigningCredentials/HmacSha256 với JwtConfiguration signing key;
`EmailOtpCodeProtector` dùng HMACSHA256 riêng. Không thấy explicit IDataProtectionProvider,
cookie/session consumer trong source API. **Suy luận giới hạn:** chưa có evidence JWT/OTP hiện
tại phụ thuộc Data Protection key ring; restart health/DB pass không chứng minh mọi framework
feature không dùng nó. Không gọi warning harmless cho module tương lai dùng cookies/antiforgery.

Đề xuất hiện tại: ghi technical limitation, không mua persistent disk/Key Vault/storage để dọn
warning. Không tự disable toàn bộ logging hoặc thay đổi key management. Khi cần Data Protection
consumer thật, review riêng giải pháp miễn phí/quản lý keys và tác động redeploy; không mở rộng
SCRUM-80 sang hệ thống lưu key mới khi chưa có yêu cầu.

## Phạm vi/rủi ro và kết luận

- Không leak JWT/HMAC/DB passwords được quan sát trong warning trích; GUID key ID không là key material.
- Chỉ xác nhận functional local tests pass; secret/log audit cloud và TLS thật vẫn chưa kiểm chứng.
- GSS cleanup là optional config follow-up, không lý do mua dịch vụ/package mới. Data Protection
  cần ghi rõ giới hạn nếu sau này bảo vệ cookies/data qua redeploy.
- Không sửa code/config trong lúc test. Muốn áp dụng đề xuất thì duyệt hướng sửa trước theo
  bug-investigation-workflow/AGENTS.md; ngân sách và các gate cloud/DB/test vẫn giữ nguyên.


## Render first deploy: invalid JWT Base64 (2026-10-08)

- Evidence: user screenshot shows deployment fa78cdb failing at startup with Backend JWT signing key must be Base64; JwtConfiguration.Load line 21, Program.cs line 15. Build cache export completed before deployment.
- Root cause: configured Jwt:SigningKey cannot be decoded by Convert.FromBase64String. Actual secret was not inspected or recorded; exact input mistake unknown.
- Proposed fix: replace only Render Jwt__SigningKey with Base64 of 32 cryptographically random bytes, generated locally and copied via clipboard. No source or database changes.
- Risk: changing signing key invalidates existing JWTs; first deployment has no confirmed running sessions. Keep key server-only and out of chat/repository.
- Verification after approval: check free quota, one redeploy, observe startup logs and health/readiness under approved test scope. No success claimed yet.
- Status: proposed configuration correction awaiting review; no config mutation or redeploy performed by agent.


## Render registration unavailable: missing verifier key (2026-10-08)

- Symptom: WinForms registration reports email/schema verification not ready after six SMTP variables were configured.
- Evidence: user explicitly confirmed PasswordReset__VerifierKey absent. PasswordResetConfiguration.Load reads PasswordReset:VerifierKey and returns null when missing; Program selects UnavailableEmailSender when configuration is null. Screenshot shows service Live but no MailConfigured value; mail delivery not verified.
- Root cause: operator instructions omitted required seventh variable PasswordReset__VerifierKey.
- Proposed correction: generate independent cryptographically random 32-byte Base64 verifier key locally; add only this Render variable; preserve JWT key and SMTP configuration. No code or database edits.
- Risk: rotation would invalidate pending verification/reset verifiers; no successful mail verification established here. Keep secret server-only.
- Verification proposal: one quota-checked configuration deploy; MailConfigured=True, readiness ready; retry approved real registration/email verification/login once. Failure requires further investigation, no fake verified users.
- Status: correction proposed, awaiting approval under bug-investigation-workflow. No provider mutation performed.

## Windows cloud backup TLS failure (2026-10-08)
- Evidence: operator pg_dump18.1 screenshot: SSL error certificate verify failed, exit1, using verify-full/system CA. No successful backup claimed; failure occurs before password authentication. Exact certificate-chain failure not yet established.
- Secondary error: Set-Clipboard empty string rejected, cleanup did not clear clipboard; PGPASSWORD removal preceded error.
- Proposed bounded fix: inspect public Neon TLS chain and obtain appropriate trusted CA bundle from authoritative source; use explicit per-process PGSSLROOTCERT file while retaining verify-full. No system-wide trust changes, no TLS bypass, no cloud writes. Prepare saved local helper with masked password prompt to avoid clipboard being overwritten when copying command text.
- Risk: bundle integrity and hostname must be verified; no secrets in script/logs. Existing invalid/partial dump is not usable evidence. Retry backup once after correction approval, then restore only to isolated local DB.
- Status: investigate chain and propose local tooling correction; awaiting approval before applying correction.

## LOCAL API startup missing connection string (2026-10-09)
Evidence: Visual Studio InvalidOperationException: Database connection string CreatorFlow is missing; localhost5080 connection refused. WinForms appsettings.Local.json is separate from backend configuration. Root cause: backend effective configuration lacks ConnectionStrings:CreatorFlow. No database/network failure demonstrated.
Proposed correction: configure CreatorFlow.Api Development User Secrets with local-only PostgreSQL connection, independent local JWT configuration if absent; preserve Render/Neon config and client secrets boundary. Never copy Neon credentials into WinForms. Confirm local DB target before runtime. No migrations or writes implied.
Status: report prepared; awaiting bounded local backend configuration approval. Runtime startup/readiness and LOCAL/DEV switching to resume under previously requested acceptance scope after correction.

LOCAL configuration follow-up: operator confirmed Development; secrets provider index8 Root is API source directory and Path is secrets.json, so standard AppData secrets were not read by that provider. Why default builder resolved this provider there remains unproven; no source fix claimed. Approved local configuration scope now uses temporary process env overrides loaded from private AppData file by ignored helper; same supported ConnectionStrings/Jwt mechanism, no secrets in repository or cloud config edits. Startup verification pending.

## LOCAL readiness missing token_version (2026-10-09)
Evidence: startup local5080 succeeds, liveness ok, readiness not_ready; sanitized log PostgresException. Read-only catalog on localhost5432 / creatorflow / postgres confirms users lacks token_version; password_reset_requests, email_verification_requests and user_avatars columns present. DatabaseReadinessRepository selects users.token_version LIMIT0, so absent column prevents readiness. No password/authentication failure in catalog check.
Proposed bounded fix: first backup existing local creatorflow outside repository, then inventory migration06 preconditions and apply only database/06_auth_token_version.sql if absent; verify_auth_schema.sql and readiness. Do not rerun01/04/05 or seed02; no Neon/Render changes. Migration06 adds token_version and security trigger; existing local data preserved, later credential/security changes invalidate tokens by design. Requires explicit approval for localhost5432/creatorflow backup and migration write. No migration applied during investigation.
MailConfigured=True observed on LOCAL despite no SMTP intentionally set by helper; inherited or existing mail configuration source remains uninvestigated. Do not send local mail during switch tests or claim local SMTP configuration isolated until audited.

## Intermittent DEV login timeout during switching
Operator observed repeated30s client timeout while DEV readiness responded ready. Source/build/runtime client URL confirmed correct. Subsequent health-then-login retry succeeded with authenticated profile screenshot2026-10-09 ~12:26. Root cause unconfirmed; no source fix, timeout increase, TLS bypass or redeploy performed for this symptom. Local/DEV switch functionality verified after retry; timeout observation remains recorded for follow-up if recurrent.
