# SCRUM-50 — Notification nghiệp vụ

Trạng thái: **CHỜ DUYỆT requirements** (chưa viết plan, chưa code).

Branch: `SCRUM-50-notification-nghiệp-vụ` (tách từ `develop` `b76215d`, đã gồm SCRUM-47).

## 1. Nội dung ticket

- **In Progress / Improve Story:** Notification nghiệp vụ.
- **Description:** Thông báo khi assign, submit review, reject/feedback, approve và deadline.
- **Acceptance Criteria:**
  1. Thông báo đúng người.
  2. Có read/unread nếu triển khai.
  3. Không tạo trùng vô hạn.

## 2. Hiện trạng (bằng chứng trong code)

| Hạng mục | Hiện tại | Ghi chú |
|---|---|---|
| Bảng `notifications` | Có từ `database/01_schema.sql`: `recipient_user_id`, `project_id`, `content_id`, `type`, `title`, `message`, `is_read`, `created_at`, `read_at` + CHECK `is_read`/`read_at` khớp nhau | Chưa có ràng buộc chống trùng |
| `notification_type` (DB) | `ASSIGNMENT, REVIEW_SUBMITTED, REVIEW_APPROVED, REVIEW_REJECTED, FEEDBACK, STATUS_CHANGED, DEADLINE_NEAR, OVERDUE, INVITATION, SYSTEM` | Khớp đúng enum client |
| Index | `ix_notifications_recipient_read(recipient_user_id, is_read, created_at DESC)` | Đủ cho truy vấn "của tôi, chưa đọc" |
| `NotificationType` (client) | Có enum + `PostgresEnumMapper` map sẵn | Chưa ai dùng |
| API | **Không có** repository/service/endpoint notification nào (grep `Notification` trong `src/CreatorFlow.Api` = 0) | Phải làm mới toàn bộ |
| UI | `TopHeaderControl` vẽ icon Bell (L544) nhưng **không có vùng click/handler**; không có màn hình/panel thông báo | Chưa có chỗ hiển thị |
| Điểm phát sinh sự kiện | `TaskService.AssignContentAsync` (thêm/hủy assignment, đổi deadline), `WorkflowService.SubmitReviewAsync`, `WorkflowService.DecideReviewAsync` | Ghi qua Npgsql có transaction |
| Deadline | TaskService chỉ **validate** deadline; **không có** `BackgroundService`/scheduler nào trong API (grep = 0) | Chưa có cơ chế phát thông báo deadline |
| Dữ liệu overdue | SCRUM-47 đã có `GET /api/projects/{id}/overdue` (kèm assignees) | Có thể là nguồn cho `OVERDUE` |
| Migration | Có pattern `database/04..08_*.sql` | Thêm `09_*.sql` là phù hợp |

## 3. Mục tiêu

1. Sinh notification **đúng người** cho 5 nhóm sự kiện: assign, submit review, reject/feedback, approve, deadline.
2. Lưu vào bảng `notifications` (in-app), có read/unread.
3. **Chống trùng**: cùng một sự kiện cho cùng một người chỉ tạo một bản ghi, kể cả khi luồng bị gọi lại.

## 4. Sự kiện → người nhận (đề xuất, chờ chốt)

| Sự kiện | `type` | Người nhận | Điểm phát |
|---|---|---|---|
| Giao việc (assignment mới) | `ASSIGNMENT` | Creator được giao; **không** gửi khi người giao tự giao cho chính mình | `TaskService.AssignContentAsync` |
| Gửi duyệt | `REVIEW_SUBMITTED` | Owner + Manager đang hoạt động của Project (trừ người gửi) | `WorkflowService.SubmitReviewAsync` |
| Duyệt | `REVIEW_APPROVED` | Người đã gửi Review (`reviews.submitted_by`) | `WorkflowService.DecideReviewAsync` |
| Từ chối | `REVIEW_REJECTED` | Người đã gửi Review | `WorkflowService.DecideReviewAsync` |
| Sắp tới hạn | `DEADLINE_NEAR` | Creator được giao (assignment chưa Completed/Cancelled) | Sinh lười khi đọc (Q1) |
| Quá hạn | `OVERDUE` | Creator được giao | Sinh lười khi đọc (Q1) |

Đã chốt Q4: chỉ người trực tiếp liên quan; người thao tác không tự nhận thông báo của chính mình.

Ghi chú: `FEEDBACK` là type riêng trong enum, nhưng reject bắt buộc có feedback → đề xuất **một** bản ghi `REVIEW_REJECTED` với nội dung feedback trong `message`, tránh 2 thông báo cho cùng một sự kiện.

## 5. Chống trùng (đã chốt: cột `dedupe_key` + unique index)

Thêm cột `dedupe_key VARCHAR(200)` + **unique index có điều kiện** (`WHERE dedupe_key IS NOT NULL`, để không phá dữ liệu cũ), ghi bằng `ON CONFLICT (dedupe_key) DO NOTHING`:

- `ASSIGNMENT:{contentId}:{userId}:{assignmentId}` — giao lại sau khi cancel vẫn thông báo (assignment mới).
- `REVIEW_SUBMITTED:{contentId}:{reviewNo}`; `REVIEW_APPROVED|REVIEW_REJECTED:{contentId}:{reviewNo}` — mỗi vòng Review chỉ 1 thông báo/người.
- `DEADLINE_NEAR|OVERDUE:{contentId}:{userId}:{yyyy-MM-dd}` — tối đa 1 thông báo/ngày/người/content, không lặp vô hạn khi quét lại.

