# SCRUM-23 — Yêu cầu: System Admin gói dịch vụ, báo cáo và thống kê cơ bản

Trạng thái: **BẢN NHÁP — CHỜ DUYỆT**. Chưa viết code.
Người viết: agent. Ngày soạn: 30/10/2026.
Branch: `SCRUM-23-system-admin-goi-dịch-vụ-bao-cao-va-thống-kê-cơ-bản` (tạo từ HEAD của
`feature/SCRUM-60-ai-phan-tich-hieu-suat`, **chưa push**, chưa có upstream).

Nguồn yêu cầu: người dùng cung cấp trực tiếp từ Jira (thẻ SCRUM-69 thuộc epic SCRUM-23):

> **System Admin: gói dịch vụ, báo cáo và thống kê cơ bản**
> Triển khai phạm vi demo cho plans/subscriptions, reports/violations và thống kê hệ thống.
>
> Acceptance Criteria:
> - Có dữ liệu thật từ SQL
> - Không trộn quyền System Admin với Project Role

## 1. Kết luận khảo sát: hiện tại mới đạt 1/3 phạm vi

| Phần | Trạng thái | Bằng chứng trong repo |
| :--- | :--- | :--- |
| **reports / violations** | ✅ Đã có | `ReportEndpoints` (user tự báo cáo), `AdminEndpoints` `GET/PATCH /api/admin/reports`, `AdminReportService`, `AdminReportRepository`, UI `AdminReportsControl` + mục sidebar ADMIN → Flag |
| **plans / subscriptions** | ❌ Thiếu | DB có `plans`, `subscriptions`; `PlanRepository` chỉ có `GetById/Insert` và **không có consumer** (đăng ký DI ở `Program.cs:96`). Không có endpoint `/api/admin/plans`, không contract, không service, không UI. `subscriptions` **không có repository/model/contract nào**. |
| **thống kê hệ thống** | ❌ Thiếu | `StatisticsService` (SCRUM-55) chỉ thống kê trong **một Project**. Không có endpoint/service/UI thống kê toàn hệ thống. |

`AdminEndpoints.cs` hiện chỉ có 4 route: `/users` (GET, PATCH status) và `/reports` (GET, PATCH).

## 2. Hiện trạng kỹ thuật liên quan

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| `src/CreatorFlow.Api/Endpoints/AdminEndpoints.cs:11` | Group `/api/admin` khoá bằng `RequireAuthorization("SystemAdminOnly")` | Mọi endpoint mới thêm vào group này tự động được bảo vệ |
| `src/CreatorFlow.Api/Program.cs:65-68` | Policy `SystemAdminOnly` chỉ kiểm `User.IsSystemAdmin` từ JWT | Không dính ProjectRole — đúng AC2 |
| `AdminUserService.cs:24,58`, `AdminReportService.cs:57,92` | Mỗi service tự kiểm tra lại `IsSystemAdmin` | Giữ nguyên mẫu "phòng thủ 2 lớp" cho service mới |
| `src/CreatorFlow.Api/Repositories/IPlanRepository.cs` | `IRepository<Plan, long>` trống, impl chỉ `GetByIdAsync`/`InsertAsync` | Cần bổ sung `GetAllAsync`, `UpdateAsync`; đổi namespace hiện là `CreatorFlow.Repositories` (khác các repo khác nằm trong `Repositories.Admin`) — **không đổi** để tránh lan rộng |
| `database/02_seed.sql:13-16` | Seed 3 gói FREE / PRO / TEAM | Dữ liệu mẫu có sẵn ở script, nhưng DB local đang **0 dòng** |
| `database/*.sql` — bảng `subscriptions` | `status subscription_status` (ACTIVE/EXPIRED/CANCELLED…), `started_at`, `expires_at`, check `expires_at > started_at` | Ràng buộc đã có ở DB, service chỉ cần validate thân thiện |
| `src/CreatorFlow/Controls/AdminReportsControl.cs` | Màn admin mẫu: header card + search + segmented filter + grid + pager + empty/error state | Dùng làm khuôn cho 2 màn mới, không tạo style riêng |
| `src/CreatorFlow/Forms/Board/BoardForm.cs:1044-1100` | Màn admin mở trong `BoardForm`, chặn bằng `CurrentUser.IsSystemAdmin` + `CanLeaveProjectPage()` | Thêm 2 màn theo đúng mẫu này |
| `src/CreatorFlow/Controls/SidebarControl.cs:233-251` | Nhóm menu `ADMIN` chỉ hiện khi `SetSystemAdmin(true)` | Thêm 2 mục vào đúng nhóm |
| Trạng thái hiện tại của DB local | `plans=0, subscriptions=0, reports=0, users is_system_admin=0` | **Không thể chứng minh AC1 nếu không tạo dữ liệu** — xem §7 |

