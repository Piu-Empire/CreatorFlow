# SCRUM-53 — Yêu cầu: Dashboard Project (Total / In Progress / Waiting Review / Ready / Published / Overdue)

Trạng thái: **ĐÃ DUYỆT** (đã chốt theo đề xuất + AC1 phương án A). Branch: `SCRUM-53-dashboard-project`.
Người viết: agent. Ngày soạn: 19/10/2026.
Nguồn yêu cầu: người dùng cung cấp trực tiếp:

> Nhiệm vụ: Hiển thị Total, In Progress, Waiting Review, Ready, Published, Overdue.
> AC: (1) Dashboard đổi theo Current Project; (2) Số liệu khớp DB.

## 1. Mục tiêu

Thêm màn **Dashboard (Tổng quan Project)** hiển thị 6 chỉ số đếm content của **Project đang mở**:
**Total, In Progress, Waiting Review, Ready, Published, Overdue**. Số liệu là số thật lấy từ dữ liệu
content của project, cập nhật khi người dùng đổi Current Project.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| `Controls/SidebarControl.cs:213` | Nav item **"Summary"** (`NavIcon.Summary`) **chưa gắn `Action`** | Điểm vào đã có sẵn nhưng bấm không làm gì → cần nối |
| `docs/ux-ui/CreatorFlow_UX.md:60-74` | Mục **"Summary (Tổng quan Project)"** liệt kê đúng: Tổng số Content, đang thực hiện, đang REVIEW, READY, PUBLISHED, Overdue (+ một số thứ khác) | 6 chỉ số của task khớp tài liệu UX; phần còn lại của UX ngoài phạm vi |
| `src/CreatorFlow/Models/Enums/ContentStatus.cs` | `Idea, Script, Production, Editing, Review, Ready, Published, Archived` | Nguồn để map chỉ số |
| `Forms/Board/BoardForm.cs:274-276` (local) | `_allCards = _boardRepository.GetBoardCards(projectId)`; đã tính `overdue` | Có sẵn dữ liệu content theo project |
| `Forms/Board/BoardForm.Projects.cs:341-373` (server) | `_allCards = await _boardBackend.GetBoardCardsAsync(projectId)` | Cùng nguồn, khác kênh |
| `Services/Backends/IBoardBackend.cs` + `ApiBoardBackend.cs` | `GetBoardCardsAsync(projectId)` gọi `GET /api/projects/{id}/board` | Tái dùng được, không cần endpoint mới |
| `CreatorFlow.Api/Services/Board/BoardService.cs:16-31` | Board (`/board`) **loại Archived** (`GetCardsAsync`); Content Library mới gồm Archived (`GetProjectContentsAsync`) | `_allCards` = content **không Archived** của project |
| `Models/ContentBoardCard.cs:46-47` | `IsOverdue => TaskRules.IsOverdue(Deadline, Status == Published, Today)` | Quy tắc Overdue đã có sẵn, dùng lại để không lệch |
| `Controls/TopHeaderControl.cs:28` | Header đang hiện `{total} items · {overdue} overdue` | Đã có 2/6 chỉ số dạng thô; Dashboard là màn riêng, không thay header |
| `Forms/Board/BoardForm.cs:806` `OpenCalendar` | Màn Lịch đọc chung `_allCards` qua `CardProvider` | Mẫu kiến trúc để Dashboard tái dùng (không tự query DB) |

## 3. Phạm vi

1. **Điểm vào/ra:** nối nav **"Summary"** ở sidebar (thêm `SummaryRequested`) → mở `DashboardControl` trong vùng nội dung của `BoardForm`; có nút đóng/quay lại như các module khác (Board/Calendar).
2. **Nội dung:** đúng **6 thẻ chỉ số**: Total, In Progress, Waiting Review, Ready, Published, Overdue.
3. **Theo Current Project:** dashboard lấy số liệu của `CurrentSession.CurrentProjectId`; đổi project (ProjectSwitcher) → dashboard nạp lại đúng project mới.
4. **Nguồn dữ liệu:** tái dùng nguồn card theo project mà Board/Calendar đang dùng (local: `IBoardRepository.GetBoardCards`; server: `IBoardBackend.GetBoardCardsAsync`) → **không thêm endpoint, không migration, không đổi schema**, số liệu khớp DB vì cùng truy vấn.
5. **Logic đếm tách thuần** (một hàm static nhận danh sách card + ngày) để unit test được, không cần UI/DB.
6. **UI theo style guide:** thẻ chỉ số dạng card (nền trắng, viền `#E5E5E5`, bo 12px), theo `docs/ux-ui/creatorflow-ui-styleguide.html`.

