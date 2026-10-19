# SCRUM-50 — Kế hoạch triển khai (Notification nghiệp vụ)

Trạng thái: **CHỜ DUYỆT plan** (chưa code).

Requirements: `docs/tasks/scrum-50-notification-nghiep-vu/requirements.md` (đã chốt 5 quyết định).
Branch: `SCRUM-50-notification-nghiệp-vụ` (base `develop` `b76215d`).

## 1. Quyết định đã chốt

| # | Nội dung |
|---|---|
| Q1 | Deadline **sinh lười khi đọc** danh sách thông báo, idempotent nhờ `dedupe_key`; không thêm job nền |
| Q2 | **Có** làm panel UI trên icon Bell |
| Q3 | `dedupe_key` + unique index; **file migration phải comment rõ đã đổi schema gì** |
| Q4 | Chỉ người trực tiếp liên quan; không gửi cho chính người thao tác |
| Q5 | In-app + email cho `ASSIGNMENT`/`REVIEW_*` (best-effort); deadline chỉ in-app |

## 2. Quyết định kỹ thuật của plan (bạn có thể bác)

| # | Quyết định | Lý do |
|---|---|---|
| P1 | `dedupe_key` **bao gồm người nhận**: `TYPE:contentId:recipientUserId:eventKey` | Một sự kiện có nhiều người nhận (Owner+Manager, nhiều Creator) — thiếu recipient thì chỉ 1 người nhận được |
| P2 | Ghi notification là **best-effort**: `NotificationService` không ném ra ngoài, chỉ ghi log | Không làm hỏng assign/submit/decide; đồng thời an toàn khi DB chưa apply migration 09 |
| P3 | `DEADLINE_NEAR` = hạn trong **≤ 2 ngày** (hằng số `NearDays = 2`); `OVERDUE` = hạn < hôm nay (UTC) | Cần một mốc cụ thể; sửa 1 hằng số nếu bạn muốn khác |
| P4 | Chỉ hỗ trợ **chế độ API**. Ở chế độ local, icon Bell **vẫn hiện nhưng không giả vờ có dữ liệu**: hover hiện tooltip và bấm vào panel hiện lý do "Thông báo cần kết nối CreatorFlow API" (nêu rõ cần nối API), không gọi dữ liệu giả | Chưa có nguồn dữ liệu notification ở chế độ local; người dùng phải hiểu vì sao không có thông báo thay vì thấy panel trống |
| P5 | Gửi email **chỉ khi** bản ghi in-app thực sự được tạo (insert trả `true`) | Tránh email trùng lặp |
| P6 | Danh sách trả **50 bản ghi mới nhất** + `unreadCount`; không phân trang | Đủ cho panel; màn hình đầy đủ để ticket sau |
| P7 | Không sửa `database/01_schema.sql` (baseline); migration 09 là file riêng + cập nhật manifest | Đúng quy ước repo (`04..08` là file rời, không sửa baseline) |

## 3. Thiết kế luồng

```
AssignContentAsync ─┐
SubmitReviewAsync ──┤→ NotificationService (đúng người, dedupe, best-effort) → INotificationRepository → notifications
DecideReviewAsync ──┘                                                          └→ IEmailSender (nếu IsConfigured)

GET /api/notifications ─→ NotificationService.GetMineAsync
                            ├─ bảo đảm DEADLINE_NEAR/OVERDUE cho chính user (idempotent)
                            └─ đọc danh sách + đếm chưa đọc
```

## 4. Database

> **BẮT BUỘC — ghi chú migration:** phải chạy `database/09_notifications.sql` trên DB trước khi dùng tính năng, **và chạy lại sau mỗi lần file này thay đổi** (môi trường đã có dữ liệu). Chưa chạy → mọi insert notification lỗi (hiện chỉ ghi log nhờ P2) và AC "thông báo đúng người" không đạt. Migration **không được app tự chạy** — thao tác thủ công theo `docs/shared-dev-test-runbook.md`.

### 4.1 `database/09_notifications.sql` (mới)

Bắt buộc có comment đầu file (yêu cầu Q3) nêu: thêm cột gì/kiểu/vì sao, unique index nào, ảnh hưởng dữ liệu cũ, thứ tự chạy, cách rollback. Nội dung:

```sql
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS dedupe_key VARCHAR(200);
CREATE UNIQUE INDEX IF NOT EXISTS ux_notifications_dedupe_key
    ON notifications(dedupe_key) WHERE dedupe_key IS NOT NULL;
```

- Bọc `BEGIN; ... COMMIT;` + block verify như `08_platform_catalog_and_sprint.sql`.
- Dữ liệu cũ có `dedupe_key = NULL` → index có điều kiện nên không phá dữ liệu, không cần backfill.
- Rollback: `DROP INDEX`, `ALTER TABLE ... DROP COLUMN` (ghi trong comment, không tự chạy).

