# SCRUM-23 — Kế hoạch triển khai: System Admin gói dịch vụ, báo cáo và thống kê cơ bản

Trạng thái: **ĐÃ TRIỂN KHAI BƯỚC 1–7; CỔNG 1 ĐẠT; CỔNG 2 CHƯA CHẠY** (xem `test-results.md`).
Ngày soạn: 30/10/2026. Dựa trên `docs/tasks/scrum-23-system-admin-goi-dich-vu-bao-cao-va-thong-ke/requirements.md`.

## 0. Bối cảnh nhánh

- Branch: `SCRUM-23-system-admin-goi-dịch-vụ-bao-cao-va-thống-kê-cơ-bản`
- Base: `cdeff78` (head của `feature/SCRUM-55-...`) → **không chứa** SCRUM-60 và commit dọn docs.
- Nhánh `feature/SCRUM-60-...` đã được commit gọn 2 commit (không push):
  - `1e40b29` SCRUM-60 (code + docs task)
  - `2b538ee` docs: xoá tài liệu cũ
- Chưa có upstream; mọi việc dưới đây ở local.

**Quyết định mặc định** (áp dụng vì người dùng đã đồng ý "làm đi" theo đề xuất trong requirements §9):

| Mã | Chốt |
| :--- | :--- |
| 6.1 | Plans: xem + tạo + sửa (giá, giới hạn, bật/tắt), không xoá |
| 6.2 | Gán gói bằng cách chọn user từ danh sách admin users (tìm theo email) |
| 6.3 | Không enforce giới hạn gói |
| 6.4 | Thống kê 5 nhóm: users, projects, contents theo status, subscriptions theo gói, reports theo status |
| 6.5 | 2 màn riêng trong nhóm ADMIN |
| 6.6 | Không migration mới |

## 1. Nguyên tắc triển khai

- Đúng luồng `UI → Facade → REST → Service → Repository → PostgreSQL`.
- Mọi endpoint mới nằm trong group `/api/admin` (đã khoá `SystemAdminOnly`) và service **tự kiểm tra lại**
  `IsSystemAdmin` như `AdminUserService` / `AdminReportService` hiện có.
- Không service admin nào gọi `GetRoleAsync` hay đọc `ProjectMembers`.
- SQL luôn parameterized; mọi thao tác nhiều bước (tạo subscription thay thế gói cũ) dùng transaction.
- Không thêm NuGet, không migration, không seed vào repo.

## 2. Các bước

### Bước 1 — Contracts (`src/CreatorFlow.Contracts/Admin/`)

Tạo `AdminPlanContracts.cs`:

- `AdminPlanResponse(long PlanId, string Code, string Name, string? Description, decimal PriceMonthly, int? MaxProjects, int? MaxMembers, int? AiRequestLimit, bool IsActive, DateTimeOffset CreatedAt)`
- `AdminPlansListResponse(IReadOnlyList<AdminPlanResponse> Items)`
- `CreatePlanRequest(string? Code, string? Name, string? Description, decimal PriceMonthly, int? MaxProjects, int? MaxMembers, int? AiRequestLimit)`
- `UpdatePlanRequest(decimal? PriceMonthly, int? MaxProjects, int? MaxMembers, int? AiRequestLimit, bool? IsActive)`
- `AdminPlanResponse` dùng lại cho kết quả create/update.

Tạo `AdminSubscriptionContracts.cs`:

- `AdminSubscriptionResponse(long SubscriptionId, long UserId, string UserEmail, long PlanId, string PlanCode, string PlanName, string Status, DateTimeOffset StartedAt, DateTimeOffset? ExpiresAt)`
- `AdminSubscriptionsListResponse(IReadOnlyList<AdminSubscriptionResponse> Items, int TotalCount)`
- `CreateSubscriptionRequest(long UserId, long PlanId, DateTimeOffset? ExpiresAt)`
- `UpdateSubscriptionRequest(string? Status, DateTimeOffset? ExpiresAt)`

Tạo `AdminStatisticsContracts.cs`:

- `AdminStatisticsResponse(AdminUserStats Users, AdminProjectStats Projects, IReadOnlyList<AdminCountByKey> ContentsByStatus, IReadOnlyList<AdminPlanUsage> SubscriptionsByPlan, IReadOnlyList<AdminCountByKey> ReportsByStatus, DateTimeOffset GeneratedAt)`
- `AdminCountByKey(string Key, int Count)`, `AdminUserStats(int Total, int Active, int Locked, int Disabled, int SystemAdmins)`,
  `AdminProjectStats(int Total, int ActiveLast30Days)`, `AdminPlanUsage(long PlanId, string Code, string Name, int Total, int Active)`.

### Bước 2 — Repository

