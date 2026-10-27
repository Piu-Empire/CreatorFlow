# SCRUM-61 — Kế hoạch kiểm chứng sau thay đổi

Trạng thái: user đã duyệt và hoàn tất nhóm A: build Release pass, Backend 479/479 và client 428/428 pass, không skipped. Chi tiết tại test-results.md. User đã duyệt tiếp nhóm B/C; chưa chạy vì còn thiếu target API/DB disposable và thông tin nơi cấu hình tài khoản role test. Các biến cấu hình test chưa có ở Process/User/Machine. Chưa chạy app/API thật hoặc database. Theo AGENTS.md/post-change-test-workflow, trong giai đoạn test chỉ quan sát và ghi nhận; lỗi cần bug report và duyệt cách sửa riêng.

## Nhóm A — Build và regression không DB (đề nghị duyệt trước)

Môi trường: workspace Windows hiện tại, branch SCRUM-61 tại baseline `646b365` cộng working diff. Không chạy app với tài khoản thật, live API, AI provider hoặc PostgreSQL. Tests dùng HTTP handler/TestServer/fake; bộ client có fixture WinForms.

1. Build solution giống cấu hình CI:

   ```powershell
   dotnet build CreatorFlow.slnx --configuration Release
   ```

   Kỳ vọng: mọi project compile thành công, không thiếu type sau loại repository DB. Restore package khi build cần dùng cấu hình NuGet sẵn có; không thêm package hoặc sửa code trong giai đoạn này.

2. Chạy toàn bộ regression Backend không DB theo filter CI:

   ```powershell
   dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName!~CreatorFlow.Api.Tests.Database" --logger "trx;LogFileName=scrum61-api.trx" --results-directory TestResults/scrum61/api
   ```

   Kỳ vọng: authorization hiện có và chuỗi Project/workflow mới pass; thiếu token/role hoặc trạng thái không hợp lệ bị chặn. Fake workflow không được coi là bằng chứng lịch sử review được lưu vào DB.

3. Chạy regression client không DB/live API theo filter CI:

   ```powershell
   dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName!~CreatorFlow.IntegrationTests.Database&TestCategory!=ApiLive" --logger "trx;LogFileName=scrum61-client.trx" --results-directory TestResults/scrum61/client
   ```

   Kỳ vọng: Project shell/context và các test API client cũ không regress; ModuleIntegrationTests pass cho bearer, 401/403, network/timeout, response phiên cũ, Script AI/context và script conflict. Không cần DB connection string ở client.

4. Đọc TRX/log và project.assets/deps của output vừa build. Kỳ vọng dependency graph/assembly client không reference Npgsql hoặc CreatorFlow.Api; DB driver nằm ở Backend. Ghi rõ nếu bin cũ còn file driver không thuộc output graph mới, không coi file cũ là dependency mới.

Build fail chặn tests phụ thuộc output; không dùng binary cũ với --no-build để báo pass. Nếu một bộ test fail và bộ kia vẫn có thể chạy bằng output build thành công, tiếp tục bộ còn lại rồi tổng hợp. Thiếu test report, skipped/inconclusive không được tính là pass đầy đủ.

## Nhóm B — Persistence Backend trên PostgreSQL test (chưa được phép chạy)

Trước khi chạy cần user xác nhận:

- API/DB target là test disposable, không phải dữ liệu team/production; schema và migrations đã được chuẩn bị sẵn. Không tự apply migration/seed.
- Connection string cung cấp qua môi trường test, không ghi vào repo/log; `CREATORFLOW_TEST_ALLOW_WRITES=true` chỉ sau xác nhận quyền tạo/sửa/xóa fixture.
- Cho phép fixture tạo user/project/Idea/Content/tags/platform links/metrics và dọn đúng các ID tự tạo. Không dùng transaction rollback của repository cũ để giả định toàn bộ dữ liệu tự hoàn tác: repository Backend thực hiện transaction riêng.

Lệnh dự kiến, không tự mở rộng sang toàn bộ bộ Database:

```powershell
dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~CreatorFlow.Api.Tests.Database.ContentPlanningPersistenceTests|FullyQualifiedName~CreatorFlow.Api.Tests.Database.IdeaPersistenceTests|FullyQualifiedName~CreatorFlow.Api.Tests.Database.ContentReferenceRepositoryIntegrationTests|FullyQualifiedName~CreatorFlow.Api.Tests.Database.BoardCardRepositoryIntegrationTests" --logger "trx;LogFileName=scrum61-persistence.trx" --results-directory TestResults/scrum61/persistence
```

