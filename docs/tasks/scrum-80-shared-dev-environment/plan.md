# SCRUM-80 — Implementation plan

Trạng thái: PLAN ĐÃ DUYỆT ngày 07/10/2026, gồm guardrail cuối cùng. Được chuyển sang triển khai source/docs; các gate Git/provision/deploy/migration/test/mail vẫn giữ nguyên.
Ngân sách: 0 đồng. Chỉ dùng gói miễn phí; không mua domain, nâng cấp trả phí hoặc dựa vào credit trial ngắn hạn.
Đã tạo branch SCRUM-80 theo duyệt riêng, triển khai source/docs và hoàn tất đợt1/2 được duyệt:
build pass (8 warnings), 42API + 31ApiFoundation stub; Linux51/51 và DB23/23 local,
schema/upgrade/verify/dump/restore/restart/health/readiness pass. Xem test-results.md và
test-results-wave2.md. Chưa release commit/commit-push-PR, provision/deploy/cloud migration/mail/two-machine;
warnings Linux runtime đã report riêng, chưa sửa.

## ZERO-COST GUARDRAIL

- Primary stack: Render Free Web Service + Neon Free PostgreSQL, HTTPS onrender.com. Không mua domain; không dùng Render Free Postgres; không chọn Koyeb/Oracle mới làm default.
- Không thêm payment method vào bất kỳ provider nào: Render, Neon, Brevo, registry hoặc dịch vụ phụ trợ. Free tier bắt buộc thẻ hoặc có khả năng auto-charge thì dừng xin review. Vượt free bandwidth/build quota thì chấp nhận suspend/ngừng build; không thêm thẻ hoặc upgrade để tiếp tục.
- Mọi thao tác có khả năng tạo billing phải dừng xin review. Không dùng trial có nguy cơ auto-charge/paid conversion, paid registry, persistent disk, object storage hoặc backup service trả phí. Không paid add-on, auto-upgrade hoặc tài nguyên vượt Free allowance.
- Ưu tiên thử Brevo Free SMTP 2525 chỉ nếu account/sender hoàn toàn miễn phí và TLS hợp lệ. Nếu verification blocked, ghi BLOCKED/follow-up; không mua domain/email service hoặc bypass Auth.
- Backup pg_dump ngoài repository bằng tài nguyên sẵn có; không thuê storage/backup. Registry nếu cần chỉ dùng gói miễn phí đã xác nhận, nếu không thì dừng review.
- Xác nhận Free plan/quota và điều kiện billing trước thao tác tài khoản; không xác minh được thì dừng. Duyệt plan không thay đổi ngân sách 0 đồng và không cấp quyền provision/deploy.

## 1. Kết quả và ranh giới

Tầng bắt buộc: API HTTPS chung, PostgreSQL persistent, commit/version xác định, schema/migration + verify, backup/restore, server secrets/config, LOCAL ↔ DEV, hai máy, Auth/User runtime thật, restart/liveness/readiness/logging.

Tầng mở rộng: Project/Invitation/Board/Assignment/Review/Workflow/Notification chỉ nghiệm thu khi API-ready ở release đã chọn. Chưa sẵn sàng ghi BLOCKED kèm module/ticket, không ngăn Done tầng bắt buộc. Không tạo identity hoặc Project context giả.

Không di chuyển legacy Board/Workflow trong task. Ghi technical debt riêng; không phân phối connection string DEV cho client, không cấu hình WinForms truy cập DB DEV chung. Auth/User và module đã migrate chỉ dùng API.

## 2. Baseline repository đã kiểm tra tĩnh