### 4.2 `deploy/database/schema-manifest.md` (sửa)

Thêm dòng cho `database/09_notifications.sql` (phân loại Migration, tiền đề "Sau 08", SHA256) và cập nhật thứ tự `01 → 04 → 05 → 06 → 07 → 08 → 09`. Không tự apply lên DB nào.

## 5. Server (CreatorFlow.Api + Contracts)

### 5.1 Contracts — `src/CreatorFlow.Contracts/Notifications/NotificationContracts.cs` (mới)

```csharp
public sealed record NotificationResponse(
    long NotificationId, string Type, string Title, string Message,
    bool IsRead, DateTime CreatedAt, DateTime? ReadAt,
    long? ProjectId, long? ContentId, string? ContentCode);

public sealed record NotificationsResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);
public sealed record NotificationReadResponse(long NotificationId, bool IsRead, DateTime? ReadAt);
public sealed record MarkAllReadResponse(int MarkedCount);
```

### 5.2 Models — `src/CreatorFlow.Api/Models/Notifications/NotificationModels.cs` (mới)

`NotificationRecord`, `NotificationInsert(long RecipientUserId, long? ProjectId, long? ContentId, NotificationType Type, string Title, string Message, string DedupeKey)`, `DueAssignment(long ContentId, long ProjectId, long UserId, string ContentCode, string ContentTitle, DateOnly Deadline, bool IsOverdue)`.

### 5.3 Repository — `Repositories/Notifications/{INotificationRepository,NotificationRepository}.cs` (mới)

- `InsertAsync(NotificationInsert)` → `INSERT ... ON CONFLICT (dedupe_key) DO NOTHING` + `RETURNING notification_id`; trả `bool` (có tạo hay không).
- `GetForUserAsync(userId, unreadOnly, limit)` → join `contents` để lấy `code` (`CNT-xxx`) cho `contentId`.
- `CountUnreadAsync(userId)`, `MarkReadAsync(notificationId, userId, readAtUtc)` (chỉ bản ghi của chính mình; set cả `is_read` và `read_at` để khớp CHECK), `MarkAllReadAsync(userId, readAtUtc)`.
- `GetDueAssignmentsForUserAsync(userId, today, nearDays)` → SQL dựa trên `content_assignments JOIN contents`:
  `ca.assignee_id = @user_id AND ca.status NOT IN ('COMPLETED','CANCELLED') AND c.status NOT IN ('PUBLISHED','ARCHIVED') AND COALESCE(ca.deadline, c.deadline) IS NOT NULL AND COALESCE(ca.deadline, c.deadline)::date <= @today + @near_days` (deadline riêng từng assignment ưu tiên như `GetBoardAssigneesAsync`).

### 5.4 Service — `src/CreatorFlow.Api/Services/Notifications/NotificationService.cs` (mới)

Phụ thuộc: `INotificationRepository`, `ITaskRepository` (danh sách Owner/Manager), `IUserRepository` (email người nhận), `IEmailSender`, `CurrentAuthenticatedUser`, `TimeProvider`, `ILogger`.

Đọc (endpoint):
- `GetMineAsync(unreadOnly)`: 401 nếu chưa đăng nhập → `EnsureDeadlineAsync(user)` → `GetForUserAsync` + `CountUnreadAsync`.
- `MarkReadAsync(notificationId)`: 401; không phải của mình/không tồn tại → 404.
- `MarkAllReadAsync()`: 401; trả số dòng đã đánh dấu.

Ghi (best-effort, không ném — P2):
- `NotifyAssignmentAsync(contentId, projectId, code, title, assignmentId, recipientUserId, actorUserId)`: bỏ qua nếu `recipient == actor`; `ASSIGNMENT`; key `ASSIGNMENT:{contentId}:{recipient}:{assignmentId}`; email nếu đã tạo bản ghi.
- `NotifyReviewSubmittedAsync(contentId, projectId, code, title, reviewNo, actorUserId)`: người nhận = Owner + Manager **đang hoạt động** (`GetMembersAsync`) trừ actor; `REVIEW_SUBMITTED`; key `REVIEW_SUBMITTED:{contentId}:{reviewNo}:{recipient}`.
- `NotifyReviewDecidedAsync(contentId, projectId, code, title, reviewNo, submitterUserId, approve, feedback, actorUserId)`: người nhận = submitter, trừ actor; `REVIEW_APPROVED`/`REVIEW_REJECTED` (feedback nằm trong `message`, không tạo thêm bản `FEEDBACK`); key `REVIEW_...:{contentId}:{reviewNo}:{recipient}`.
- `EnsureDeadlineAsync(user)`: nếu `OVERDUE` → key `OVERDUE:{contentId}:{user}:{yyyy-MM-dd}`; ngược lại `DEADLINE_NEAR:{contentId}:{user}:{yyyy-MM-dd}` (ngày UTC) → quét lại bao nhiêu lần cũng không thêm dòng.

