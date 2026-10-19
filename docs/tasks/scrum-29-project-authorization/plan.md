# SCRUM-29 — Plan phân quyền theo Project

## Cơ sở và trạng thái

- Requirements được người dùng duyệt ngày 19/10/2026.
- Plan đã được người dùng duyệt. Người dùng đã cung cấp tên branch; đã tạo/chuyển sang `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator`, giữ nguyên thay đổi có sẵn.
- Đã triển khai và kiểm chứng theo phạm vi người dùng duyệt: build thành công, 316 test passed, không fail/skip. Xem `test-results.md`; chưa kiểm chứng database/API triển khai thật.

## Kết quả khảo sát bổ sung

- JWT middleware xác thực `sub`, token version và trạng thái tài khoản rồi điền `CurrentAuthenticatedUser`. Giữ cơ chế này làm nguồn danh tính.
- Task/Workflow repository truy vấn `project_members` theo Project ID, User ID và `is_active`; Project Service và Members Service đã kiểm tra Owner. Không cần viết lại authentication hoặc di chuyển toàn bộ repository client.
- Startup đăng nhập truyền các API backend vào Board. Content Detail đã phân nhánh gọi API khi submit/approve/reject; giữ đường gọi hiện có. Service/repository cục bộ còn phục vụ chế độ local và một số đường UI phải kiểm kê để không fallback khi đang đăng nhập.
- `ProjectApiFacade` đã có revalidation Current Project; `ContentDetailPanel` có cache role theo Project. Tái sử dụng cơ chế hiện có, bổ sung invalidation/xử lý từ chối theo context.
- Có các test Service/route hiện có để mở rộng; một số fixture route gán sẵn authenticated user nên cần thêm bằng chứng tích hợp JWT thực, không coi fixture đó là kiểm chứng JWT.

## 1. Chốt ma trận thao tác từ code và ghi nguồn cấp quyền

**Khu vực:** `Services/{Projects,Invitations,Tasks,Ideas,ContentDetail,Workflow,Board,Metrics}` và `Endpoints` của API; `docs/tasks/scrum-29-project-authorization/permission-matrix.md` (mới).

- Lập bảng từng thao tác hiện có, endpoint/Service, nguồn actor, nguồn Project và điều kiện cho Owner/Manager/Creator.
- Owner quản trị Project/membership/role/invitation; Owner/Manager giao việc cho người khác, đổi deadline và quyết định Review. Giữ quyền Creator hiện có đối với Idea của mình, Content/Script được phép, progress của mình và submit/resubmit.
- Quyền đọc Calendar/Performance không bị cấm chỉ vì Creator; không tạo API/màn hình mới cho module chưa triển khai.
- Ghi rõ ngoại lệ Creator tự nhận Content do mình tạo. Tự nhận không được trở thành đường đổi deadline hiện có hoặc giao việc cho người khác. Giữ dữ liệu planning khi tạo nội dung theo nghiệp vụ hiện có; không dùng task này để tự thay chính sách khởi tạo Content.

**Verify tĩnh:** mỗi nhóm yêu cầu có thao tác và nguồn cấp quyền cụ thể; quyền không rõ từ tài liệu/code phải được nêu để chốt trước khi sửa, không suy diễn mở rộng.

## 2. Bổ sung kiểm tra tại Backend Service ở những điểm thiếu

**Khu vực có thể sửa theo ma trận:** `src/CreatorFlow.Api/Services/Projects/{ProjectService,ProjectMembersService}.cs`, `Services/Invitations/InvitationService.cs`, `Services/Tasks/{TaskService,TaskRules}.cs`, `Services/ContentDetail/ContentDetailService.cs`, `Services/Workflow/WorkflowService.cs`, `Services/Ideas/{IdeaService,IdeaRules}.cs`, `Services/Board/BoardService.cs`, `Services/Metrics/{MetricService,MetricRules}.cs`.

- Actor luôn từ `CurrentAuthenticatedUser`; role hiện hành luôn từ membership phía server của Project đích, không từ DTO/query/header/claim role của client.
- Tài nguyên con lấy Project ID từ record server. Kiểm tra actor và assignee/member mục tiêu độc lập.
- Bổ sung guard thiếu và chỉ hợp nhất predicate quyền nhỏ khi trực tiếp cần cho tính nhất quán; giữ kiểm tra đúng hiện có, không refactor toàn bộ Service hoặc thêm framework authorization.
- Kiểm soát đường tự nhận việc để Creator không cập nhật deadline của assignment đã tồn tại qua `AssignContentAsync` thay vì endpoint deadline.
- Quyền thiếu/không xác thực trả kết quả lỗi theo conventions hiện có; không ghi dữ liệu trước guard và không tự thêm bypass System Admin.

**Repository/endpoint chỉ sửa khi cần:** truy vấn role trong `Repositories/{Projects,Tasks,Workflow,Invitations}` và mapping tương ứng trong `Endpoints`. Giữ parameterized SQL, transaction và bất biến Owner; không migration/schema change. Nếu phát hiện cần thay interface dùng rộng hoặc kiến trúc đáng kể ngoài phạm vi, cập nhật plan xin duyệt lại.

