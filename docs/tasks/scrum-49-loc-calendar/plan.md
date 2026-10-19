# SCRUM-49 — Kế hoạch: Lọc lịch theo Project, Creator, Platform và Status

Trạng thái: **ĐÃ CHỐT — đang triển khai**. Branch: `SCRUM-49-lọc-calendar`.
Người viết: agent. Ngày soạn: 19/10/2026.
Quyết định đã chốt: (UI) dùng nút **"Bộ lọc" mở panel** (popover) chứa Project/Creator/Platform/Status + thêm **ô Search**
theo Page Header chuẩn; (Môi trường) test bằng **local mode** (InMemory), Calendar đọc chung `_allCards` của Board nên
vẫn chạy được ở server mode. Đồ tạm: xem `local-test-cleanup.md`.

## 1. Yêu cầu và Acceptance Criteria

* **Tính năng:** màn Lịch Content (Calendar) cho lọc lịch theo 4 tiêu chí: **Project**, **Creator**, **Platform**, **Status**.
* **AC (duy nhất):** *Filter cập nhật lịch chính xác* — đổi bất kỳ bộ lọc nào thì lưới lịch (chip theo ngày + dải "Chưa xếp lịch" + dòng summary) phải phản ánh đúng tập content sau lọc, không sót chip cũ.
* **Căn cứ tài liệu:** UX `docs/ux-ui/CreatorFlow_UX.md` mục Calendar (dòng 132–151: Creator/Platform/Status/Priority), kế hoạch `docs/impl/CreatorFlow_KeHoach_ContentCalendar.md` §3.4 (AC4 – Lọc) và ma trận test **CAL-07**.
* **Không đổi nghiệp vụ:** lọc thuần client-side trên `ContentBoardCard` (giống `BoardForm.ApplyFilters`), không thêm endpoint, không migration, không đổi schema.

## 2. Hiện trạng (đọc tĩnh từ code)

| Khu vực | Hiện trạng | Hệ quả cho task |
| :--- | :--- | :--- |
| `Controls/CalendarControl.cs:176` `Filter()` | Đã lọc theo **Platform** (`segPlatform`) + **Status** (`cboStatus`) | 2/4 tiêu chí đã xong — chỉ cần verify |
| `Controls/CalendarControl.cs:14-28` | `PlatformItems` cố định 5 mục; `StatusItems` 9 mục | Mẫu để thêm Creator/Project |
| `Controls/CalendarControl.cs:118` `RefreshData()` | `Filter(cards)` → `BuildChips` + `Unscheduled` → vẽ | Đúng thứ tự: filter trước, dựng lịch sau ⇒ AC tự nhiên đúng nếu filter đúng |
| `Controls/CalendarControl.cs:82-91` nút "Xóa bộ lọc" | Chỉ reset `segPlatform` + `cboStatus` | Phải reset thêm Creator/Project |
| `Controls/CalendarControl.Designer.cs:102-149` `flpToolbar` | Chứa view switch, `btnToday`, `segPlatform`, `cboStatus` | Thêm control Creator (+ Project) vào đây |
| `Models/ContentBoardCard.cs` | Có `ProjectId`(12), `Status`(19), `Platforms`(21), `Assignees`(26), `AssigneeName`(22) | Đủ dữ liệu lọc 4 tiêu chí, không cần query thêm |
| `Forms/Board/CardAssignee.cs:6` | `record CardAssignee(long UserId, string Name, AssignmentStatus, int, DateTime?)` | Creator lọc theo `UserId` (ổn định), hiển thị `Name` |
| `Forms/Board/BoardForm.cs:802-821` `OpenCalendar` | `CardProvider = () => HasProjectContext && _projects is null ? _allCards : null` | Xem mục 3.2 — ở server mode Calendar **không có dữ liệu** |
| `Program.cs:23,124` | `UseDatabase=false` nhưng luôn truyền `projects:` non-null | App chạy **server mode**; `_projects is not null` |

## 3. Quyết định cần chốt trước khi code

### 3.1. Tiêu chí "Project" nghĩa là gì?

Lịch hiện là view **của 1 project đang mở** (nguồn dữ liệu = `_allCards`). Có 3 hướng:

* **(A) Bỏ tiêu chí Project** — lọc Project đã được thể hiện bằng việc chọn project ở shell. Ít việc nhất; nhưng không khớp chữ "lọc theo Project" trong yêu cầu.
* **(B) Calendar gom content nhiều project** rồi thêm dropdown Project — đúng chữ yêu cầu nhưng phạm vi lớn (đổi nguồn dữ liệu cross-project).
* **(C) Thêm dropdown Project chỉ để *chuyển* project đang xem** (không phải lọc trên 1 tập đa project): đổi project → nạp lại card của project đó → `RefreshData`. Tái dùng `_boardRepository.GetBoardCards(projectId)` (local) / `_boardBackend` (server). Trung dung.

