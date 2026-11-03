# SCRUM-55 — Yêu cầu: Thống kê theo Creator và Project

Trạng thái: **CHỜ DUYỆT** (chưa viết code, chưa tạo branch).
Người viết: agent. Ngày soạn: 27/10/2026.
Nguồn yêu cầu: ticket Jira SCRUM-55 (ảnh người dùng cung cấp):

> **Description:** Tổng hợp số Content hoàn thành, hiệu suất và tình trạng Published/Overdue.
> **Acceptance Criteria:**
> - Có filter Project/Creator
> - Không biến metric cá nhân thành dữ liệu global sai scope

## 1. Mục tiêu (nói thẳng)

Có một màn **thống kê** trả lời được 3 câu:

1. Trong **Project đang xem**, mỗi **Creator** đã làm xong bao nhiêu Content?
2. Hiệu suất (Views / Likes / Comments / Shares / Engagement) của từng Creator là bao nhiêu?
3. Trong số đó, bao nhiêu đã **Published**, bao nhiêu đang **Overdue**?

Và phải lọc được **theo Project** (Project hiện tại) và **theo Creator** (một người trong project),
mà không bị cộng nhầm số của người này sang người khác hoặc của project này sang project khác.

## 2. Hiện trạng đọc từ repository (đã có gì, thiếu gì)

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| SCRUM-53 `Controls/DashboardControl.cs` + `DashboardMetrics.cs` | Đã có 6 ô Total / In Progress / Waiting Review / Ready / Published / Overdue, **theo Project**, nguồn là `_allCards` của Board | Đã có phần "tình trạng" ở mức toàn Project, **không tách theo Creator** |
| SCRUM-52 `Controls/ProjectReportsControl.cs` | Màn "Báo cáo Metrics" có 4 nút Content / Creator / Platform / Project, cột Views/Likes/Comments/Shares/Engagement | Đã có "hiệu suất theo Creator" nhưng **chỉ có metrics**, không có số Content, không có Published/Overdue, **không có filter Creator** |
| `MetricService.GetSummaryAsync` + `MetricRepository.GetProjectSummaryAsync` | Đã scope theo `project_id`, đã lấy snapshot mới nhất mỗi platform | Tái dùng được công thức Engagement, không phải viết lại |
| `SidebarControl.cs:222` | Nav **"Reports"** đã bấm được → `OpenProjectReports()` | Không cần thêm nav mới nếu đặt thống kê vào Reports |
| `TaskRepository.GetMembersAsync` | Đã có danh sách thành viên active của project (user_id, display_name, role) | Dùng làm nguồn cho dropdown **Creator** |
| `database/01_schema.sql` `contents(deadline, status, created_by)`, `content_assignments(assignee_id, completed_at, status)`, `content_platforms(published_at, publication_status)` | Đủ dữ liệu để đếm hoàn thành / published / overdue | **Không cần migration, không đổi schema** |
| `Services/TaskRules.IsOverdue` + `OverdueRules.IsOverdue` | Quy tắc overdue đã có sẵn | Phải dùng lại, không tự định nghĩa mới |
| Local mode (InMemory) | Không có metrics, không có thống kê | Màn này chỉ chạy **server mode + DB** (giống SCRUM-52) |

## 3. Phạm vi đề xuất

### 3.1. Backend (CreatorFlow.Api) — nơi tính số thật

Thêm module **Statistics** (không sửa module Metrics đã chốt):

- `Repositories/Statistics/StatisticsRepository.cs` — 1 câu SQL tổng hợp theo creator, scope `c.project_id = @pid`, có thể lọc `@creatorId`:

```sql
WITH latest AS (            -- snapshot mới nhất mỗi content_platform (tái dùng đúng mẫu SCRUM-52)
    SELECT DISTINCT ON (m.content_platform_id)
           cp.content_id, cp.platform_id, c.project_id, c.created_by,
           m.views, m.likes, m.comments, m.shares
    FROM content_metrics m
    JOIN content_platforms cp ON cp.content_platform_id = m.content_platform_id
    JOIN contents c           ON c.content_id = cp.content_id
    WHERE c.project_id = @pid
    ORDER BY m.content_platform_id, m.recorded_at DESC, m.metric_id DESC
),
content_rollup AS (         -- số content theo creator: total / published / in-progress / overdue
    SELECT c.created_by,
           COUNT(*) FILTER (WHERE c.status <> 'ARCHIVED')                                    AS total,
           COUNT(*) FILTER (WHERE c.status = 'PUBLISHED')                                    AS published,
           COUNT(*) FILTER (WHERE c.status IN ('SCRIPT','PRODUCTION','EDITING','REVIEW','READY')) AS in_progress,
           COUNT(*) FILTER (WHERE c.status NOT IN ('PUBLISHED','ARCHIVED')
                              AND c.deadline < CURRENT_DATE)                                 AS overdue
    FROM contents c
    WHERE c.project_id = @pid
      AND (@creatorId IS NULL OR c.created_by = @creatorId)
    GROUP BY c.created_by
)
SELECT u.user_id, COALESCE(NULLIF(u.display_name,''), u.email),
       r.total, r.published, r.in_progress, r.overdue,
       COALESCE(SUM(l.views),0), COALESCE(SUM(l.likes),0),
       COALESCE(SUM(l.comments),0), COALESCE(SUM(l.shares),0)
FROM content_rollup r
JOIN project_members pm ON pm.user_id = r.created_by
                       AND pm.project_id = @pid AND pm.is_active
JOIN users u ON u.user_id = r.created_by
LEFT JOIN latest l ON l.created_by = r.created_by
GROUP BY u.user_id, u.display_name, u.email, r.total, r.published, r.in_progress, r.overdue
ORDER BY r.total DESC, u.user_id;
```

