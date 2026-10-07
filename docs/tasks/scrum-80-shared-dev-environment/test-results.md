# SCRUM-80 — Kết quả kiểm chứng đợt 1

Ngày 07/10/2026, khoảng 20:35–20:39 Asia/Saigon (UTC+07). Người dùng đã duyệt đợt 1 qua chat.
Branch: `feature/SCRUM-80-shared-dev-environment`. Baseline HEAD:
`fdb184136e6e94f52e03e17d243c72202cff4b09` + working-tree changes SCRUM-80,
không phải commit release/deployed SHA. Không sửa source trong khi test.

## Kết quả

| Phạm vi | Kết quả |
|---|---|
| Build solution | PASS, exit 0, 8 warnings, 0 errors |
| API selected categories | PASS: 42 executed, 42 passed, 0 failed/skipped/inconclusive/aborted |
| ApiFoundation | PASS: 31 executed, 31 passed, 0 failed/skipped/inconclusive/aborted |
| DB/mail/cloud/Docker/app UI | NOT RUN; ngoài scope đợt 1 |

Build command thành công:

```powershell
dotnet build CreatorFlow.slnx --no-restore --disable-build-servers -m:1 -v:normal
```

Lượt `dotnet build CreatorFlow.slnx` mặc định exit1 nhưng chỉ in Build FAILED/0 warnings/0 errors,
không nêu nguyên nhân. Chạy lại cùng solution với một node và build server disabled thành công;
không có bằng chứng để kết luận nguyên nhân chính xác của lượt mặc định.

8 warnings nằm trong file client không bị sửa bởi SCRUM-80:

- CS0105 ×2: ContentDetailsRepository (duplicate using).
- CS8619 ×1: ContentDetailPanel (nullability List).
- CS8604 ×3: WorkflowService (note/feedback nullable).
- CS0067 ×2: ModulePageHeaderControl (unused events).

Không dọn các warnings ngoài phạm vi.

## Test commands và evidence

Hai nhóm thử ban đầu trong sandbox abort sau 90s do vstest.console không kết nối được testhost.
Sau khi chạy lại với quyền thực thi ngoài sandbox, cùng filter/build đã chuẩn bị pass; không
đổi code hoặc kéo dài timeout để che lỗi. TRX cuối ghi lượt pass và overwrite lượt aborted;
diagnostic log nằm trong ignored TestResults, không commit/log secrets.

```powershell
dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --no-build --no-restore --filter "TestCategory=ApiReadiness|TestCategory=AuthApiUnit|TestCategory=AuthApiIntegration|TestCategory=AuthAvatarUnit|TestCategory=PasswordResetUnit" --logger "trx;LogFileName=scrum80-api.trx" --results-directory TestResults/scrum80-wave1/api --diag TestResults/scrum80-wave1/api/vstest-diag.log
dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --no-build --no-restore --filter "TestCategory=ApiFoundation" --logger "trx;LogFileName=scrum80-foundation.trx" --results-directory TestResults/scrum80-wave1/foundation --diag TestResults/scrum80-wave1/foundation/vstest-diag.log
```

Artifacts local, ignored:

- `TestResults/scrum80-wave1/api/scrum80-api.trx`
- `TestResults/scrum80-wave1/foundation/scrum80-foundation.trx`

Counters được đọc lại từ TRX: 42/42 và 31/31, 0 errors/timeouts/aborted/inconclusive.
Sáu tests mới thực sự chạy/pass:

- ReadyEndpoint_ProbesEveryRequest_WhileLivenessDoesNotProbeDatabase.
- ProbeTimeout_IsBoundedAndCancelsDependency: 5,016 giây.
- CallerCancellation_IsPropagatedInsteadOfReportedAsDatabaseFailure.
- DatabaseFailure_LogsTypeWithoutRawExceptionOrConnectionDetails.
- FailedVerification_LogsCategoryWithoutEmailOrOtp_AndKeepsRequestUnusable.
- FailedReset_LogsCategoryButKeepsGenericResponseAndNoDeliveredCode.

Scope dùng TestServer/fake repository/mail sender và client HTTP stub; không DB/schema writes,
SMTP thật, public HTTP/cloud hoặc Docker. DB test connection/writes opt-in env không hiện diện
ở đầu đợt. Không chạy unfiltered suite, AuthApiDatabase/live API hoặc ứng dụng WinForms.

## Giới hạn và phần còn lại

- Readiness routing/service timeout/logging đã pass bằng stub; SQL probe/verification chưa chạy
  trên PostgreSQL thật. Không gọi đó là DB readiness/migration pass.
- Docker revision label/Linux image/native runtime và provider RENDER_GIT_COMMIT mapping chưa
  build/deploy nghiệm thu; chưa có release commit/provenance evidence cloud.
- Backup/restore, restart persistence, SMTP delivery, two-machine Auth và switching runtime
  LOCAL ↔ DEV chưa chạy. Phải qua gate tương ứng và zero-cost quota checks.
- Project/Board/workflow API integration vẫn BLOCKED bởi module/API-ready release.
- SCRUM-80 chưa Done; không có provision/deploy/migration/mail/Git commit/push/PR/merge mới.
