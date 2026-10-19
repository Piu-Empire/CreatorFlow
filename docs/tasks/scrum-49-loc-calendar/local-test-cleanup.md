# ĐỒ TẠM LOCAL — XÓA TRƯỚC KHI PUSH (SCRUM-49)

File này liệt kê **toàn bộ đồ tạm** chỉ phục vụ test local cho task lọc Calendar (SCRUM-49).
Trước khi mở PR: đi hết checklist, mọi món tạm trong code đều đánh dấu **`TẠM-LOCAL`** để grep 1 lần.

> Tham chiếu quy trình chung: `docs/impl/LOCAL-TEST-CLEANUP.md`. File đó là của đợt test trước (Calendar/Users/Metrics);
> file này là **của riêng SCRUM-49**, tạo cùng lúc với `plan.md`.

## 1. Bối cảnh (vì sao phải có đồ tạm này)

Hiện app chạy **server mode** (`Program.cs` luôn truyền `projects:` non-null) nên màn Lịch
không có nguồn Content ⇒ `CalendarControl` hiện empty state "Nguồn dữ liệu chưa khả dụng".
Muốn có card để test bộ lọc thì phải bật **local mode** tạm thời (InMemory + `_projects is null`).
Bật local mode là **đồ tạm** — xóa trước khi push. Việc nối Lịch với API server KHÔNG nằm trong task này (BLOCKED riêng).

## 2. Code tạm trong `src/` (xóa hẳn)

| # | Vị trí | Nội dung tạm | Cách xóa |
| :--- | :--- | :--- | :--- |
| 1 | `src/CreatorFlow/Program.cs` | Khối `TẠM-LOCAL`: cờ `bool useLocalTest = true;` + gán `CurrentSession` demo + `Application.Run(BoardForm local)` bằng InMemory, bỏ qua login/API | Xóa cả khối `bool useLocalTest = true; if (useLocalTest) { ... }` |
| 2 | `src/CreatorFlow/Forms/Board/BoardForm.cs` | Khối `TẠM-LOCAL`: `if (auth is null) { ... }` gắn menu Calendar và Summary khi local không có auth | Xóa cả khối `if (auth is null) { ... }` + comment |

> Đây là danh sách thực tế đã thêm. Kiểm tra lại bằng `Select-String "TẠM-LOCAL" src/`.

## 3. Config local (không commit, nhưng dọn để khỏi nhầm)

* `src/CreatorFlow/appsettings.Local.json` (đã gitignore): nếu có thêm khối `Dev`/`Api` để test thì xóa khối `Dev`, giữ `Api`.
* Nếu có chỉnh tạm `appsettings.json` (file tracked) → **phải hoàn nguyên** vì sẽ lọt lên Git.

## 4. Không phải đồ tạm (GIỮ LẠI — đừng xóa nhầm)

* `CalendarDateLogic.ApplyFilters` + unit test (logic thật của task).
* `Controls/CalendarFilterPanel.cs`, `Controls/SearchBox.cs` (control UI thật của task).
* `searchBox` + `btnFilter` trên toolbar Calendar, panel "Bộ lọc", `Filter()` mới, reset "Xóa bộ lọc" (nghiệp vụ thật).
* `docs/tasks/scrum-49-loc-calendar/plan.md` (tài liệu task, giữ).

## 5. Checklist chốt trước PR

* [ ] Mục 2–3 xong; `Select-String "TẠM-LOCAL" src/` trả về **rỗng**.
* [ ] `git status -- database/` trống (không migration lạ).
* [ ] Solution build 0 error (ghi rõ số warning).
* [ ] App mở ở server mode như cũ: Calendar hiện empty state "chưa khả dụng" (đúng hành vi chờ nối API).
* [ ] `appsettings.json` (tracked) đã hoàn nguyên.
* [ ] Ghi 1 dòng evidence "đã dọn đồ tạm" vào PR.
