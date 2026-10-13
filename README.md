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

## Git Workflow

```text
main
└── develop
    ├── feature/SCRUM-XX-...
    ├── feature/SCRUM-XX-...
    └── feature/SCRUM-XX-...
```