| File | Việc |
| :--- | :--- |
| `Repositories/IPlanRepository.cs` + `PlanRepository.cs` (sửa) | Thêm `GetAllAsync`, `CreateAsync`, `UpdateAsync`; giữ `GetByIdAsync`/`InsertAsync` cũ để không phá test hiện có |
| `Repositories/Admin/ISubscriptionRepository.cs` + `SubscriptionRepository.cs` (mới) | `GetAsync(status, planId, search, limit, offset)`, `GetActiveByUserAsync(userId)`, `CreateAsync`, `UpdateAsync`, `GetByUserAndPlanAsync` |
| `Repositories/Admin/IAdminStatisticsRepository.cs` + `AdminStatisticsRepository.cs` (mới) | 5 câu aggregate, một lần mở connection; `ActiveLast30Days` = project có content Published trong 30 ngày |

Ghi chú SQL: `reports`/`contents`/`users` dùng index sẵn có; `subscriptions` thêm điều kiện `status = 'ACTIVE'`
cho ràng buộc "1 gói active / user" (kiểm tra ở service, không thêm unique index — không migration).

### Bước 3 — Service + validation

| File | Việc |
| :--- | :--- |
| `Services/Admin/AdminPlanService.cs` (mới) | `GetAsync`, `CreateAsync`, `UpdateAsync`; validate code/name bắt buộc, code ≤ 50 ký tự và không trùng (không phân biệt hoa/thường), `PriceMonthly >= 0`, `Max* >= 0` nếu có |
| `Services/Admin/AdminSubscriptionService.cs` (mới) | `GetAsync`, `CreateAsync`, `UpdateAsync`; user phải tồn tại, plan phải tồn tại và `IsActive`, mỗi user tối đa 1 ACTIVE (409), `ExpiresAt` phải sau `StartedAt` |
| `Services/Admin/AdminStatisticsService.cs` (mới) | Chỉ đọc; map record → contract; không cache |

Chuỗi thông báo lỗi bằng tiếng Việt, thống nhất với `AdminReportService` (ví dụ "Gói dịch vụ đã tồn tại.",
"Người dùng này đang có gói còn hiệu lực.").

### Bước 4 — Endpoints (`Endpoints/AdminEndpoints.cs`, sửa)

Thêm vào group đã có (không tạo group mới):

| Route | Service |
| :--- | :--- |
| `GET /api/admin/plans` | `AdminPlanService.GetAsync` |
| `POST /api/admin/plans` | `AdminPlanService.CreateAsync` |
| `PATCH /api/admin/plans/{planId:long}` | `AdminPlanService.UpdateAsync` |
| `GET /api/admin/subscriptions` | `AdminSubscriptionService.GetAsync` |
| `POST /api/admin/subscriptions` | `AdminSubscriptionService.CreateAsync` |
| `PATCH /api/admin/subscriptions/{subscriptionId:long}` | `AdminSubscriptionService.UpdateAsync` |
| `GET /api/admin/statistics` | `AdminStatisticsService.GetAsync` |

Đăng ký DI trong `src/CreatorFlow.Api/Program.cs` cạnh các service admin hiện có.

### Bước 5 — Client gọi API

| File | Việc |
| :--- | :--- |
| `src/CreatorFlow/ApiClients/ApiClient.Admin.cs` (sửa) | Thêm 7 lời gọi tương ứng, cùng kiểu trả về/`ApiException` như phần admin hiện có |
| `src/CreatorFlow/Services/AuthApiFacade.cs` (sửa) | Thêm `GetAdminPlansAsync`, `CreatePlanAsync`, `UpdatePlanAsync`, `GetAdminSubscriptionsAsync`, `CreateSubscriptionAsync`, `UpdateSubscriptionAsync`, `GetAdminStatisticsAsync`; giữ mẫu kết quả `Success/Failure` + kiểm tra `Generation` của phiên |

### Bước 6 — UI

| File | Việc |
| :--- | :--- |
| `Controls/AdminPlansControl.cs` (mới) | 2 khu: bảng Plans (code, tên, giá, giới hạn, trạng thái) và bảng Subscriptions (user, gói, trạng thái, hạn). Nút "Tạo gói", "Sửa", "Gán gói", ô tìm + lọc trạng thái, phân trang như `AdminReportsControl` |
| `Controls/AdminPlansDialogs.cs` (mới) | Dialog tạo/sửa gói và dialog gán gói (chọn user qua ô tìm email) |
| `Controls/AdminStatisticsControl.cs` (mới) | 5 khối số liệu + 3 bảng nhỏ; nút "Tải lại"; trạng thái rỗng/lỗi/đang tải |
| `Controls/SidebarControl.cs` (sửa) | Thêm 2 mục trong nhóm `ADMIN`: "Gói dịch vụ" (icon Gear/Shield có sẵn) và "Thống kê hệ thống" (icon Chart); thêm 2 event |
| `Forms/Board/BoardForm.cs` (sửa) | Mở 2 màn mới theo đúng mẫu `OpenAdminWorkspace`/`OpenAdminReports` (kiểm `IsSystemAdmin` + `CanLeaveProjectPage`) |

