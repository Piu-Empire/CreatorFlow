# SCRUM-61 — Ma trận tích hợp và baseline

Requirements và plan đã được duyệt. Tài liệu này ghi khảo sát tĩnh ngày 2026-10-26, chưa phải kết quả build/test/runtime.

## Baseline

### Cập nhật sau khi xác minh GitHub và được phép tạo branch

- SCRUM-29 đã merge vào develop qua PR #38: https://github.com/Piu-Empire/CreatorFlow/pull/38, merge commit `5f06294`. GitHub API trả `state=closed` và `merged_at` có giá trị. Nhận định từ develop local cũ không phản ánh trạng thái PR trên GitHub.
- Đã đọc remote develop bằng `git ls-remote`, fetch riêng ref develop và tạo/chuyển branch được người dùng cho phép: `SCRUM-61-Tích-hợp-các-module-trên-develop`, từ `origin/develop` tại `646b365e2567fa5a2b802befdb53a7cfe10a9787`.
- Không merge lại SCRUM-29. Source tích hợp xuất phát từ develop mới nhất đã đọc; develop local cũ không được dùng làm baseline triển khai.
- Thay đổi AGENTS.md và tài liệu untracked được Git giữ nguyên khi chuyển branch; chưa commit/push/PR/DB/build/test.
- Commit develop mới nhất là PR #40; ngoài SCRUM-29, có thêm Metrics summary/engagement, Calendar/Dashboard và AI Idea/Title/Outline/Script. So với HEAD SCRUM-29 cũ có 42 file source thay đổi. Các quan sát legacy ở dưới cần đối chiếu lại source mới trước khi sửa.
- Program mới vẫn giữ UseDatabase và khởi tạo in-memory/local services; đã thêm AIFactory và ScriptAiService thật. Không được thay AI thật bằng NotConfiguredAiService hoặc xóa đường AI để loại dependency local. AI hiện là POC chưa migrate API; việc chuyển toàn bộ AI qua Backend là scope riêng trừ khi được duyệt bổ sung.
- Calendar và Dashboard dùng nguồn card chung; cần giữ refresh/context. ProjectReportsControl dùng ApiMetricBackend.GetSummary; giữ endpoint metrics summary hiện có.

### Baseline khảo sát ban đầu (đã cũ, giữ để đối chiếu)

| Hạng mục | Giá trị quan sát |
| --- | --- |
| Branch hiện tại | `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator` |
| HEAD | `c5282fee1df73e9e20d69642bec92313f8b0180b` |
| develop local | `b227de3190102898b7d63511d2e276450941aacf` |
| Remote | Chưa fetch/pull hoặc xác minh; không coi develop local là mới nhất. |
| Git conflict | Khảo sát ban đầu `git ls-files -u` không có entry. |
| Thay đổi sẵn có | AGENTS.md modified; `.agent/`, tài liệu UX/kế hoạch/quy tắc, `docs/bugs/`, nhiều thư mục task, `docs/ui/` và tài liệu setup untracked. Giữ nguyên, không gom vào commit hoặc overwrite. |
| Source khác develop local | Có khác biệt ở Backend, Contracts, WinForms và tests; chưa chốt đây là toàn bộ nguồn cần ghép. |
| Thông tin còn chờ | Tên branch Jira SCRUM-61 chính xác, nguồn branch/PR cần tích hợp và yêu cầu tạo/chuyển branch. |

## Ma trận đường API đã tìm thấy

Tên đường dẫn dưới đây tương đối với repository. Việc tìm thấy source không chứng minh hành vi runtime đúng. Các nhóm route protected được khai báo RequireAuthorization tại endpoint; Auth public và health là ngoại lệ có chủ đích.

