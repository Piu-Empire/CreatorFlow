# CreatorFlow — Shared DEV/TEST runbook

Trạng thái: [Đợt 1](tasks/scrum-80-shared-dev-environment/test-results.md) build/stub tests pass;
[đợt 2 local](tasks/scrum-80-shared-dev-environment/test-results-wave2.md) Linux51/51, DB23/23,
schema/upgrade/verify/dump/restore/restart/health pass. Có runtime warnings được ghi riêng,
chưa sửa. Chưa release commit/provision/deploy/cloud migrations/mail/two-machine; chưa có URL
hay evidence môi trường chung hoạt động. Các pass local không thay acceptance DEV cloud.

## ZERO-COST GUARDRAIL

Ngân sách tuyệt đối **0 đồng**. Stack đã chốt: **Render Free Web Service + Neon Free PostgreSQL**, URL HTTPS **onrender.com**. Không mua domain. Không dùng Render Free Postgres; không tạo Koyeb/Oracle mới làm default.

1. **Dừng và xin review trước mọi thao tác có khả năng tạo billing.** Ghi thao tác dự kiến, tài nguyên/gói/quota liên quan và điều kiện có thể phát sinh phí, không đính kèm secrets. Chưa có kết quả review thì không thực hiện. Quyền deploy hoặc duyệt plan không đồng nghĩa cho phép chi tiền; ngân sách vẫn 0 đồng.
2. **Không thêm payment method vào bất kỳ provider nào: Render, Neon, Brevo, registry hoặc dịch vụ phụ trợ. Free tier bắt buộc thẻ hoặc có khả năng auto-charge thì dừng xin review.** Dùng Free service; khi vượt free bandwidth/build quota thì chấp nhận suspend/ngừng build. Không thêm thẻ, nâng cấp hoặc bật paid add-on để khôi phục.
3. **Không dùng trial có nguy cơ auto-charge hoặc paid conversion.** Không chọn paid registry, persistent disk, object storage hay backup service; không bật auto-upgrade. Kiểm tra Free allowance/điều kiện billing trên tài khoản trước tạo hoặc đổi tài nguyên. Không xác minh được miễn phí thì dừng review.
4. **SMTP ưu tiên Brevo Free qua port 2525**, chỉ khi account/sender hoạt động hoàn toàn miễn phí và TLS hợp lệ. Nếu sender/domain verification bị blocked, ghi BLOCKED/follow-up; không mua domain/email service, không đổi sang plaintext, không bypass verification hoặc fake PASS. SMTP chưa hoạt động không thay đổi liveness API; flow Auth phụ thuộc ghi kết quả riêng.
5. **Backup bằng pg_dump ngoài repository trên tài nguyên sẵn có.** Dùng máy/dung lượng đang có, bản copy trên tài nguyên sẵn có với quyền hạn chế; không thuê storage/backup service. Dump có dữ liệu nhạy cảm, không commit hoặc gửi vào chat. Nếu thiếu dung lượng thì dừng review.
6. **Quota hết là giới hạn được chấp nhận.** Ghi tình trạng và chờ quota reset hoặc review phương án miễn phí; không tạo tài khoản nhằm né quota, mua thêm compute/storage/bandwidth hay tự chuyển provider.

## Trạng thái và quyền thực hiện

- Chưa có URL deploy, evidence health/readiness/Auth, migration record hoặc backup/restore drill. Không coi bản khung này là bằng chứng hệ thống đã hoạt động.
- Provision/deploy, DB migration/writes, gửi mail test, build/test/runtime và Git/external actions cần quyền tương ứng theo AGENTS.md và plan. Chỉ tạo branch SCRUM-80 đã được phép; chưa commit/push/PR/merge.
- Quy trình chi tiết và kiểm chứng theo [plan SCRUM-80](tasks/scrum-80-shared-dev-environment/plan.md); yêu cầu nghiệm thu hai tầng theo [requirements](tasks/scrum-80-shared-dev-environment/requirements.md).

## Guardrail cuối cùng — đã duyệt

