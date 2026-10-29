# Bug report: Project Shell navigation tests fail for Invitations

## Triệu chứng

Test Explorer báo fail hai test `IntegratedShell_RealProjectKeepsBoardAndDevelopNavigation_WithoutDemo`, tương ứng hai `DataRow(false)` và `DataRow(true)` trong `ProjectShellTests.cs`.

## Root cause và bằng chứng

- Cả hai trường hợp đều kiểm tra danh sách điều hướng có mục `Invitations` bằng `CollectionAssert.Contains` tại `tests/CreatorFlow.IntegrationTests/ApiClients/ProjectShellTests.cs:403`.
- Bản hiện tại của `src/CreatorFlow/Controls/SidebarControl.cs` đã xóa mục `Invitations` khỏi `_items`.
- `git diff` xác nhận đây là thay đổi cục bộ một dòng; phiên bản tại `HEAD` vẫn có mục và callback `InvitationsRequested`.
- Các thay đổi Repurpose không sửa `SidebarControl.cs`; hai test này không kiểm tra luồng Repurpose.

## Cách xử lý

Người dùng xác nhận giữ việc xóa mục `Invitations`. Cập nhật test để xác nhận các mục điều hướng còn lại hiện diện và `Invitations` không xuất hiện. Không thay đổi `SidebarControl.cs` hay phần giao diện.

## Trạng thái

Assertion đã được cập nhật theo UX người dùng chọn. Filter `IntegratedShell_RealProjectKeepsBoardAndDevelopNavigation_WithoutDemo` pass 2/2 (admin và non-admin); không chạy database test.