**Verify tĩnh:** truy ngược từng guard đến authenticated actor và membership đang hoạt động; các nhánh bị từ chối không gọi repository mutation; kiểm tra đúng Project của tài nguyên.

## 3. Đồng bộ UX quyền và xử lý context lỗi thời

**Khu vực:** `src/CreatorFlow/Services/{ProjectContext,ProjectApiFacade}.cs`, `Services/Backends/{ApiTaskCall,ApiContentDetailBackend,ApiAssignmentBackend}.cs`, `Forms/Board/BoardForm.cs`, `Controls/{SidebarControl,ContentDetailPanel,ProjectSettingsControl}.cs`, `Forms/ProjectSettings/ProjectMembersDialog.cs`; các control MyTasks/IdeaBank và dialog giao việc liên quan trực tiếp nếu ma trận chỉ ra khoảng trống.

- Tận dụng Project context và role trả từ API để ẩn/disable chức năng quản trị Owner và thao tác quản lý không dành Creator; handler kiểm tra lại trạng thái UI trước thao tác.
- Đổi Project/đăng xuất xóa quyền và dữ liệu context cũ; bỏ qua response bất đồng bộ thuộc generation cũ.
- Khi API từ chối do quyền đổi, thông báo phù hợp, revalidate Current Project và làm mới cache/control; mất membership thì rời workspace theo luồng hiện có. Không tự retry thao tác ghi.
- Khi chạy phiên đăng nhập, thao tác ghi phải qua API hoặc bị chặn với thông báo chưa hỗ trợ; không fallback sang repository/service local. Không mở rộng thành migration toàn bộ module local.
- Giữ đường API workflow hiện có của Content Detail; không thêm adapter trùng lặp. Giữ layout/style/điều hướng đã có, không thiết kế lại màn hình.

**Verify tĩnh:** trace handler từ UI tới API; cache gắn đúng Project/context; 403 không biến thành thành công hoặc retry ghi. Không để UI chứa SQL hoặc quyết định authorization cuối cùng.

## 4. Viết kiểm chứng bảo mật và hồi quy có trọng tâm

**Khu vực:** mở rộng `tests/CreatorFlow.Api.Tests/{Services,Tasks,Ideas,ContentDetail,Invitations,Metrics,Api}` theo thay đổi; thêm `Workflow/WorkflowServiceTests.cs` và kiểm chứng route workflow nếu chưa có; mở rộng `tests/CreatorFlow.IntegrationTests/ApiClients/ProjectShellTests.cs` và backend/client test liên quan.

- Kiểm tra đủ Owner/Manager/Creator/không membership/inactive membership; cùng actor có role khác nhau ở hai Project.
- Service gọi trực tiếp: từ chối trước mutation; positive case vẫn ghi đúng actor và Project. Đổi role/gỡ membership giữa hai request phải có hiệu lực request sau.
- API gọi trực tiếp: 401 khi thiếu token; Creator bị cấm chức năng Manager, Manager bị cấm chức năng Owner; chèn actor/role vào body/query/header không nâng quyền; UserId mục tiêu hợp lệ vẫn hoạt động.
- Dùng resource ID chéo Project để kiểm tra đọc/ghi không vượt quyền. Kiểm tra đường Creator tự nhận việc không sửa deadline quản lý.
- Dùng test JWT/repository fake phù hợp để xác nhận authenticated context thực, không chỉ gán sẵn actor trong Service fixture.
- UI kiểm tra chuyển Project, logout, response context cũ, role đổi/membership bị thu hồi và các thao tác Creator hợp lệ.

**Verify ở bước này:** review nội dung test và điều kiện mong đợi; chưa chạy bất kỳ test host/build/test nào.

## 5. Trình phạm vi test và chờ duyệt trước khi chạy

**Tài liệu:** `docs/tasks/scrum-29-project-authorization/test-plan.md` sau triển khai, theo `post-change-test-workflow`.

- Đề xuất `dotnet build CreatorFlow.slnx`, các test Service/API/client đã thay đổi và smoke UI theo ma trận; nêu command/filter cụ thể sau khi biết test cuối cùng.
- Ưu tiên fake repository/TestServer để không cần database hoặc API bên ngoài. Test host vẫn thuộc runtime và phải chờ duyệt.
- Nếu cần persistence thật, tách bước riêng với môi trường test disposable và quyền ghi được xác nhận; không tự dùng database cấu hình của app.
- Khi được duyệt chạy test chỉ quan sát và ghi kết quả. Phát hiện lỗi thì hoàn tất phần test độc lập còn chạy được, lập bug-report và chờ duyệt fix theo workflow.

**Tiêu chí hoàn tất:** build và các kiểm chứng liên quan được phép đã chạy, ghi đúng pass/fail/skip/chưa xác minh; ma trận UI/Service/API đáp ứng requirements. PR/Jira chỉ thực hiện khi được yêu cầu rõ, không tự coi đã hoàn tất.
