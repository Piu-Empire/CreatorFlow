# SCRUM-29 — Ma trận quyền của các thao tác hiện có

Nguồn danh tính chung: JWT đã được middleware kiểm chữ ký/issuer/audience/lifetime, `sub`, token version, trạng thái tài khoản và email verified; Service đọc `CurrentAuthenticatedUser`. Claim role không cấp quyền Project. Nguồn role: membership `project_members` đang hoạt động của đúng Project, đọc mỗi request qua repository; không cache role trên Backend giữa các request. System Admin không có bypass Project.

`P` dưới đây là Project trên route với thao tác cấp Project, hoặc Project đọc từ record tài nguyên phía server với thao tác qua ID con. `O/M/C` là Owner/Manager/Creator của P. Mọi thao tác đều cần authenticated actor; role/member ID trong payload quản trị chỉ là đối tượng mục tiêu.

| API / thao tác | Service quyết định | O | M | C | Nguồn Project và giới hạn |
| --- | --- | --- | --- | --- | --- |
| GET `/api/projects` | ProjectService.ListAsync | Có | Có | Có | Chỉ Project có membership actor hoạt động |
| POST `/api/projects` | ProjectService.CreateAsync | User đăng nhập | User đăng nhập | User đăng nhập | Tạo mới gắn Owner với actor, không nhận owner của client |
| GET `/api/projects/{P}`, `/{P}/access` | ProjectService.GetAsync/AccessAsync | Có | Có | Có | Access yêu cầu Project ACTIVE |
| PUT/DELETE `/api/projects/{P}`; POST archive/restore | ProjectService.Update/Delete/Archive/Restore | Có | Không | Không | Membership đọc trong transaction; restore yêu cầu ARCHIVED |
| GET members; PUT member role; DELETE member | ProjectMembersService | Có | Không | Không | Actor membership và member mục tiêu cùng P; giữ Owner cuối cùng |
| GET/POST `/api/projects/{P}/invitations` | InvitationService | Có | Không | Không | Invite target role chỉ Manager/Creator |
| GET `/api/projects/{P}/board`, `/{P}/contents` | BoardService | Có | Có | Có | Chỉ đọc Project actor tham gia, giữ quyền đọc Creator hiện có |
| GET ideas/idea-tags; POST ideas | IdeaService | Có | Có | Có | Tạo Idea dùng actor phía server |
| PUT/DELETE `/api/ideas/{id}`; POST convert | IdeaService + IdeaRules | Có | Có | Idea của mình | Lấy P từ Idea; giữ quy tắc Converted và chuyển Idea hiện có |
| GET `/api/projects/{P}/my-tasks`, my-role | TaskService | Có | Có | Có | My Tasks chỉ của actor trong P |
| GET assignable-members | TaskService | Mọi Creator | Mọi Creator | Chính mình | Danh sách mục tiêu không cấp quyền mutation |
| GET board/assignees, content assignments | TaskService | Có | Có | Có | Lấy P từ route/Content; giữ quyền đọc hiện có |
| PUT `/api/contents/{id}/assignments` | TaskService.AssignContentAsync | Có | Có | Chỉ tự nhận Content do mình tạo khi chưa có người khác | P từ Content; target phải là Creator của P; Creator không sửa deadline assignment đã tồn tại |
| PUT assignment deadline | TaskService.ChangeDeadlineAsync | Có | Có | Không | P từ assignment phía server |
| PUT content progress | TaskService.UpdateProgressAsync | Theo assignment của actor | Theo assignment của actor | Assignment của mình | Giữ nghiệp vụ hiện có: active membership + assignment của actor; không nhận actor từ body |
| POST `/api/projects/{P}/contents` | ContentDetailService.CreateAsync | Có | Có | Có, chỉ tự nhận nếu gán người | Giữ planning ban đầu hiện có; không tạo trực tiếp Review |
| GET content detail/script/activity | ContentDetailService | Có | Có | Có | P từ resource, membership trước trả dữ liệu |
| PUT content detail | ContentDetailService.UpdateAsync | Có | Có | Chỉ Content do mình tạo | Creator không đổi deadline; giữ các quy tắc dữ liệu/planning hiện có |
| PUT content script | ContentDetailService.SaveScriptAsync | Có | Có | Do mình tạo hoặc được giao | P từ Script; Published/Archived khóa sửa |
| PUT content status; POST content review | WorkflowService | Có | Có | Có | P từ Content; giữ workflow/submit hiện có, không tự thu hẹp thành chỉ assignee |
| PUT content review/decision | WorkflowService.DecideReviewAsync | Có | Có | Không | P từ Content; Reject bắt buộc feedback |
| GET content status-history | WorkflowService | Có | Có | Có | P từ Content, active membership |
| GET content/platform metrics | MetricService | Có | Có | Có | P từ Content; platform phải thuộc Content |
| POST content/platform metrics | MetricService.RecordAsync | Có | Có | Không | Giữ điều kiện Published/eligible hiện có |

Không membership hoạt động: từ chối mọi thao tác trên Project đó, kể cả khi actor là System Admin hoặc Owner của Project khác. Thao tác tạo Project mới không cần membership trước đó. Profile, invitation inbox của người nhận và quản trị hệ thống dùng quyền cấp user/hệ thống riêng, không lấy role Project làm quyền hệ thống.

## Điểm bổ sung trong task

- Chặn Creator thay deadline assignment đã tồn tại qua đường tự nhận việc; `null` giữ deadline cũ theo convention hiện có, không xóa deadline.
- Owner/Manager/Creator guard đã có ở các Service còn lại được giữ. Không thêm authorization framework hoặc hợp nhất repository ngoài nhu cầu.
- UI: vô hiệu hóa Review Queue cho Creator; guard tại handler quản trị thành viên/lời mời và review/giao việc; xóa role cache/action khi context đổi hoặc bị từ chối, revalidate qua API sau 403, không retry mutation.
- Phiên API không đọc danh sách thành viên từ local repository khi thiếu assignment backend. Các nhánh submit/approve/reject qua API và guard không fallback của Board được giữ nguyên.

## Giới hạn bằng chứng

Đã đối chiếu tĩnh SQL của Project/Task/Workflow/Invitation repository: actor + Project + `is_active` được dùng khi lookup role. Test mới dùng repository fake để mô phỏng mất membership; chưa chạy kiểm chứng PostgreSQL thật. Test resubmit ở Service xác nhận nhánh gọi và quyền; tính nguyên vẹn transaction/lịch sử Review trong DB chưa được kiểm chứng bởi fake này.