| Module/thao tác | Client entry/facade/adapter | API và Backend | Việc cần hoàn tất |
| --- | --- | --- | --- |
| Login/Register/verify/reset/Profile | Forms Auth, Profile; `Services/AuthApiFacade.cs`; `ApiClients/ApiClient.Auth.cs` | AuthEndpoints/UserEndpoints → Services/Auth → Repositories/Auth | Rà vòng đời session, profile/avatar và lỗi chung, response phiên cũ. |
| Project CRUD/access/membership | ProjectSelection/Settings/Members; `ProjectApiFacade`; `BoardForm.Projects.cs` | `/api/projects`, access/members/role → ProjectService/ProjectMembersService → Projects/ProjectRepository | Giữ permission SCRUM-29, chọn Project hợp lệ, 403/revalidate và đồng bộ context. |
| Invite/inbox | Invitation dialogs; ProjectInvitationService/InvitationInboxService; ApiClient.Invitations/MyInvitations | InvitationEndpoints/MyInvitationEndpoints → Services/Invitations → Repositories/Invitations | Rà token và cập nhật membership/context sau accept. |
| Board/content library | BoardForm/BoardForm.ContentLibrary; ApiBoardBackend; ApiClient.Board | GET projects/{id}/board, projects/{id}/contents → BoardService → BoardCardRepository | Bỏ `_boardRepository.GetBoardCards` và đường local; kiểm tra refresh và response Project cũ. |
| Global Search | BoardForm.GlobalSearch → AuthApiFacade.SearchGlobalAsync → ApiClient.Search | GET `/api/search` → GlobalSearchService → GlobalSearchRepository | Giữ kiểm tra session/context khi điều hướng sang kết quả ở Project khác. |
| Idea CRUD/convert | IdeaBankControl; ApiIdeaBackend; ApiClient.Ideas | IdeaEndpoints → IdeaService → IdeaRepository | Bỏ chọn IdeaService local khi projects null; kiểm tra convert trả Content và Board đọc lại cùng dữ liệu. |
| Content create/detail/update/platform | BoardForm/CreateContentDialog/ContentDetailPanel; ApiContentDetailBackend; ApiClient.ContentDetail | ContentDetailEndpoints → ContentDetailService → ContentDetailRepository | Bỏ dependency local ContentService/repository, không chỉ bỏ khởi tạo trong Program. |
| Script open/save | ScriptEditorControl → ApiContentDetailBackend | GET/PUT contents/{id}/script → ContentDetailService → ContentDetailRepository | Editor vẫn bắt buộc ScriptService để Initialize/Open/Save; tách API wiring và utility thuần, giữ version conflict. AI hiện còn gọi service local, cần đối chiếu hành vi đã tồn tại trước xử lý. |
| References/activity | ContentReferencesControl/ContentDetailPanel → ApiContentDetailBackend | contents/{id}/references, activity → ContentDetailService → ContentDetailRepository | Bỏ local activity dependency và kiểm tra refresh sau ghi/reference batch. |
| Assignment/My Work/progress/deadline | ApiAssignmentBackend/ApiMyTaskBackend; ApiClient.Tasks | TaskEndpoints → TaskService → TaskRepository | Dùng API cho danh sách member/assignment và My Work, không giữ MyTaskService local ở app. |
| Workflow/submit/decide/history | BoardForm/ContentDetailPanel → ApiContentDetailBackend | WorkflowEndpoints → WorkflowService → WorkflowRepository | Bỏ workflow local; thao tác hiển thị next status không được giữ local business service chỉ vì helper; Backend tiếp tục quyết định trạng thái/quyền. |
| Review Queue | ReviewQueueForm → ApiReviewQueueBackend | GET projects/{id}/reviews/pending → ReviewQueueService/ReviewQueueRepository; quyết định qua Workflow API | Bỏ constructor/path dùng repository + WorkflowService local khỏi runtime. |
| Metrics | ContentDetailPanel → ApiMetricBackend → ApiClient.Metrics | MetricEndpoints → MetricService → MetricRepository, TaskRepository | Giữ ContentPlatform identity, quyền ghi và refresh; kiểm tra lỗi chung. |
| Notifications/Overdue | NotificationPanel/BoardForm → ApiNotificationBackend; ApiClient.Notifications | NotificationEndpoints/OverdueEndpoints → NotificationService/OverdueService → Notification/Workflow repositories | Notification API đã được inject; trace riêng client consumer Overdue trước đánh dấu hoàn tất. |
| Admin/Reports | AdminUserManagementControl/AdminReportsControl → AuthApiFacade; ApiClient.Admin/Reports | AdminEndpoints/ReportEndpoints → Services/Admin → Repositories/Admin | Giữ quyền hệ thống độc lập Project; rà token và lỗi chung. |
| Calendar | CalendarControl.CardProvider lấy ContentBoardCard từ BoardForm; click mở drawer | Dùng nguồn Board/content API, chưa thấy nhu cầu endpoint Calendar riêng từ control | Control không mở DB; cần bảo đảm provider trả dữ liệu API đúng Project, refresh sau ghi, không local repo. |
| Summary/AI và view khác | Chưa trace đầy đủ | Chưa kết luận coverage hoặc migration | Không tự tạo module/endpoint mới; chỉ hoàn tất trace các điểm vào thực tế và báo phần thiếu nếu bắt buộc đạt AC. |

