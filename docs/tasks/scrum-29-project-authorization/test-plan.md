# SCRUM-29 — Phạm vi kiểm chứng đã duyệt

## Trạng thái

Đã triển khai trên `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator`. Người dùng đã duyệt chạy test. Build thành công; Backend/API 243 passed, client/UI 73 passed, không fail/skip. Xem `test-results.md` và TRX kèm theo. Không thao tác database thật. Thay đổi AGENTS.md/README.md/.github và các tài liệu khác có sẵn được giữ nguyên, không thuộc SCRUM-29.

## Những phần cần test

| Phần | Cách kiểm chứng | Kết quả mong đợi |
| --- | --- | --- |
| Build | Build solution và compile test projects qua dotnet test | Không lỗi biên dịch do thay đổi |
| Creator tự nhận việc | TaskServiceTests: đổi deadline cũ, giữ deadline cũ/null, tự nhận mới | Đổi deadline cũ bị 403 trước mutation; các trường hợp hợp lệ không bị chặn |
| Workflow Service | WorkflowServiceTests gọi trực tiếp Service | O/M quyết định Review, C không; C submit/resubmit được; reject thiếu feedback bị từ chối |
| API authorization với JWT thật | ProjectAuthorizationTests dùng TestServer + JWT middleware thật + repository fake | 401 với token thiếu/sai/revoked; token role OWNER hay System Admin không bypass membership; body/query/header không đổi actor; role đổi/membership mất có hiệu lực request sau |
| Quyền Owner | ProjectServiceTests, ProjectMembersServiceTests, InvitationServiceTests và route tests liên quan | Manager/Creator không quản trị Project/member/role/invitation; Owner vẫn thao tác hợp lệ |
| UI/cache/context | ProjectClientTests, ProjectShellTests | Chuyển Project đổi quyền menu; xóa cache cũ; logout mất quyền; 403 làm mới role không retry write; response Project cũ không ghi đè context mới |
| Hồi quy các quyền đang có | Task/ContentDetail/Idea/Metric test hiện có | Assignment/deadline/Script/Idea/metrics vẫn giữ các quyền hợp lệ |

## Lệnh đề xuất sau khi được duyệt

```powershell
dotnet build CreatorFlow.slnx
dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj --filter "TestCategory=ProjectAuthorization|FullyQualifiedName~TaskServiceTests|FullyQualifiedName~TaskRoutingTests|FullyQualifiedName~ProjectServiceTests|FullyQualifiedName~ProjectMembersServiceTests|FullyQualifiedName~ProjectRoutingTests|FullyQualifiedName~InvitationServiceTests|FullyQualifiedName~FeatureRoutingTests|FullyQualifiedName~ContentDetailServiceTests|FullyQualifiedName~IdeaServiceTests|FullyQualifiedName~MetricServiceTests"
dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --filter "TestCategory=ProjectClient|TestCategory=ProjectShell|FullyQualifiedName~ApiTaskBackendTests"
```

Các test chọn trên dùng fake repository, HTTP stub/TestServer, không gọi API triển khai và không ghi DB. TestServer chạy trong process là runtime và vẫn chờ duyệt. Nếu restore/build bị giới hạn mạng hoặc thiếu dependency, ghi nhận chính xác; không tự đổi package.

## Rủi ro cần chú ý khi kiểm chứng

- Làm mới quyền sau 403 phải chạy sau handler UI hiện tại, tránh reentrancy hoặc lặp request; chỉ revalidate/read, không tự thử lại thao tác ghi.
- Invalidation role không được làm quyền Owner của Project A xuất hiện ở Project B. Bản nháp Content không bị xóa bởi thao tác refresh permissions; khi mất membership, reset workspace theo luồng hiện có.
- Guard deadline chỉ áp dụng assignment đã tồn tại của Creator tự nhận, không vô tình chặn Owner/Manager hoặc thay chính sách deadline lúc tạo mới.
- Không coi test fake là bằng chứng SQL/transaction/persistence hoặc kiểm chứng UI trên môi trường API triển khai thật.

## Kiểm chứng thủ công riêng với API/DB thật (chưa xin chạy trong đợt này)

Cần môi trường và tài khoản test được người dùng xác nhận riêng: cùng user Owner ở A, Creator ở B; Manager riêng; thử quyền UI và API trực tiếp; Owner đổi role/gỡ membership rồi dùng UI/token cũ; quan sát DB không đổi khi bị từ chối. Chưa khởi động hoặc thao tác môi trường này.

Trong đợt test chỉ chạy và ghi nhận kết quả. Nếu lỗi, không sửa code tại chỗ; điều tra/lập bug-report và chờ duyệt fix theo workflow.