| Khu vực | Hiện trạng | Hệ quả cho plan |
|---|---|---|
| Git | `feature/SCRUM-39-auth-board-integration-fix`, HEAD `fdb184136e6e94f52e03e17d243c72202cff4b09`; `AGENTS.md`, `docs/` untracked | Đây là baseline đọc, chưa phải commit release; giữ nguyên dữ liệu đang có, không tự đổi branch |
| API | net10.0, Npgsql 10.0.3; startup validate DB/JWT config nhưng chưa mở DB | Readiness phải kiểm tra DB thật riêng |
| Docker | `src/CreatorFlow.Api/Dockerfile`, Linux x64/noble, non-root; SDK/runtime pin; target tests riêng | Tái sử dụng, không thêm package; chưa chứng minh image chạy |
| Server compose | `deploy/docker/compose.server.yml` chỉ có API, loopback 5080, image từ env, SMTP bắt buộc trong compose | Không tự coi có PostgreSQL persistent/HTTPS; điều chỉnh SMTP tùy chọn; PaaS không dùng compose trực tiếp |
| Local compose | `compose.dev.yml`, PostgreSQL 18 và volume `/var/lib/postgresql`, API 8080 | Giữ local độc lập, không biến volume local thành DB team |
| Health | `HealthEndpoints` + `HealthService`, `/api/health` trả `ok` | Giữ payload/contract; thêm readiness riêng |
| Auth mail | `PasswordResetConfiguration.Load()` trả null khi thiếu/sai cấu hình; `UnavailableEmailSender`; MailKit hỗ trợ StartTls/SslOnConnect | Không báo mail ready chỉ vì API chạy; không hạ TLS để gửi được |
| Client | `ApiClientConfiguration.Load()`: appsettings → appsettings.Local → environment; `Api__BaseUrl` override, timeout mặc định 30s | Switching đã có; ưu tiên tài liệu, chưa cần UI hoặc sửa loader |
| Legacy | `Program.UseDatabase=false`; nhánh Npgsql/local hoặc in-memory còn tồn tại; ProjectId khởi tạo 0 | Không bật nhánh DB để test shared DEV; không gọi in-memory là persistence chung |
| Logging | `ApiExceptionHandler` chỉ log trace/type, tránh raw exception; mail delivery failure được xử lý trong Services | Bổ sung log có cấu trúc tại nơi xử lý lỗi, tránh bỏ sót lỗi provider đã catch |
| SQL | 01 tạo 22 bảng; 04 thêm reset; 05 thêm verification/avatar và invalidates OTP v1; 06 thêm token_version/trigger | Verify schema cuối không được chỉ kỳ vọng 22 bảng |
| Seed/verify | 02 chứa demo users với DEV_HASH_*; 03 có truy vấn dữ liệu và negative tests comment | Seed tùy chọn, không chạy cho nghiệm thu Auth; thêm verification schema sau 04/05/06 |

Nếu source release khác baseline, đọc lại diff/scripts/config rồi cập nhật plan nếu thay đổi đáng kể. Không mặc định deploy checkout hiện tại.

## 3. So sánh hosting miễn phí

Tra cứu nguồn chính thức ngày 07/10/2026; phải xác nhận lại gói/điều kiện trên tài khoản trước provision. Giá 0 chỉ áp dụng trong free allowance; không cam kết uptime 24/7.

| Phương án | .NET 10/Docker | PostgreSQL persistent | HTTPS, env/secrets, logs | Chi phí và giới hạn | Đánh giá |
|---|---|---|---|---|---|
| Render Free API + Neon Free DB | Dockerfile hiện có chạy Linux x64; chọn runtime target | DB nằm ở Neon, độc lập filesystem API | URL onrender.com HTTPS; env/server config và log dashboard | $0 trong quota, API ngủ sau 15 phút; 750 instance-hours/workspace/tháng; không thêm payment method; SMTP 25/465/587 bị chặn | Đề xuất cho hạ tầng 0 đồng, có điều kiện giải quyết SMTP |
| Koyeb Free API + Neon Free DB | Docker, x64 | Dùng Neon; free API không có volume persistent | URL provider/TLS, secrets, stdout logs | API 512MB/0.1 vCPU, ngủ sau 1h; đăng ký yêu cầu thẻ, giữ $29 và tài liệu nêu charge plan đăng ký | Không chọn mặc định với ngân sách tuyệt đối 0; chỉ cân nhắc nếu tài khoản sẵn có được xác nhận $0 |
| Oracle Always Free VM + PostgreSQL volume | Docker trên VM; ưu tiên x64 phù hợp image | Tự quản DB trên persistent storage, backup thủ công | Tự cấu hình reverse proxy/TLS, env và docker logs | Always Free có điều kiện thẻ/capacity/reclaim; không dùng tài nguyên trial trả phí; ARM cần đổi build/native validation | Phương án dự phòng nếu team có sẵn VM miễn phí và HTTPS hostname; nhiều vận hành hơn |