Yêu cầu riêng của bạn (Q3): **mọi thay đổi schema phải được ghi lại bằng comment trong file migration** — file `database/09_notifications.sql` phải có comment đầu file nêu rõ: thêm cột gì, kiểu gì, vì sao, unique index nào, có ảnh hưởng dữ liệu cũ không, và cách chạy/rollback.

## 6. Phạm vi dự kiến

### Trong phạm vi

- Migration `database/09_notifications.sql`: cột `dedupe_key` + unique index (theo Q3).
- `INotificationRepository` + `NotificationRepository` (Npgsql): insert bỏ qua trùng, liệt kê của tôi, đếm chưa đọc, đánh dấu đã đọc / tất cả đã đọc.
- `NotificationService`: xác định người nhận + nội dung + gọi repository, kiểm tra quyền (chỉ đọc/sửa thông báo của chính mình).
- Contracts + endpoints: `GET /api/notifications` (lọc `unreadOnly`), `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`.
- Hooks sinh thông báo trong `TaskService.AssignContentAsync`, `WorkflowService.SubmitReviewAsync`, `WorkflowService.DecideReviewAsync`; deadline theo Q1.
- Test: service (đúng người, không trùng khi gọi lại, không gửi cho chính actor), routing 401/403, migration không phá dữ liệu cũ.
- UI (Q2 đã chốt có làm): nối icon Bell trong `TopHeaderControl` (hiện chỉ vẽ, chưa có vùng click) → panel danh sách thông báo + badge số chưa đọc + đánh dấu đã đọc/đã đọc tất cả; phía client thêm `ApiClient.Notification` + backend tương ứng.
- Email (Q5 đã chốt): gửi kèm email cho `ASSIGNMENT` và `REVIEW_*` qua `IEmailSender`; **best-effort**, không làm hỏng nghiệp vụ khi SMTP chưa cấu hình (khác `InvitationService` vốn trả 503). `DEADLINE_NEAR`/`OVERDUE` chỉ in-app.

### Ngoài phạm vi (mặc định)

- Realtime/push (SignalR, WebSocket), thông báo đẩy.
- Type ngoài phạm vi ticket (`INVITATION`, `STATUS_CHANGED`, `SYSTEM`) và cấu hình bật/tắt theo người dùng.
- Màn hình "Notification Center" đầy đủ (phân trang, lọc theo project) nếu Q2 chỉ chọn panel đơn giản.

## 7. Quyết định đã chốt

| # | Nội dung | Quyết định |
|---|---|---|
| Q1 | Cách phát thông báo deadline | **Sinh lười khi đọc** (idempotent nhờ `dedupe_key`), không thêm job nền |
| Q2 | Phạm vi UI | **Có làm panel UI trên icon Bell** trong ticket này |
| Q3 | Chống trùng | **`dedupe_key` + unique index** trong `database/09_notifications.sql`, và **file migration phải ghi rõ thay đổi schema bằng comment** |
| Q4 | Người nhận | **Chỉ người trực tiếp liên quan**; không gửi cho chính người thao tác |
| Q5 | Kênh gửi | **In-app + email** cho `ASSIGNMENT`/`REVIEW_*` (best-effort); deadline chỉ in-app |

Bổ sung sau khi duyệt plan:
- **Chế độ local (không API):** icon Bell vẫn hiển thị nhưng phải nói rõ **lý do** không dùng được — tooltip và nội dung panel ghi "cần kết nối CreatorFlow API", không hiện danh sách rỗng.
- **Migration:** phải chạy `database/09_notifications.sql` trên DB và **chạy lại mỗi khi file đổi**; không có migration thì notification chỉ được ghi log và tính năng coi như chưa hoạt động.

## 8. Rủi ro / lưu ý

- **Deadline** là phần khó nhất: không có scheduler, Render free tier hay sleep → phương án "sinh lười khi đọc" (Q1a) là thực tế nhất nhưng nghĩa là thông báo chỉ xuất hiện khi người dùng mở app.
- **Migration mới**: phải chạy `database/09_notifications.sql` trên DB DEV; nếu không, mọi insert notification sẽ lỗi cột không tồn tại.
- **Hook trong luồng nghiệp vụ**: nếu sinh thông báo trong transaction của repository thì nguyên tử nhưng phải sửa repository; nếu sinh sau khi repository trả về thì đơn giản hơn nhưng có thể mất thông báo khi lỗi giữa chừng (không tạo trùng, chỉ thiếu). Chọn ở bước plan.
- `notifications.content_id` là `ON DELETE CASCADE` → xoá Content sẽ xoá thông báo liên quan (chấp nhận).
- Thông báo cho Owner/Manager chỉ gửi cho `project_members.is_active = TRUE`.
- **Email best-effort**: DEV chưa cấu hình SMTP (`IEmailSender.IsConfigured = false`) → email bị bỏ qua và chỉ ghi log; thông báo in-app vẫn được tạo, nghiệp vụ không bị chặn.
- **UI Bell**: `TopHeaderControl` hiện chỉ vẽ icon, chưa có vùng click; thêm panel nghĩa là thêm state/handler vào control dùng chung. AGENTS yêu cầu đọc `docs/ui/creatorflow-ui-styleguide.html` trước khi thiết kế UI, nhưng **file này không tồn tại trong repo** (đã kiểm tra) — sẽ bám theo `src/CreatorFlow/Theme/UITheme.cs` và các control lân cận, đồng thời báo lại phần thiếu này thay vì tự tạo style mới.
