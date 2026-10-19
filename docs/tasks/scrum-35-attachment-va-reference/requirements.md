# Requirements — Gắn file/link tham khảo vào Content

Trạng thái: **Đã triển khai theo yêu cầu trực tiếp của người dùng; xem `plan.md` để biết phạm vi và kết quả kiểm tra**.

## 1. Yêu cầu gốc

Gắn reference tới tài liệu/file hoặc trang web vào Content.

Acceptance Criteria:

1. Thêm/xóa reference.
2. Mở lại danh sách reference từ Content Detail.
3. Link lỗi không làm mất Content.

Quyết định của người dùng: **chỉ lưu URL/link tới file, không upload bytes lên hệ thống**.

## 2. Hiện trạng code tại lúc khảo sát

- Branch hiện tại `SCRUM-35-attachment-va-reference`, HEAD `f97bb7d` (`origin/develop`); working tree sạch.
- `database/01_schema.sql` đã có `content_attachments` gồm `attachment_id`, `content_id`, `display_name`, `attachment_type`, `resource_url`, `added_by`, `created_at`. `GlobalSearchRepository` đã tìm theo tên và URL reference.
- API Content Detail đã có endpoint, Service, Repository Npgsql, Contracts và ApiClient cho detail/script/activity; chưa có endpoint/service/client cho references.
- `ContentDetailPanel` là nơi hiển thị drawer chi tiết, hiện có Overview, Script, Checklist, Activity và Metrics. References cần được truy cập tại đây.
- API dùng `RequireAuthorization`; Service hiện kiểm tra membership của Project và quyền sửa Content. Quyền mới cần tiếp tục được kiểm tra ở Service.
- Repo hiện không có object/file storage riêng; theo quyết định trên, URL được lưu như metadata trong `content_attachments`.

## 3. Phạm vi chức năng đề xuất

### 3.1 Quản lý reference

- Lưu reference dạng `displayName` + URL tuyệt đối `http` hoặc `https`; không lưu file bytes hoặc đường dẫn cục bộ của máy người dùng.
- Cho phép liệt kê, thêm và xóa reference thuộc một Content.
- Sắp xếp danh sách theo thời điểm thêm, mới nhất trước.
- Validate tên không rỗng và tối đa 255 ký tự theo schema; validate URL có cú pháp tuyệt đối và scheme `http`/`https`.
- API không truy cập URL bên ngoài khi thêm hoặc đọc reference. Link chết, timeout hoặc host không tồn tại không làm thất bại việc tải Content hay xóa dữ liệu Content.
- Xóa reference chỉ xóa đúng hàng `content_attachments` loại `LINK` được chỉ định cho đúng Content; không xóa Content hoặc các attachment khác.

### 3.2 API và persistence

- Thêm Contracts cho danh sách/tạo reference; bổ sung phương thức repository interface và Npgsql implementation. SQL phải parameterized.
- Thêm các route được bảo vệ bằng authentication:
  - `GET /api/contents/{contentId}/references`
  - `POST /api/contents/{contentId}/references` với `{ displayName, resourceUrl }`
  - `DELETE /api/contents/{contentId}/references/{referenceId}`
  - `PUT /api/contents/{contentId}/references` với `{ additions, deletedReferenceIds }` để áp dụng draft thêm/xóa trong một transaction.
- API kiểm tra Content tồn tại và người dùng là thành viên Project trước khi trả danh sách hoặc thay đổi dữ liệu.
- Đề xuất quyền thay đổi theo quy tắc Content Detail hiện tại: Owner/Manager quản lý reference của Content trong Project; Creator quản lý reference của Content do mình tạo; Content Published chỉ đọc. Cần duyệt cùng requirements vì acceptance chưa chỉ rõ quyền.
- Dùng bảng `content_attachments` hiện có, loại `LINK`; không thêm migration nếu schema triển khai có đúng bảng/cột như `database/01_schema.sql`. Không tự apply schema vào database.

### 3.3 Content Detail và xử lý link lỗi