- `Services/Statistics/StatisticsService.cs` — kiểm tra **người gọi là thành viên active của project** (dùng `ITaskRepository.GetRoleAsync` như `MetricService`), sau đó mới trả số; map Engagement bằng `MetricEngagement.Percent` (không viết công thức mới).
- `Endpoints/StatisticsEndpoints.cs` — `GET /api/projects/{projectId}/statistics/creators?creatorId={id?}`; đăng ký trong `Program.cs`.
- Response gồm 2 phần: **project totals** (Total / In Progress / Published / Overdue / Views / Likes / Comments / Shares / Engagement) và **danh sách theo creator**.

### 3.2. Contracts

`src/CreatorFlow.Contracts/Statistics/CreatorStatisticsContracts.cs`:

```csharp
public sealed record CreatorStatisticsRow(long CreatorId, string Name, int Total, int InProgress,
    int Published, int Overdue, long Views, long Likes, long Comments, long Shares, decimal EngagementPercent);

public sealed record CreatorStatisticsResponse(long ProjectId, CreatorStatisticsRow Project,
    IReadOnlyList<CreatorStatisticsRow> Creators);
```

### 3.3. Client (WinForms)

- `ApiClients/ApiClient.Statistics.cs` + `Services/Backends/IStatisticsBackend.cs` + `ApiStatisticsBackend.cs` (cùng kiểu `ApiMetricBackend`), wire trong `Program.cs`.
- `Controls/ProjectReportsControl.cs`: thêm vùng **"Creator & Project"** (đề xuất) gồm:
  - Combo **Creator** (mặc định "Tất cả Creator", nguồn = thành viên active của project);
  - Bảng creator: Creator | Total | In Progress | Published | Overdue | Views | Likes | Comments | Shares | Engagement;
  - Dòng/tóm tắt **Project** ở trên (tổng toàn project) để thấy ngay bức tranh chung;
  - Giữ nguyên 4 nút Content / Creator / Platform / Project hiện có.
- Không thêm nav mới (Reports đã có sẵn, đúng mục UX "Creator Performance" + "Project Summary").
- Empty state rõ ràng khi chưa có metrics/no data; không hiện "0%" gây hiểu nhầm khi Views = 0 (giữ quy ước SCRUM-52: hiện "–").

### 3.4. Test

- Unit test `StatisticsService`: 
  - chỉ trả creator thuộc project; 
  - `creatorId` của người **không phải** thành viên → 403/empty, không lộ số; 
  - người không phải thành viên project → 403; 
  - engagement Views = 0 → 0 (UI hiện "–");
  - map đúng cột total/in-progress/published/overdue.
- Unit test logic đếm (nếu tách hàm thuần) cho: Archived không tính Total; overdue bỏ qua Published/Archived; deadline hôm nay không tính overdue.
- Integration test SQL nếu DB dev sẵn có (đối chiếu `COUNT` thật).

## 4. AC2 — "không biến metric cá nhân thành dữ liệu global sai scope": thiết kế chống sai

Đây là AC dễ làm sai nhất, nên chốt bằng 4 ràng buộc:

1. **Luôn bắt buộc `projectId`** lấy từ Current Project. Không có endpoint "thống kê toàn hệ thống cho creator"; không cộng dồn nhiều project.
2. **Creator filter chỉ thu hẹp trong project hiện tại** (`AND c.created_by = @creatorId`), không đổi phạm vi project.
3. **Join `project_members`**: chỉ user còn active trong project mới xuất hiện; creator đã rời project không làm row "ma".
4. **Service kiểm tra quyền trước khi trả số** (không tin UI): người gọi phải là thành viên active. Nếu chốt phương án "Creator chỉ thấy số của mình" thì Service ép `creatorId = current user` khi role = CREATOR.

## 5. Ngoài phạm vi

