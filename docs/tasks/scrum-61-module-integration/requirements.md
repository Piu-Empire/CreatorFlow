# SCRUM-61 — Tích hợp các module trên develop

Trạng thái: requirements và plan đã được người dùng duyệt; thay đổi source đã áp dụng. Nhóm A đã được duyệt và hoàn tất: build Release pass, 907/907 regression không DB pass. PostgreSQL/end-to-end và nghiệm thu develop còn chờ; xem test-results.md và test-plan.md. Chưa thao tác database.

## Mục tiêu

Ghép các module hiện có và xử lý conflict/integration issues để develop build thành công và các module đã migrate hoạt động xuyên suốt qua CreatorFlow.Api tới PostgreSQL.

Kiến trúc nghiệm thu theo yêu cầu trực tiếp của task:

```text
WinForms UI → Client Service/API facade → REST API → CreatorFlow.Api
    → Backend Service → Repository Interface → Repository Implementation → Npgsql → PostgreSQL
```

Yêu cầu này thay thế đường truy cập DB tại WinForms trong mô tả kiến trúc cũ. Giữ nguyên nguyên tắc permission, validation, workflow ở Backend Service; không chỉ dựa vào UI.

## Hiện trạng khảo sát tĩnh

- Branch hiện tại: `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator`, chưa phải branch SCRUM-61. Working tree có AGENTS.md bị sửa và nhiều tài liệu/thư mục chưa được Git theo dõi; phải bảo toàn.
- `git ls-files -u` không có entry: chưa có conflict Git đang tồn tại. Chưa fetch/pull nên chưa xác minh trạng thái develop trên remote.
- `src/CreatorFlow/CreatorFlow.csproj` vẫn reference Npgsql, chỉ reference project Contracts, và exclude cây bản sao `CreatorFlow/**` khỏi compile.
- `src/CreatorFlow/Program.cs` vẫn khởi tạo local services/repositories; `UseDatabase = false` hiện dùng in-memory nhưng còn nhánh `NpgsqlUnitOfWork(DbConfig.ConnectionString)`. Có API backends được truyền vào Board ở luồng đăng nhập bình thường; sự tồn tại của chúng chưa chứng minh mọi thao tác đều đi API.
- Backend đã map Health, Auth/User, Admin/Reports, Metrics, Tasks, Workflow, Review Queue, Overdue, Notifications, Invitations, Projects, Ideas, Board, Global Search và Content Detail. Chưa xác minh runtime hoặc độ đầy đủ của từng route.
- `ApiTaskCall` đã có logic token, 401/403 và chuyển lỗi API; cần kiểm tra mọi đường gọi liên quan dùng nhất quán, kể cả facade ngoài helper này.
- README mô tả foundation chuyển tiếp và còn cho phép Board/Workflow dùng DB tại client. Cần cập nhật phần này theo kết quả tích hợp thực tế.

## Phạm vi

1. Lập danh sách module đã migrate, điểm vào UI, client facade/backend adapter, DTO, API route, DI, Backend Service và Repository tương ứng. Dùng danh sách này làm phạm vi nghiệm thu, tránh bỏ sót một module đã có API.
2. Đối chiếu develop và các nguồn tích hợp được xác nhận; xử lý phần ghép module, contract/namespace/reference, DI, route, session và Project context trực tiếp phục vụ task. Giữ chức năng đã có và thay đổi local của người dùng.
3. Loại bỏ đường truy cập PostgreSQL khỏi ứng dụng WinForms được build/phát hành: dependency Npgsql, repository implementation truy cập DB, DB session/factory/config và nhánh khởi tạo DB. Không thay bằng dữ liệu in-memory để coi là tích hợp thành công.
4. Npgsql và repository implementation truy cập PostgreSQL chỉ được compile/chạy ở Backend. Client dùng Contracts và API facade/service; không reference Backend executable hoặc cần DB connection string để khởi động/thao tác.
5. Nối các thao tác module đã migrate vào API, bao gồm đọc/ghi và làm mới dữ liệu giữa các màn hình, giữ đúng authenticated user và Current Project.
6. Attach bearer token cho request cần xác thực; thống nhất phản ứng 401, 403, network/timeout qua cơ chế dùng chung. Không lộ token, config nhạy cảm hay raw server error; không tự retry thao tác ghi có nguy cơ tạo trùng.
7. Giữ workflow hợp lệ, reject có feedback, resubmit tạo review mới, lịch sử cũ không bị overwrite; permission kiểm tra ở Backend Service và dữ liệu nhiều bước dùng transaction thích hợp.
8. Cập nhật tài liệu chạy client/backend và bằng chứng kiểm chứng tương ứng với kết quả thực tế.

Ngoài phạm vi: phát triển nghiệp vụ mới cho module chưa migrate; thiết kế lại giao diện/điều hướng; đổi schema/migration, package hoặc shared contract không cần thiết cho tích hợp; deploy, cập nhật Jira, commit/push/tạo hoặc merge PR khi chưa được yêu cầu cụ thể.