## 4. Ngoài phạm vi

- Các phần khác của UX "Summary": Upcoming Deadlines, Recent Activity, Team workload, Quick actions (không được yêu cầu trong task này).
- Biểu đồ/charts, Creator Performance, Platform Performance.
- Endpoint tổng hợp phía server; realtime; notification; đổi nghiệp vụ workflow/status.
- Không sửa `TopHeaderControl`, sidebar ngoài việc nối item "Summary".

## 5. Định nghĩa 6 chỉ số (đề xuất — cần chốt)

Nguồn: `_allCards` của project (đã **loại Archived**, khớp Board).

| Chỉ số | Đề xuất | Ghi chú |
| :--- | :--- | :--- |
| **Total** | Số card của project (không tính Archived) | Bằng `_allCards.Count` — khớp Board |
| **In Progress** | `Production` + `Editing` | "Đang thực hiện" (đang làm nội dung). **Cần chốt**: có gộp `Script` không? |
| **Waiting Review** | `Review` | Có thể dùng `HasPendingReview`; mặc định đếm theo `Status == Review` |
| **Ready** | `Ready` | |
| **Published** | `Published` | |
| **Overdue** | Card chưa Published mà `Deadline < hôm nay` (dùng `ContentBoardCard.IsOverdue`) | Đếm trên mọi status (trừ Archived); Published không tính |

## 6. Giả định và quyết định cần bạn chốt

1. **"In Progress" gồm status nào?** Đề xuất `Production + Editing`. Phương án khác: `Script + Production + Editing`.
2. **Total có tính Archived không?** Đề xuất **không** (đồng bộ với Board, vì board không có cột Archived). Nếu muốn tính cả Archived thì phải lấy nguồn khác (Content Library / `GetProjectContentsAsync`).
3. **Waiting Review** đếm theo `Status == Review` (đề xuất) hay theo cờ `HasPendingReview` (nội dung đã resubmit)? Hai cách có thể lệch nhau.
4. **Nguồn dữ liệu:** tái dùng card của Board (đề xuất, nhẹ, khớp DB) hay bắt buộc một **endpoint tổng hợp mới** ở server để DB tự `COUNT`.
5. **Tên màn hình:** nav sidebar là "Summary"; task gọi "Dashboard". Đề xuất control `DashboardControl`, tiêu đề hiển thị "Tổng quan Project", **giữ nguyên** tên nav "Summary" (không đổi điều hướng khác).
6. **Môi trường test:** giống SCRUM-49 — có hỗ trợ **local mode (InMemory)** để test offline không, hay test trên server mode/DB thật?

## 7. Tiêu chí hoàn thành (Acceptance) và evidence

- [ ] **AC1 — Dashboard đổi theo Current Project:** đổi project ở ProjectSwitcher → 6 chỉ số đổi đúng theo project đó (không giữ số cũ).
- [ ] **AC2 — Số liệu khớp DB:** mỗi chỉ số khớp số content trong DB của project. Evidence: đối chiếu với `_allCards` (nguồn Board, cùng truy vấn DB) và/hoặc chạy 1 câu SQL `COUNT` theo status/overdue trên DB test rồi so.
- [ ] **Unit test** hàm đếm thuần: từng chỉ số, kết hợp, rỗng, biên Overdue (đúng hôm nay/quá hạn/Published).
- [ ] **UI** đúng style guide; không tự đổi điều hướng/tên nav khác.
- [ ] Build `dotnet build CreatorFlow.slnx` 0 error; không đổi schema/migration.

## 8. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước build/test/runtime phải trình
checklist theo `post-change-test-workflow` và chờ duyệt. Test chỉ quan sát, không sửa code. Nếu số liệu lệch DB → quay lại `bug-investigation-workflow`.

File này chỉ kiểm tra tĩnh, không cần runtime test.

## 9. Câu hỏi mở cho người dùng

1. Chốt định nghĩa **In Progress** (mục 6.1)?
2. **Total** loại Archived — đồng ý không (6.2)?
3. **Waiting Review** theo `Status == Review` hay `HasPendingReview` (6.3)?
4. Nguồn: tái dùng card Board, hay cần endpoint tổng hợp server (6.4)?
5. Test bằng **local mode** hay server/DB thật (6.6)?
