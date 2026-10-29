# SCRUM-55 — Kế hoạch: Thống kê theo Creator và Project

Trạng thái: **CHỜ DUYỆT** (chưa code, chưa tạo branch, chưa ghi DB).
Tiền đề: `requirements.md` cùng thư mục. Người dùng đã nói "oke làm", plan này chốt các điểm còn mở
**theo đề xuất trong requirements** để triển khai một mạch; nếu muốn khác thì sửa trước khi duyệt.

## 1. Quyết định chốt

| # | Điểm | Chốt |
| :--- | :--- | :--- |
| 1 | "Content hoàn thành" | `contents.status = PUBLISHED` (không tính ARCHIVED) |
| 2 | "Creator" | `contents.created_by` — cùng định nghĩa với SCRUM-52 để số khớp màn Reports hiện có |
| 3 | Quyền xem | Mọi thành viên **active** của project đều xem được (giống quyền đọc metrics hiện tại). Không thêm luật role mới; service chặn người ngoài project |
| 4 | Vị trí UI | Thêm vùng **"Creator & Project"** trong màn Reports hiện có. Không thêm nav |
| 5 | Thời gian | Không lọc theo khoảng thời gian; lấy trạng thái hiện tại + snapshot metrics mới nhất |
| 6 | API | Endpoint đọc riêng `GET /api/projects/{projectId}/statistics/creators`, không sửa endpoint metrics của SCRUM-52 |
| 7 | Schema | Không migration, không đổi bảng/cột |

## 2. Kiến trúc và file thay đổi

Luồng bắt buộc: `WinForms → Service/Backend client → REST API → Service → Repository → Npgsql → PostgreSQL`.

| Lớp | File | Nội dung |
| :--- | :--- | :--- |
| Contracts | `src/CreatorFlow.Contracts/Statistics/CreatorStatisticsContracts.cs` (mới) | `CreatorStatisticsRow`, `CreatorStatisticsResponse` |
| Model API | `src/CreatorFlow.Api/Models/Statistics/StatisticsModels.cs` (mới) | `CreatorStatisticsRecord` (record đọc từ SQL) |
| Repository | `src/CreatorFlow.Api/Repositories/Statistics/IStatisticsRepository.cs` + `StatisticsRepository.cs` (mới) | 1 câu SQL: CTE `latest` (snapshot mới nhất mỗi content_platform) + rollup `contents` theo `created_by` + join `project_members` active |
| Service | `src/CreatorFlow.Api/Services/Statistics/StatisticsService.cs` (mới) | Kiểm tra đăng nhập + role trong project (`ITaskRepository.GetRoleAsync`); map record → response; Engagement bằng `MetricEngagement.Percent` |
| Endpoint | `src/CreatorFlow.Api/Endpoints/StatisticsEndpoints.cs` (mới) + đăng ký trong `Program.cs` | `GET api/projects/{projectId}/statistics/creators?creatorId=` |
| Client API | `src/CreatorFlow/ApiClients/ApiClient.Statistics.cs` (mới) | Gọi endpoint, cùng kiểu `ApiClient.Metrics.cs` |
| Backend client | `src/CreatorFlow/Services/Backends/IStatisticsBackend.cs` + `ApiStatisticsBackend.cs` (mới) | Cùng kiểu `ApiMetricBackend` |
| Wire | `src/CreatorFlow/Program.cs` | Tạo `ApiStatisticsBackend`, truyền vào `BoardForm` |
| UI | `src/CreatorFlow/Controls/ProjectReportsControl.cs` (sửa) | Thêm nhóm "Creator & Project": combo Creator (nguồn `IAssignmentBackend.GetAssignableMembers` / members của project), bảng creator, dòng tổng Project; giữ 4 nút cũ |
| UI shell | `src/CreatorFlow/Forms/Board/BoardForm.cs` (sửa tối thiểu) | Truyền backend mới xuống `ProjectReportsControl`; giữ nguyên `OpenProjectReports` |
| Test | `tests/CreatorFlow.Api.Tests/Statistics/StatisticsServiceTests.cs` (mới) | Case quyền + scope + map cột + engagement |

Không sửa: `DashboardControl`, `MetricService`, `MetricRepository`, `TopHeaderControl`, `SidebarControl`.

### Câu SQL dự kiến (tóm tắt)

- `latest`: `DISTINCT ON (content_platform_id)` lấy snapshot mới nhất, `WHERE c.project_id = @pid`.
- `content_rollup`: `GROUP BY created_by` với `COUNT(*) FILTER (...)` cho total / published / in-progress / overdue;
  overdue = `status NOT IN ('PUBLISHED','ARCHIVED') AND deadline < CURRENT_DATE` (khớp `OverdueRules`, không có deadline thì không trễ).
- Kết quả join `project_members` (active) để loại người đã rời project; `LEFT JOIN latest` để creator chưa có metrics vẫn có dòng.
- Nếu có `@creatorId` thì thêm `AND c.created_by = @creatorId` ở rollup; **không** đổi `@pid`.
- Tham số hoá toàn bộ; không nối chuỗi SQL.

