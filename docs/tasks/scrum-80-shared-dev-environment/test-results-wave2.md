# SCRUM-80 — Kết quả đợt 2 Docker/PostgreSQL local

Ngày 07/10/2026, kết thúc khoảng 21:22 Asia/Saigon (UTC+07). Người dùng đã duyệt đợt 2 qua chat.
Không sửa source trong lúc test; không cloud, SMTP thật, payment method, registry push hoặc Git commit/push.

## Source và tài nguyên

Branch `feature/SCRUM-80-shared-dev-environment`; HEAD baseline
`fdb184136e6e94f52e03e17d243c72202cff4b09` + working-tree SCRUM-80. Chưa có clean release commit.
Image test mang revision **local**, không gán HEAD cũ cho source mới rồi coi là release provenance.

Docker Desktop 4.73.0, engine 29.4.3, linux/amd64. Dung lượng trước test: C ~95.95GB, D ~440.35GB.
PostgreSQL/pg_dump/pg_restore trong container: 18.6. Có PostgreSQL18 tools trên host nhưng đợt này
dùng tools container cùng server version. Sandbox không truy cập Docker pipe; quyền thực thi ngoài
sandbox được dùng cho đúng Docker/local test scope. Không thay đổi container/volume hiện có.

Tài nguyên disposable riêng:

- Network: `creatorflow-scrum80-b66fc1a7a234-net`.
- Volume: `creatorflow-scrum80-b66fc1a7a234-pgdata`, mount `/var/lib/postgresql` đúng PostgreSQL18.
- DB container: `creatorflow-scrum80-b66fc1a7a234-db`, chỉ loopback `127.0.0.1:55480`.
- DB names: `creatorflow_scrum80_fresh`, `creatorflow_scrum80_upgrade`, `creatorflow_scrum80_restore`.
- API container: `creatorflow-scrum80-b66fc1a7a234-api`, loopback5088, DB fresh.
- Upgrade API: `creatorflow-scrum80-b66fc1a7a234-upgrade-api`, loopback5089, DB upgrade.
- Credential/key ngẫu nhiên chỉ private local state ngoài repo; không ghi giá trị vào report/command/output.

## Kết quả

| Kiểm tra | Kết quả và evidence |
|---|---|
| Linux runtime image | PASS; build runtime, linux/amd64, User1654 non-root, port8080 |
| Local version wiring | PASS OCI revision=local và startup Version=local; clean deploy SHA vẫn chưa kiểm chứng |
| Linux/native tests | PASS 51/51, 0 failed/skipped; target tests filter TestCategory!=AuthApiDatabase |
| DB target | PASS xác nhận current_database/user/version trước thao tác; không fallback DB khác |
| Fresh schema/migrations | PASS 01 → 04 → 05 → 06; mỗi script ON_ERROR_STOP, exit0 |
| Fresh verification | PASS verify_auth_schema.sql → auth_schema_verified |
| Empty schema health | Liveness200/ok; readiness503/not_ready, ~192ms; không raw SQL/connection trong response |
| Migrated readiness | PASS 200/ready |
| Upgrade missing06 | PASS negative case: ready503, verification reject users.token_version thiếu |
| Upgrade06 | PASS backup trước06, apply06/verify exit0, fixture platform vẫn đúng |
| Backup/restore | PASS pg_dump custom trước/sau06 ngoài repo; pg_restore vào DB restore rỗng riêng, verify pass |
| DB/Auth integration tests | PASS 23/23 AuthApiDatabase, 0 failed/skipped/inconclusive |
| DB down | Liveness200/ok ~65ms; readiness503/not_ready ~9ms |
| DB restart persistence | PASS data upgrade/restore còn và fingerprint khớp; schema fresh verify lại pass |
| API restart | PASS ready200; Version local và mail unavailable giữ nguyên |
| Logging | Probe failure chỉ type/trace/status; startup có Version/Environment/MailConfigured; có runtime warnings cần review riêng |
| Cloud/two-machine/email/HTTPS | NOT RUN, không được thay bằng loopback/stub pass |