Nếu phát hiện một luồng chưa migrate nhưng bắt buộc để đạt AC không còn DB tại WinForms, phải nêu rõ phần còn thiếu trong plan để được duyệt, không âm thầm bỏ chức năng hoặc chuyển sang dữ liệu giả.

## Giả định

- “Ghép các module” là tích hợp các chức năng đã có, không thực hiện thay toàn bộ backlog của dự án.
- develop là đích tích hợp; không sửa source trực tiếp trên develop/main. Kết quả build trên branch hiện tại không thay thế bằng chứng build develop ở commit được tích hợp.
- Chỉ sửa UI wiring/context/error handling cần cho tích hợp; giữ luồng UX hiện có. Nếu cần đổi giao diện/điều hướng phải đọc UX và UI Style Guide trước, trình rõ thay đổi trong plan.
- Không dùng cây bản sao cũ hoặc archive làm đường chạy phụ để né các tiêu chí kiến trúc.
- Tài liệu requirements được tạo trong workspace hiện tại; chưa thay branch hoặc ghép code ở bước này.

## Thông tin cần chốt trước bước phụ thuộc

- Tên branch Jira chính xác cho SCRUM-61 và nguồn tích hợp (develop local/remote, các branch hoặc PR chưa ghép). Ảnh chỉ cung cấp mã issue và tiêu đề, chưa cung cấp tên branch Jira chính thức.
- Target API/PostgreSQL dùng nghiệm thu end-to-end, tài khoản/role test, dữ liệu được phép tạo/sửa và quyền dọn dữ liệu test. Không ghi credential vào tài liệu.
- Yêu cầu hiện tại nêu rõ xử lý conflict/integration; plan sẽ xác định các thao tác Git cần thiết sau khi rõ nguồn/đích. Chưa tự suy diễn quyền pull, tạo branch, commit, push hoặc merge PR.

## Tiêu chí hoàn thành

| AC | Bằng chứng cần có |
| --- | --- |
| develop build thành công | `dotnet build CreatorFlow.slnx` pass trên develop sau tích hợp, ghi commit và cấu hình; nếu chưa tích hợp develop thì ghi rõ còn chờ. |
| Module đã migrate giao tiếp qua API | Ma trận thao tác/module và kiểm chứng không chạy repository DB hoặc fallback in-memory trong luồng sử dụng thật. |
| WinForms không truy cập PostgreSQL | Kiểm tra source/dependency/output cho thấy client không có đường mở DB, SQL hoặc Npgsql dependency. |
| WinForms không cần DB connection string | Khởi động/đăng nhập và thao tác module bằng cấu hình API, không có DB connection string ở client. |
| Npgsql/repository DB chỉ ở Backend | Kiểm tra project/reference/compile boundary và runtime Backend truy cập PostgreSQL. |
| Token được attach đúng | Request protected có token phiên hiện tại; không có/hết hạn token bị từ chối, không tái dùng token của phiên cũ. |
| 401/403/network xử lý thống nhất | 401 kết thúc phiên và về đăng nhập; 403 phản ánh mất quyền và đồng bộ context phù hợp, không coi là mất mạng; network/timeout báo lỗi an toàn, giữ phiên, không giả thành dữ liệu rỗng. Response phiên cũ không đổi phiên/context mới. |
| Client ↔ API ↔ PostgreSQL end-to-end | Trên môi trường được duyệt: Login → chọn/tạo Project → Idea/Content → phân công/My Work → workflow → submit/reject/feedback/resubmit/approve → đọc lại ở màn liên quan; bổ sung Metrics/Notifications và module đã migrate theo ma trận. Dữ liệu ghi được đọc lại từ PostgreSQL qua API. |
| Permission và toàn vẹn | OWNER/MANAGER/CREATOR, truy cập chéo Project bị chặn tại Backend; trạng thái, review/history và transaction không regress. |

## Cổng duyệt và kiểm chứng

- Chờ duyệt requirements này trước khi tạo `plan.md`; chờ duyệt plan trước khi sửa source.
- Sau triển khai, trình phạm vi/bước/kết quả mong đợi và chờ đồng ý trước build, test, chạy ứng dụng/API hoặc kiểm tra database theo `post-change-test-workflow`.
- Mọi ghi dữ liệu cần xác nhận đúng môi trường và phạm vi; không tự chạy migration/seed hoặc xóa dữ liệu team.
- Báo riêng kiểm tra tĩnh, test bằng fake/stub và end-to-end thật. Không coi health OK, test skipped/inconclusive hoặc fixture in-memory là bằng chứng persistence.
- Nếu phát hiện lỗi runtime, ghi nhận và áp dụng `bug-investigation-workflow` trước khi sửa.
- PR merge/Jira Done còn lại nếu chưa được yêu cầu hoặc chưa thực hiện; không tự đánh dấu task hoàn tất chỉ từ source diff.
