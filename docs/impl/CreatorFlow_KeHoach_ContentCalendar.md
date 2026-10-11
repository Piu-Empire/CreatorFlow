# CREATORFLOW — KẾ HOẠCH TRIỂN KHAI CONTENT CALENDAR (NGÀY/TUẦN/THÁNG)

## 1. Thông Tin Chung
* **Tên tính năng:** Content Calendar — xem Content theo mốc thời gian ở 3 chế độ ngày/tuần/tháng.
* **Mức độ ưu tiên:** P1 (`KeHoachDuAn` mục 10).
* **Người phụ trách:** Tính (Calendar/Analytics/AI theo `PhanCong_Timeline`).
* **Nguyên tắc bao trùm:** Calendar là **view suy từ dữ liệu Content** (`UX` mục Calendar + nguyên tắc 6/20), không phải module dữ liệu mới.

## 2. Căn Cứ Tài Liệu
* `UX` mục Calendar: 3 chế độ `[ Month ] [ Week ] [ Day ]`, filter Creator/Platform/Status/Priority, click → Quick Preview → Open Content Detail.
* `UX` nguyên tắc 6: Calendar dùng cùng dữ liệu Content; mục 17: mọi màn hình phải có empty/loading/error state.
* Style guide: Page Header bắt buộc mọi module incl. Calendar (mục 26); click dòng Board/Calendar → mở Drawer (mục 15); Segmented lọc nhanh dùng được trên thanh công cụ Board/Calendar (mục 18); avatar cỡ xs trong thẻ Kanban/Calendar nhỏ (mục 21).
* `KeHoachDuAn` mục Calendar: Day/Week/Month, production schedule, deadline, planned publish date, filter theo Project/Creator/Platform/Status.
* `QuyTacLamViec`: Form chỉ gọi Service, không SQL trong UI; permission check ở Service (Calendar chỉ đọc nên không phát sinh check mới).

## 3. Acceptance Criteria

### 3.1. AC1: Không bảng lịch độc lập
* **0 migration.** Không tạo bảng/calendar event riêng, không cột mới, không trigger mới.
* Nguồn dữ liệu duy nhất: `contents` qua đúng 1 call đã tồn tại `IBoardRepository.GetBoardCards(projectId)` — cùng nguồn Board đang dùng, tự tương thích cả 2 chế độ local Npgsql và InMemory (decorator API đi qua, không sửa).
* Thay đổi dữ liệu duy nhất: thêm **1 cột vào SELECT có sẵn** (`c.planned_publish_at`) + **1 property** `PlannedPublishAt` vào `ContentBoardCard` (+ map ở 2 cài đặt Npgsql/InMemory). Lý do: card hiện chỉ có `Deadline`, thiếu mốc xuất bản thì AC "lịch đăng" không làm được.

### 3.2. AC2: Đủ 3 chế độ + điều hướng
* Segmented chuyển chế độ `[ Tháng | Tuần | Ngày ]` (tái dùng `SegmentedControl`), kèm nút `‹ Hôm nay ›`.
* **Month:** lưới 7×6, mỗi ô ngày chứa tối đa 3 chip + dòng `+n nữa`; số hôm nay highlight; ngày ngoài tháng hiện mờ.
* **Week:** 7 cột ngày (T2–CN), chip xếp dọc, cột hôm nay highlight.
* **Day:** danh sách chip của đúng 1 ngày + khu vực "Cả ngày" (không giờ cụ thể — Content không có giờ, chỉ có ngày).
* Chip hiển thị: mã + tiêu đề rút gọn + chấm màu trạng thái; quá hạn (deadline < hôm nay và chưa PUBLISHED) viền/chữ đỏ (tái dùng logic overdue của MyTasks).

### 3.3. AC3: Click mở Content
* Click chip → mở **đúng Drawer chi tiết có sẵn** (`BoardForm.OpenContentById` + `ContentDetailPanel`), không vẽ detail riêng. Được phép vì drawer dock ở cấp Form, đè lên được màn Calendar host trong `_pnlMain` (đã xác minh `BoardForm.Designer.cs:101-109,122-125`).
* Calendar nhận đúng instance `_allCards` BoardForm đã load (callback `Action<long>`), không query riêng → id luôn khớp, không lệch dữ liệu với Board.
* Quick Preview riêng (UX) lùi sang P2 — P0 click thẳng mở Drawer (đã đủ AC "Click mở Content").

### 3.4. AC4: Lọc (giữ đúng thứ tự toolbar mục 26)
* P0: Platform (Segmented) + Status — dữ liệu đã có sẵn trên card, lọc client-side.
* P1 (làm ngay nếu còn sức, cùng pattern): Creator (theo `Assignees`) + Priority. Không thêm endpoint lọc server-side ở P0.

### 3.5. AC5: Ngữ nghĩa ngày (quy tắc ánh xạ, chốt để khỏi cãi nhau lúc test)
* `deadline` = mốc **sản xuất** (marker chính, chấm màu theo status).
* `planned_publish_at` = mốc **xuất bản** (marker phụ, viền/icon khác để phân biệt).
* Content có cả 2 ngày → hiện ở **cả 2 ô** (2 chip phân biệt bằng marker).
* Cả 2 null → không lên lưới, gom vào khu vực **"Chưa xếp lịch"** cuối màn hình (đúng luật empty-state UX).
* So sánh theo **ngày local của máy** (phần date của `DateTime`); không quy đổi múi giờ phức tạp ở P0.

### 3.6. AC6: Tích hợp shell
* Mục Sidebar "Calendar" hiện đang **chết** (có icon, không có Action — `SidebarControl.cs:156`) → gắn event `CalendarRequested`, BoardForm host `CalendarControl` trong `_pnlMain` theo đúng pattern màn admin (ẩn `_pnlBoardArea`, toast tiêu đề "Lịch Content").
* Chưa chọn project → empty state yêu cầu chọn project (không crash, không hiện lịch trống).

## 4. Luồng Hoạt Động
```text
[Sidebar: Calendar] ──CalendarRequested──► [BoardForm]
                                                │ ẩn _pnlBoardArea, hiện CalendarControl
                                                ▼
                                   [CalendarControl] ◄── _allCards (dùng chung với Board)
                                                │
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
| CAL-06 | Chuyển Tháng/Tuần/Ngày + ‹ Hôm nay › | Đúng khoảng ngày, không mất filter |
| CAL-07 | Lọc Platform/Status | Chỉ chip khớp còn lại |
| CAL-08 | Click chip | Drawer mở đúng ContentId |
| CAL-09 | Sửa deadline trong Drawer rồi đóng | Chip nhảy sang ô mới (reload khi quay lại) |
| CAL-10 | Chưa chọn project | Empty state, không crash |
| CAL-11 | Mở Calendar rồi mở Board/Profile | Loại trừ lẫn nhau, không chồng màn hình |

## 8. Ngoài Phạm Vi (P2, ghi để khỏi phình)
* Kéo-thả chip để dời deadline; Quick Preview riêng; nhắc việc/notification; lịch lặp lại (recurring); endpoint calendar server-side riêng; quy đổi múi giờ hiển thị.

## 9. Rủi Ro Đã Thấy Trước
* `OpenContentById` tra `_allCards` của Board — Calendar dùng chung instance nên khớp; nếu Board chưa load (lỗi mạng local) thì Calendar cũng empty theo — chấp nhận, đúng bản chất "cùng dữ liệu".
* Lưới Month vẽ tay 42 ô: giữ mỗi ô gọn (3 chip + `+n`), không nhồi chi tiết — chi tiết nằm ở Drawer.
