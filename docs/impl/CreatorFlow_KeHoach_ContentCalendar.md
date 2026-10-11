# CREATORFLOW — KẾ HOẠCH TRIỂN KHAI CONTENT CALENDAR (NGÀY/TUẦN/THÁNG)

## 1. Thông Tin Chung
* **Tên tính năng:** Content Calendar — xem Content theo mốc thời gian ở 3 chế độ ngày/tuần/tháng.
* **Mức độ ưu tiên:** P1 (`KeHoachDuAn` mục 10).
* **Người phụ trách:** Tính (Calendar/Analytics/AI theo `PhanCong_Timeline`).
* **Nguyên tắc bao trùm:** Calendar là **view suy từ dữ liệu Content** (`UX` mục Calendar + nguyên tắc 6/20), không phải module dữ liệu mới.

## 2. Căn Cứ Tài Liệu (đã đối chiếu từng dòng)
* `UX` mục Calendar (dòng 132–151): 3 chế độ `[ Month ] [ Week ] [ Day ]`, filter Creator/Platform/Status/Priority, click → Quick Preview → Open Content Detail.
* `UX` nguyên tắc 6 (“Calendar và Reports dùng cùng dữ liệu Content”) và mục 17 (EMPTY / LOADING / ERROR STATE bắt buộc mọi màn hình).
* Style guide: Page Header bắt buộc mọi module incl. Calendar (mục 26); click dòng Board/Calendar → mở Drawer (dòng 635); Segmented lọc nhanh dùng được trên thanh công cụ Board/Calendar (mục 18, biến thể nền trắng–nút đen); avatar cỡ xs trong thẻ Kanban/Calendar nhỏ (mục 21). **Style guide không có mẫu lưới tháng/tuần** — lưới lịch là custom drawing mới, xem §3.2.
* `KeHoachDuAn` mục Calendar (dòng 105–109: Deadline, Planned Publish Date) + vai trò Manager/Creator “theo dõi Calendar” (dòng 42–54).
* `QuyTacLamViec` dòng 5, 14–16: chuỗi `WinForms UI → Service → Repository…`, cấm SQL trong Form/UserControl, Form chỉ xử lý UI và gọi Service.
* `PhanCong_Timeline` dòng 82–83, 127, 152, 174: Calendar thuộc Tính; mốc tuần 2 “Basic Calendar”, tuần 3 “Calendar dùng dữ liệu thật”.

## 2b. Mẫu Có Sẵn Được Tái Dùng (không phát minh lại)
| Mẫu | Ở đâu | Dùng cho Calendar |
| :--- | :--- | :--- |
| Hiển thị ngày + quá hạn | `MyTasksForm.FormatDeadline` (:546–556: `dd/MM/yyyy`, hậu tố “(trễ n ngày)”/“(hôm nay)”, màu `UITheme.OverdueText` khi quá hạn) | Format ngày trên chip + chữ đỏ quá hạn |
| Logic quá hạn | `TaskRules.IsOverdue(deadline, isFinished, today)` (:83) + `DaysLate` (:87) | Gọi trực tiếp, `isFinished = status là Published/Archived` |
| Khung màn hình + toolbar | `ModulePageHeaderControl` + `SegmentedControl` (ô đen-chữ trắng khi chọn) | Page Header + nút chuyển Tháng/Tuần/Ngày + lọc Platform |
| Vẽ chip | `KanbanCardControl` (deadline `dd/MM`, badge, avatar) | Kiểu chip trên lưới (mã + tên rút gọn + chấm màu) |
| Dữ liệu test có sẵn | `InMemoryDataStore` dòng 83: 1 content vừa có `Deadline` (+5 ngày) vừa có `PlannedPublishAt` (+8 ngày) | Fixture test CAL-03, không cần seed thêm |
| Drawer chi tiết | `BoardForm.OpenContentById` (:427–452) + drawer dock cấp Form (`Designer.cs:101–109`) | Click chip mở đúng drawer này |

## 3. Acceptance Criteria

### 3.1. AC1: Không bảng lịch độc lập
* **0 migration.** Không tạo bảng/calendar event riêng, không cột mới, không trigger mới.
* Nguồn dữ liệu duy nhất: `contents` qua đúng 1 call đã tồn tại `IBoardRepository.GetBoardCards(projectId)` — cùng nguồn Board đang dùng, tự tương thích cả 2 chế độ local Npgsql và InMemory (decorator API đi qua, không sửa).
* Thay đổi dữ liệu duy nhất: thêm **1 cột vào SELECT có sẵn** (`c.planned_publish_at`) + **1 property** `PlannedPublishAt` vào `ContentBoardCard` (+ map ở 2 cài đặt Npgsql/InMemory). Lý do: card hiện chỉ có `Deadline`, thiếu mốc xuất bản thì AC "lịch đăng" không làm được.

