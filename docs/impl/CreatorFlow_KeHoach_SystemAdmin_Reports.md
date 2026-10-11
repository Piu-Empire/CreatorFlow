# CREATORFLOW — KẾ HOẠCH TRIỂN KHAI SYSTEM ADMIN XỬ LÝ BÁO CÁO VI PHẠM

## 1. Thông Tin Chung
* **Tên tính năng:** System Admin — Tiếp nhận & xử lý báo cáo vi phạm (Reports).
* **Mức độ ưu tiên:** P0 (thuộc Basic System Admin: `KeHoachDuAn` mục 2 + `PhanCong` mục Phú).
* **Phạm vi:** user tố cáo qua API + admin xem/xử lý qua API và màn hình. **Ngoài phạm vi:** nút "Tố cáo" trên UI user (thuộc module khác), khóa tài khoản ngay từ màn hình report (dùng màn hình users), unique index chống trùng (service-check đủ, không đụng SQL).

## 2. AC

### 2.1. AC1: User tố cáo (`POST /api/reports`, `RequireAuthorization`)
* Reporter lấy từ `CurrentAuthenticatedUser` (không từ body). `reported` không tồn tại → `404`; tự tố cáo mình → `400 self_report`; `reason` bắt buộc ≤ 500 ký tự, `details` ≤ 2000; đã có báo cáo OPEN/IN_REVIEW cùng cặp → `409 duplicate_report`.
* Chưa đăng nhập → `401` (do middleware, service check null → `Fail("unauthorized", 401)`).

### 2.2. AC2: Admin xem (`GET /api/admin/reports`, policy `SystemAdminOnly` cấp group có sẵn)
* Lọc `ALL/OPEN/IN_REVIEW/RESOLVED/REJECTED` (sai → `400`), tìm theo email reporter/reported (`ILIKE ... ESCAPE '\'`, ≤ 100 ký tự), phân trang `limit` 1–100 (mặc định 20) + `offset` 0–10000. Không `SELECT` cột nhạy cảm ngoài email hiển thị.

### 2.3. AC3: Admin xử lý (`PATCH /api/admin/reports/{id}`, policy group)
* `switch` tường minh `IN_REVIEW/RESOLVED/REJECTED` (mọi giá trị khác → `400`); `note` ≤ 1000 ký tự; không tồn tại → `404`.
* Transaction: khóa dòng report (`FOR UPDATE OF r`) → `UPDATE status/handled_by/handled_note/handled_at` → `INSERT audit_logs` (`REPORT_REVIEW/REPORT_RESOLVE/REPORT_REJECT`, description `"{ACTION} report {id} ({reporter} -> {reported}) {OLD}->{NEW} trace={traceId}"`, `actor` từ server). Audit lỗi → rollback.
* Cho phép chuyển tiếp giữa các trạng thái (mở lại điều tra), mỗi lần đều audit.

### 2.4. AC4: UI "Báo cáo vi phạm" (nhóm ADMIN, System Admin mới thấy)
* Page Header Card + Segmented trạng thái + grid pill (`OPEN` đen / `IN_REVIEW` vàng / `RESOLVED` xanh / `REJECTED` xám) + tooltip lý do/chi tiết + dialog xử lý (chọn trạng thái + note) + pager 20 + states + toast. Sidebar thêm mục "Báo cáo vi phạm" (icon Flag), BoardForm đấu nối loại trừ lẫn nhau với các màn hình khác.

## 3. Tệp
| Tệp | Trách nhiệm | Trạng thái |
| :--- | :--- | :--- |
| `Contracts/Admin/AdminReportContracts.cs` | 6 DTO dùng chung | Tạo mới |
| `Api/Models/Admin/AdminReportRecord.cs` (+ `ReportStatus`) | Row nội bộ; map DB `IN_REVIEW` có gạch dưới qua hàm riêng (không `ToString()`) | Tạo mới |
| `Api/Repositories/Admin/IAdminReportRepository.cs` + `AdminReportRepository.cs` | Search/Count/Create/Handle parameterized + transaction | Tạo mới |
| `Api/Services/Admin/AdminReportService.cs` | Validate + gọi repo + log xử lý | Tạo mới |
| `Api/Endpoints/ReportEndpoints.cs` | `POST /api/reports` (`RequireAuthorization`) | Tạo mới |
| `Api/Endpoints/AdminEndpoints.cs` + `Program.cs` | Thêm 2 endpoint vào group policy có sẵn + DI | Chỉnh sửa |
| `ApiClients/ApiClient.Reports.cs` + whitelist mã mới | 3 call + `self_report`/`duplicate_report` | Tạo mới + chỉnh sửa |
| `Services/AdminReportResults.cs` + `AuthApiFacade.cs` | Result records + 2 facade methods (401 clear, 403 giữ) | Tạo mới + chỉnh sửa |
| `Controls/AdminReportsControl.cs` + `HandleReportDialog.cs` | Màn hình + dialog xử lý | Tạo mới |
| `Controls/BadgeLabel.cs` (+ refactor màn users dùng chung) | Pill header dùng chung | Tạo mới + chỉnh sửa |
| `Theme/UIIcons.cs` (`Flag`) + `SidebarControl.cs` + `BoardForm.cs` | Icon + mục menu + đấu nối | Chỉnh sửa |
| `tests/.../AdminReportServiceTests.cs` (+ fake) | 11 unit test | Tạo mới |

## 4. Kiểm Thử
* Unit (đã pass): 401 ẩn danh, 400 tự tố cáo/lý do rỗng, 404 target, 409 trùng, 403 cả 2 chiều admin, 400 status lạ (`OPEN`/null), 3 chuyển đổi + message, 404 handle, 503 DB chết.
* Sống API local (đã pass): tạo OPEN → trùng 409 → tự tố cáo 400 → creator xem 403 → admin list 1 → IN_REVIEW → RESOLVED → audit đủ 2 dòng.
* Còn lại (cần click tay): TC UI màn reports (menu ẩn/hiện, lọc, dialog, pager, toast), đồng thời PATCH cùng report.

## 5. Khôi Phục (bổ sung mục 10 của kế hoạch Users)
* Fake reports trên DB local: `DELETE FROM reports;` (DB vứt đi, chỉ chứa data fake) + audit của chúng tự sạch theo checklist audit mồ côi.
* Tuyệt đối không seed reports fake lên Neon/DEV; file seed (nếu có) để ngoài repo.
* Dọn đồ tạm trước PR theo `LOCAL-TEST-CLEANUP.md` (riêng mảng Reports không có code tạm nào — chỉ dọn data).
