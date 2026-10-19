# SCRUM-52 — Kế hoạch: Tính Engagement và tổng hợp Metrics

Trạng thái: **ĐÃ TRIỂN KHAI (chờ verify runtime AC2)**. Branch: `SCRUM-52-tinh-engagement-va-tổng-hợp-metrics`.
Người viết: agent. Ngày: 19/10/2026.

Quyết định đã chốt (theo `requirements.md`):
- AC3 làm **cả hai**: (a) tổng hợp chỉ lấy snapshot mới nhất mỗi platform (không đếm trùng);
  (b) chặn ghi lại y nguyên một snapshot.
- Có **màn tổng hợp**, nối mục nav **"Reports"** của Project.
- Views=0: backend trả **0**, UI hiển thị **—**.
- "Creator" = **người tạo Content** (`contents.created_by`).
- Engagement tính trên **tổng nhóm**, làm tròn **2 chữ số**.
- AC2: code + test bằng repository giả; **chưa có DB để verify runtime** → ghi rõ phần còn lại.

## 1. Luồng & kiến trúc

```text
WinForms (ProjectReportsControl)
   └─ IMetricBackend.GetMetricsSummary(projectId, groupBy)
        └─ ApiClient  GET /api/projects/{id}/metrics/summary?groupBy=...
             └─ MetricService.GetSummaryAsync   (quyền: thành viên Project)
                  ├─ MetricEngagement.Percent(...)  (hàm thuần, xử lý Views=0, làm tròn 2)
                  └─ IMetricRepository.GetProjectSummaryAsync(projectId, groupBy)
                       └─ SQL: WITH latest (DISTINCT ON content_platform_id ...)
                               GROUP BY <chiều> → SUM(views/likes/comments/shares)
```

Điểm quan trọng: SQL **chỉ cộng trên snapshot mới nhất của mỗi ContentPlatform** (`DISTINCT ON`),
nên nhiều lần nhập không bị đếm trùng (AC3a).

## 2. Chi tiết thay đổi

### 2.1 Contracts — `src/CreatorFlow.Contracts/Metrics/MetricsSummaryContracts.cs` (mới)
```csharp
public sealed record MetricsSummaryRow(long Key, string Name, long Views, long Likes, long Comments,
    long Shares, double EngagementPercent);
public sealed record MetricsSummaryResponse(long ProjectId, string GroupBy, IReadOnlyList<MetricsSummaryRow> Rows);
```

### 2.2 API Models — `src/CreatorFlow.Api/Models/Metrics/MetricModels.cs`
Thêm `enum MetricGroupBy { Project, Content, Creator, Platform }` và
`record MetricSummaryRecord(long Key, string Name, long Views, long Likes, long Comments, long Shares)`.

### 2.3 Repository — `src/CreatorFlow.Api/Repositories/Metrics/MetricRepository.cs`
Thêm `GetProjectSummaryAsync(projectId, MetricGroupBy, ct)`:
- CTE `latest` = `DISTINCT ON (content_platform_id)` (đúng mẫu `GetLatestAsync`).
- Nhánh theo chiều: Project (LEFT JOIN projects, luôn 1 dòng), Content (`contents.title`),
  Creator (`contents.created_by` → `users`), Platform (`platforms.name`).
- `SUM(...)::bigint`, `ORDER BY SUM(views) DESC`.

### 2.4 Service — `src/CreatorFlow.Api/Services/Metrics/MetricService.cs`
- `GetSummaryAsync(projectId, groupBy, ct)`: kiểm tra đăng nhập + thành viên; parse `groupBy`
  (mặc định `content`); gọi repository; map sang response với `MetricEngagement.Percent`.
- **AC3b**: trong `RecordAsync`, sau khi xác thực target, đọc snapshot mới nhất (`GetHistoryAsync` đầu
  danh sách); nếu giá trị sau khi gộp **y hệt** snapshot mới nhất → **không INSERT**, trả về bản mới nhất.
  (Đánh đổi: kiểm tra ở service thay vì SQL nên có khe hở nhỏ giữa đọc và ghi — chấp nhận cho POC.)

### 2.5 Hàm thuần — `src/CreatorFlow.Api/Services/Metrics/MetricEngagement.cs` (mới)
```csharp
public static double Percent(long views, long likes, long comments, long shares) =>
    views <= 0 ? 0d : Math.Round((likes + comments + shares) / (double)views * 100d, 2, MidpointRounding.AwayFromZero);
```

### 2.6 Endpoint — `src/CreatorFlow.Api/Endpoints/MetricEndpoints.cs`
`GET /api/projects/{projectId:long}/metrics/summary?groupBy=content|creator|platform|project`.

### 2.7 Client
- `Contracts` đã có ở 2.1.
- `ApiClients/ApiClient.Metrics.cs`: `GetMetricsSummaryAsync(projectId, groupBy, bearer, token)`.
- `Services/Backends/IMetricBackend.cs` + `ApiMetricBackend.cs`: `GetMetricsSummary(projectId, groupBy)`.
- `Controls/ProjectReportsControl.cs` (mới): chọn chiều (4 nút/combo) + `DataGridView` cột
  Name · Views · Likes · Comments · Shares · Engagement%; Views=0 hiển thị `—`; có trạng thái tải/lỗi.

### 2.8 Điều hướng
- `Controls/SidebarControl.cs`: thêm `event ReportsRequested`; gắn `Action` cho mục nav "Reports".
- `Forms/Board/BoardForm.cs`: lưu `_metricBackend`; gắn `ReportsRequested` (khối auth + TẠM-LOCAL);
  `OpenProjectReports()`/`CloseProjectReports()` dùng `ShowProjectPage`; thêm vào danh sách đóng trang
  (giống Dashboard/Reports khác).

### 2.9 Test (`tests/CreatorFlow.Api.Tests/Metrics/`)
- `MetricEngagementTests`: Views=0 → 0; số bình thường; làm tròn 2.
- `MetricServiceSummaryTests`: parse groupBy (mặc định + sai → 400), không phải thành viên → 403,
  map engagement đúng; fake repo mới `GetProjectSummaryAsync`.
- `MetricServiceTests`: `Record_SameValuesTwice_SkipsSecondInsert` (AC3b).
- Cập nhật `FakeMetricRepository` (thêm phương thức interface mới).

## 3. Ngoài phạm vi
Biểu đồ, lọc theo thời gian, AI Performance Analysis, xuất báo cáo, đổi công thức/quyền.

## 4. Kiểm chứng
- Build `dotnet build CreatorFlow.slnx` 0 error.
- `dotnet test` nhóm Metrics (Api.Tests) — không đụng DB/API.
- **AC2 chưa verify runtime**: cần DB dev + seed để đối chiếu truy vấn thật; nếu bạn có môi trường,
  chạy `curl` endpoint và so với `SELECT ... FROM content_metrics`.
