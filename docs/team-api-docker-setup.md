# CreatorFlow — Docker local và backend chung cho team

Hướng được người dùng chốt: mỗi thành viên phát triển/test local độc lập; server/database chung để nghiệm thu. Windows chạy WinForms và Docker Desktop; API trong container Linux x64 Ubuntu24.04/glibc. Không cần đổi Windows sang Linux. Docker không tự tạo server trên internet.

## Thành phần và mức hoàn thành

- src/CreatorFlow.Api/Dockerfile: build .NET10, runtime non-root, publish linux-x64; target tests chạy portable API/native-image tests trong Linux, không DB/SMTP.
- deploy/docker/compose.dev.yml: PostgreSQL18 và API riêng trên máy thành viên, API localhost5080; không public DB, không auto schema/migration.
- deploy/docker/compose.server.yml: dùng API image đã review và database server do operator chuẩn bị; port loopback để đặt sau reverse proxy HTTPS. Không tạo VPS/domain/certificate/database từ recipe.
- SMTP/HMAC/JWT/DB credentials chỉ environment/User Secrets của backend hoặc file secret server, ngoài Git. WinForms chỉ cần API URL/timeout, token memory. Không sao chép secret từ cấu hình client cũ vào source.
- LocalCLI Docker29.4.3 được phát hiện, chưa chứng minh engine/container/ảnh Linux chạy. Source Docker/avatar đã viết; chưa build/run/test phiên bản này, chưa apply06 hoặc deploy server. Theo dõi [checklist](tasks/auth-user-client-server/test-checklist.md).

## A. Setup local — một lần mỗi máy dev

1. Cài Docker Desktop phù hợp Windows, chọn Linux containers, Docker engine đang chạy; nếu chạy WinForms từ source cần .NETSDK10. Không chạy Docker trong chế độ Windows containers cho image này. Team target Linuxamd64, máyARM phải review platform riêng.
2. Lấy source đã được team cấp (branch/PR đang review chưa phải release merged). Sao chép deploy/docker/.env.example thành deploy/docker/.env, nhập secret **trên máy mình**, không chat/Git. Không ghi secret vào câu lệnh terminal hoặc docker build ARG.
3. CREATORFLOW_DB_PASSWORD: password database dev riêng, khuyến nghị random Base64 để không chứa dấu phân cách connection string; CREATORFLOW_JWT_KEY Base64>=32randombytes; CREATORFLOW_OTP_KEY Base64 đúng32randombytes. Hai key khác nhau; mỗi môi trường local độc lập có thể dùng key riêng, API instances cùng DB/môi trường phải dùng cùng key tương ứng. Giữ ổn định qua restart, không tạo lại mỗi lần mở app.
4. Nếu cần register/verify/reset email thật, điền sender/provider SMTP vào .env local. Username/from thuộc mailbox gửi của môi trường, password là app password với Gmail. Email đăng ký của thành viên không cần app password. SMTP thiếu thì capabilities không sẵn sàng, không fake delivery. Không phân phối app password mailbox cá nhân vào repository; mailbox dev dành cho dự án là lựa chọn phù hợp khi nhiều dev cần thử email.
5. File .env được Git ignore và Docker build context loại trừ. File vẫn là plaintext local và người có quyền Docker có thể xem environment; không xem đây là vault. Trên server cấp quyền hạn chế file và ưu tiên secret store/process environment của operator.

Các command bên dưới chỉ chạy sau khi target/runtime/DB write approval của môi trường đã có. Từ repository root:

```powershell
# Build Linux API image; không kết nối DB hoặc gửi email.
docker build --platform linux/amd64 -f src/CreatorFlow.Api/Dockerfile -t creatorflow-api:dev .

# Opt-in Linux test target: có chạy test thật trong image build, cần approval test.
docker build --platform linux/amd64 --target tests -f src/CreatorFlow.Api/Dockerfile -t creatorflow-api-tests:dev .

# Chỉ start database dev riêng; chưa có schema/Auth tables.
docker compose --env-file deploy/docker/.env -f deploy/docker/compose.dev.yml up -d db
```

