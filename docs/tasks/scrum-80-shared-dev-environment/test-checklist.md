# SCRUM-80 — Checklist và cổng duyệt test

Trạng thái: ĐỢT 1 PASS sau khi người dùng duyệt ngày 07/10/2026. Build solution thành công (8 warnings, 0 errors); API 42/42 và ApiFoundation 31/31 pass, không skipped/inconclusive. Không sửa source trong giai đoạn test. Chi tiết tại [test-results](test-results.md).

Chưa app UI/Docker/DB/SMTP/cloud/live HTTP; TestServer chỉ xử lý HTTP stub trong tiến trình. Các đợt 2/3 cần approval riêng.

## Đợt 1 đề nghị duyệt — build/tests không DB/mail/cloud

Đã duyệt và thực hiện. Lượt sandbox ban đầu abort vì testhost không kết nối; chạy lại ngoài sandbox cùng filter đã pass. Build dùng no-restore/disable-build-servers/m:1 sau lượt mặc định exit1 không diagnostic; không đổi source để khắc phục.

Checkout SCRUM-80, không secrets/DB test env:

1. `dotnet build CreatorFlow.slnx`: kỳ vọng exit 0; ghi warnings. Kiểm tra signatures/DI/types, client không lỗi compile.
2. `dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --filter "TestCategory=ApiReadiness|TestCategory=AuthApiUnit|TestCategory=AuthApiIntegration|TestCategory=AuthAvatarUnit|TestCategory=PasswordResetUnit"`.
   Kỳ vọng readiness 200/503, không DB probe ở liveness, timeout/caller cancellation đúng, log không raw exception; mail failure không làm code usable/đổi generic reset response; routing/token/permission/avatar regression pass. TestServer/fakes, không DB/mail thật.
3. `dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --filter "TestCategory=ApiFoundation"`.
   Kỳ vọng client configuration/health/error handling và sanitizer pass. Stub tests, không live API/DB.

Không AuthApiDatabase/live API/app/Docker/full unfiltered suite trong đợt 1. Restore có thể tải packages, không tạo dịch vụ billing; network/dependency thiếu ghi BLOCKED. TRX vào ignored TestResults, không commit artifacts. Fail → điều tra/report/chờ duyệt fix, không sửa lúc test.

## Đợt 2 — Linux image/DB local disposable, quyền riêng

Đã được duyệt và hoàn tất ngày07/10/2026: Linux51/51, DB23/23 pass; fresh/upgrade/verify,
dump/restore/restart và health/readiness local pass. Image revision `local`, clean release SHA
vẫn chưa kiểm chứng. Có runtime warnings đã ghi report, chưa sửa. Xem [wave2 results](test-results-wave2.md).

- Quota/resource check trên máy sẵn có; engine/tools xác nhận. Sau duyệt build: runtime image từ clean SHA, OCI label/startup Version/non-root/port đúng; target tests cần test approval, không DB/SMTP.
- Sau DB local disposable write/migration approval: apply 01/04/05/06, verify, Auth DB tests explicit env + writes opt-in, không fallback team configs.
- Upgrade drill từ schema cũ với data hợp lệ; dump/restore vào local disposable riêng được duyệt; không Neon projects/branches để thử không cần thiết.
- DB down/thiếu schema ready 503 ~5s, alive 200; restart local DB giữ data, không xóa volume/public DB port.

## Đợt 3 — DEV cloud/two-machine sau approval riêng

Quota check Render/Neon/Brevo trước đợt lớn; không payment method ở mọi provider.

| Tầng | Kiểm tra/kỳ vọng | Hiện trạng |
|---|---|---|
| Bắt buộc | Source version dashboard/startup/OCI nếu exposed khớp clean SHA | NOT RUN |
| Bắt buộc | Public HTTPS health/cert thật; process down không fake healthy | BLOCKED: chưa deploy |
| Bắt buộc | Readiness 200 DB ready; lỗi DB/schema 503; SMTP/AI không ảnh hưởng alive | BLOCKED: chưa DB/deploy |
| Bắt buộc | API restart giữ version/key/data; DB compute lifecycle giữ data | BLOCKED; provider limitation phải review |
| Bắt buộc | Migration target/checksum/verify đúng; upgrade giữ data | BLOCKED: chưa DB approval |
| Bắt buộc | Backup checksum/metadata + local restore drill thật | NOT RUN |
| Bắt buộc | Secrets audit Auth/User/client/contracts/tracked/log; sanitized failures | Static reviewed; runtime NOT RUN |
| Bắt buộc | Hai máy/account thật cùng API: login/profile persistence, invalid login, 401, logout/login; B không sửa profile A qua me | BLOCKED: cần máy/user/DEV |
| Bắt buộc | LOCAL → DEV → LOCAL chỉ config, local độc lập | NOT RUN |
| Mail khi configured | Register → Verify → Login bằng mail thật; reset/old token rejection như Auth hiện tại; không OTP log | BLOCKED: chưa sender/config |
| Mở rộng | Project invite/member/context/permission qua API | BLOCKED: cần API-ready/SCRUM-25 |
| Mở rộng | Board/Assignment/Review/Workflow/Notification API-ready | BLOCKED: module dependencies |

Mail chưa configured ghi follow-up, không fake PASS. Auth/User runtime vẫn bắt buộc; không verified accounts hợp lệ thì chưa đủ tầng 1. Mở rộng BLOCKED không treo task khi tầng 1 pass. Không seed fake identity, sửa email_verified_at, hardcode UserId/ProjectId hoặc client kết nối Neon để pass.

Mỗi lần ghi scope approval, giờ/timezone, SHA/environment, PASS/FAIL/BLOCKED/NOT RUN và evidence sạch. PR/merge/Jira Done chưa được yêu cầu.