### 3.2. AC2: Đủ 3 chế độ + điều hướng (quy tắc vẽ chốt cứng)
* Segmented chuyển chế độ `[ Tháng | Tuần | Ngày ]` (tái dùng `SegmentedControl`), kèm nút `‹ Hôm nay ›`.
* **Cách vẽ:** 1 control custom-drawn duy nhất (GDI+, theo tiền lệ `SidebarControl`/`KanbanColumnControl) — **cấm** tạo 42 UserControl cho 42 ô (nặng, khó maintain). Không có mẫu lưới tháng/tuần trong style guide nên tuân thủ token màu/font/chip của guide thay vì copy mẫu.
* **Month:** lưới 7×6 = 42 ô, tuần bắt đầu **Thứ 2**; mỗi ô chứa tối đa 3 chip + dòng `+n nữa` — click `+n` nhảy sang Day view của đúng ngày đó (không dùng tooltip thay điều hướng). Số hôm nay highlight (nền đen `UITheme.Black`, chữ trắng — đồng bộ nút Primary). Ngày ngoài tháng hiện mờ (`Neutral400`), không vẽ chip, không nhận click.
* **Week:** 7 cột T2–CN, chip xếp dọc, cột hôm nay highlight.
* **Day:** danh sách chip full-width của đúng 1 ngày, sắp xếp deadline trước, publish sau; khu vực "Cả ngày" (Content không có giờ, chỉ có ngày).
* **Chip (định dạng chốt):** `[Hạn] CNT-012 · Tên rút gọn` cho deadline, `[Đăng] CNT-012 · Tên rút gọn` cho publish — tiền tố chữ thay icon để test không nhầm được. Chấm màu trạng thái đầu chip; quá hạn (`TaskRules.IsOverdue(deadline, status là Published/Archived, hôm nay)`) viền/chữ đỏ `OverdueText`, đúng format `MyTasksForm.FormatDeadline`.

### 3.3. AC3: Click mở Content
* Click chip → mở **đúng Drawer chi tiết có sẵn** (`BoardForm.OpenContentById` + `ContentDetailPanel`), không vẽ detail riêng. Được phép vì drawer dock ở cấp Form, đè lên được màn Calendar host trong `_pnlMain` (đã xác minh `BoardForm.Designer.cs:101-109,122-125`).
* Calendar nhận provider `Func<List<ContentBoardCard>>` trỏ về `_allCards` của BoardForm (không giữ snapshot, không query riêng) → id luôn khớp; reload khi mở màn hình và khi Drawer đóng (xong CAL-09).
* Quick Preview riêng (UX) lùi sang P2 — P0 click thẳng mở Drawer (đã đủ AC "Click mở Content").

### 3.4. AC4: Lọc (giữ đúng thứ tự toolbar mục 26)
* P0: Platform (Segmented) + Status — dữ liệu đã có sẵn trên card, lọc client-side.
* P1 (làm ngay nếu còn sức, cùng pattern): Creator (theo `Assignees`) + Priority. Không thêm endpoint lọc server-side ở P0.

### 3.5. AC5: Ngữ nghĩa ngày (quy tắc ánh xạ, chốt để khỏi cãi nhau lúc test)
* `deadline` = mốc **sản xuất** (marker chính, chấm màu theo status).
* `planned_publish_at` = mốc **xuất bản** (marker phụ, viền/icon khác để phân biệt).
* Content có cả 2 ngày → hiện ở **cả 2 ô** (2 chip phân biệt bằng marker).
* Cả 2 null → không lên lưới, gom vào khu vực **"Chưa xếp lịch"** cố định cuối màn hình (scroll riêng, mỗi dòng mã + tên + chữ "chưa có ngày", click vẫn mở Drawer).
* So sánh theo **ngày local của máy** (phần date của `DateTime`); không quy đổi múi giờ phức tạp ở P0.

### 3.6. AC6: Tích hợp shell
* Mục Sidebar "Calendar" hiện đang **chết** (có icon, không có Action — `SidebarControl.cs:156`) → gắn event `CalendarRequested`, BoardForm host `CalendarControl` trong `_pnlMain` theo đúng pattern màn admin (ẩn `_pnlBoardArea`, toast tiêu đề "Lịch Content").
* Chưa chọn project → empty state yêu cầu chọn project (không crash, không hiện lịch trống).

## 4. Luồng Hoạt Động
```text
[Sidebar: Calendar] ──CalendarRequested──► [BoardForm]
                                                │ ẩn _pnlBoardArea, hiện CalendarControl
                                                ▼
                                   [CalendarControl] ◄── provider Func → _allCards (dùng chung với Board)
                                                │ reload khi mở + khi Drawer đóng
                                                │ click chip(contentId) ──callback──► BoardForm.OpenContentById
                                                ▼
                                   [Drawer ContentDetailPanel đè lên trên]
```

## 5. Tệp Triển Khai
| Tệp | Việc | Loại |
| :--- | :--- | :--- |
| `Models/ContentBoardCard.cs` | Thêm `DateTime? PlannedPublishAt` | Chỉnh sửa (1 dòng) |
| `Repositories/Implementations/BoardRepository.cs` | Thêm `c.planned_publish_at` vào SELECT + map reader | Chỉnh sửa (2 dòng) |
| `Repositories/InMemory/InMemoryBoardRepository.cs` | Map thêm trường mới | Chỉnh sửa (1 dòng) |
| `Controls/CalendarControl.cs` | Màn hình: header card + segmented view + lưới Month/Week/Day + filter + unscheduled + states | Tạo mới |
| `Controls/CalendarDateLogic.cs` (static) | Hàm thuần: dựng lưới tháng, gom chip theo ngày, tính overdue — để unit test không cần UI/DB | Tạo mới |
| `Controls/SidebarControl.cs` | Event `CalendarRequested` + gắn Action cho mục Calendar | Chỉnh sửa |
| `Forms/Board/BoardForm.cs` | `OpenCalendar/CloseCalendar` + đấu nối loại trừ với Board/Profile/Admin/Reports | Chỉnh sửa |

## 6. Các Bước
1. **Model + repo (nửa buổi):** thêm property, SELECT, map InMemory; build.
2. **Logic ngày thuần (nửa buổi):** `CalendarDateLogic` + unit test ngay (lưới tháng đủ 42 ô, chip 2 marker, overdue, unscheduled).
3. **Khung màn hình (1 buổi):** header card + segmented + pager-less (lưới tự đủ) + states theo mục 13/26.
4. **3 view (1–2 buổi):** Month grid → Week columns → Day list, chip vẽ tay (màu status, avatar xs nếu có assignee).
5. **Đấu nối (nửa buổi):** sidebar event + BoardForm host + click callback + filter.
6. **Test tay ma trận + tick DoD.**

## 7. Ma Trận Test
| ID | Kịch bản | Kỳ vọng |
| :--- | :--- | :--- |
| CAL-01 | Không migration mới | `git status -- database/` trống sau khi xong |
| CAL-02 | Tháng có ngày đầu/cuối lẻ | Lưới luôn 42 ô, ngày ngoài tháng mờ |
| CAL-03 | Content có cả 2 ngày | Hiện 2 chip, marker phân biệt được |
| CAL-04 | Content không ngày nào | Nằm khu "Chưa xếp lịch", không lên lưới |
| CAL-05 | Deadline quá hạn, chưa PUBLISHED | Chip đỏ; đã PUBLISHED thì không đỏ |
| CAL-06 | Chuyển Tháng/Tuần/Ngày + ‹ Hôm nay › | Đúng khoảng ngày, filter + vị trí cuộn giữ nguyên |
| CAL-07 | Lọc Platform/Status | Chỉ chip khớp còn lại |
| CAL-08 | Click chip | Drawer mở đúng ContentId |
| CAL-09 | Sửa deadline trong Drawer rồi đóng | Chip nhảy sang ô mới (reload khi quay lại) |
| CAL-10 | Chưa chọn project | Empty state, không crash |
| CAL-11 | Mở Calendar rồi mở Board/Profile | Loại trừ lẫn nhau, không chồng màn hình |
| CAL-12 | Tháng bất kỳ | Tuần luôn bắt đầu Thứ 2, đúng 42 ô |
| CAL-13 | Click `+n nữa` trong ô đông | Nhảy sang Day view đúng ngày đó |
| CAL-14 | Chip 2 marker | Đọc được chữ `[Hạn]` / `[Đăng]`, không đoán bằng màu |
| CAL-15 | Overdue (unit, dùng fixture InMemory) | Gọi đúng `TaskRules.IsOverdue`, PUBLISHED/ARCHIVED không đỏ |
| CAL-16 | Review code: cách vẽ lưới | 1 control custom-drawn, không tạo 42 UserControl |

## 8. Ngoài Phạm Vi (P2, ghi để khỏi phình)
* Kéo-thả chip để dời deadline; Quick Preview riêng; nhắc việc/notification; lịch lặp lại (recurring); endpoint calendar server-side riêng; quy đổi múi giờ hiển thị.

## 9. Rủi Ro Đã Thấy Trước (và cách khóa)
* Dữ liệu chung với Board: Calendar đọc qua provider `Func` + reload khi mở/khi Drawer đóng → không snapshot cũ, không query riêng.
* Lưới Month vẽ tay 42 ô: 1 control custom-drawn duy nhất (CAL-16 khóa ở review), mỗi ô gọn (3 chip + `+n`), chi tiết nằm ở Drawer.
* `OpenContentById` highlight thẻ trên Board (`FindCardControl` trả null khi Board ẩn — code đã null-check) → click từ Calendar vẫn mở Drawer bình thường, chỉ thiếu viền chọn trên Board (chấp nhận).

## 10. Chia Việc Designer (lập trình trực quan)
* Mục tiêu: người làm UI kéo-thả khung màn hình trong Visual Studio, code logic do dev viết — 2 bên không giẫm chân nhau.
* Đã mở khóa Toolbox (commit trước): `RoundedPanel` (FillColor/BorderColor/Radius), `RoundedButton` (Style/ShowPlusIcon/BorderRadius), `BadgeLabel` (Text/BgColor/FgColor, thêm dựng không tham số) — build solution 1 lần là hiện trong Toolbox. `SegmentedControl` chỉ kéo vị trí/kích thước, Items set bằng code 1 dòng.
* **Quy trình template-trước (chốt):**
  1. Team tạo UserControl `CalendarControl` trong Designer TRƯỚC (khung, tên, vị trí theo bảng dưới).
  2. Dev chỉ viết `CalendarControl.cs` (logic + style + nhét control động vào placeholder), **tuyệt đối không sửa `.Designer.cs`**.
  3. Sau này team mở lại Designer vẫn kéo-thả sửa được mọi control mình đã tạo — vì code dev không xóa/không đổi tên/không dời control của team.
  4. Ngoại lệ duy nhất: control dev tự thêm bằng code (lưới vẽ tay, items của Segmented) chỉ xuất hiện lúc chạy, không có trong Designer — muốn dời chúng thì dời **khung placeholder** chứa chúng.
  5. Nếu team đổi tên/xóa control: build báo lỗi đúng dòng code dev tham chiếu tới — sửa tên theo, không đoán mò.

| Name | Loại (Toolbox) | Bố trí | Ghi chú |
| :--- | :--- | :--- | :--- |
| (root) | UserControl | BackColor `#FAFAFA`, Padding 24,20,24,20 | Font để mặc định, code ép sau |
| pnlHeaderCard | RoundedPanel | Dock Top, Height 168, Fill White, Border `#E5E5E5`, Radius 12 | Card trắng theo mục 26 |
| lblTitle | Label | (20,12), Text "Lịch Content" | Vị trí + chữ của bạn |
| badgeKind | BadgeLabel | Cạnh phải lblTitle, Text "Lịch" | Màu set ở Properties |
| lblDesc | Label | (20,52), Text mô tả 1 dòng | Vị trí + chữ của bạn |
| flpToolbar | FlowLayoutPanel | (20,84), AutoSize | Chứa 3 món dưới |
| pnlViewSwitch | Panel | (~200,36), để trống | Dev nhét Segmented vào |
| btnToday | RoundedButton | Text "‹ Hôm nay ›", Style Secondary, 110×36 | Kéo từ Toolbox sau khi build |
| segPlatform | SegmentedControl | (~300,36) | Vị trí của bạn, Items dev set |
| pnlGridHost | Panel | Dock Fill, để trống | Dev nhét lưới vẽ tay vào |
| pnlPager | Panel | Dock Bottom, Height 48 | Chứa 4 món dưới |
| lblSummary | Label | Dock Left, Text "Đang hiện…" | Vị trí + chữ mẫu của bạn |
| btnPrev / lblPage / btnNext | RoundedButton/Label/RoundedButton | Cạnh phải pnlPager | Text "Trước"/"Trang 1/1"/"Sau" |
| pnlState | Panel | Dock Fill, Visible False, BackColor White | Overlay khi rỗng/lỗi |
| lblStateTitle / lblStateSub / btnState | Label/Label/RoundedButton | Giữa pnlState | Text mẫu + nút "Thử lại" |