- Render build trực tiếp từ Git commit là đường triển khai chính. Không dùng registry riêng nếu Render build trực tiếp được; registry chỉ là fallback cuối cùng, cần review riêng kể cả registry miễn phí.
- Trước mỗi đợt deploy/test lớn, operator kiểm tra Render bandwidth/build quota và Neon compute/storage trên dashboard; ghi thời điểm, mức dùng/còn lại và reset nếu có vào deployment/test record, không ghi secrets.
- Nếu gần giới hạn free hoặc không đủ cho đợt dự kiến: dừng test không cần thiết, chờ quota/reset; với storage không reset thì chỉ tiếp tục khi có dung lượng miễn phí đủ và phương án đã review. Không upgrade hoặc tự xóa dữ liệu để lấy quota.
- Neon chỉ tạo tài nguyên DEV tối thiểu cần thiết. Không tạo thêm project/branch/database để thử nếu không cần; restore/upgrade drill ưu tiên PostgreSQL disposable local trên tài nguyên sẵn có, không dùng thêm cloud resources khi chưa review nhu cầu/quota.
- Không thao tác nào được phép làm ngân sách lớn hơn 0 đồng để đạt PASS. Điều kiện miễn phí không đáp ứng thì ghi BLOCKED, không mua workaround.

## Quota check trước deploy/test lớn

Operator đọc dashboard, ghi thời điểm/timezone, plan Free và không payment method trên mọi provider vào [deployment record](tasks/scrum-80-shared-dev-environment/deployment-record.md).

| Provider | Ghi nhận trước đợt chạy |
|---|---|
| Render | Build minutes/bandwidth/instance-hours đã dùng/còn, reset time, số build và tải dự kiến |
| Neon | Compute/storage/transfer đã dùng/còn, reset nếu có, mức tăng dự kiến |
| Brevo | Mail quota còn lại, sender enabled miễn phí, số mail dự kiến nếu gửi mail |

Gần giới hạn khi dùng >=80% allowance hoặc còn không đủ cho đợt dự kiến cùng khoảng dự phòng. Không xác định được free quota thì dừng. Dừng test không cần thiết, chờ reset, không upgrade; chấp nhận suspend/ngừng build. Storage không tự reset: không tự xóa DEV data hoặc mua dung lượng, review phương án miễn phí. Không keep-awake pinger, tài khoản phụ để né quota hoặc auto-retry build/POST.

## Prerequisites và deployment config

Xác nhận operator/account/region, DB mới hay có dữ liệu, release SHA sạch chứa SCRUM-80 và quyền provision/deploy/migration/test/mail riêng. Tạo Render service có thể tự deploy nên chỉ click Create sau approval deploy và config/target đã chuẩn bị. Tools psql/pg_dump/pg_restore và PostgreSQL local disposable dùng tài nguyên sẵn có; chưa có thì BLOCKED/review, không thuê dịch vụ.

Theo [Render recipe](../deploy/render/README.md) và [environment keys](../deploy/render/environment.example): repo root Docker context, API Dockerfile runtime cuối, PORT và ASPNETCORE_HTTP_PORTS=8080, AllowedHosts đúng hostname onrender.com đã cấp. Auto-deploy off, không persistent disk/Render Postgres, không domain/DNS. AllowedHosts localhost mặc định phải được override đúng hostname DEV; không dùng `*` để che lỗi cấu hình.

ASPNETCORE_ENVIRONMENT=Production là chế độ xử lý lỗi ASP.NET trên server DEV, không biến môi trường thành nghiệp vụ Production. DB/JWT/HMAC/SMTP riêng DEV, ổn định qua restart. Giá trị thật chỉ server config, ngoài Git/chat/client/command literal/Docker ARG.

Neon direct Npgsql format: `Host=...;Database=...;Username=...;Password=...;SSL Mode=VerifyFull;Maximum Pool Size=10`. Không đưa postgres:// URL trực tiếp vào loader, không Trust Server Certificate/disable TLS. Không bật Include Error Detail/log parameters. Operator chỉ dùng direct endpoint để migration/dump/restore; client không biết DB credential.