DB upgrade fixture là một row platform `SCRUM80_UPGRADE_TEST`, không fake identity/Project để bypass Auth.
Restore/upgrade data fingerprint sau restart: `d7c53f49fbe8ae2d3356ab00a1eb9e4f` (platform JSON MD5,
chỉ so sánh dữ liệu test, không security hash). Các Auth DB tests hiện có dùng fixture riêng, có direct
SQL trong test để setup verified/security states; đây là DB integration tests local, **không evidence
email verification/Login thật trên DEV**. Không SMTP hoặc fake PASS real-email acceptance.

## Commands/evidence chính

```powershell
docker build --platform linux/amd64 --target runtime --build-arg SOURCE_REVISION=local -f src/CreatorFlow.Api/Dockerfile -t creatorflow-api:scrum80-wave2 .
docker build --platform linux/amd64 --target tests --build-arg SOURCE_REVISION=local -f src/CreatorFlow.Api/Dockerfile -t creatorflow-api-tests:scrum80-wave2 .
dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --no-build --no-restore --filter 'TestCategory=AuthApiDatabase' --logger 'trx;LogFileName=scrum80-auth-db.trx' --results-directory TestResults/scrum80-wave2/database
```

DB tests process nhận explicit connection tới fresh database mới trên55480 và writes=true;
giá trị secret không in ra/đưa vào command literal. TRX:
`TestResults/scrum80-wave2/database/scrum80-auth-db.trx`, ignored; đọc lại counters23/23.
Linux TRX `/test-results/auth-linux.trx` trong image test; build output51/51.

Image runtime ID/manifest list:
`sha256:eba793da7d590bdf9cc459a14b1659562cff82d8d1f09288966162eade9ee123`.
Test image manifest list:
`sha256:759e6519728027a90df4427b94ccb5ed7253f6f12c4817173a4080d90dc7f3b8`.
Không push registry; đây là Docker local artifacts, không Render deploy evidence.

Backup folder ngoài repo:
`C:\Users\Lenovo\AppData\Local\Temp\creatorflow-scrum80-b66fc1a7a234`.

| Dump | Bytes | SHA256 nguyên bản |
|---|---|---|
| upgrade-before06.dump | 89005 | 40f3a759e3c06b88942b1e95f1d67e79a27defade3fed1e3dc899c167b6f333b |
| upgrade-after06.dump | 90361 | 30cdb2c0fe08f612c26599765c4b7f73726123a0154ce5021657ca023608ff67 |

pg_restore --list xác nhận custom format/server/tool18.6 và metadata; không xuất dump data/hashes.
Restored DB verify và platform fingerprint khớp sau DB restart. Dump trong Temp là evidence test,
không thay quy trình retention backup DEV dài hạn/copy trước demo.

## Warnings và giới hạn

[Báo cáo điều tra](bug-report.md) ghi GSS/Kerberos missing library fallback và Data Protection
filesystem/unencrypted-key warnings. Không có assertion failure; chưa sửa source/config để dọn warnings.
Không kết luận mọi log-path đều đã audit hoặc keys đều production-ready từ test này.

Release version/commit còn BLOCKED do chưa clean release commit/Git approval; Render/Neon/Brevo
account quota/card-free eligibility chưa xác nhận. Chưa public HTTPS, DB TLS VerifyFull với Neon,
cloud restart/health/readiness/log audit, SMTP delivery, WinForms switching/two-machine Auth.
SCRUM-80 chưa Done; module integration mở rộng vẫn BLOCKED khi chưa API-ready.

## Trạng thái tài nguyên sau test

Đã dừng **chỉ 3 container disposable** sau khi xác nhận label creatorflow.scope=disposable-wave2;
exit0, không giữ DB/API mới chạy nền. Giữ network/volume/images và dumps cho review; không xóa
volume/data hoặc container hiện có. Cleanup destructive cần quyền riêng và xác nhận đúng resources.
Các container trước test vẫn running; không thay source trong lúc test.