## 3. Phạm vi triển khai

### 3.1. Plans (gói dịch vụ)

| Endpoint | Việc |
| :--- | :--- |
| `GET /api/admin/plans` | Danh sách toàn bộ gói (kể cả `is_active = false`) |
| `POST /api/admin/plans` | Tạo gói: `code`, `name`, `description`, `priceMonthly`, `maxProjects`, `maxMembers`, `aiRequestLimit` |
| `PATCH /api/admin/plans/{planId}` | Sửa giá/giới hạn và bật–tắt `isActive` |

Quy tắc: `code` duy nhất (không phân biệt hoa/thường), `code`/`name` bắt buộc, `priceMonthly >= 0`,
`max*` null = không giới hạn; **không cho xoá gói** (giữ lịch sử subscription) — chỉ tắt.

### 3.2. Subscriptions (gán gói cho user)

| Endpoint | Việc |
| :--- | :--- |
| `GET /api/admin/subscriptions?status=&planId=&search=` | Danh sách kèm email user + tên gói, lọc theo trạng thái/gói/từ khoá |
| `POST /api/admin/subscriptions` | Gán gói: `userId`, `planId`, `expiresAt?` |
| `PATCH /api/admin/subscriptions/{id}` | Đổi trạng thái (ACTIVE / EXPIRED / CANCELLED) hoặc gia hạn `expiresAt` |

Quy tắc: mỗi user **tối đa 1 subscription ACTIVE** (tạo mới khi đang có → 409 kèm thông báo rõ);
`expiresAt` nếu có phải sau thời điểm bắt đầu; gói `is_active = false` không gán mới được.

### 3.3. Thống kê hệ thống (chỉ đọc)

`GET /api/admin/statistics` trả một response gồm 5 nhóm, **tất cả bằng SQL aggregate**:

1. **Users:** tổng, đang Active, Locked/Disabled, số System Admin.
2. **Projects:** tổng, số project có ≥ 1 content Published trong 30 ngày.
3. **Contents:** tổng và phân bố theo `ContentStatus`.
4. **Subscriptions:** tổng theo từng gói + số đang ACTIVE.
5. **Reports:** tổng theo `ReportStatus` (Open/InReview/Resolved/Rejected).

Không tính metric Views/Likes toàn hệ thống (nặng và không cần cho "cơ bản").

### 3.4. Reports / violations

Đã có đủ luồng user → admin. Trong task này **chỉ**:

- Bổ sung test còn thiếu cho `AdminReportService` nếu danh sách hiện tại chưa phủ nhánh lỗi.
- Đối chiếu UI `AdminReportsControl` với dữ liệu thật sau khi có report trong DB.
- Không đổi contract/endpoint hiện có.

### 3.5. UI (2 màn mới trong nhóm ADMIN)

| Màn | Nội dung |
| :--- | :--- |
| **Plans & Subscriptions** | Bảng gói (code, tên, giá, giới hạn, trạng thái) + dialog tạo/sửa; bảng subscription (user, gói, trạng thái, hạn) + dialog gán gói |
| **System Statistics** | 5 khối số liệu theo §3.3, dạng card + bảng nhỏ; có nút Tải lại, trạng thái rỗng/lỗi rõ ràng |

Cả hai dùng `UITheme`, `RoundedPanel`, `RoundedButton`, `SegmentedControl`, `DataGridView` theo
`docs/ui` style hiện có; không tạo màu/font mới; không đổi nav ngoài việc thêm 2 mục vào nhóm ADMIN.

## 4. Kiến trúc và quyền

```text
WinForms UI → AuthApiFacade/AdminBackend → REST /api/admin/*  (policy SystemAdminOnly)
            → AdminPlanService / AdminSubscriptionService / AdminStatisticsService
            → Repository (Npgsql, parameterized) → PostgreSQL
```

**AC2 — không trộn quyền:**

