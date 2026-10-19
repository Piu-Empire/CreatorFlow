# Plan — SCRUM-35 Gắn file/link tham khảo vào Content

Trạng thái: **Đã triển khai; các kết quả build/API tests bên dưới là lần chạy trước lượt rà soát tiếp theo. Các cập nhật batch, LINK-only và giữ draft hiện chưa được kiểm chứng lại.**

## Phạm vi triển khai

1. Tái sử dụng `content_attachments`; lưu `display_name`, `resource_url`, `added_by`, `created_at`, với `attachment_type='LINK'`.
2. Thêm DTO, repository methods (SQL parameterized), Service validation/permission và routes GET/POST/DELETE cùng PUT batch dưới `/api/contents/{contentId}/references`.
3. Thêm methods vào `ApiClient` và `IContentDetailBackend`; map DTO sang model UI.
4. Thêm tab **References** trong Content Detail để xem, thêm/xóa bằng draft tới khi bấm Lưu, và mở HTTP(S) link bằng trình duyệt mặc định.
5. Tách lỗi tải/mở reference khỏi dữ liệu Content. API không gọi URL bên ngoài; lỗi link không xóa Content hoặc reference. Đóng/bỏ draft không gửi thay đổi lên API.
6. Thêm service/routing/API-client tests cho batch thành công và lỗi ID xóa; PostgreSQL repository integration test kiểm tra transaction, LINK-only và có opt-in write guard.

## Quyền và validation

- Thành viên Project được xem reference.
- Owner/Manager được quản lý reference trong Project; Creator chỉ được quản lý reference của Content do mình tạo.
- Published/Archived chỉ đọc.
- Tên bắt buộc, tối đa 255 ký tự; URL phải là địa chỉ tuyệt đối HTTP(S).
- GET/DELETE lọc theo `content_id`, `attachment_id` và `attachment_type='LINK'`; batch chạy trong transaction và rollback nếu thiếu bất kỳ reference cần xóa nào.

## Kết quả kiểm tra

- `dotnet test tests/CreatorFlow.Api.Tests --filter "FullyQualifiedName!~.Database."`: **413 passed, 0 failed**.
- `dotnet test tests/CreatorFlow.IntegrationTests --filter "TestCategory=ApiFoundation"`: **40 passed, 0 failed**.
- `dotnet build CreatorFlow.slnx`: **0 errors, 8 warnings** (NU1902/NU1903 về `System.Security.Cryptography.Pkcs` 10.0.0 trong hai test projects).
- PostgreSQL integration test được thêm nhưng không chạy vì cần `CREATORFLOW_TEST_CONNECTION_STRING` trỏ tới database test disposable và `CREATORFLOW_TEST_ALLOW_WRITES=true`.
- Chưa chạy ứng dụng WinForms để kiểm tra UI bằng tay.
- Các thay đổi review tiếp theo chưa chạy build/test; cần kiểm chứng lại trước khi coi là hoàn tất.