### 5.5 Endpoints — `src/CreatorFlow.Api/Endpoints/NotificationEndpoints.cs` (mới)

```
GET  /api/notifications?unreadOnly={bool}
POST /api/notifications/{notificationId:long}/read
POST /api/notifications/read-all
```
Nhóm `/api` + `RequireAuthorization()`; đăng ký `AddScoped<NotificationService>` và `AddScoped<INotificationRepository, NotificationRepository>()` + `app.MapNotificationEndpoints()` trong `Program.cs`.

### 5.6 Hook vào nghiệp vụ

- `TaskService`: thêm phụ thuộc `NotificationService`; sau `ApplyAssignmentsAsync` + đọc lại assignments, gọi `NotifyAssignmentAsync` cho từng Creator **mới được thêm** (dùng `ContentCode`/`ContentTitle`/`AssignmentId` từ dữ liệu vừa đọc).
- `WorkflowService`: thêm phụ thuộc `NotificationService`.
  - `SubmitReviewAsync`: sau khi thành công → cần `reviewNo` + người gửi + code/title → thêm method `IWorkflowRepository.GetPendingReviewInfoAsync(contentId)` trả `(ReviewNo, SubmittedBy, ProjectId, ContentCode, ContentTitle)` (đọc bản Review PENDING).
  - `DecideReviewAsync`: đọc `GetPendingReviewInfoAsync` **trước** khi quyết định (sau đó Review không còn PENDING) → sau khi thành công gọi `NotifyReviewDecidedAsync`.
- Không sửa luồng quyền/validation hiện có; notification là side-effect sau khi nghiệp vụ đã thành công.

### 5.7 Email

Thêm `Task SendNotificationAsync(string email, string title, string message, CancellationToken)` vào `IEmailSender`; cập nhật 3 implementation: `SmtpEmailSender` (dùng cùng cấu hình SMTP), `UnavailableEmailSender` (ném `EmailDeliveryException` như các method khác — service không gọi khi `IsConfigured == false`), `TestEmailSender` (tests, ghi lại để assert).

## 6. Client (WinForms)

- `src/CreatorFlow/ApiClients/ApiClient.Notifications.cs` (mới): `GetNotificationsAsync(bearer, unreadOnly)`, `MarkNotificationReadAsync(id, bearer)`, `MarkAllNotificationsReadAsync(bearer)` (dùng `SendTasksAsync`).
- `src/CreatorFlow/Models/NotificationItem.cs` (mới) + `Services/Backends/INotificationBackend.cs` + `ApiNotificationBackend.cs` (mới), theo đúng khuôn `IReviewQueueBackend`/`ApiReviewQueueBackend` (đồng bộ qua `ApiTaskCall`, lỗi thành `ContentValidationException`).
- `Controls/TopHeaderControl.cs`: thêm vùng click cho icon Bell (hiện chỉ vẽ), hover, `event BellClicked`, `SetUnreadCount(int)` (vẽ badge số bằng `UITheme.Danger`), `Rectangle BellBounds` để neo panel; thêm `SetNotificationsEnabled(bool)` cho P4.
- `Controls/NotificationPanelControl.cs` (mới): panel bo góc theo `UITheme` — danh sách (title, message, thời gian `dd/MM/yyyy HH:mm`), chấm "chưa đọc", nút "Đánh dấu tất cả đã đọc", trạng thái rỗng; phát event `ItemReadRequested`/`MarkAllRequested`/`CloseRequested` (control **không** gọi API trực tiếp — đúng kiến trúc UI → Service).
- `Forms/Board/BoardForm`: tạo panel, neo dưới `BellBounds`, `BellClicked` → nạp dữ liệu qua `INotificationBackend` (bọc try/catch → toast lỗi như Review Queue), xử lý mark-read, đóng khi click ngoài (theo cách `TopHeaderControl` đang xử lý popup tìm kiếm).
- `Program.cs`: tạo `ApiNotificationBackend` (chỉ khi có API) và truyền vào `BoardForm`.
- **Chế độ local (không API) — P4:** `TopHeaderControl.SetNotificationsEnabled(false)` vẫn vẽ icon Bell nhưng: (1) tooltip "Thông báo cần kết nối CreatorFlow API", (2) bấm vào panel hiện duy nhất dòng lý do "Chế độ dữ liệu local chưa hỗ trợ thông báo. Hãy kết nối CreatorFlow API." — không hiện danh sách rỗng gây hiểu nhầm, không gọi API.