Kỳ vọng: CRUD/planning/script/platform/metrics/tag/convert persistence thực tế đúng như coverage mapping ở implementation-notes.md. Trường hợp thiếu config/opt-in được ghi là chưa chạy, không phải pass. Nếu cleanup lỗi, ghi rõ ID fixture còn lại; không tự mở rộng thao tác dọn dữ liệu ngoài phạm vi được phép.

## Nhóm C — UI và Client ↔ API ↔ PostgreSQL (chưa được phép chạy)

Cần xác nhận URL API, DB test target, tài khoản test OWNER/MANAGER/CREATOR ở ít nhất hai Project và quyền ghi/dọn bộ dữ liệu demo. Chỉ khởi động app/API sau duyệt. Không gọi AI thật có chi phí nếu chưa được phép.

| Luồng | Bước kiểm tra | Kết quả mong đợi |
| --- | --- | --- |
| Khởi động/đăng nhập | Client chỉ cấu hình Api:BaseUrl/timeout, không có DB connection string; Login rồi chọn/tạo Project | Mở đúng workspace qua API; không mở PostgreSQL từ client. |
| Idea → Content | Tạo/sửa/lọc Idea, convert; đọc Content từ Board và library | Cùng ID/Project, thông tin/tag persist; convert lần hai không tạo trùng. |
| Content/Script/References | Tạo Content, sửa planning/platform, lưu Script, mở lại, tạo/sửa/xóa reference | Các màn đọc cùng dữ liệu API; stale script version bị từ chối; không mất metrics của platform được giữ. |
| Assignment/My Work | OWNER/MANAGER giao CREATOR; CREATOR đọc task và cập nhật progress | Đúng người/Project; Board và My Work đọc lại tiến độ thống nhất; người không đủ quyền bị API chặn. |
| Review/workflow | Editing → submit → reject có feedback → sửa → resubmit → approve → Ready → Published | Hai review riêng, review cũ và history còn nguyên, chỉ chuyển trạng thái hợp lệ; feedback bắt buộc khi reject. |
| Calendar/Summary/Reports | Mở Calendar ngay khi Board còn tải, click Content, đổi Project, sửa deadline/publish date rồi mở lại; xem metrics summary | Dữ liệu đúng Project, mở drawer qua API, refresh đúng; lỗi API được báo thay vì coi là danh sách rỗng. |
| Session/quyền/lỗi | Thiếu/hết token; role bị thu hồi bằng tài khoản/fixture test được phép; API ngắt/timeout; đổi phiên/Project khi request đang chờ | 401 về Login; 403 giữ phiên và cập nhật quyền/context; network giữ phiên; response cũ không làm đổi phiên/Project mới. Không tự retry ghi. |
| Notifications/Admin | Sau assignment/review đọc notification; Admin mở workspace hệ thống | Notification đúng tài khoản; System Admin không thay thế membership Project; API tiếp tục kiểm quyền. |
| Script AI | Stub provider hoặc provider được phép; sinh preview rồi đổi Content/đóng drawer trong lúc chờ; mở lại và áp dụng/lưu chủ động | Ngữ cảnh/script lấy từ API, không tự lưu, preview cũ không chèn vào Content mới. Provider POC vẫn ở client và chưa thuộc migration Backend của task. |

## Khu vực rủi ro

- Constructor API dùng wiring hiện có cùng constructor legacy phục vụ fixture; cần đặc biệt kiểm tra null guard của Content Detail/Script và thao tác workflow.
- Test DB đã thay fixture rollback cũ bằng fixture Backend tự tạo/dọn dữ liệu; cần schema đúng và môi trường disposable.
- Session 403 có thể kích hoạt revalidate; kiểm tra không tự gửi lại mutation và không làm mất bản nháp khi network lỗi.
- Build branch SCRUM-61 pass không xác nhận develop pass. Sau khi user yêu cầu tích hợp Git cụ thể, ghi commit develop và build lại ở commit đó theo phạm vi được duyệt.

## Kết quả và xử lý lỗi

Ghi test-results.md với commit/working diff, lệnh, số passed/failed/skipped, thời gian, môi trường không chứa credential và đường dẫn TRX. Khi phát hiện lỗi, hoàn tất phần còn có thể kiểm tra rồi lập bug-report.md với bằng chứng/root cause/đề xuất sửa; không vá source trong giai đoạn test. PR merge/Jira Done chưa được thực hiện và không tự được cấp quyền bởi việc duyệt test nhóm A.