Render supplies RENDER_GIT_COMMIT; Docker SOURCE_REVISION mặc định từ SHA đó, OCI revision label và assembly informational version cùng SHA. Không override hai biến trên Render. Dashboard deployment commit và startup Version phải khớp release. Record digest/label nếu exposed, limitation nếu không; không invent evidence. Local build không SHA mang `local`, không đủ release evidence. Build từ Git là chính, registry fallback cần review riêng.

## Health/readiness/logging

| Check | Kỳ vọng | Ý nghĩa |
|---|---|---|
| GET /api/health | 200 status ok | Process sống; không probe DB/SMTP/AI |
| GET /api/health/ready | 200 ready hoặc 503 not_ready | DB/cột/bảng Auth/User probe, budget 5s, không đọc user/OTP |
| verify_auth_schema.sql | auth_schema_verified hoặc lỗi | Catalog schema check sau migration; readiness không thay thế verify đầy đủ |

Provider dùng liveness. Operator readiness sau deploy/nghiệm thu, không poll liên tục làm tiêu compute Neon. SMTP/AI không quyết định process alive. Process down/suspend/provider error không fake healthy. Startup log version/environment/mail configured; request failure trace/status, unhandled/DB readiness loại lỗi, provider failure operation. Không raw exception/query/body/header/OTP/password/token/connection string. Audit cả framework/provider logs; không tự bật verbose/protocol logging.

## Schema/migration procedure

Theo [schema manifest](../deploy/database/schema-manifest.md): 01 là schema baseline; 04/05/06... là migration. 02 là optional DEV seed, không migration; có DEV_HASH_* nên không tự apply cho Auth nghiệm thu. 03 và verify_auth_schema.sql là verification, không migration. Negative write samples comment trong 03 không chạy ở đây.

DB mới/rỗng được duyệt: 01 → 04 → 05 → 06 → migration mới trong release theo dependency đã review. DB existing: inventory catalog/applied-script record, chỉ apply phần thiếu đúng precondition. Không chạy lại CREATE scripts, không IF NOT EXISTS che drift. 05 invalidates OTP v1; 06 có security/token-version trigger, phải review tác động trước release.

Release cần schema mới: quota/target/quyền → backup/checksum → inventory/scripts SHA256 → apply theo thứ tự → verify → deploy SHA tương ứng → smoke → record. Maintenance window nếu không backward-compatible. Script đã BEGIN/COMMIT, không bọc transaction chồng. Không migrate trong image build/startup/health/test.

Operator đặt PGHOST/PGPORT/PGDATABASE/PGUSER/PGSSLMODE=verify-full và PGPASSFILE ngoài repository có quyền hạn chế, không password literal. Direct TLS endpoint, không fallback client/local config. Chỉ sau approval đúng target, từ repo root:

```powershell
psql --no-password --set=ON_ERROR_STOP=1 --command="SELECT current_database(), current_user, current_setting('server_version');"
# Chỉ DB mới/rỗng đã duyệt. Chạy từng lệnh, dừng nếu LASTEXITCODE khác 0.
psql --no-password --set=ON_ERROR_STOP=1 --file=database/01_schema.sql
psql --no-password --set=ON_ERROR_STOP=1 --file=database/04_password_reset_requests.sql
psql --no-password --set=ON_ERROR_STOP=1 --file=database/05_auth_email_avatar.sql
psql --no-password --set=ON_ERROR_STOP=1 --file=database/06_auth_token_version.sql
psql --no-password --set=ON_ERROR_STOP=1 --file=database/verify_auth_schema.sql
```

Không paste block chạy qua lỗi; kiểm tra `$LASTEXITCODE` từng lệnh. Baseline sau 04/05 có 25 bảng nghiệp vụ, nhưng count/SELECT 1 không đủ chứng minh schema. Verify tên/cột/type/index/constraint/trigger; drift definitions phải so schema dump với scripts, không chỉ tên objects. Dừng nếu verify fail; điều tra/report trước sửa.

## Backup/restore trên tài nguyên sẵn có

