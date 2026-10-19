# CreatorFlow

CreatorFlow là hệ thống hỗ trợ Creator và nhóm sáng tạo nội dung quản lý, lập kế hoạch, sản xuất, kiểm duyệt và theo dõi hiệu suất nội dung đa nền tảng.

## Công nghệ

- C# / .NET 10
- Windows Forms
- PostgreSQL
- Npgsql
- REST API
- AI/LLM API

## Kiến trúc

CreatorFlow sử dụng kiến trúc phân tầng:

WinForms UI → Service → Repository → Npgsql → PostgreSQL

- **Forms / Controls:** giao diện và tương tác người dùng.
- **Models:** các model/domain model dùng trong hệ thống.
- **Services:** xử lý nghiệp vụ, phân quyền và workflow.
- **Repositories:** truy cập dữ liệu thông qua PostgreSQL.
- **Data:** cấu hình và quản lý kết nối cơ sở dữ liệu.
- **Helpers:** các tiện ích dùng chung.

## Backend/API Foundation (SCRUM-78)

Kiến trúc mục tiêu từ Week 2:

```text
WinForms Client → REST API → CreatorFlow.Api → Service → Repository → Npgsql → PostgreSQL
```

`CreatorFlow.Api` (.NET 10) giữ connection factory, configuration và PlanRepository đã chuyển từ SCRUM-16. `CreatorFlow.Contracts` chỉ chứa DTO giao tiếp; WinForms reference Contracts, không reference API. Solution vẫn là `CreatorFlow.slnx`.

Đây là foundation chuyển tiếp. Board/Workflow SCRUM-42 vẫn dùng `DbConfig`, `NpgsqlUnitOfWork`, repository implementations/in-memory và Services hiện tại ở WinForms. Models/enums, `ContentWorkflowPolicy`, `ContentVisibilityPolicy`, `PostgresEnumMapper` của SCRUM-17 tạm giữ ở client để tránh duplicate hoặc kéo domain Board vào PR này. Các dependency trên sẽ migrate ở phase Board/Workflow riêng; PR này chưa loại bỏ Npgsql/business logic hoặc toàn bộ nhu cầu config DB khỏi client. Cây bản sao `src/CreatorFlow/CreatorFlow/` và archive được giữ nguyên, bản sao tiếp tục bị exclude khỏi compile.

Backend đọc `ConnectionStrings:CreatorFlow` bằng ASP.NET Core configuration: dùng .NET User Secrets trong Development hoặc environment `ConnectionStrings__CreatorFlow`. Không commit giá trị thật. API không đọc config local của WinForms và không tự apply migration; thiếu/malformed connection string báo lỗi config trước startup, không in giá trị. Host và Database phải được cấu hình; startup chỉ validate config, chưa mở DB.

Sau khi build/runtime được duyệt, chạy API bằng `dotnet run --project src/CreatorFlow.Api --launch-profile http`. Profile local dùng `http://localhost:5080`. `GET /api/health` trả `{"status":"ok"}` và chỉ xác nhận API đáp ứng, không xác nhận PostgreSQL connectivity.

Client đọc `Api:BaseUrl`/`Api:TimeoutSeconds` từ appsettings hoặc environment `Api__BaseUrl`/`Api__TimeoutSeconds` (mặc định mẫu: localhost:5080, 30 giây). Tạo một `ApiClient.Create(ApiClientConfiguration.Load())` cho vòng đời app, reuse instance rồi dispose khi kết thúc; không tạo HttpClient trong từng Form. PR này chưa nối health vào startup Board; health có live smoke test riêng. Client hỗ trợ cancellation, phân loại HTTP/network/timeout/JSON lỗi; không hiển thị raw server error. JWT/Auth sẽ migrate ở phase sau.

Tests `ApiFoundation` dùng HTTP stub và không DB/network. `ApiLive` chỉ chạy khi có `CREATORFLOW_TEST_API_BASE_URL` trỏ loopback API đã được phép khởi động. `DatabaseConnectivity` dùng backend factory/SELECT 1 và yêu cầu `CREATORFLOW_TEST_CONNECTION_STRING`; không fallback app config. `DatabasePersistence` thêm điều kiện `CREATORFLOW_TEST_ALLOW_WRITES=true` và chỉ được chạy trên DB test disposable đã duyệt vì có insert/read/delete. Không coi inconclusive là pass. Mọi build/test/startup/DB check phải qua cổng duyệt trong AGENTS.md.

## Continuous Integration (SCRUM-82)

Workflow `CI` tại `.github/workflows/ci.yml` chạy trên Windows với .NET SDK `10.0.x`: restore dependency, build toàn bộ `CreatorFlow.slnx` ở cấu hình Release rồi chạy hai project MSTest hiện có.

Workflow chạy khi tạo/cập nhật PR hướng vào `develop` hoặc `main`, push vào hai nhánh này, hoặc kích hoạt bằng `workflow_dispatch`. PR thông thường của nhóm vẫn hướng vào `develop` theo AGENTS.md.

Phạm vi test:

- `CreatorFlow.Api.Tests`: filter `FullyQualifiedName!~CreatorFlow.Api.Tests.Database`.
- `CreatorFlow.IntegrationTests`: filter `FullyQualifiedName!~CreatorFlow.IntegrationTests.Database&TestCategory!=ApiLive`.

Các test dùng fake repository, HTTP handler hoặc TestServer trong bộ nhớ vẫn chạy. Các test trong namespace Database và category `ApiLive` được loại trừ rõ ràng; workflow không cấu hình DB, live API hoặc secret thật. Test DB mới phải nằm trong namespace Database tương ứng; test gọi live API phải được đánh dấu `ApiLive` và cập nhật filter nếu thêm vào project API.

Lỗi restore/build/test làm check thất bại. Sau build thành công, cả hai bộ test được thử chạy dù bộ đầu thất bại. Workflow kiểm tra TRX của từng project, yêu cầu có test thực thi và toàn bộ test được chọn phải passed; thiếu báo cáo, skipped/inconclusive hoặc lỗi không được xem là pass đầy đủ.

Trong PR, mở **Checks → Build and test (Windows)** để đọc log; hoặc vào **Actions → CI → chọn lần chạy** để xem bảng kết quả và tải artifact `test-results-windows` chứa TRX (lưu 7 ngày). Nếu restore/build lỗi trước khi có báo cáo, bước upload chỉ cảnh báo thiếu file; check vẫn thất bại vì lỗi trước đó.

CI chưa xác minh PostgreSQL persistence, live API hoặc luồng thao tác UI thủ công. Build/test này không thay thế kiểm chứng các khu vực đó. Workflow chỉ chạy thực tế trên GitHub sau khi được đưa lên repository; việc thêm file local không tự commit, push, tạo PR, deploy hoặc thay branch protection.

## Git Workflow

```text
main
└── develop
    ├── feature/SCRUM-XX-...
    ├── feature/SCRUM-XX-...
    └── feature/SCRUM-XX-...
```
