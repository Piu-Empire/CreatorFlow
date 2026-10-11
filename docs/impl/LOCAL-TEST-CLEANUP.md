# DỌN DẸP TRƯỚC KHI PUSH (local test artifacts — không bao giờ lên DEV)

File này liệt kê TOÀN BỘ đồ tạm phục vụ test local. Trước khi mở PR, đi hết checklist,
đánh dấu vào PR làm evidence. Mọi món tạm trong code đều đánh dấu `TẠM-LOCAL` để grep ra 1 lần.

## 1. Code tạm trong `src/` (xóa hẳn)

| # | Vị trí | Nội dung tạm | Cách xóa |
| :--- | :--- | :--- | :--- |
| 1 | `CreatorFlow/Program.cs` | Khối đọc `Dev:CurrentProjectId`/`Dev:CurrentProjectName` + `using Microsoft.Extensions.Configuration` | Xóa cả khối + dòng using (kiểm tra using không còn dùng chỗ khác) |
| 2 | `Controls/CalendarControl.Designer.cs` | `segProject` (khai báo + khởi tạo + Add vào toolbar) | Xóa 3 chỗ |
| 3 | `Controls/CalendarControl.cs` | Event `SampleProjectChanged` + khối setup `segProject` | Xóa 2 chỗ |
| 4 | `Forms/Board/BoardForm.cs` | Handler `SampleProjectChanged` trong `OpenCalendar` | Xóa khối subscribe |

Xác nhận sau xóa: `grep "TẠM-LOCAL" src/` (PowerShell: `Select-String`) **trả về rỗng**, solution build 0 warning.

## 2. Config local (không commit, nhưng dọn để khỏi nhầm)

* `src/CreatorFlow/appsettings.Local.json`: xóa khối `"Dev"` (giữ khối `"Api"`). File này gitignore nên không lọt lên Git, nhưng để lại là lần sau mở app tự dính project Demo.

## 3. Data fake trên DB local (xem thêm mục 10 của kế hoạch Users)

* User fake: `DELETE FROM users WHERE email LIKE '%@test.dev';` + xác nhận COUNT = 0.
* Reports fake: `DELETE FROM reports;` (DB local chỉ chứa data fake).
* Audit test: `DELETE FROM audit_logs WHERE entity_type = 'users' AND entity_id NOT IN (SELECT user_id FROM users);`
* Container dùng xong: `docker stop creatorflow-local-pg && docker rm creatorflow-local-pg`.
* File tạm ngoài repo: xóa `%TEMP%\opencode\seed_fake_local.sql`, `seed_sample_local.sql`, `hashgen\`, `genkeys.py`, `e2e_*.ps1` (giữ hay xóa tùy — chúng nằm ngoài repo, không ảnh hưởng push).

## 4. Những thứ GIỮ LẠI (không phải đồ tạm, đừng xóa nhầm)

* `SetActiveMenu` (Sidebar) + icon Shield/Flag + `BadgeLabel` + mở khóa Toolbox Designer — cải tiến thật, dùng lâu dài.
* `PlannedPublishAt` trên card + SELECT (Calendar) — nghiệp vụ thật.
* `verify_auth_schema.sql` pass trên Neon DEV — kiểm tra, không phải dọn.
* Tuyệt đối không chạy seed fake / file `%TEMP%` lên Neon DEV chung.

## 5. Checklist chốt trước PR

* [ ] Mục 1–3 xong, `TẠM-LOCAL` grep rỗng, build 0 warning 0 error.
* [ ] `git status -- database/` trống (không migration lạ).
* [ ] Mở app không config Dev: breadcrumb "Chưa chọn dự án", màn Calendar hiện empty state (đúng hành vi chờ SCRUM-25).
* [ ] Ghi 1 dòng evidence đã dọn vào PR.