### Hợp đồng dữ liệu

```
CreatorStatisticsRow(CreatorId, Name, Total, InProgress, Published, Overdue,
                     Views, Likes, Comments, Shares, EngagementPercent)
CreatorStatisticsResponse(ProjectId, Project: CreatorStatisticsRow "Toàn project",
                          Creators: CreatorStatisticsRow[])
```

Dòng "Toàn project" tính bằng tổng các creator **trong project**, không query riêng project khác.

## 3. Các bước triển khai (theo thứ tự)

1. Contracts + model API.
2. Repository + SQL; unit-test SQL không chạy được offline → để integration test riêng.
3. Service + unit test (fake repository, giống `MetricServiceTests`).
4. Endpoint + đăng ký DI.
5. `ApiClient` + `IStatisticsBackend`/`ApiStatisticsBackend` + wire `Program.cs`/`BoardForm`.
6. UI trong `ProjectReportsControl`: combo Creator, bảng, dòng project, trạng thái rỗng/ lỗi.
7. Build `dotnet build CreatorFlow.slnx` (sau khi được duyệt test) → chạy app + data mẫu để bạn xem.

## 4. Ma trận test

| ID | Kịch bản | Kỳ vọng |
| :--- | :--- | :--- |
| S-01 | Thành viên project gọi thống kê | Thấy đủ creator + dòng project |
| S-02 | Người không thuộc project gọi | 403, không lộ số |
| S-03 | `creatorId` là người ngoài project | Không trả dòng nào (rỗng), không lỗi 500 |
| S-04 | Creator có content nhưng chưa có metrics | Dòng vẫn hiện, Views… = 0, Engagement hiện "–" ở UI |
| S-05 | Content ARCHIVED | Không tính vào Total/Published/Overdue |
| S-06 | Content Published nhưng quá deadline | Không tính Overdue |
| S-07 | Content chưa Published, deadline hôm nay | Không tính Overdue |
| S-08 | Content của project khác | Không lọt vào số (test scope, AC2) |
| S-09 | Đổi Project trên app | Bảng đổi theo project mới |
| S-10 | Chọn Creator ở combo | Chỉ còn dòng creator đó; dòng project không đổi |
| S-11 | Chọn "Tất cả Creator" | Trở lại đủ danh sách |

## 5. Demo để bạn duyệt (app + data mẫu)

App WinForms luôn chạy qua API (`Api:BaseUrl` trong `appsettings.Local.json`, hiện trỏ
`https://creatorflow-dev-api.onrender.com/`). Vì vậy "mở app + data mẫu" cần chốt **dùng DB nào**:

- **PA-A (đề xuất): Docker local** — `db` + `api` tại `localhost:5080`, áp schema vào DB dev riêng, tạo dữ liệu mẫu
  bằng tay (1 project, 3 tài khoản, ~10 content đủ trạng thái/trễ, metrics cho một số content). Cách ly hoàn toàn,
  không ảnh hưởng team. Cần: Docker Desktop đang chạy + quyền ghi DB local + đổi `Api:BaseUrl` tạm về localhost (hoặc
  dùng biến môi trường/không commit).
- **PA-B: DB dev dùng chung** (API onrender) — tạo project mẫu trên môi trường team. Nhanh nhưng **ghi vào dữ liệu
  dùng chung**, cần bạn xác nhận rõ và cần tài khoản đăng nhập.
- **PA-C: chỉ đọc dữ liệu có sẵn** trên môi trường dev dùng chung — không tạo dữ liệu; chỉ chứng minh màn hình chạy
  và số khớp bằng truy vấn đối chiếu.

Kịch bản demo (giống requirements): project "Tết 2026", 3 creator với số liệu chênh lệch rõ; sau đó lọc từng creator
và đổi project để chứng minh AC1/AC2.

## 6. Rủi ro

- **Sai định nghĩa Creator** nếu sau này team chuyển sang assignee → phải sửa SQL; hiện chốt `created_by`.
- **Creator đã rời project** vẫn là `created_by` của content: chốt loại khỏi danh sách creator nhưng **vẫn nằm trong
  tổng project** (nếu không, tổng sẽ thiếu). Cần bạn xác nhận cách này.
- **Local mode không có metrics**: màn chỉ có số khi chạy server mode; app hiện luôn server mode nên không ảnh hưởng.
- **Dữ liệu mẫu**: nếu chọn PA-B sẽ ghi vào DB dùng chung — chỉ làm khi bạn xác nhận.
- **Không tự tạo branch/commit/push**: chờ bạn chỉ định branch đích.

## 7. Cổng kiểm chứng

`requirements → duyệt → plan (file này) → duyệt → code → build/unit test → demo app + data mẫu → bạn duyệt`.
Trước khi build/chạy app/ghi dữ liệu phải theo `post-change-test-workflow` (trình checklist, chờ duyệt). Trong lúc test
chỉ quan sát, không sửa code; phát hiện lỗi thì quay lại `bug-investigation-workflow`.
