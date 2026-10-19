# Bug report — Reference API không giới hạn loại attachment

## Phát hiện

API reference có thể trả về hoặc xóa các hàng `content_attachments` không thuộc loại `LINK`.

## Bằng chứng và nguyên nhân tại thời điểm điều tra

- Yêu cầu và plan của task quy định reference dùng `attachment_type='LINK'`.
- `AddReferenceAsync` có ghi rõ loại `LINK`.
- `GetReferencesAsync` khi đó lọc theo `content_id` nhưng không lọc `attachment_type`.
- `DeleteReferenceAsync` khi đó cũng chỉ giới hạn theo `content_id` và `attachment_id`.
- Database test khi đó chưa tạo attachment loại khác để kiểm tra trường hợp này.

Vì vậy, nếu Content có hàng attachment thuộc loại khác, endpoint GET có thể hiển thị nó như URL reference; endpoint DELETE có thể xóa hàng đó khi nhận đúng ID.

## Cách sửa đã áp dụng

1. Giới hạn GET, DELETE đơn và batch DELETE bằng `attachment_type = 'LINK'`.
2. Bổ sung database integration test đảm bảo attachment không phải `LINK` không được trả về/xóa, đồng thời batch rollback nếu gặp ID đó.
3. Chạy API tests và database integration test sau khi được duyệt theo cổng kiểm chứng.

## Phạm vi và rủi ro

Chỉ đổi truy vấn reference và kiểm thử repository cho `content_attachments`; không cần schema migration. Cần xác minh schema database mục tiêu có cùng cột `attachment_type` như `database/01_schema.sql`.

## Trạng thái kiểm chứng

- Đã giới hạn GET, DELETE đơn và batch DELETE theo `attachment_type = 'LINK'`.
- Đã bổ sung database integration test kiểm tra loại attachment, rollback batch và giữ nguyên hàng `FILE`.
- Test/build chưa chạy sau các thay đổi hiện tại; các kết quả chạy trước đó không xác minh code mới.
- Database integration test cần database disposable cùng `CREATORFLOW_TEST_CONNECTION_STRING` và `CREATORFLOW_TEST_ALLOW_WRITES=true`.

## Phát hiện bổ sung — Reference được lưu ngay trước khi bấm Lưu

### Tái hiện

1. Mở Content Detail → tab **References**.
2. Nhập tên và URL, bấm **Thêm reference**.
3. Reference xuất hiện ngay trong danh sách và đã được ghi qua API; nút **Lưu thay đổi** vẫn bị vô hiệu nếu không có thay đổi Content khác.
4. Nút **Xóa reference** cũng gọi API xóa ngay. Các thay đổi reference không được tính vào `HasUnsavedChanges`.

### Root cause

- `ContentReferencesControl.AddReference` gọi `_backend.AddReference(...)` rồi tải lại danh sách ngay.
- `ContentReferencesControl.DeleteSelectedReference` gọi `_backend.DeleteReference(...)` ngay sau xác nhận.
- `ContentDetailPanel.SaveChanges` chỉ gọi API cập nhật `ContentDraft`; không nhận hay lưu thay đổi references.
- `ContentDetailPanel.HasUnsavedChanges` chỉ kiểm tra `_isDirty` và `_scriptEditor.IsDirty`, nên thay đổi references không bật **Lưu thay đổi** hoặc hiện hộp thoại bỏ thay đổi.

### Hướng sửa đề xuất

- Đổi Thêm/Xóa thành thao tác trên danh sách draft trong UI, không ghi API ngay.
- Báo cho `ContentDetailPanel` khi draft references thay đổi để bật **Lưu thay đổi** và đưa references vào `HasUnsavedChanges`.
- Khi Save thành công, gửi các thay đổi reference qua API, refresh danh sách và xóa trạng thái draft. Khi đóng/bỏ thay đổi, không gọi API và hủy draft.
- Kiểm thử bằng API fake: trước Save không có POST/DELETE; sau Save có ghi; hủy/đóng không lưu; Save thất bại thì giữ draft và trạng thái chưa lưu.

### Phạm vi và trạng thái

Phạm vi là `ContentReferencesControl`, `ContentDetailPanel`, luồng Board và backend/API test của task này. UI giữ additions/deletions ở draft, đưa chúng vào trạng thái chưa lưu và chỉ gửi batch khi bấm Lưu; Board hỏi trước khi tải lại Content đang có draft. Repository chạy additions/deletions trong transaction, chỉ đọc/xóa hàng `LINK`, và rollback nếu ID xóa không phải reference hợp lệ. Đã bổ sung route test cho nhánh thiếu ID/link lỗi, API-client test cho lỗi `reference_not_found` và database integration test cho rollback cùng loại attachment. Test/build chưa chạy trong lượt này; cần người dùng đồng ý theo cổng `post-change-test-workflow` trước khi kiểm chứng runtime.

## Phát hiện bổ sung — Test mở archived Content thiếu response references

### Tái hiện và bằng chứng

- Test `ContentLibrary_LoadsProjectContentAndOpensArchivedContentDetailThroughApi` mở Content 901 có trạng thái Archived.
- Test Explorer báo `ArgumentNullException` với parameter `source`; stack trace đi từ `ApiContentDetailBackend.GetReferences` qua `ContentReferencesControl.LoadReferences` khi mở Content Detail.
- `ApiContentDetailBackend.GetReferences` gọi `response.References.Select(...)`.
- `ProjectShellTests.UxHandler` có response cho detail, script và activity của Content 901 nhưng không có nhánh `/api/contents/901/references`. Request rơi xuống fallback trả response dạng Project, không có `references`, nên deserialize thành `References == null`.

### Root cause

Test fixture chưa được cập nhật để mô phỏng endpoint references mới. Lỗi nằm trong HTTP mock của test, không phải bằng chứng endpoint production trả danh sách null.

### Hướng sửa đề xuất

- Thêm nhánh `GET /api/contents/901/references` trong `UxHandler`, trả `ContentReferencesResponse` rỗng và `CanManage = false` cho Content Archived.
- Bổ sung assertion rằng test đã gọi đúng route references khi mở Content Detail.
- Không đổi backend production; contract API yêu cầu danh sách `References` không null và route thật trả danh sách đó.

### Phạm vi, rủi ro và kiểm chứng

Phạm vi là test fixture và assertion trong `ProjectShellTests`; không thay đổi API, service, repository hoặc dữ liệu. Screenshot ghi nhận 833 test pass, 1 fail, 64 skipped. Chưa chạy lại test trong lượt điều tra; cần người dùng đồng ý theo `post-change-test-workflow` trước khi sửa và xác minh.