**Đề xuất: (C)** — vừa có mục Project trên toolbar đúng yêu cầu, vừa không phình. *Cần người dùng chốt.*

### 3.2. Môi trường test: local hay server?

* Ở **server mode** (hiện tại), `CardProvider` trả `null` ⇒ Calendar rỗng ("Nguồn dữ liệu chưa khả dụng"). Muốn test filter phải **nối `IBoardBackend` vào Calendar** — việc này lớn và nên là task riêng.
* Ở **local mode** (`_projects is null`), Calendar nhận `_allCards` từ `InMemoryBoardRepository` ⇒ có card để lọc ngay.

**Đề xuất:** triển khai + test filter ở **local mode** trước (đúng tinh thần các task Calendar trước). Phần nối API cho Calendar ghi **ngoài phạm vi / BLOCKED** ở task này. Đồ tạm để bật local mode xem mục 7.

### 3.3. Creator lọc theo `UserId` hay `Name`?

Lọc theo **`UserId`** (danh tính ổn định, tránh trùng tên); dropdown hiển thị `Name`.

## 4. Thiết kế đề xuất

### 4.1. Logic thuần (để test không cần UI)

Thêm hàm thuần vào `Controls/CalendarDateLogic.cs` (đúng chỗ logic lịch, không cần WinForms):

```csharp
public static List<ContentBoardCard> ApplyFilters(
    IEnumerable<ContentBoardCard> cards,
    string? platform, ContentStatus? status, long? creatorUserId, long? projectId);
```

`CalendarControl.Filter` sẽ gọi hàm này. Nhờ vậy AC "filter chính xác" kiểm chứng được bằng unit test.

### 4.2. UI (`CalendarControl.Designer.cs` + `.cs`)

Cơ chế UI chốt theo style guide §26 (Page Header chuẩn): toolbar = view switch → điều hướng → **ô Search** → **nút "Bộ lọc"**.

* Bỏ 4 control lọc cũ inline (`segPlatform`, `cboStatus`, `cboCreator`, `cboProject`) khỏi `flpToolbar`.
* Thêm **`searchBox`** (`Controls/SearchBox.cs` — khung trắng viền xám bo 8px + icon kính lúp) tìm theo `Code`/`Title`.
* Thêm **`btnFilter`** (`RoundedButton` secondary); nhãn "Bộ lọc" / "Bộ lọc (n)" với `n` = số tiêu chí đang bật (gồm cả từ khóa).
* **Panel `CalendarFilterPanel`** (`Controls/CalendarFilterPanel.cs`): 4 field có label (Project/Creator/Platform/Status) + nút "Xóa bộ lọc"; mở dưới nút bằng `ToolStripDropDown` + `ToolStripControlHost` (đúng cơ chế popover app đang dùng cho menu Project ở Board), có bo góc 8px + viền `#E5E5E5`.
* `CalendarControl.Filter` đọc lựa chọn từ panel + `searchBox.QueryText`; panel phát `FiltersChanged` → `RefreshData`.
* `ApplyTheme()`/màu control mới ép `UITheme` (panel tự ép font/màu theo `UITheme`).

### 4.3. Nguồn dữ liệu cho dropdown Creator/Project

* Creator: **từ `cards`** (distinct assignee theo `UserId`) — không thêm call.
* Project: **từ `cards`** (distinct `ProjectId`) — hiện Calendar chỉ nạp 1 project nên danh sách thường 1 mục (đúng phạm vi "lọc"), tên hiển thị theo `CurrentSession`.

## 5. Các bước triển khai

1. **Logic thuần + test:** thêm `CalendarDateLogic.ApplyFilters` + unit test (mục 6). Build.
2. **UI control:** thêm `searchBox` + `btnFilter` vào Designer; tạo `CalendarFilterPanel` (popover) + `SearchBox.Clear()`; gắn sự kiện.
3. **Nối Filter:** sửa `CalendarControl.Filter` gọi `ApplyFilters`; cập nhật nút "Xóa bộ lọc".
4. **Bật local mode để test (đồ tạm):** theo mục 7, đánh dấu `TẠM-LOCAL`.
5. **Test tay ma trận** (mục 6) theo `post-change-test-workflow`: trình kịch bản → chờ duyệt → chạy online → chỉ quan sát/ghi nhận.
6. **Dọn đồ tạm** trước khi PR (mục 7 + `local-test-cleanup.md`).

## 6. Ma trận test (mở rộng CAL-07)