Không dùng docker compose config bản đầy đủ để gửi log vì có thể in secret; kiểm tra cấu hình bằng config --quiet khi cần. Không mount source SQL vào /docker-entrypoint-initdb.d để tự migrate.

## B. Schema local — operator áp thủ công sau review/approval

Database compose tên creatorflow_dev, user creatorflow_dev, servicehostname db. PostgreSQL18 volume đúng /var/lib/postgresql, không dùng mount data path của bản17. Không nhầm với localhost:5432/creatorflow máy Lenovo đang có dữ liệu.

Trên **DB mới/rỗng được phép**, thứ tự exactscripts: database/01_schema.sql →04_password_reset_requests.sql →05_auth_email_avatar.sql →06_auth_token_version.sql. Không cần seed02 vì DEV_HASH_* không login được; không đổi seed để demo. Script03 chỉ verification sau review, không là migration.

Trên DB đã có01/04/05, chỉ review/apply06; đừng chạy lại scriptscreate. Backup và kiểm tra schema/history trước apply; không dùng IF NOT EXISTS để che drift. API/startup/tests không tự migrate. SQL06 ảnh hưởng password/status/security version và invalidate oldJWT như [proposal](tasks/auth-user-client-server/migration-proposal.md).

Ví dụ vận chuyển exactscript từ host vào container (không chạy script bằng pipeline Get-Content có thể đổi encoding):

```powershell
# Ví dụ01 chỉ cho DB mới đã được duyệt; lặp với04/05/06 theo trạng thái thực tế.
docker compose --env-file deploy/docker/.env -f deploy/docker/compose.dev.yml cp database/01_schema.sql db:/tmp/creatorflow-schema.sql
docker compose --env-file deploy/docker/.env -f deploy/docker/compose.dev.yml exec -T db psql -U creatorflow_dev -d creatorflow_dev -v ON_ERROR_STOP=1 -f /tmp/creatorflow-schema.sql
```

Không tự xóa volume/DB để làm test lại. docker compose down dừng resources nhưng giữ volume; down -v phá hủy dữ liệu, không nằm trong hướng dẫn nghiệm thu thông thường.

Sau đủ schema, cấu hình và approval runtime:

```powershell
docker compose --env-file deploy/docker/.env -f deploy/docker/compose.dev.yml up -d --build api
```

API localhost5080; health GET /api/health chỉ chứng minh app đáp ứng, không schema/SMTP ready. JWT config bắt buộc; user cũ chưa verified không vào protected endpoints. Npgsql connection string trong container dùng Host=db, không localhost. Nếu dùng DB host bên ngoài compose, operator cung cấp route/cert/credential riêng, không sửa hostDB/firewall tùy tiện.

## C. WinForms và Postman

WinForms: đặt Api__BaseUrl=http://localhost:5080 và Api__TimeoutSeconds=30 ở environment hoặc appsettings.Local.json được ignore. Nếu chạy app qua VisualStudio/terminal cũ, restart process để nhận cấu hình. Auth WinForms không đọc SMTP/HMAC/JWT signing/DB credentials. Board/Workflow legacy còn DB/in-memory dependencies ngoài phaseAuth, không gọi đây là full-app migration.

Postman: base URL local http://localhost:5080; thử health, register, verification requests/confirm, login, me/profile/password và reset theo routes trong [plan](tasks/auth-user-client-server/plan.md). Login response có accessToken dùng Bearer cho /api/users/me; không share/export real token/password/OTP vào repository. Form profile PUT /api/users/me/profile dùng multipart displayName, avatarAction Keep/Url/Upload/Remove, avatarUrl tùy chọn và filefield image khi Upload; không gửi userId/admin/role. Backend xác định chủ thể.

Luồng user: đăng ký email riêng/passwordCreatorFlow →nhận mã→verify→Login; không autoLogin. Password Gmail/app password chỉ SMTP sender, không credentialCreatorFlow. Reset/change thành công phải đăng nhập lại; token cũ bị reject sau security change. Avatar backend JPEG/PNG<=2MiB/4096px, PNG<=512px/metadata stripped, bytea+URL; preview/cancel chưa persist.

Local riêng không tự sync tài khoản sang server chung. Muốn dùng dữ liệu chung, WinForms đổi URL sang API server đã triển khai; không copy database local vào server bằng cách replace dữ liệu.

