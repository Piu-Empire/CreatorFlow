# SCRUM-61 — Plan tích hợp module

Requirements: `requirements.md`, đã được người dùng duyệt ngày 2026-10-26.

Trạng thái: plan đã được người dùng duyệt; thay đổi source đã áp dụng theo implementation-notes.md. Nhóm A đã được duyệt và hoàn tất: build Release pass, 907/907 regression không DB pass; xem test-results.md. Nhóm B/C và nghiệm thu develop chưa thực hiện. Duyệt plan không tự cấp quyền ghi database, commit/push/merge PR hoặc deploy.

Cập nhật: người dùng đã duyệt plan; đã cho phép tạo/chuyển branch SCRUM-61 theo tiêu đề Jira. Đã tạo `SCRUM-61-Tích-hợp-các-module-trên-develop` từ `origin/develop` tại `646b365` sau khi xác minh PR SCRUM-29 #38 đã merge. Không cần merge lại SCRUM-29. Chi tiết baseline và module mới nằm trong `integration-matrix.md`; các baseline bên dưới là lịch sử khảo sát ban đầu, không phải nguồn triển khai hiện tại.

## 1. Chốt baseline và nguồn tích hợp

- Ghi HEAD, develop local, trạng thái working tree và các nguồn module vào `integration-matrix.md` trong thư mục task. Baseline khảo sát: HEAD `c5282fe`, develop local `b227de3`; chưa xác minh remote.
- Cần người dùng cung cấp tên branch Jira chính xác cho SCRUM-61 và nguồn branch/PR cần ghép. Không tự tạo tên từ ảnh. Trước khi thay branch, kiểm tra thay đổi local; không reset, stash hoặc mang thay đổi chưa commit sang branch khác một cách ngầm định.
- Sau khi được yêu cầu cụ thể các thao tác Git cần thiết, chuẩn bị branch tích hợp từ baseline develop đã xác nhận; đối chiếu từng nguồn và resolve conflict trong phạm vi module, giữ phần authorization SCRUM-29 và module khác. Không sửa trực tiếp main/develop hoặc tự merge PR.
- Nếu chưa có nguồn/branch, vẫn có thể hoàn thiện khảo sát chỉ đọc; dừng trước source edit/Git mutation phụ thuộc thông tin đó.

Khu vực: Git refs/diff chỉ đọc; `docs/tasks/scrum-61-module-integration/integration-matrix.md`.

Verify: ghi rõ commit nguồn/đích, file local cần bảo toàn, module đã có/chưa có; không dùng kết quả branch khác để tuyên bố develop đạt AC.

## 2. Lập ma trận đường đi của từng thao tác

- Trace UI → client service/backend → ApiClient partial → Contracts → API endpoints/DI → Backend Service → Repository cho Auth/Profile, Project/Membership/Invitations, Board/Content Library/Search, Idea/convert, Content Detail/Script/References, Assignment/My Work, Workflow/Review/History, Metrics, Notifications/Overdue và Admin/Reports.
- Với Calendar/Summary/AI hoặc view khác đang được gọi, xác định nó đọc cùng dữ liệu qua API hay còn gọi local. Chỉ sửa đường tích hợp của chức năng đã tồn tại; nếu thiếu backend bắt buộc thì trình phần bổ sung cụ thể trước khi mở rộng plan.
- Liệt kê constructor/call site còn fallback local, DTO/enum/route không khớp, thông tin role/context, refresh sau ghi và response đến muộn.

Khu vực: `src/CreatorFlow/Forms`, `Controls`, `Services`, `ApiClients`; `src/CreatorFlow.Contracts`; `src/CreatorFlow.Api/Endpoints`, `Services`, `Repositories`, `Program.cs`.

Verify tĩnh: mỗi thao tác đã migrate có một đường API đầy đủ; mỗi chỗ còn local có cách xử lý cụ thể, không coi việc có endpoint là đã tích hợp xong.

## 3. Chuyển composition root và UI wiring sang API

- Sửa `src/CreatorFlow/Program.cs`: bỏ UseDatabase, DbConfig, khởi tạo Npgsql/in-memory repositories và local business services khỏi luồng app thật; khởi tạo một ApiClient theo vòng đời ứng dụng, UserSession và các facade/backend API hiện có.
- Điều chỉnh constructor/Initialize và call site của BoardForm, ContentDetailPanel, Idea Bank, My Work, Review Queue, dialog tạo/sửa/phân công và các view liên quan trong ma trận. Dùng các interface backend hiện có; chỉ bổ sung thao tác adapter tối thiểu nếu endpoint tương ứng đã có.
- Loại nhánh fallback repository/local service trong những luồng runtime đã migrate. Không thay dữ liệu API lỗi bằng dữ liệu demo hoặc danh sách rỗng.
- Giữ constructor dành Designer và đường kiểm tra layout không thực hiện DB/network; nếu cần fake thì chỉ ở test fixture, không làm fallback cho app thật.
- Giữ luồng Login → Create/Select Project → Board, chuyển Project và mở content/detail đúng UX; giữ style hiện có. Không sửa layout/Designer/resources trừ phần wiring bắt buộc.