- Biểu đồ/charts, xu hướng theo thời gian, export Excel/PDF.
- Thống kê xuyên project (global) cho System Admin — đó là `AdminReports` (báo cáo vi phạm), khác hẳn.
- Đổi công thức Engagement, đổi quyền nhập metrics, đổi workflow/status.
- Sửa `DashboardControl` hay module Metrics đã chốt ở SCRUM-52/53.
- Migration/schema mới.

## 6. Điểm cần người dùng chốt (quan trọng)

1. **"Content hoàn thành" nghĩa là gì?**
   - **A (đề xuất):** `contents.status = PUBLISHED` (không tính ARCHIVED). Đơn giản, khớp Dashboard SCRUM-53 và luật "chỉ Published mới nhập metrics".
   - **B:** assignment đã `completed_at` (`content_assignments`). Đúng nghĩa "creator đã làm xong việc" nhưng 1 content nhiều assignee → dễ đếm trùng, phải chốt cách cộng.
2. **"Creator" là ai?**
   - **A (đề xuất):** `contents.created_by` — đúng như SCRUM-52 đang làm, số liệu khớp màn Reports hiện tại.
   - **B:** `content_assignments.assignee_id` (người được giao). Nếu chọn B phải nói rõ 1 content giao 2 người thì tính cho ai / cả hai.
3. **Quyền xem:** Owner/Manager thấy tất cả creator; **Creator chỉ thấy chính mình** (đề xuất, chặt hơn AC) hay Creator cũng thấy số của người khác trong project? (ảnh hưởng trực tiếp AC2)
4. **Đặt UI ở đâu?** Thêm vùng "Creator & Project" trong màn **Reports** hiện có (đề xuất, không thêm nav) hay tạo nav riêng "Statistics"?
5. **Có lọc theo khoảng thời gian không?** (đề xuất: không, lấy trạng thái hiện tại + snapshot mới nhất — giống SCRUM-52)
6. **Môi trường kiểm chứng:** có DB dev/docker sẵn để đối chiếu SQL thật không, hay chỉ unit test + ghi rõ AC "khớp DB" chưa verify runtime (như SCRUM-53)?

## 7. Tiêu chí hoàn thành (map AC)

- [ ] **AC "filter Project/Creator":** màn thống kê lấy đúng Project hiện tại; đổi Project → số đổi theo; chọn Creator → chỉ còn số của creator đó, tổng project vẫn đúng.
- [ ] **AC "không sai scope":** có test chứng minh creator ngoài project / project khác không lọt vào kết quả; service chặn người không phải thành viên.
- [ ] Có đủ 3 nhóm số: **số Content** (Total / In Progress / Published / Overdue), **hiệu suất** (Views/Likes/Comments/Shares/Engagement), **trạng thái** Published/Overdue.
- [ ] Số lấy từ PostgreSQL bằng SQL, không tính ở UI; Engagement dùng lại `MetricEngagement.Percent`.
- [ ] Không thêm migration; không đổi schema.
- [ ] Build `dotnet build CreatorFlow.slnx` 0 error; unit test pass; UI theo `docs/ux-ui/creatorflow-ui-styleguide.html`.

## 8. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước khi build/test/runtime phải trình checklist theo `post-change-test-workflow` và chờ duyệt; test chỉ quan sát, không sửa code. Nếu số lệch DB → quay lại `bug-investigation-workflow`.

File này chỉ kiểm tra tĩnh, không cần runtime test.

## 9. Giải thích tiếng người (để người không đọc code hiểu)

App hiện đã có 2 màn liên quan:

- **Summary** (SCRUM-53): nhìn cả project đang có bao nhiêu content, đang làm, chờ duyệt, xong, trễ. Nhưng **không biết ai làm**.
- **Reports** (SCRUM-52): nhìn hiệu suất (view/like/comment/share) theo Content, Creator, Platform, Project. Nhưng **chỉ có hiệu suất**, không có "đã xong bao nhiêu content", không lọc được một creator cụ thể.

SCRUM-55 là **ghép hai thứ đó lại, thêm bộ lọc**: một bảng cho biết **từng creator trong project** đã xong bao nhiêu content, còn bao nhiêu đang làm, bao nhiêu đã đăng, bao nhiêu trễ, kèm hiệu suất. Chọn creator ở ô lọc thì bảng chỉ còn người đó; đổi project ở sidebar thì cả bảng đổi theo project mới.

Phần "không sai scope" nghĩa là: **không được lấy số của creator ở project A cộng với project B rồi gọi là "thành tích"**, và **không để người ngoài project xem được số của người trong project**. Cách làm: mọi câu SQL đều kèm điều kiện project hiện tại + kiểm tra người xem có trong project, và chỉ lấy người còn là thành viên active.

Không phải sửa database, không thêm bảng, không đổi nghiệp vụ — chỉ thêm một câu truy vấn đọc, một API đọc, và một vùng hiển thị trong màn Reports.