- Mọi service admin mới kiểm `IsSystemAdmin`, **không** gọi `GetRoleAsync`/`ProjectMembers`.
- Không service Project/Content/Metrics nào được thêm nhánh `IsSystemAdmin` để bypass.
- System Admin vẫn phải có membership mới thao tác được trong Project — giữ nguyên hành vi hiện tại.
- UI chỉ là lớp hiển thị; quyền quyết định ở API (đã có policy + service check).

## 5. Ngoài phạm vi

- Thanh toán/billing, invoice, cổng thanh toán.
- **Enforce** giới hạn gói (chặn tạo project khi vượt `max_projects`, chặn AI khi hết `ai_request_limit`).
- User tự đăng ký/đổi gói; email thông báo hết hạn; cron expire subscription.
- Sửa luồng reports/violations hiện có.
- Migration mới (schema `plans`/`subscriptions` đã đủ dùng).

## 6. Acceptance Criteria → cách kiểm chứng

| AC | Cách kiểm chứng cụ thể |
| :--- | :--- |
| **Có dữ liệu thật từ SQL** | (1) Tạo admin user trên DB local, đăng nhập, tạo 1 gói mới qua UI → `SELECT * FROM plans` thấy dòng mới. (2) Gán gói cho 1 user → `SELECT * FROM subscriptions` thấy dòng mới đúng `user_id/plan_id/status`. (3) Report: user gửi report → admin thấy trong danh sách → xử lý → `SELECT status, handled_by, handled_at FROM reports` đổi đúng. (4) Màn Statistics: đối chiếu từng con số với `SELECT count(*)` tương ứng. (5) Không có mảng/danh sách giá trị cứng trong code UI (grep). |
| **Không trộn quyền System Admin với Project Role** | (1) User thường gọi thẳng `GET/POST/PATCH /api/admin/*` → 401/403. (2) System Admin **không phải** thành viên Project gọi API content/metrics của Project đó → vẫn 403 (test sẵn có + bổ sung nếu thiếu). (3) Admin đổi status user không làm thay đổi `ProjectMembers`. (4) Không có `GetRoleAsync` trong service admin (review code). |

## 7. Dữ liệu kiểm chứng (chỉ local, không commit)

- Tạo user System Admin trên DB local bằng SQL thủ công (không dùng `02_seed.sql` vì hash là placeholder).
- Tạo/gán plans & subscriptions **qua UI** của chính task này (đây cũng là bằng chứng AC1).
- Không thêm file seed, không commit script dữ liệu, không đụng DB chia sẻ.

## 8. Kiểm thử dự kiến

- **Unit test (Api.Tests, fake repository):** validate plan (code trùng, giá âm, max âm), gán subscription
  (user không tồn tại, gói inactive, đã có ACTIVE, `expiresAt` sai), statistics map số liệu, chặn non-admin.
- **Integration test (DB thật, nếu harness sẵn có):** repository plans/subscriptions/statistics.
- **Kiểm chứng tay:** checklist §6 trên Docker local.
- **Build:** `dotnet build CreatorFlow.slnx` 0 error.

## 9. Câu hỏi mở (cần bạn chốt trước khi mình viết `plan.md`)

| # | Câu hỏi | Đề xuất |
| :--- | :--- | :--- |
| 6.1 | Plans: chỉ xem + bật/tắt, hay cho tạo/sửa giá? | **Xem + tạo + sửa** (đủ để demo, không quá lớn) |
| 6.2 | Gán subscription bằng cách nào? | Chọn user từ danh sách admin users (ô tìm theo email) |
| 6.3 | Có enforce giới hạn gói không? | **Không** — task riêng |
| 6.4 | Statistics gồm những gì? | 5 nhóm ở §3.3 |
| 6.5 | UI: 2 màn riêng hay 1 màn có tab? | **2 màn riêng** trong nhóm ADMIN |
| 6.6 | Dùng migration mới không? | **Không**, schema đã đủ |
| 6.7 | 44 thay đổi chưa commit (SCRUM-60 + xoá docs) đang nằm trên nhánh này — xử lý thế nào? | **Commit SCRUM-60 trên nhánh SCRUM-60 trước**, rồi nhánh này mới sạch; nếu không, mọi commit của SCRUM-23 sẽ lẫn nội dung SCRUM-60 |

## 10. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. File này chỉ kiểm tra tĩnh.
Sau khi bạn duyệt mục §9, mình viết `plan.md` (chia bước nhỏ, điểm dừng kiểm chứng). Trước khi build/chạy
app phải trình checklist theo `post-change-test-workflow` và chờ duyệt.