## 7. Test

| File | Nội dung |
|---|---|
| `tests/CreatorFlow.Api.Tests/Notifications/NotificationServiceTests.cs` (mới) | Đúng người: assign → assignee; submit → Owner+Manager (trừ actor); approve/reject → submitter; không gửi cho chính actor. Không trùng: gọi lại 2 lần chỉ 1 bản ghi (fake repo theo `dedupe_key`). Deadline: `OVERDUE`/`DEADLINE_NEAR` đúng ngưỡng 2 ngày, key theo ngày nên gọi lại không tăng; content Published/Archived và assignment Completed/Cancelled bị bỏ. Read/unread: mark read chỉ bản ghi của mình (404 nếu của người khác), mark all. 401 khi chưa đăng nhập. Email chỉ gửi khi tạo được bản ghi và khi `IsConfigured`. |
| `tests/CreatorFlow.Api.Tests/Api/FeatureRoutingTests.cs` (sửa) | Thêm 3 route vào `AllRoutes` (401 khi chưa đăng nhập) + test HTTP: list trả `unreadCount`/`items`, mark read đổi `isRead`, mark all. |
| `tests/CreatorFlow.Api.Tests/Tasks/TaskServiceTests.cs` (sửa) | Giao việc mới → có notification `ASSIGNMENT` cho Creator; tự nhận việc (actor == recipient) → không tạo. |
| `tests/CreatorFlow.Api.Tests/Services/WorkflowServiceTests*` (sửa) | Submit → `REVIEW_SUBMITTED` cho Owner/Manager trừ người gửi; Approve → `REVIEW_APPROVED` cho submitter; Reject → `REVIEW_REJECTED` có feedback trong message. |
| `tests/CreatorFlow.IntegrationTests/ApiClients/NotificationTests.cs` (mới) | Client backend map đúng DTO → model, gọi đúng đường dẫn/verb (theo khuôn `ContentLibraryTests`). |

Không test được: nội dung migration 09 (cần DB thật) — sẽ ghi rõ là chưa kiểm chứng.

## 8. Ngoài phạm vi

- Realtime/push, `INVITATION`/`STATUS_CHANGED`/`SYSTEM`, cấu hình bật/tắt theo người dùng, Notification Center phân trang/lọc.
- Job nền quét deadline (đã chọn sinh lười).
- Chế độ local (không API) cho notification (P4).

## 9. Kiểm chứng dự kiến

| Kiểm tra | Cách |
|---|---|
| Build | `dotnet build CreatorFlow.slnx` (cần đóng app đang chạy) |
| API tests | `dotnet test tests/CreatorFlow.Api.Tests` |
| Client tests | `dotnet test tests/CreatorFlow.IntegrationTests` |
| Migration | **Không chạy** — cần bạn duyệt target DB; chỉ review script + manifest |
| Runtime thật | Cần DB có migration 09 (local có mật khẩu, hoặc DEV sau khi deploy). Xin phép riêng theo `post-change-test-workflow` |

## 10. Rủi ro

- **Migration chưa apply ⇒ mọi insert notification lỗi cột thiếu.** Giảm nhẹ bằng P2 (best-effort, chỉ log) nhưng khi đó AC "thông báo đúng người" không đạt trên môi trường chưa migrate → **phải chạy `database/09_notifications.sql` và chạy lại mỗi khi file đổi**; nhắc trong checklist deploy.
- **Sinh lười khi đọc**: thông báo hạn chỉ xuất hiện khi người dùng mở panel; người không mở app sẽ không thấy (đã chốt).
- **`GetPendingReviewInfoAsync` thêm vào `IWorkflowRepository`** → mọi fake repository trong test phải cập nhật (nhiều file test).
- Email thêm method vào interface dùng chung (`IEmailSender`) → cập nhật 3 implementation.
- UI panel nằm trong control dùng chung (`TopHeaderControl` + `BoardForm`) → dễ ảnh hưởng màn hình khác; sẽ kiểm tra các màn hình đang dùng header (Board, Library, My Tasks, Review Queue, Admin…).

## 11. Thứ tự thực hiện sau khi plan được duyệt

1. Contracts + Models + Repository (+ fake cho test).
2. `NotificationService` (ghi best-effort + đọc + deadline lazy).
3. Hooks: `TaskService`, `WorkflowService` (+ `GetPendingReviewInfoAsync`), `IEmailSender.SendNotificationAsync`.
4. Endpoints + DI + `Program.cs`.
5. Test API (service, routing, task/workflow hooks).
6. Client: `ApiClient.Notifications`, backend, `NotificationItem`, panel, `TopHeaderControl`, `BoardForm`, `Program.cs`.
7. Test client + build; báo kết quả; dừng trước bước migration/runtime.