## D. Shared DEV/TEST SCRUM-80 — Render Free + Neon Free

Primary stack đã chốt: Render Free Web Service build trực tiếp từ Git commit, Neon Free PostgreSQL, URL HTTPS onrender.com; không mua domain. Recipe tại [deploy/render](../deploy/render/README.md), [runbook](shared-dev-test-runbook.md), [checklist SCRUM-80](tasks/scrum-80-shared-dev-environment/test-checklist.md).

Không thêm payment method ở Render/Neon/Brevo/registry/dịch vụ phụ trợ. Free tier yêu cầu thẻ hoặc có khả năng billing thì dừng review. Không trial auto-charge/paid conversion, paid disk/storage/backup/registry; registry chỉ fallback cuối cùng được review riêng. Không Render Free Postgres. Quota check trước deploy/test lớn; gần limit dừng test không cần thiết/chờ reset, không upgrade để PASS.

Cấu hình server dùng environment reference, AllowedHosts đúng hostname Render, API port8080, không DB DEV config trên client. Provider dùng liveness /api/health; operator kiểm tra /api/health/ready riêng, readiness không thay migration/schema verification. Schema01 → migrations04/05/06 theo trạng thái DB, seed02 optional không cho Auth bypass, verify03/verify_auth_schema.sql không là migration. Backup pg_dump ngoài repo bằng tài nguyên sẵn có, restore drill local disposable được duyệt; không tạo thêm Neon project/branch/database chỉ để thử.

Brevo Free2525/StartTls là SMTP ưu tiên, chỉ nếu account/sender hoàn toàn miễn phí/TLS/mail thật hoạt động. Chưa configured ghi BLOCKED/follow-up, không mua domain/email hoặc chỉnh email_verified_at. Auth/User runtime thật vẫn bắt buộc.

WinForms chỉ đổi Api__BaseUrl sang URL DEV được cấp rồi restart process; local riêng vẫn độc lập. Legacy Board/Workflow DB dependency là technical debt, không kết nối Neon DEV chung từ WinForms. Project/Invitation/Board/Assignment/Review/Workflow integration khi API-ready, chưa sẵn sàng ghi BLOCKED tầng mở rộng; không hardcode identity.

compose.server.yml chỉ còn recipe VM tham khảo, không primary stack SCRUM-80. Lượt triển khai source/docs này chưa build/test, provision/deploy, mail hoặc migration; recipe không chứng minh server đã chạy. Các thao tác bên ngoài vẫn cần approval riêng.

## E. Nghiệm thu và phát triển nhóm

Mỗi người code/test local riêng →PR/merge được review→update image server chung→nghiệm thu tích hợp. Không test write tùy tiện lên DB chung hoặc thay credential user thật.

- Build/test Windows và Linux image/native avatar, API authentication/routing/contracts; kết quả33API/64client trước ảnh không thay evidence phiên bản mới.
- Approved disposable DB: port baseline persistence/OTP races/rollback và06version; set CREATORFLOW_TEST_CONNECTION_STRING + CREATORFLOW_TEST_ALLOW_WRITES=true chỉ process test, nofallback, UUIDcleanup.
- User nghiệm thu Register/Verify realemail; ForgotReset old/new password; Logout/expiry; Profile preview/cancel/save/remove/URL/restart; sourcemetadata stripped và máy khác cùng server đọc đúng ảnh.
- Board shell chưa project context chỉ Profile/Logout, không gán user/projectdemo để bypass; projectselection/permissions là phase riêng.
- Ghi ticket19/20/21 acceptance riêng, failures report trước fix; build/test/migration/mail/deploy/Git actions cần approval tương ứng. Chưa call Done khi chưa có evidence thực tế. PR/merge/JiraDone riêng.

Nguồn primary: [SkiaSharp Linux NativeAssets](https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux.NoDependencies/4.151.2), [Microsoft .NET SDK images](https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md), [PostgreSQL container](https://github.com/docker-library/docs/blob/master/postgres/content.md). Hướng dẫn tĩnh chưa được chạy setup/container/server trong lượt viết này.