## Dependency cần tháo trước khi gỡ Npgsql

1. `Program.cs` khởi tạo local WorkflowService, ContentService, MyTaskService, ScriptService, IdeaService và toàn bộ repositories trước khi dựng API/session. UseDatabase=false vẫn để local business path tồn tại và Npgsql được compile.
2. BoardForm constructor nhận local services/repositories cùng API backend optional. ReloadBoard, review/workflow, create content, My Work, Idea Bank và Content Library có các nhánh local. Luồng app thật cần API dependency đầy đủ, Designer phải tiếp tục mở được.
3. ContentDetailPanel.Initialize/LoadContent vẫn yêu cầu local services/repositories ngay cả khi detailBackend được inject; chỉ truyền null từ Program sẽ gây lỗi guard. Các constant/default ở ContentService cần giữ bằng nguồn thuần phù hợp sau khi bỏ business service.
4. ScriptEditorControl vẫn guard `_service` và gọi local service cho AI. Không xóa service trước khi trace và xử lý đúng chức năng hiện hữu.
5. `Data/DbConfig.cs`, NpgsqlUnitOfWork, IDbSession, DbSessionExtensions và repositories SQL còn ở client; `ApiAssigneeBoardRepository` là adapter HTTP nên không được xóa theo tên thư mục. Helper PostgresEnumMapper cần xử lý theo consumer thực tế.
6. Tests Database cũ dùng NpgsqlUnitOfWork và repository client; tests Shared còn kiểm tra local services. Cần mapping coverage sang Backend/API trước khi loại các type này, không skip/xóa test chỉ để build pass.

## Auth/error boundary quan sát được

- ApiClient.Auth tạo Authorization header theo từng request từ bearer truyền vào; chưa thấy căn cứ để cần thay toàn bộ cơ chế HTTP client.
- ApiTaskCall có Run/RunAsync, generation guard khi xử lý 401/403, network/timeout chuyển sang lỗi hiển thị; cần kiểm tra generation/project guard khi xử lý thành công tại consumer.
- ProjectApiFacade dùng đường gửi riêng và validation response, cần đối chiếu semantics với AuthApiFacade/ApiTaskCall; không thêm abstraction mới nếu cơ chế hiện hữu có thể thống nhất bằng sửa nhỏ.
- `ApiAssigneeBoardRepository` gọi ApiTaskCall.Run không truyền session; nếu còn consumer runtime phải xử lý session hoặc loại cùng đường legacy đã được thay bằng ApiBoardBackend.

## Trạng thái thực hiện

Cập nhật triển khai: đã áp dụng thay đổi source trên SCRUM-61; xem `implementation-notes.md` để biết wiring API, loại DB dependency, thay coverage DB legacy và các giới hạn còn lại. Đã viết `test-plan.md`, đang chờ duyệt nhóm A (build/regression không DB). Chưa chạy build/test/start app/API hoặc truy cập DB, chưa commit/push/PR/merge. Danh sách bên dưới ghi trạng thái khảo sát ban đầu, không phải trạng thái mới nhất.

- Hoàn tất: requirements, plan được duyệt; baseline local và ma trận khảo sát ban đầu; nhận diện dependency/local fallback cần xử lý.
- Chưa thực hiện: tạo/chuyển branch, fetch/pull/merge/resolve conflict, sửa source, build/test/start API/client, kiểm tra hoặc ghi DB, commit/push/PR/Jira.
- Bước tiếp theo: chốt branch/nguồn tích hợp theo bước 1 của plan, rồi sửa source theo bước 3–6. Không coi tài liệu này là bằng chứng các AC đã đạt.
