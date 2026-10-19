# SCRUM-29 — Kiểm tra quyền Owner/Manager/Creator theo Project

## Mục tiêu

Thực thi phân quyền theo từng Project tại Service của Backend API. WinForms ẩn hoặc vô hiệu hóa thao tác theo quyền để cải thiện UX; quyết định cho phép cuối cùng luôn thuộc Backend.

## Cơ sở khảo sát

- Đã đối chiếu README, kế hoạch dự án, quy tắc làm việc, phần Project context/role-based UX trong `docs/CreatorFlow_UX.md` và UI Style Guide.
- `CurrentAuthenticatedUser` đã có authenticated context theo HTTP request. `ProjectService`, `ProjectMembersService` đã kiểm tra Owner; `WorkflowService` kiểm tra Owner/Manager khi quyết định Review. Các module Task, Idea, Content Detail, Invitation cũng có kiểm tra riêng. Đây là nền tảng cần kiểm kê và bổ sung, không mặc định viết lại toàn bộ.
- WinForms có `ProjectContext` và một số kiểm tra quyền tại Project Settings, Content Detail. Quyền lưu ở client chỉ phục vụ UX, không phải bằng chứng cấp quyền cho API.
- README còn mô tả giai đoạn chuyển tiếp có đường truy cập DB/business logic phía client. Cần xác định đường chạy thực tế trong bước lập plan; thao tác thuộc phạm vi task phải đi qua Backend để tránh bỏ qua authorization.
- Branch hiện tại là `SCRUM-82-Thiết-lập-CI-bằng-GitHub-Actions-cho-CreatorFlow`; chưa tạo/chuyển branch. Trước triển khai cần người dùng yêu cầu rõ thao tác Git và cung cấp tên branch Jira chính xác cho SCRUM-29.

## Phạm vi

1. Kiểm kê các thao tác Project đang được triển khai và chốt ma trận quyền cụ thể trong plan, dựa trên nghiệp vụ hiện có.
2. Bổ sung/đồng bộ authorization trong Backend Service cho các thao tác đó; bảo vệ cả API gọi trực tiếp và Service được gọi trực tiếp.
3. Lấy actor từ authenticated context đã xác thực. Đọc membership đang hoạt động và role từ `project_members` đúng Project; không dùng UserId hoặc project_role do client gửi làm danh tính/quyền của actor.
4. Với thao tác qua content/task/script/review ID, xác định Project từ tài nguyên phía server và kiểm tra đúng membership; không cho đổi Project ID để truy cập tài nguyên Project khác.
5. Đồng bộ trạng thái ẩn/disable và chặn thao tác UI theo Current Project. Khi đổi Project, đăng xuất, bị gỡ membership hoặc đổi role, quyền cũ không được cho phép request tiếp theo trên Backend; UI xử lý bị từ chối và cập nhật context phù hợp.
6. Bổ sung kiểm chứng theo ma trận quyền, giả mạo actor/role và truy cập chéo Project. Chỉ chạy build/test/runtime sau khi người dùng duyệt phạm vi kiểm chứng.

### Ma trận quyền yêu cầu

| Nhóm thao tác | Owner | Manager | Creator |
| --- | --- | --- | --- |
| Quản trị Project: sửa, archive/restore, xóa, cấu hình quản trị hiện có | Có | Không | Không |
| Quản trị thành viên, đổi role, gỡ thành viên, quản lý lời mời | Có | Không | Không |
| Giao việc cho người khác, quản lý deadline/planning và quyết định Review | Có | Có | Không |
| Xem và thao tác nội dung/công việc theo phạm vi nghiệp vụ hiện có | Có | Có | Chỉ phần Creator được phép |

Owner kế thừa quyền nghiệp vụ của Manager. System Admin là quyền hệ thống riêng, không tự động vượt kiểm tra membership Project. Người không có membership hoạt động không có quyền Project.

### Ngoài phạm vi

- Không tạo custom role, màn hình/module nghiệp vụ mới, đổi workflow hoặc mở rộng quyền Creator.
- Không thay hệ thống đăng nhập/token nếu không có nhu cầu trực tiếp để thực thi yêu cầu này; phát sinh thay đổi kiến trúc đáng kể phải trình duyệt trong plan.
- Không tự migration, sửa dữ liệu thật, thêm package, commit/push/PR hoặc cập nhật Jira.

## Giả định và điểm cần chốt

- “Creator không dùng chức năng Manager” nghĩa là cấm thao tác quản lý dành riêng Manager; không cấm quyền Creator hợp lệ đã có như tạo Idea và sửa Idea của mình, sửa Script theo quyền, cập nhật progress, submit/resubmit. Các quyền đọc Calendar/Performance theo tài liệu vẫn được giữ.
- UserId trong route/body có thể là đối tượng cần giao việc hoặc quản trị thành viên. Backend vẫn được nhận ID đối tượng đó nhưng phải kiểm tra actor và đối tượng độc lập; không loại bỏ máy móc mọi trường UserId.
- Role trong request đổi role/mời thành viên là dữ liệu mục tiêu, không phải role của người gọi. Chỉ actor có quyền Owner mới được thực hiện.
- Chưa xác nhận lỗi cụ thể ở các kiểm tra hiện có; khảo sát hiện tại chỉ xác định nền tảng và phạm vi cần kiểm kê.
- Không yêu cầu cơ chế realtime/push khi quyền đổi; Backend phải áp dụng quyền hiện hành ở request tiếp theo.

## Tiêu chí hoàn thành

1. Cùng user có role khác nhau ở hai Project được cấp đúng quyền từng Project, không rò quyền giữa các Project.
2. Creator không giao việc cho người khác, đổi deadline thuộc quyền quản lý hoặc approve/reject Review qua UI hay gọi API trực tiếp.
3. Manager không sửa/archive/restore/xóa Project hoặc quản trị membership/role/lời mời dành riêng Owner.
4. Owner thực hiện được quyền Manager và Owner hợp lệ, giữ các bất biến Owner/membership và lịch sử nghiệp vụ hiện có.
5. API không xác thực từ chối với 401; đã xác thực nhưng thiếu quyền bị từ chối theo quy ước 403/404 phù hợp, không thay đổi dữ liệu và không lộ dữ liệu ngoài quyền.
6. Giả mạo UserId/project_role/header/body hoặc gọi bằng resource ID của Project khác không nâng quyền.
7. Đổi role/gỡ membership có hiệu lực với request tiếp theo dù UI đang giữ role cũ; UI hiển thị thông báo phù hợp và không báo thành công giả.
8. Các thao tác Creator hợp lệ và điều hướng Project theo UX tiếp tục hoạt động; UI tuân thủ Style Guide.
9. Có kết quả kiểm chứng trung thực cho Service, API và UI sau cổng duyệt test; nêu rõ phần chưa chạy và dependency môi trường, không coi test bị bỏ qua là pass.

## Trạng thái

Requirements và plan đã được người dùng duyệt. Đã triển khai và thêm test trên branch Jira được cung cấp; chờ duyệt `test-plan.md`, chưa chạy build/test/runtime/API/database.