Khu vực: `Program.cs`, `Forms/Board/BoardForm*.cs`, `Controls` và `Forms` có dependency trực tiếp, `Services/Backends` và facade liên quan.

Verify tĩnh: truy ngược mọi call site thay đổi tới yêu cầu; app composition không tạo DB/in-memory repository; UI không quyết định permission nghiệp vụ thay Backend; không đổi điều hướng ngoài requirements.

## 4. Loại dependency PostgreSQL khỏi WinForms

- Gỡ Npgsql PackageReference khỏi `src/CreatorFlow/CreatorFlow.csproj` sau khi đã loại dependency source.
- Loại code DB legacy ở `src/CreatorFlow/Data` và các repository SQL trong `Repositories/Implementations` khỏi client. Đối chiếu chức năng với Backend trước khi xóa phần đã thay thế; nếu có logic còn cần, đưa đúng phần đó về Backend Service/Repository hiện có, không tạo bản sao repository thứ hai chỉ để giữ code cũ.
- Không xóa cả thư mục theo tên: `ApiAssigneeBoardRepository` là adapter HTTP, `PostgresEnumMapper` cần đọc dependency thực tế. Chỉ giữ/chuyển helper, interface, model còn được dùng; không đưa type Npgsql vào Contracts.
- Loại DB config requirement và hướng dẫn DB client trong config mẫu/tài liệu được track. Không đọc/in secret local, không tự sửa config cá nhân của người dùng.
- Local services/in-memory code không còn được app dùng: loại phần không còn consumer sau cập nhật test; giữ utility thuần phục vụ hiển thị như ScriptTextAnalyzer. Không dọn code lân cận ngoài phạm vi.

Khu vực: WinForms csproj, `Data`, `Repositories`, local business services đã được thay thế, config mẫu; Backend chỉ ở nơi cần bảo toàn chức năng.

Verify tĩnh: client compile graph không có Npgsql/SQL/DB factory/repository implementation PostgreSQL, không reference Api project; Contracts không kéo DB dependency; bản sao cũ vẫn exclude khỏi compile và không đưa vào output.

## 5. Đồng bộ token, lỗi và context xuyên module

- Rà `ApiClient.Auth.cs` và các partial protected request để bearer token được đặt theo request từ session hiện tại, không dùng header chung lưu token phiên cũ.
- Tái sử dụng và chỉnh tối thiểu `ApiTaskCall`, `AuthApiFacade`, `ProjectApiFacade`, `UserSession`, `ProjectContext`, `AuthenticationApplicationContext` và adapter liên quan để có cùng semantics: 401 clear session/về Login; 403 làm mới quyền/context phù hợp; mất mạng/timeout giữ phiên và báo lỗi an toàn.
- Kiểm tra session generation, cancellation và project identity khi request hoàn thành; response của phiên/Project cũ không cập nhật UI mới. Không tự retry POST/ghi dữ liệu.
- Sau tạo/chỉnh/sửa trạng thái/review, làm mới các view liên quan qua API và giữ đúng content/project; không chỉ cập nhật một bản local khác nguồn dữ liệu.

Khu vực: `ApiClients/ApiClient*.cs`, `Services/Backends`, Auth/Project facade/session/context, UI handlers tương ứng.

Verify tĩnh: protected request có token; phân biệt 401/403/network; lỗi không thành empty success; không log token/raw server error; context được kiểm tra trước áp dụng response.

## 6. Hoàn thiện Backend và coverage tích hợp

- Sửa thiếu/trùng DI, endpoint mapping, DTO/enum/date/nullability và service/repository contract được chứng minh bởi ma trận; giữ các module đã có trong `CreatorFlow.Api/Program.cs`.
- Permission, workflow và transaction tiếp tục được kiểm tra ở Backend; không tin userId/role do client gửi thay authenticated principal. Không thay schema hoặc thêm package theo mặc định.
- Cập nhật test fixture bị ảnh hưởng constructor/dependency. Chuyển những kiểm tra DB cũ ở `tests/CreatorFlow.IntegrationTests/Database` đang dùng NpgsqlUnitOfWork/repository client sang Backend/API tương ứng; giữ coverage CRUD, conversion, platform/planning và persistence cần thiết. Chỉ bỏ test obsolete khi có kiểm tra hành vi thay thế được chỉ rõ.
- Bổ sung test có ý nghĩa cho token/401/403/network, session/project response cũ và ít nhất một chuỗi thao tác qua endpoint/service dùng cùng repository fake. Ghi rõ đây chưa phải end-to-end PostgreSQL thật.
- Giữ filter CI tách test DB/live API. Chỉ cập nhật `.github/workflows/ci.yml` nếu tên/category/vị trí test thay đổi làm filter không còn đúng.