| ID | Kịch bản | Kỳ vọng |
| :--- | :--- | :--- |
| F-01 | Lọc Platform = 1 nền tảng | Chỉ chip/unscheduled có nền tảng đó |
| F-02 | Lọc Status | Chỉ chip đúng trạng thái |
| F-03 | Lọc Creator = 1 người | Chỉ content có assignee đó (theo `UserId`) |
| F-04 | Lọc Project (phương án C) | Lịch nạp lại đúng project chọn |
| F-05 | Kết hợp 3–4 filter | Giao của các điều kiện, không sót |
| F-06 | Lọc ra rỗng | Empty state "Chưa có content nào" + nút "Xóa bộ lọc" reset **cả 4** |
| F-07 | Summary & Chưa xếp lịch | "n mốc trên lịch · m chưa xếp lịch" khớp tập sau lọc |
| F-08 | Đổi filter khi đang ở view Tuần/Ngày | Lịch cập nhật, giữ đúng khoảng ngày đang xem |
| F-09 | Unit test `ApplyFilters` | Từng tiêu chí + kết hợp + null = giữ nguyên |

## 7. Đồ tạm local — xóa trước khi push

Chi tiết đầy đủ ở `docs/tasks/scrum-49-loc-calendar/local-test-cleanup.md`. Tóm tắt:

* Mọi món tạm trong code đánh dấu comment **`TẠM-LOCAL`** để grep 1 lần (`Select-String "TẠM-LOCAL" src/`).
* Dự kiến gồm: chỉnh tạm cách app chạy **local mode** (không truyền `projects`/backend API) để Calendar có dữ liệu; có thể là 1 block trong `Program.cs`.
* `src/CreatorFlow/appsettings.Local.json` (đã gitignore): nếu có thêm khối `Dev` để test thì xóa trước push.
* Xác nhận sau dọn: grep `TẠM-LOCAL` rỗng, solution build 0 warning, `git status -- database/` trống.

## 8. Rủi ro và cách khóa

* **Calendar rỗng ở server mode** (lớn nhất): đã tách "nối API" ra ngoài phạm vi (mục 3.2); ghi BLOCKED nếu cần.
* **Đổi `CardProvider` signature** (thêm tham số project) sẽ đụng `BoardForm` và mọi test dùng provider → giữ tương thích, thêm overload/không đổi nếu chọn phương án (A).
* **Trùng tên Creator:** lọc theo `UserId`, không theo tên.
* **Danh sách filter cứng (PlatformItems) lệch dữ liệu thật:** cân nhắc dựng Platform từ `cards` nếu dữ liệu có nền tảng khác; không mở rộng ngoài yêu cầu.
* Không sửa `CalendarGridView` (chỉ vẽ theo dữ liệu đã lọc) → giảm rủi ro.

## 9. Giải thích tiếng người

Nói ngắn gọn: **tính năng này nhẹ hơn vẻ ngoài, nhưng có một cái bẫy.**

Màn Lịch đã có sẵn hai bộ lọc Nền tảng (TikTok/YouTube...) và Trạng thái, chạy đúng. Việc còn lại là thêm hai bộ lọc nữa: **Creator** (lọc theo người được giao) và **Project** (chọn xem lịch của project nào). Vì mọi dữ liệu cần thiết (ai làm, thuộc project nào, nền tảng nào, trạng thái gì) đã nằm sẵn trên từng thẻ content, ta chỉ cần lọc ngay trên máy — **không đụng database, không thêm API, không migration.**

Cái bẫy nằm ở chỗ khác: hiện app chạy bằng **dữ liệu server**, mà màn Lịch lại **chưa được nối vào nguồn server** — nên mở Lịch bây giờ chỉ thấy trống. Nói cách khác, phần "lọc" viết ra sẽ đúng, nhưng **không có gì để lọc** cho tới khi bật chế độ local để test. Vì thế kế hoạch tách làm hai: (1) làm bộ lọc và test bằng dữ liệu local có sẵn; (2) việc nối Lịch với API server để riêng một task khác (ghi BLOCKED).

Riêng tiêu chí **Project** cần bạn quyết: màn Lịch vốn chỉ xem một project một lúc, nên "lọc theo Project" hợp lý nhất là biến nó thành nút **chuyển project** (đổi project → lịch nạp lại), thay vì ôm content nhiều project. Nếu bạn muốn ôm nhiều project thật thì phạm vi sẽ lớn hơn nhiều.

Cuối cùng, để chạy được ở local mình sẽ phải **sửa tạm vài dòng trong code** cho app bật chế độ local. Những dòng đó là **đồ tạm**, đã được liệt kê trong `local-test-cleanup.md` và đánh dấu `TẠM-LOCAL`, **sẽ bị xóa sạch trước khi push** lên Git — chỉ để test trên máy, không lên nhánh chung.