Dùng `UITheme`, `RoundedPanel`, `RoundedButton`, `SegmentedControl`, `DataGridView` — không tạo màu/font mới.

### Bước 7 — Test

| Loại | Nội dung |
| :--- | :--- |
| `tests/CreatorFlow.Api.Tests/Services/AdminPlanServiceTests.cs` | non-admin bị chặn; code trùng; giá âm; max âm; tạo OK; update bật/tắt |
| `tests/CreatorFlow.Api.Tests/Services/AdminSubscriptionServiceTests.cs` | user không tồn tại; plan inactive; đã có ACTIVE → 409; `ExpiresAt` sai; tạo OK; đổi trạng thái |
| `tests/CreatorFlow.Api.Tests/Services/AdminStatisticsServiceTests.cs` | map đủ 5 nhóm; dữ liệu rỗng → toàn 0, không null |
| `tests/CreatorFlow.IntegrationTests/Database/` | repository plans/subscriptions/statistics chạy trên DB thật (nếu harness sẵn có), có rollback |
| `tests/CreatorFlow.Api.Tests/...` | thêm test thiếu cho `AdminReportService` nếu có |

## 3. Cổng kiểm chứng

**Cổng 1 — tĩnh:** `dotnet build CreatorFlow.slnx` 0 error + chạy test admin vừa thêm. Trình kết quả, chờ duyệt.

**Cổng 2 — runtime (xin phép trước khi chạy):**

1. Chuẩn bị DB local: tạo 1 user System Admin (SQL tay, không commit); dùng 3 gói seed có sẵn hoặc tạo mới qua UI.
2. Đăng nhập admin → tạo 1 gói mới → kiểm `psql`: `SELECT * FROM plans`.
3. Gán gói cho 1 user → kiểm `SELECT * FROM subscriptions` (đúng user/plan/status).
4. Gán gói thứ hai cho cùng user khi đang ACTIVE → phải bị từ chối với thông báo rõ.
5. Tắt gói vừa tạo → gán gói đó cho user khác → phải bị từ chối.
6. Mở màn Thống kê → đối chiếu từng con số với `SELECT count(*)` tương ứng.
7. Gửi 1 report bằng user thường → admin xử lý → kiểm `reports.status/handled_by/handled_at`.
8. **Kiểm quyền:** user thường gọi thẳng các route `/api/admin/plans|subscriptions|statistics` → 403; admin đổi status user không làm thay đổi `project_members`.

**Bằng chứng:** ghi kết quả thật vào `docs/tasks/scrum-23-.../test-results.md`, không suy diễn.

## 4. Rủi ro và cách xử lý

| Rủi ro | Xử lý |
| :--- | :--- |
| Nhánh chứa 2 commit SCRUM-55 từ base | Nếu cần PR sạch, rebase lên `develop` trước khi mở PR — sẽ hỏi lại bạn, không tự làm |
| `PlanRepository` nằm ở namespace `CreatorFlow.Repositories` (khác các repo khác) | Giữ nguyên, chỉ thêm method; không di chuyển file |
| Đã có sẵn `subscriptions` nhưng chưa từng dùng | Không thêm unique index; ràng buộc "1 ACTIVE" kiểm ở service + transaction để tránh race |
| Màn admin mở trong `BoardForm` (ngữ cảnh Project) | Giữ mẫu hiện có, chỉ chặn bằng `IsSystemAdmin`; tách workspace riêng là task khác |
| DB local đang 0 dòng plans/subscriptions | Tạo dữ liệu qua chính UI của task này (đồng thời là bằng chứng AC1) |

## 5. Definition of Done

- 7 endpoint admin mới hoạt động, đều nằm sau policy `SystemAdminOnly` + kiểm tra trong service.
- 2 màn hình mới dùng đúng style guide, có trạng thái rỗng/lỗi.
- Build 0 error; test admin mới pass toàn bộ.
- Checklist Cổng 2 chạy hết, `test-results.md` ghi kết quả thật.
- Không có thay đổi DB schema, không seed trong git, không push.

## 6. Thứ tự thực hiện đề xuất

1. Contracts → 2. Repository → 3. Service + validation → 4. Endpoints + DI →
5. Client facade → 6. UI (Plans trước, Statistics sau) → 7. Test → **Cổng 1** → **Cổng 2**.

Mỗi bước nhỏ đều build được; không gộp bước 1–4 thành một lần sửa lớn.
