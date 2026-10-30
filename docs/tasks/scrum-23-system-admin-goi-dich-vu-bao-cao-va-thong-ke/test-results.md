# SCRUM-23 — Kết quả kiểm chứng

Ngày chạy: 30/10/2026. Môi trường: máy local, Windows, .NET 10.0.

## Cổng 1 — kiểm tra tĩnh (ĐẠT)

| Kiểm tra | Lệnh | Kết quả |
| :--- | :--- | :--- |
| Build solution | `dotnet build CreatorFlow.slnx` | Build succeeded — 0 error, 1 warning (CS0067 `NotificationPanelControl.CloseRequested` chưa dùng, có từ trước) |
| Test admin (toàn bộ nhóm) | `dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --filter "TestCategory=AdminApiUnit"` | 71 passed, 0 failed |
| Test 3 service mới | lọc theo `AdminPlanServiceTests`, `AdminSubscriptionServiceTests`, `AdminStatisticsServiceTests` | 30 passed, 0 failed |

Lỗi biên dịch phát hiện và đã sửa trong lần build đầu:

- `src/CreatorFlow/Controls/AdminCatalogDialogs.cs`: `PlanItem.Plan.Id` → `Plan.PlanId` (thuộc phần UI của chính task này, viết trước khi chạy build).

Phạm vi test mới:

- `AdminPlanServiceTests`: chặn non-admin/anonymous, code rỗng, tên rỗng, trùng code không phân biệt hoa/thường, số âm, tạo hợp lệ (trim + uppercase code), update giữ field null và bật/tắt, 404, DB lỗi → 503.
- `AdminSubscriptionServiceTests`: chặn non-admin/anonymous, lọc trạng thái sai, user/gói không tồn tại, gói inactive, đã có gói ACTIVE, hạn dùng quá khứ, tạo hợp lệ, update không có thay đổi/trạng thái sai/hạn trước ngày bắt đầu, bật lại khi còn gói ACTIVE khác, đổi trạng thái hợp lệ, 404, DB lỗi → 503.
- `AdminStatisticsServiceTests`: chặn non-admin/anonymous, map đủ 5 nhóm, dữ liệu rỗng trả 0 và list rỗng, DB lỗi → 503.

## Cổng 2 — kiểm tra runtime (CHƯA CHẠY)

Chưa thực hiện vì cần DB local có user System Admin và dữ liệu thật; sẽ chạy khi người dùng xác nhận môi trường. Các bước còn lại đúng theo `plan.md` §3:

1. Tạo user System Admin, đăng nhập.
2. Tạo gói mới qua UI → đối chiếu `SELECT * FROM plans`.
3. Gán gói cho user → đối chiếu `SELECT * FROM subscriptions`.
4. Gán gói thứ hai khi đang ACTIVE → phải bị từ chối.
5. Tắt gói → gán gói đó cho user khác → phải bị từ chối.
6. Mở màn Thống kê → đối chiếu từng số với `SELECT count(*)`.
7. Xử lý report bằng admin → kiểm `reports.status/handled_by/handled_at`.
8. User thường gọi thẳng `/api/admin/plans|subscriptions|statistics` → 403.

## Ghi chú

- Đã nối 2 màn mới vào `SidebarControl` và `BoardForm` (mở/đóng, chặn khi không phải System Admin, đóng khi chuyển trang).
- Chưa commit/push; chưa áp dụng migration hay ghi dữ liệu DB.
