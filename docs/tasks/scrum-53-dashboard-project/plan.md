# SCRUM-53 — Kế hoạch: Dashboard Project (6 chỉ số)

Trạng thái: **ĐÃ DUYỆT** — người dùng chốt AC1 **phương án A** (dashboard đứng yên, tự cập nhật khi đổi Project) ngày 19/10/2026. Branch: `SCRUM-53-dashboard-project`.
Tiền đề: `requirements.md` cùng thư mục. Người dùng đã "làm đi theo đề xuất" và nhắc tuân thủ quy tắc dự án.

## 1. Quyết định đã chốt (theo đề xuất trong requirements)

| # | Điểm | Chốt |
| :--- | :--- | :--- |
| 1 | **In Progress** | `Production` + `Editing` |
| 2 | **Total** | Tất cả content của project, **loại Archived** (đồng bộ Board) |
| 3 | **Waiting Review** | Đếm theo `Status == Review` |
| 4 | **Nguồn dữ liệu** | Tái dùng nguồn card của Board/Calendar (`_allCards`): local `IBoardRepository.GetBoardCards`, server `IBoardBackend.GetBoardCardsAsync`. **Không endpoint mới, không migration** |
| 5 | **Tên & vị trí** | Nav "Summary" → mở `DashboardControl`, tiêu đề "Tổng quan Project"; giữ nguyên tên nav |
| 6 | **Điểm đầu vào** | Nối nav **"Summary"** ở sidebar (đang chưa có Action) |

## 2. Kiến trúc & file thay đổi

### 2.1. Logic đếm thuần (unit test được)

Tạo **`src/CreatorFlow/Controls/DashboardMetrics.cs`** (static, không chạm UI/DB, giống `CalendarDateLogic`):

```csharp
public sealed record DashboardCounts(int Total, int InProgress, int WaitingReview, int Ready, int Published, int Overdue);

public static class DashboardMetrics
{
    public static DashboardCounts From(IEnumerable<ContentBoardCard> cards, DateTime today);
}
```

Quy tắc:
- `Total` = số card.
- `InProgress` = `Production` + `Editing`.
- `WaitingReview` = `Review`.
- `Ready` = `Ready`.
- `Published` = `Published`.
- `Overdue` = card **chưa Published** và `TaskRules.IsOverdue(Deadline, isFinished: Status == Published, today)` — tái dùng `ContentBoardCard.IsOverdue` để không lệch với Board.

### 2.2. Màn Dashboard

Tạo **`src/CreatorFlow/Controls/DashboardControl.cs`** (UserControl, dựng layout bằng code như `ModulePageHeaderControl`):
- Tiêu đề "Tổng quan Project" + mô tả ngắn (theo Page Header §26 style guide).
- Lưới **6 thẻ chỉ số** (card nền trắng, viền `#E5E5E5`, bo 12px, số lớn + nhãn). Tên nhãn tiếng Anh theo yêu cầu: Total / In Progress / Waiting Review / Ready / Published / Overdue.
- API tối thiểu: `Func<List<ContentBoardCard>?>? CardProvider`, `void RefreshData()`, `string? UnavailableReason` (giống `CalendarControl`) để BoardForm nạp dữ liệu và tái dùng khi project đổi.

### 2.3. Nối vào shell

- **`Controls/SidebarControl.cs`**: thêm `event EventHandler? SummaryRequested;`; gắn `Action` cho item "Summary" (dòng 213) và `AlwaysEnabled = true` (giống Calendar).
- **`Forms/Board/BoardForm.cs`**:
  - Thêm field `DashboardControl? _dashboard` và method `OpenDashboard()` / `CloseDashboard()` (dập khuôn `OpenCalendar`/`CloseCalendar`): tạo control, `CardProvider = () => HasProjectContext ? _allCards : null`, `SetActiveMenu("Summary")`, ẩn board.
  - Wire `_sidebarControl.SummaryRequested += (_, _) => OpenDashboard();` (kể cả khi local không auth — theo `TẠM-LOCAL` sẵn có nếu cần).
  - `UnavailableReason` khi `_projects is not null && _boardBackend is null` (giống Calendar).
- **`Forms/Board/BoardForm.Projects.cs`**: nối `_dashboard?.RefreshData()` vào các chỗ `_allCards` đổi (local `ReloadBoard`, server `LoadServerBoardAsync`), giống `_calendar?.RefreshData()`.

### 2.4. AC1 — "Dashboard đổi theo Current Project"

Giữ dashboard **ở lại và cập nhật trực tiếp** khi đổi project (không đóng như Calendar), để đúng AC:

1. `ResetProjectState()` (đang `CloseCalendar()`) **không đóng** `_dashboard`; hiện trạng "đang tải" trong lúc chờ.
2. **Local**: `ReloadBoard()` set `_allCards` đồng bộ → gọi `_dashboard?.RefreshData()` cuối hàm.
3. **Server**: cho phép `LoadServerBoardAsync` chạy khi dashboard đang mở (mở điều kiện `Valid()`/`RenderServerWorkspace` đang chặn theo `_calendar`...), tải xong `_allCards` → `_dashboard?.RefreshData()`.
4. `SelectProjectCoreAsync` (dòng 228) hiện `CloseCalendar()` — bổ sung nhánh giữ dashboard.