Không dùng PostgreSQL trong filesystem ephemeral của free API. Không chọn Render Free Postgres làm DB dài hạn vì hết hạn sau 30 ngày. Koyeb Free DB chỉ có 5 giờ compute/tháng nên không ưu tiên. Neon tách persistent storage khỏi compute; thông báo chính thức 02/10/2026 nêu Free 1GB/project. Xác nhận thêm quota compute/transfer/connection trên account; không dựa vào con số cũ khi pricing page thay đổi.

**Primary stack đã được người dùng chọn:** Render Free + Neon Free, dùng URL HTTPS onrender.com, không mua domain/DNS và không thêm payment method. So sánh Koyeb/Oracle ở trên chỉ là dữ liệu tham khảo, không phải nhánh triển khai mặc định hoặc fallback tự động. Chưa provision. Nếu SMTP hiện tại chỉ Gmail: Gmail không tự đổi sang 2525; Render không phù hợp trực tiếp với SMTP Gmail 587/465.

**SMTP miễn phí có điều kiện:** Brevo Free có transactional mail 300 email/ngày và SMTP 2525. Chỉ thử nếu tài khoản/sender được chấp thuận và endpoint thực sự hỗ trợ StartTLS trên cổng này. Tài liệu có yêu cầu xác thực sending domain/deliverability; không coi email Gmail cá nhân hoặc hostname onrender.com là domain team sở hữu. Không mua domain và không hứa mail pass khi chưa có sender phù hợp. Nếu không đáp ứng: ghi BLOCKED, trình phương án miễn phí khác trước khi đổi provider/adapter; không chuyển sang plaintext hoặc bypass verify.

Auth/User runtime vẫn bắt buộc: nếu SMTP chưa cấu hình, chỉ test login/profile với tài khoản đã verified hợp lệ từ quy trình thật; không tự chỉnh `email_verified_at`. Nếu chưa có tài khoản như vậy, Auth/User acceptance còn BLOCKED và chưa đủ Done. SMTP/reset/registration flow phụ thuộc ghi follow-up rõ ràng.

Nguồn chính thức:

