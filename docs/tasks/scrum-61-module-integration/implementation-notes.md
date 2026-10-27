# SCRUM-61 — Thay đổi đã áp dụng

Cập nhật kiểm chứng: nhóm A đã được duyệt và hoàn tất — build Release pass, 907/907 regression không DB pass. Xem test-results.md. Các mục “chưa chạy/chờ duyệt” bên dưới ghi trạng thái lúc hoàn tất triển khai; PostgreSQL thật, UI end-to-end và nghiệm thu develop vẫn chưa thực hiện.

Branch: `SCRUM-61-Tích-hợp-các-module-trên-develop`, baseline develop `646b365`. Chưa commit/push/PR/merge hoặc cập nhật Jira.

## Thay đổi

- Program tạo một ApiClient, session, facade và các API backend cho ứng dụng; không còn UseDatabase, DbConfig, NpgsqlUnitOfWork hoặc khởi tạo repository DB/in-memory.
- Thêm constructor Board dùng các API dependency hiện có. Constructor legacy và các service/in-memory thuần vẫn được giữ cho fixture đang sử dụng; không có SQL/Npgsql implementation tương ứng tại client và Program không dùng chúng làm fallback. Không refactor toàn bộ bộ test/UI legacy trong task này.
- ContentDetailPanel nhận API backend mà không bắt buộc local services; mở/sửa/giao việc và workflow đi theo nhánh API. Helper xác định nhãn bước kế tiếp là hàm thuần; Backend vẫn quyết định quyền/chuyển trạng thái.
- ScriptService có đường API để mở/lưu Script và đọc dữ liệu Content/Platforms/Script đã lưu cho AI. Không dùng repository local để dựng ngữ cảnh AI; actor lấy từ session hiện tại. Kết quả AI vẫn chỉ preview, chờ user chọn áp dụng/lưu.
- Loại 4 file DB infrastructure và 12 SQL repository implementations khỏi WinForms; giữ adapter HTTP ApiAssigneeBoardRepository và helper enum thuần. Gỡ Npgsql PackageReference và ConnectionStrings khỏi cấu hình mẫu client. Không sửa config local cá nhân.
- ApiTaskCall xử lý token/401/403/network chung, kiểm tra generation trước trả thành công. Idea backend dùng chung helper, xóa cache role khi phiên/quyền thay đổi và không trả backlog=0 khi API lỗi. Auth facade cũng phát thông báo 403 để UI đồng bộ quyền.
- Calendar mở Content bằng đường API đã có, nhận dữ liệu Board mới và hiển thị lỗi nguồn dữ liệu thay vì giữ cache rỗng khi mở trong lúc Board đang tải. Script preview bỏ response/kết quả của Content cũ sau khi đổi drawer.
- Bắt lỗi tải badge Backlog trong luồng tải Board, giữ badge cũ và báo lỗi. Loại đăng ký TaskRepository bị trùng tại Backend.
- README cập nhật ranh giới client/API/DB và hướng dẫn cấu hình.

## Chuyển coverage DB cũ sang Backend

Các test cũ phụ thuộc repository DB của WinForms đã được thay bằng các test trong `CreatorFlow.Api.Tests.Database`. Fixture Backend dùng IDbConnectionFactory/NpgsqlConnectionFactory, tạo dữ liệu riêng và chỉ chạy khi có connection string test cùng `CREATORFLOW_TEST_ALLOW_WRITES=true`. Không chạy hoặc apply schema ở bước này.

| Bộ test cũ | Coverage thay thế |
| --- | --- |
| ContentPlanningIntegrationTests | ContentPlanningPersistenceTests: create/update/clear description/date/script, Unicode, version compare-and-set, status/creator không thay đổi, unknown content. Script được lưu riêng qua repository Script theo API hiện tại. |
| ContentPlatformIntegrationTests | ContentPlanningPersistenceTests: 4 platform seed, create/read hai platform, Board đọc cùng platform, bỏ platform giữ nguyên link/metrics của platform còn lại, unique constraint. |
| IdeaRepositoryIntegrationTests | IdeaPersistenceTests: CRUD/empty fields, tag sync/giữ tag ID/xóa tag links, search title/description/note/tag không phân biệt hoa thường, wildcard literal, status/tag filters, cách ly Project/tags, unknown ID. |
| IdeaConvertToContentIntegrationTests | IdeaPersistenceTests: Draft và Backlog conversion, source link/content fields/tags/initial history, giữ Idea và trạng thái Converted, không tạo trùng, Archived/unknown không ghi, xóa Idea giữ Content với source null. |

Các assertion được gom theo chuỗi thao tác trên fixture riêng nên số test method không giữ nguyên số method cũ. Không coi việc viết test mới là bằng chứng test đã chạy/pass.

## Test bổ sung chưa chạy

- ModuleIntegrationTests: bearer/session policy 401/403, network/timeout giữ phiên và không empty success, response phiên cũ, Script AI đọc ngữ cảnh qua API và không ghi, script conflict, assembly client không reference Npgsql/Backend.
- ProjectAuthorizationTests: chuỗi HTTP Project access → submit → reject → resubmit → approve → publish dùng cùng workflow fake. Đây là coverage endpoint/service, không phải bằng chứng review history/persistence PostgreSQL.
- Các regression tests hiện có tiếp tục được giữ, trừ 4 bộ DB legacy đã có mapping ở trên. Filter CI hiện có đã bao phủ namespace test mới, chưa cần sửa workflow CI.

## Rà tĩnh đã thực hiện

- `git diff --check` không báo lỗi whitespace.
- Đọc project/solution XML và cấu hình mẫu JSON thành công.
- Rà các file C# client ngoài cây `src/CreatorFlow/CreatorFlow/` đã exclude: không còn using/khởi tạo/type Npgsql, DbConfig, IDbSession hoặc NpgsqlUnitOfWork. Các comment nhắc Npgsql trong interface/in-memory không phải dependency driver.
- Cây bản sao và archive được giữ nguyên theo scope, không nằm trong compile của project client chính.
- Chưa xác minh package/output bằng build; thư mục bin/obj cũ có thể còn artifact trước thay đổi. Không dùng artifact cũ để kết luận boundary mới đã đạt.

## Còn chờ

- Duyệt và chạy nhóm build/regression không DB theo test-plan.md.
- Xác nhận môi trường API/PostgreSQL test, role/account, quyền ghi/dọn fixture trước persistence/end-to-end.
- Kiểm chứng runtime các guard/null dependency ở constructor API, UI workflow/Calendar/Script, role/session và race khi chuyển Project.
- AI provider vẫn là POC phía client từ develop, chưa migrate Backend; giữ chức năng đã có, không đánh dấu hoàn tất migration AI.
- Build trên develop sau tích hợp và PR/Jira là bước chưa thực hiện. Task chưa đạt toàn bộ AC chỉ từ thay đổi source này.