Operator chọn absolute taskBackupPath ngoài repo, dung lượng đủ/quyền owner, direct TLS PG* như trên; pg_dump major >= server major. Chỉ sau backup/DB read approval:

```powershell
pg_dump --no-password --format=custom --file="$taskBackupPath"
# Dừng nếu exit code khác 0; chỉ list metadata, không xuất data/hashes.
pg_restore --list "$taskBackupPath"
```

Ghi SHA256, giờ/timezone, version/DB/release/migration set, location không credential, owner. Giữ bản trước migration rủi ro và trước demo; copy bằng tài nguyên sẵn có với quyền hạn chế. Không dump vào repo/chat hoặc thuê object storage/backup. Ignore dump chỉ là bảo vệ thêm.

Restore drill vào PostgreSQL local disposable rỗng được duyệt, không shared DEV. Operator đổi PG* sang host/port/DB/user local rõ ràng, credentials riêng; không để Neon PGHOST/PGDATABASE sót trong process. TLS local theo môi trường local được phép, không hạ TLS kết nối Neon. Không tạo thêm Neon resource để drill nếu không cần:

```powershell
psql --no-password --set=ON_ERROR_STOP=1 --command="SELECT current_database(), current_user, current_setting('server_version');"
pg_restore --no-password --exit-on-error --no-owner --no-privileges --dbname="$env:PGDATABASE" "$taskBackupPath"
psql --no-password --set=ON_ERROR_STOP=1 --file=database/verify_auth_schema.sql
```

Kiểm tra exit từng lệnh; verify data/Auth persistence thật, không publish password hashes hoặc sửa email_verified_at để bypass. Không `--clean`, DROP DB/xóa volume/restore đè DEV. Thiếu tools/DB/quyền ghi BLOCKED.

## Deploy/restart/rollback

Sau approval: quota → DEV target → backup → inventory/migration → verify → manual deploy Git commit → HTTPS liveness/readiness/Auth → record. Restart API bằng provider action được phép, SHA/keys/data giữ. Managed DB dùng suspend/resume/compute lifecycle/wake sau idle được provider hỗ trợ; không tự tạo endpoint để gọi là restart. Nếu không có lifecycle control thì ghi limitation/evidence thay thế và review, không fake PASS.

Rollback API về SHA/digest cũ chỉ nếu schema compatible. Không tự down-migrate 05/06 hoặc restore đè DEV; quyết định riêng và backup trước thao tác mất dữ liệu. Free service suspended thì chờ/reset hoặc BLOCKED; không mua phục hồi để PASS.

## WinForms LOCAL ↔ DEV và nghiệm thu

Precedence: appsettings.json → ignored appsettings.Local.json → environment. Restart app/Visual Studio/process sau đổi env:

```powershell
# LOCAL
$env:Api__BaseUrl = 'http://localhost:5080'
$env:Api__TimeoutSeconds = '30'
# DEV: thay bằng hostname thực được cấp sau deployment.
$env:Api__BaseUrl = 'https://<assigned-service>.onrender.com'
```

Không sửa source; DEV config chỉ URL/timeout. Account local không tự sync DEV; access token runtime và logout theo Auth. Cold start có thể vượt timeout 30s: chờ health rồi explicit retry, không auto-retry POST/disable cert validation.

Hai máy/tài khoản thật, cùng URL/SHA: login/profile/save/reopen, 401 no token, invalid login, logout/login lại, LOCAL → DEV → LOCAL. SMTP configured phải Register/Verify/reset mail thật. Không mail/verified account hợp lệ thì BLOCKED Auth phụ thuộc, không fake PASS. [Technical debt](tasks/scrum-80-shared-dev-environment/technical-debt.md): không client Npgsql tới Neon chung, không hardcode user/project; module chưa API-ready ghi BLOCKED mở rộng, không treo tầng hạ tầng/Auth đã pass.

Ghi [checklist](tasks/scrum-80-shared-dev-environment/test-checklist.md) theo scope approval, giờ/SHA/environment/evidence sạch: PASS/FAIL/BLOCKED/NOT RUN. Test chỉ ghi nhận, không sửa code. PR/merge/Jira Done còn quyền riêng.