- [Render free limits](https://render.com/docs/free), [Render FAQ về billing](https://render.com/docs/faq), [Render Docker](https://render.com/docs/docker), [Web services/HTTPS/config](https://render.com/docs/web-services), [SMTP restrictions](https://render.com/changelog/free-web-services-will-no-longer-allow-outbound-traffic-to-smtp-ports).
- [Neon Free storage cập nhật](https://neon.com/blog/neon-free-plan-1-gb-per-project), [Neon pricing để kiểm tra lại quota](https://neon.com/pricing), [Neon direct/pooled guidance](https://github.com/neondatabase/agent-skills/blob/main/skills/neon-postgres/SKILL.md?plain=1).
- [Koyeb free instances](https://www.koyeb.com/docs/reference/instances), [billing/card](https://www.koyeb.com/docs/faqs/pricing), [DB limits](https://www.koyeb.com/docs/databases), [SMTP ports](https://www.koyeb.com/docs/faqs/general), [secrets](https://www.koyeb.com/docs/reference/secrets), [logs](https://www.koyeb.com/docs/run-and-scale/log-exporter).
- [Oracle Free Tier](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier.htm), [capacity/card FAQ](https://www.oracle.com/cloud/free/faq/), [reclaim conditions](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm).
- [Brevo Free](https://help.brevo.com/hc/en-us/articles/208589409-About-Brevo-s-pricing-plans), [SMTP ports](https://help.brevo.com/hc/en-us/articles/10905415650322-Which-SMTP-port-should-I-use-Port-587-465-or-2525), [sending domain requirements](https://help.brevo.com/hc/en-us/articles/35852083084178-Domain-setup-for-better-email-deliverability).

## 4. Thứ tự triển khai sau khi plan được duyệt

### Bước 1 — Chốt target và release, trước mọi thao tác bên ngoài

- Hosting/DB đã chốt Render Free + Neon Free; xác nhận người giữ tài khoản, region/quota miễn phí, Brevo account/sender miễn phí, DB mới hay đang có dữ liệu, nguồn release. Không còn cổng chọn lại provider; quyền provision/deploy vẫn riêng.
- Branch dự kiến `feature/SCRUM-80-shared-dev-environment`. Chỉ tạo/switch khi được yêu cầu riêng, không tự pull hoặc xử lý thay đổi untracked hiện có. Không triển khai SCRUM-80 trên branch SCRUM-39 để rồi gán release nhầm.
- Khi được phép Git, base từ develop hoặc commit chỉ định. Sau review/merge theo yêu cầu riêng, deploy commit xác định chứa thay đổi SCRUM-80.
- Khu vực: tài liệu `deployment-record.md` trong thư mục task; Git chỉ khi được phép.
- Verify: record ghi source SHA, provider/free plan, region, owner, URL dự kiến không secret; chưa coi target là deployed.

### Bước 2 — Chuẩn hóa image và provenance

- Sửa `src/CreatorFlow.Api/Dockerfile`: build arg `SOURCE_REVISION` không secret, gắn OCI revision label và assembly informational version tương ứng SHA; không tự lấy `.git` trong image vì build context đã exclude.
- Giữ net10/Linux x64/non-root và pin hiện hữu; chọn runtime stage, không dùng target tests để deploy. Không thay native avatar library/package.
- Chuẩn bị hướng dẫn build từ clean commit; record SHA, image tag cố định/digest, thời điểm UTC, migration set. Không dùng `latest` làm bằng chứng duy nhất; không đưa secret vào ARG/layer.
- Với Render: ưu tiên deploy từ commit cụ thể bằng Dockerfile, tắt auto-deploy để không tự chạy source/migration mới. Dashboard commit phải khớp revision image/runtime. Nếu build arg không thể cung cấp SHA đúng ở provider, đề xuất reviewed prebuilt image có digest trên registry miễn phí đã xác nhận; chỉ push khi được phép. Không dùng paid registry; không xác nhận được miễn phí thì dừng review. Không gán SHA cố định cho các build tương lai.
- Khu vực: Dockerfile; API `Program.cs` log version startup từ assembly; runbook/record. Không cần endpoint version public mới hoặc sửa Contracts.
- Verify sau duyệt test: image labels, startup version và dashboard cùng SHA; native runtime test phù hợp pass; không leak secret trong history/log.

### Bước 3 — Cấu hình deploy theo provider đã chọn

- Tạo `deploy/render/README.md` và `deploy/render/environment.example` với key/placeholder; cấu hình Render Free web service bằng dashboard sau quyền provision, repo root build context, Dockerfile API, runtime target, listen `0.0.0.0:8080` với PORT/provider config tương ứng, URL HTTPS onrender.com. Không thêm payment method, persistent disk hoặc Blueprint auto-provision DB trả phí.
- Không triển khai recipe Koyeb/Oracle/VM trong plan này. Chuyển provider phải cập nhật plan và được duyệt lại; không coi là fallback tự động.
- Trên PaaS, `compose.server.yml` vẫn là recipe VM; sửa SMTP optional trong compose/env sample để API có thể start khi mail chưa cấu hình, không thay secret thành giá trị giả. Giữ JWT và DB config bắt buộc.
- Environment giữ `ASPNETCORE_ENVIRONMENT=Production` để tránh developer errors; đây là chế độ ASP.NET trên server DEV, không có nghĩa triển khai nghiệp vụ Production. Issuer/audience và secrets riêng DEV.
- Khu vực: recipe provider đã chọn, compose.server/env.server.example, `docs/team-api-docker-setup.md`.
- Verify: diff chỉ placeholder; mapping keys khớp loader; mọi resource free, không disk/DB trả phí; port bind đúng; local compose giữ độc lập.

### Bước 4 — PostgreSQL, migration và verification

- Managed DB: tạo logical DB DEV riêng nếu được hỗ trợ; runtime credential chỉ Backend; operator migration credential ngoài client, không share cho team qua appsettings.
- Với Neon: dùng direct connection cho migration/dump/restore; ban đầu dùng direct TLS Npgsql cho API với pool nhỏ (ví dụ MaxPoolSize=10), tránh thêm dependency pooler chưa cần. Dùng `SSL Mode=VerifyFull`, CA chuẩn; không `Trust Server Certificate`, không disable cert validation. Xác nhận version PostgreSQL và quota trước tạo.
- Tạo `deploy/database/schema-manifest.md`: phân loại và SHA256 script của commit release. Không thêm migration framework/table mới chỉ để task này.
- DB mới: apply **01 → 04 → 05 → 06**, sau đó những migration mới trong commit release theo dependency đã review. **02 chỉ optional DEV seed**, không tự apply; **03 verification**, không migration. Không chạy lại CREATE scripts trên DB đã có schema.
- DB existing: inventory schema/cột/constraint/index/function/trigger trước; chỉ apply scripts còn thiếu đúng tiền điều kiện; drift/partial apply phải dừng, không che bằng IF NOT EXISTS.
- Thêm `database/verify_auth_schema.sql` read-only, fail rõ khi schema Auth thiếu: reset/verification/avatar tables, email_verified_at/token_version, constraint/index/trigger/function cần thiết. Giữ 03 cũ; không chạy negative write tests comment hoặc dump toàn user/OTP ra evidence. Expected baseline sau 04/05 có 25 bảng nghiệp vụ, dùng kiểm tra thành phần thay vì chỉ table count.
- Runbook dùng psql ON_ERROR_STOP để dừng khi script lỗi; script đã có BEGIN/COMMIT nên không bọc transaction chồng. Không migrate trong Docker build/entrypoint/startup.
- Evidence operator ngoài DB: target/project ID không secret, DB name, version, SHA/checksum, scripts đã apply, thời gian, exit status, verify summary. Không tự apply trong giai đoạn viết code.
- Khu vực: manifest, verification SQL mới, runbook; không sửa 01/02/03/04/05/06 hay nghiệp vụ.
- Verify sau cho phép DB: DB mới pass; upgrade trên DB disposable giữ dữ liệu mẫu hợp lệ; không dựa table count/SELECT 1 để kết luận schema đầy đủ.

### Bước 5 — Readiness và liveness

- Giữ `GET /api/health` và payload hiện tại, không truy vấn SMTP/AI/DB ở liveness.
- Thêm `GET /api/health/ready`: endpoint mỏng gọi `DatabaseReadinessService` → `IDbConnectionFactory` → PostgreSQL. Không SQL trong UI, không thêm health package.
- Check bounded 5 giây/cancellation: mở connection, SELECT 1 và probe đọc zero-row các cột/bảng cần Auth/User (users email_verified_at/token_version, reset/verification/avatar). Kết quả 200 `{status:ready}`; unavailable/timeout/schema thiếu trả 503 `{status:not_ready}`. Không lộ hostname, DB name, credential, SQL hoặc exception trong response.
- SMTP/AI không được gọi bởi readiness DB, không làm process alive phụ thuộc external provider. Trạng thái mail configured/unavailable chỉ log trạng thái cấu hình và checklist riêng, không cam kết deliverability.
- Phân biệt cancellation caller với dependency timeout. Không retry vô hạn, không auto migrate, không return healthy từ cache khi DB down.
- Provider health dùng `/api/health` tránh restart loop khi DB tạm down và tránh mỗi probe đánh thức DB serverless. Readiness operator kiểm tra khi deploy/nghiệm thu, không tạo uptime pinger để giữ free services awake.
- Khu vực: `Endpoints/HealthEndpoints.cs`, `Services/DatabaseReadinessService.cs` mới, `Program.cs`; test `tests/CreatorFlow.Api.Tests/Api/ReadinessTests.cs` mới.
- File mapping khi triển khai: thêm `Repositories/IDatabaseReadinessRepository.cs` và `Repositories/DatabaseReadinessRepository.cs` để SQL ở Repository theo AGENTS.md, Service chỉ quản lý timeout/kết quả; interface cho phép test stub không DB. Không thêm package hoặc sửa shared Contracts.
- Verify: DB thật ready 200, DB sai/down/thiếu schema 503 trong giới hạn; `/api/health` vẫn 200 khi process sống; process dừng thì health không trả app ok. Test có thể stub dependency và runtime dùng DB disposable đã duyệt.

### Bước 6 — Logging và secrets

- Startup log version, environment, mail configured/unavailable, không log config values. Request failure log traceId/status/type; readiness DB failure log category/type, không raw Npgsql message/query.
- Với mail lỗi đã catch, thêm ILogger log provider failure không email, OTP, body hoặc raw exception tại `EmailVerificationService`/`PasswordResetService`. Giữ response/business logic, SMTP TLS và timeout 10s hiện tại.
- Dùng console/stdout hiện có + provider logs; không Serilog/monitoring package mới. Không HTTP body/Authorization headers/query string logging; không SMTP protocol logger.
- Runbook cấm Npgsql Include Error Detail/Log Parameters chứa dữ liệu nhạy cảm. Review cả framework logs và error paths, không chỉ log do mình thêm.
- `.gitignore` bổ sung đường backup cụ thể `deploy/backups/` và dump artifacts nếu cần; Docker context đã exclude dumps/secrets. `deploy/render/environment.example` chỉ placeholder; không thu thập file local secret để in output.
- Audit phạm vi: Auth/User, module API-ready, Contracts, deploy tracked artifacts/log; legacy local DB dependency ghi technical debt, không cấp DEV credential. Nếu thấy secret thật đã tracked, báo và xử lý theo phạm vi riêng, không tự viết lại lịch sử Git.
- Khu vực: Program, readiness, 2 mail Services, `.gitignore` nếu cần, `docs/tasks/.../technical-debt.md`.
- Verify sau duyệt: induced safe failures có trace/category, evidence không raw token/OTP/password/connection string; không sửa auth/permission/token behavior.

### Bước 7 — LOCAL ↔ DEV và hướng dẫn team

- Reuse `Api__BaseUrl` và `Api__TimeoutSeconds`; document JSON/environment precedence và restart process sau đổi env. LOCAL localhost:5080, DEV HTTPS provider URL không credential/query.
- Không thêm environment dropdown/Form hoặc DB settings UI. `ApiClientConfiguration.cs`/client appsettings dự kiến không cần sửa.
- Dùng timeout 30s hiện hữu; free cold start có thể lâu hơn, hướng dẫn mở health chờ rồi retry Login. Nếu team cần nâng timeout, dùng config, không blanket retry POST Register/OTP.
- Team DEV config chỉ API URL/timeout, runtime access token theo session; không set DB DEV connection string. Không bật legacy UseDatabase để truy cập server. Board chưa API-ready giữ BLOCKED.
- Khu vực: `docs/team-api-docker-setup.md`, runbook, technical-debt; không đổi `Program.cs` client hoặc nghiệp vụ UI.
- Verify sau duyệt: hai máy đổi config → đúng API → local lại, session đúng user; không sửa source, không mất local DB riêng.

### Bước 8 — Backup/restore, deploy và rollback runbook

- Tạo `docs/shared-dev-test-runbook.md`: operator/target/version, quota/free sleep, config keys, migration ordering, DB verify, deploy, smoke, backup/restore, rollback, logging và team switching.
- Backup bằng pg_dump custom format qua direct TLS; credential dùng secret process/file permission hạn chế ngoài Git, không command line literal; dump giữ ngoài repository trên máy/dung lượng sẵn có và có bản copy riêng bằng tài nguyên sẵn có với owner/access rõ ràng. Không mua object storage/backup service. Dùng client pg_dump tương thích server major.
- Ghi checksum, thời gian, DB/commit/migration set, retention tối thiểu bản trước migration và bản trước demo; dump có dữ liệu cá nhân/hash nên không gửi vào chat/commit.
- Restore drill ưu tiên PostgreSQL disposable local được duyệt trên tài nguyên sẵn có, không tạo thêm Neon project/branch/database nếu không cần; không overwrite DEV hiện tại. pg_restore, verify schema, kiểm tra dữ liệu và Auth phù hợp; sau restore không coi token/OTP lịch sử là identity bypass.
- Release order: xác nhận DEV target → backup → inventory → apply migration → verify → deploy commit/image → health/readiness/Auth smoke → record. Có maintenance window khi schema không backward-compatible.
- Rollback API chỉ về digest/commit cũ nếu tương thích schema. Không tự down-migrate 05/06 hoặc restore đè DB để rollback; cần quyết định riêng và backup trước hành động mất dữ liệu.
- Managed PostgreSQL không cho operator restart process tùy ý: nghiệm thu persistence bằng suspend/resume hoặc provider-supported compute lifecycle. Nếu không có thao tác, ghi giới hạn/evidence thay thế để người dùng review, không fake restart pass.
- Khu vực: runbook, `deployment-record.md`, `test-checklist.md` trong task.
- Verify: operator có thể làm theo quy trình không cần đoán secrets/order; dump/restore drill pass thật sau cổng DB approval.

### Bước 9 — Provision/deploy sau lựa chọn và quyền riêng

- Đây là bước thực hiện bên ngoài, chưa được cho phép bởi duyệt plan. Stack đã chọn; chỉ bắt đầu sau người dùng cho phép provision/deploy đúng free resources/target, cấp quyền qua phương thức phù hợp và kiểm tra ZERO-COST GUARDRAIL.
- Không mua dịch vụ, DNS/domain hoặc bật paid plan. Nếu đăng ký bắt buộc phí, dừng nhánh đó và trình lựa chọn khác.
- Operator tạo server secrets, DB/schema có approval riêng; agent không xin secret qua chat. Deploy release chứa SCRUM-80 và record SHA/URL/provider revision; không tự push/PR/merge để có source deploy.
- Verify: public HTTPS liveness, readiness, cert, log startup version đúng; DB persistence/backup và smoke sau cho phép test; laptop một thành viên tắt không làm API cloud tắt.

### Bước 10 — Nghiệm thu và bàn giao

- Trình checklist theo post-change-test-workflow trước khi build, test, chạy app/container/API/DB/mail. Test giai đoạn chỉ quan sát/ghi kết quả; không sửa code khi fail.
- Tầng 1 pass có evidence thật thì SCRUM-80 đủ nghiệm thu theo phạm vi; SMTP chưa configured ghi follow-up, nhưng thiếu Auth/User runtime thật vẫn chưa đủ Done.
- Tầng 2 chưa API-ready ghi BLOCKED theo module/commit/ticket, không kéo dài hạ tầng hoặc hardcode để pass.
- Bàn giao URL, SHA/digest, migration/verify record, backup drill summary, config guide, checklist và technical debt; không giao credential trong repository.
- Git commit/push/PR vào develop/merge và Jira Done chỉ làm khi được yêu cầu riêng.

## 5. Checklist kiểm chứng dự kiến — chưa chạy

| Nhóm | Kiểm tra và kết quả mong đợi | Evidence/điều kiện |
|---|---|---|
| Build | `dotnet build CreatorFlow.slnx`; Linux runtime image build/native avatar phù hợp | Chỉ sau duyệt build/test, recorded SHA; không dùng secret/DB trong build |
| Focused tests | Readiness healthy/unavailable/timeout/sanitization; Auth API routing/unit hiện có; ApiFoundation client config/errors | Chọn category liên quan; không chạy DB tests trên DEV team |
| DB disposable | Schema 01/04/05/06 + verification; upgrade với dữ liệu cũ; restore drill | Target + quyền writes/migration riêng; không fallback app config |
| Health | Internet HTTPS health 200; readiness 200; DB unavailable/thiếu schema readiness 503, liveness 200 | Fault injection trên disposable hoặc maintenance DEV được phép, không đổi secret live tùy tiện |
| Restart | API restart cùng version; DB compute restart/resume dữ liệu vẫn còn; health/readiness recover | Managed-provider equivalent ghi rõ, không ngầm coi redeploy API là restart DB |
| Auth/User | Register/Verify thật khi mail configured; login hợp lệ/invalid; 401 no token; profile save/reopen, logout clear session/login lại | Không bypass verify; mail unavailable ghi BLOCKED, có valid verified accounts mới test login/profile được |
| SMTP | Verification + reset email đến mailbox thật, OTP không có trong log | Chỉ khi DEV SMTP configured; owner cho phép gửi test mail; sandbox không thay mailbox delivery |
| Two machines | Máy A/B mỗi người tài khoản riêng, cùng URL/SHA; profile A persist sau restart, B không đọc/sửa profile A qua me | Ghi máy/user alias không token; không cần Project để pass Auth multi-machine |
| Switching | A/B LOCAL → DEV → LOCAL; URL qua config, local DB độc lập | Không đổi source; process restart nhận env |
| Secrets/TLS | Tracked artifacts, Auth/User/client config, logs không secret; HTTPS/DB TLS validation thật | Không print secret để chứng minh; không scan/output ignored private config indiscriminately |
| Extended integration | Project invite/member/permission, Board/assignment/review/workflow khi API-ready | BLOCKED có ticket, không ảnh hưởng tầng 1; không fake UserId/ProjectId |

Mỗi mục ghi NOT RUN/PASS/FAIL/BLOCKED, thời điểm, release SHA, môi trường, evidence đã làm sạch và owner. Inconclusive không là pass. Lỗi được điều tra/report rồi chờ duyệt fix theo bug-investigation-workflow.

## 6. Những quyết định cần review trước triển khai

1. Stack đã chốt Render Free + Neon Free/onrender.com, không thêm payment method; còn quyền provision/deploy riêng, chưa được cấp trong lượt này.
2. SMTP: team có sender/domain được phép dùng miễn phí, hoặc tài khoản verified thật đã tồn tại không? Nếu chưa, mail/Auth phụ thuộc ghi BLOCKED; chốt phương án miễn phí trước nghiệm thu thật.
3. Nguồn commit release và quyền tạo branch SCRUM-80 là bước riêng; chưa tạo trong lượt plan.
4. DB mới hay có dữ liệu cần giữ; ai giữ account/backup, hai máy nào tham gia nghiệm thu?

Người dùng đã cho phép chuyển sang triển khai source/docs theo plan; các approval gate bên dưới vẫn giữ nguyên. Provision, deploy, migration, test/runtime, gửi mail và Git/external actions vẫn cần quyền tương ứng. Mọi thao tác có khả năng billing phải dừng xin review; không trial auto-charge/paid conversion. Nếu đổi stack, thay SMTP adapter sang HTTP hoặc migrate business module, cập nhật plan và xin duyệt lại.

## Guardrail cuối cùng — đã duyệt

- Render build trực tiếp từ Git commit là đường triển khai chính. Không dùng registry riêng nếu Render build trực tiếp được; registry chỉ là fallback cuối cùng, cần review riêng kể cả registry miễn phí.
- Trước mỗi đợt deploy/test lớn, operator kiểm tra Render bandwidth/build quota và Neon compute/storage trên dashboard; ghi thời điểm, mức dùng/còn lại và reset nếu có vào deployment/test record, không ghi secrets.
- Nếu gần giới hạn free hoặc không đủ cho đợt dự kiến: dừng test không cần thiết, chờ quota/reset; với storage không reset thì chỉ tiếp tục khi có dung lượng miễn phí đủ và phương án đã review. Không upgrade hoặc tự xóa dữ liệu để lấy quota.
- Neon chỉ tạo tài nguyên DEV tối thiểu cần thiết. Không tạo thêm project/branch/database để thử nếu không cần; restore/upgrade drill ưu tiên PostgreSQL disposable local trên tài nguyên sẵn có, không dùng thêm cloud resources khi chưa review nhu cầu/quota.
- Không thao tác nào được phép làm ngân sách lớn hơn 0 đồng để đạt PASS. Điều kiện miễn phí không đáp ứng thì ghi BLOCKED, không mua workaround.

## Tiến độ source/docs — chưa nghiệm thu runtime

- Đã triển khai readiness/service/repository, sanitized logging, revision image/assembly, optional mail server compose; không sửa client UI/legacy nghiệp vụ.
- Đã viết verification SQL read-only, schema manifest/checksums, Render recipe/env reference, runbook/quota/backup/restore, technical debt và deployment/test record.
- Registry fallback vẫn cần review riêng; Render Git build dùng RENDER_GIT_COMMIT theo docs chính thức. Không payment method/paid resource mới.
- Static diff review không thay build/test. Đợt 1 đề nghị trong test-checklist; các đợt Docker/DB/cloud/two-machine/mail cần quyền riêng.
- SCRUM-80 chưa Done: chưa release commit/deploy/DB/health/Auth/two-machine evidence. Mở rộng integration ghi BLOCKED khi chưa API-ready.