Khu vực: API Program/Endpoints/Services/Repositories, Contracts khi cần khớp API; hai project test hiện có và CI filter khi bị tác động trực tiếp.

Verify tĩnh trước test: coverage mapping cũ → mới, không test DB tự fallback config hoặc chạy ghi ngoài opt-in, không đưa dependency DB trở lại app vì test cũ. Nếu thiếu endpoint/schema/package lớn mới được phát hiện, cập nhật plan và chờ duyệt lại.

## 7. Review tĩnh và trình test plan

- Review diff, reference graph, conflict markers, API routes/DI, client config/output declarations và bảo toàn thay đổi local; cập nhật README từ foundation chuyển tiếp sang trạng thái thực tế.
- Tạo `test-plan.md` ghi lệnh, prerequisites, dữ liệu/role, bước UI/API, kỳ vọng và rủi ro. Dừng chờ đồng ý theo post-change-test-workflow; chưa restore/build/test/start app hoặc truy cập DB trong bước triển khai.

Khu vực: source diff, README, tài liệu chạy liên quan, thư mục task.

Verify: từng AC có phép kiểm tra cụ thể; phân biệt build, stub/fake, UI và PostgreSQL thật; liệt kê đầy đủ phần còn chờ thông tin/quyền.

## 8. Kiểm chứng được duyệt và nghiệm thu develop

Chỉ thực hiện sau cổng duyệt test, đúng phạm vi môi trường/dữ liệu được xác nhận:

1. Build `dotnet build CreatorFlow.slnx`; chạy các test API/client liên quan và nhóm regression không DB. Kiểm tra dependency/output WinForms không còn Npgsql và client khởi động bằng cấu hình API.
2. Chạy API/client trên môi trường đã chốt. Kiểm tra Login → Project → Idea/Content → assign/My Work → workflow/review reject-feedback-resubmit-approve; kiểm tra module còn lại theo ma trận, persistence đọc lại qua API, permission các role và truy cập chéo Project.
3. Kiểm tra 401/hết phiên, 403/mất membership, API không khả dụng/timeout và chuyển Project/phiên khi request còn chạy. Không thao tác phá hủy hoặc sửa quyền dữ liệu team để tạo tình huống lỗi; dùng fixture/account test đã duyệt.
4. Ghi `test-results.md` với commit, môi trường không chứa secret, kết quả thực tế/pass/fail/chưa chạy. Trong giai đoạn test chỉ quan sát, không sửa code; lỗi mới đi qua bug-investigation-workflow.
5. Chuẩn bị diff và ghi phần Git còn lại. Chỉ khi có yêu cầu rõ cho thao tác Git tương ứng mới commit/push/PR target develop/merge. Sau tích hợp, build develop tại commit cụ thể theo phạm vi test đã duyệt; không tuyên bố AC develop pass từ branch SCRUM-61.

Verify: đủ bằng chứng theo requirements; không coi health OK hoặc fake repository là PostgreSQL end-to-end. Nếu Git integration, DB test hoặc môi trường chưa được phép/thực hiện, báo rõ AC còn chờ; không tự cập nhật Jira Done.

## Rủi ro và điểm dừng

- Working tree có tài liệu chưa commit; branch nguồn/đích cần chốt trước code integration để tránh ghi đè/mang nhầm thay đổi.
- Bỏ local dependency ảnh hưởng constructor UI và test cũ: xử lý call site cùng coverage trong cùng phạm vi, giữ khả năng Designer.
- API thiếu hành vi mà UI local đang có: cần bổ sung cụ thể vào plan và duyệt lại nếu vượt phần wiring/contract fix nhỏ; không bỏ chức năng.
- Chưa có môi trường end-to-end/role test được xác nhận: không tự dùng DB shared hoặc production. Có thể hoàn tất source và kiểm tra không DB sau duyệt, nhưng chưa nghiệm thu persistence.
- Điểm chờ branch đã được giải quyết: user cho phép tạo/chuyển branch SCRUM-61; baseline là develop mới nhất đã fetch ở `646b365`, SCRUM-29 đã merge qua PR #38. Không tự chọn hoặc merge thêm PR ngoài nguồn này.