> Nếu bạn thấy việc "giữ dashboard mở khi đổi project" phức tạp/không cần, phương án thay thế là dập khuôn Calendar (đóng khi đổi project, mở lại xem số mới). Ghi rõ ở mục 6 để bạn chọn.

## 3. Các bước triển khai

1. `DashboardMetrics` + unit test (mục 4). Build.
2. `DashboardControl` (UI 6 thẻ, style guide).
3. Nối sidebar (`SummaryRequested`) + `OpenDashboard`/`CloseDashboard` trong BoardForm.
4. AC1: refresh theo project (mục 2.4).
5. Test: unit test → chạy local (reuse `TẠM-LOCAL`) → so DB (mục 5).
6. Không đụng schema/migration/endpoint.

## 4. Ma trận test

| ID | Kịch bản | Kỳ vọng |
| :--- | :--- | :--- |
| D-01 | Unit `DashboardMetrics.From` với fixture đủ status | 6 số đúng từng buckets |
| D-02 | Overdue: hiện tại/quá hạn/đúng hạn, Published | Chỉ đếm chưa Published và deadline < hôm nay |
| D-03 | Danh sách rỗng | Cả 6 số = 0 |
| D-04 | Mở Summary từ sidebar | Mở đúng màn, không lỗi, đóng/quay lại như Calendar |
| D-05 | Đổi Current Project khi đang mở Dashboard | 6 số đổi theo project mới (AC1) |
| D-06 | Đối chiếu số với DB | Mỗi chỉ số khớp `COUNT` theo status/overdue trên DB test (AC2) |
| D-07 | Server mode thiếu nguồn (boardBackend null) | Empty/unavailable state rõ ràng, không vỡ |

## 5. Kiểm chứng & evidence

- Unit test chạy offline (không DB).
- **AC2 "khớp DB"**: chạy app (local InMemory để test nhanh logic; nếu cần khớp DB thật thì chỉ API Dev/DB test), và chạy câu SQL đếm theo status/overdue để so từng số. Ghi evidence (ảnh/số) — chỉ quan sát.
- Cổng duyệt `post-change-test-workflow`: trình checklist và chờ duyệt trước khi build/test/runtime.

## 6. Rủi ro & điểm cần bạn xác nhận

- **AC1 giữ dashboard mở khi đổi project** (mục 2.4) là phần "đụng" nhiều nhất vào BoardForm. Nếu muốn tối giản, chuyển sang phương án "đóng khi đổi project" như Calendar.
- **Overdue** tái dùng `ContentBoardCard.IsOverdue` (chỉ coi `Published` là xong; `Archived` không có trong `_allCards`) — thống nhất với Board nhưng khác một chút với ghi chú Calendar cũ (Published/Archived). Xác nhận dùng theo Board.
- Không được phép nạp dữ liệu DB thật nếu chưa xác nhận môi trường.
- Branch hiện tại còn chứa thay đổi chưa commit của SCRUM-49 (do tạo nhánh mà không commit). Không đụng tới các file đó ngoài phạm vi.

---

## 7. Giải thích tiếng người

Chuyện này nhỏ: app đã có sẵn một mục ở thanh bên tên **"Summary"** nhưng bấm vào chưa ra gì. Mình sẽ **làm cho nó mở ra một trang tổng quan** của dự án đang xem, trên đó có **6 ô số**:

- **Tổng** có bao nhiêu nội dung.
- **Đang làm** (đang sản xuất hoặc đang dựng/chỉnh).
- **Chờ duyệt**.
- **Sẵn sàng** để đăng.
- **Đã đăng**.
- **Trễ hạn**.

Các con số này **không phải bịa ra**: chúng đếm từ đúng danh sách nội dung mà app đang dùng để vẽ bảng Board và màn Lịch — nên nó **khớp với dữ liệu trong database**. Đổi sang dự án khác thì 6 ô số **đổi theo dự án đó**.

Việc chính gồm: (1) viết một hàm đếm nhỏ, gọn để kiểm thử được; (2) làm trang tổng quan với 6 ô số cho đẹp đúng chuẩn giao diện; (3) nối cái mục "Summary" cho bấm được; (4) bảo đảm khi đổi dự án thì số liệu nhảy theo.

Không phải sửa database, không thêm gì bên máy chủ, không đổi nghiệp vụ. Chỉ là "đếm cho đúng rồi bày lên cho gọn".

**Cần bạn xác nhận 1 điểm** (mục 6): khi đang mở trang Tổng quan mà đổi dự án, bạn muốn **trang đứng yên và số tự cập nhật** (đúng tinh thần AC, mình nghiêng phương án này, hơi đụng nhiều code hơn), hay **tự quay về Board rồi bấm Summary lại** (đơn giản hơn, giống màn Lịch hiện tại)?