- Bổ sung khu vực/tab **References** trong Content Detail: xem danh sách, thêm link, xóa link và mở link bằng trình duyệt mặc định.
- Khi mở lại Content Detail, nạp lại references từ API để dữ liệu tồn tại xuyên phiên/máy.
- Thêm/xóa trong tab chỉ đổi draft cục bộ; nút Lưu mới gửi batch. Nếu lưu lỗi, giữ draft để người dùng thử lại; nếu đóng và bỏ thay đổi, không gửi API.
- Nếu API lấy danh sách reference lỗi, Content Detail và dữ liệu Content vẫn hiển thị; hiện lỗi riêng trong khu vực References, không gán dữ liệu Content thành rỗng và không cho thao tác reference làm mất Content.
- Nếu trình duyệt không mở được link hoặc URL đích không hoạt động, báo lỗi tại UI nhưng giữ reference và Content. Không kiểm tra link bằng HTTP request từ API.
- Mọi lời gọi API đi qua `ApiClient`/backend interface; UI không truy cập DB hoặc tự thực thi SQL.

## 4. Tiêu chí nghiệm thu và kiểm thử dự kiến

- API test qua TestServer: chưa đăng nhập trả 401; thành viên có quyền thêm/list/xóa thành công; non-member không đọc/sửa được; Content/reference không tồn tại trả lỗi phù hợp.
- Validation test: tên rỗng/quá dài, URL sai cú pháp, scheme không phải HTTP(S) bị từ chối; lỗi không tạo hàng reference.
- Lifecycle test: POST → GET thấy reference → DELETE → GET không còn reference; xóa ID thuộc Content khác không ảnh hưởng Content/reference còn lại.
- Batch test: thêm/xóa thành công cùng nhau; nếu một ID xóa không phải LINK hoặc không tồn tại, trả lỗi và rollback toàn bộ đợt lưu.
- Broken-link test: URL không hoạt động vẫn được lưu/đọc như reference; GET Content vẫn thành công và reference vẫn còn sau khi UI báo lỗi mở link.
- Repository integration test với PostgreSQL test được cấu hình riêng để xác minh lưu/đọc/xóa thực tế; chỉ chạy trên database test được duyệt.
- Client/UI kiểm tra thủ công: đóng rồi mở lại Content Detail, reference vẫn hiện; lỗi nạp/mở link không làm rỗng hoặc đóng Content.
- Build và test/runtime chỉ chạy sau cổng duyệt trong `post-change-test-workflow`; không tự áp dụng migration hoặc ghi database thật.

## 5. Ngoài phạm vi

- Upload/lưu binary file, object storage, preview nội dung, virus scanning hoặc tự đồng bộ link bên ngoài.
- Tự xác nhận URL còn sống bằng cách gọi mạng từ API.
- Thay đổi workflow/status, Content CRUD, tìm kiếm toàn cục, Project membership hoặc permission ngoài quyền tối thiểu nêu trên.
- Thêm package mới hoặc sửa cây source trùng `src/CreatorFlow/CreatorFlow/` đang bị exclude khỏi build.

## 6. Giả định và tài liệu tham chiếu

- “Gắn file” được hiểu là lưu tên hiển thị cùng URL tới file đã nằm trên dịch vụ/ngữ cảnh truy cập được của người dùng; app không nhận nội dung file.
- Schema hiện có được tái sử dụng; nếu database mục tiêu thiếu `content_attachments`, cần dừng và lập migration/rollout riêng trước khi triển khai API.
- `AGENTS.md` tham chiếu `docs/CreatorFlow_KeHoachDuAn (1).md`, `docs/CreatorFlow_QuyTacLamViec (1).md`, `docs/CreatorFlow_UX.md` và `docs/ui/creatorflow-ui-styleguide.html`; các file này không có trong checkout hiện tại. UI plan sẽ bám theo control Content Detail đang có và ghi rõ giới hạn tài liệu.

## 7. Quyết định triển khai

- Người dùng chọn chỉ lưu URL/link; không upload file bytes.
- References được đặt thành tab riêng trong Content Detail.
- Quyền quản lý reference theo quy tắc hiện tại của Content Detail: Owner/Manager trên mọi Content trong Project; Creator trên Content do mình tạo; Published/Archived chỉ đọc. API kiểm tra quyền, UI chỉ hiển thị theo cờ `CanManage` của API.
- Không thêm migration vì `content_attachments` đã có trong `database/01_schema.sql`.
