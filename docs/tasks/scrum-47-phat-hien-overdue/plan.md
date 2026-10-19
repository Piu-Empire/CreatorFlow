# SCRUM-47 — Kế hoạch triển khai (Phát hiện Overdue)

Trạng thái: **CHỜ DUYỆT plan** (chưa code).

Requirements: `docs/tasks/scrum-47-phat-hien-overdue/requirements.md` (đã chốt 4 quyết định).
Branch: `SCRUM-47-phat-hiện-overdue` (base `develop` `f97bb7d`).

## 1. Quyết định đã chốt (không mở lại)

| # | Nội dung |
|---|---|
| Q1 | "Chưa hoàn thành" = chưa **Published** và chưa **Archived** |
| Q2 | Chỉ cung cấp **endpoint + contract**; không sinh Notification, không làm UI Dashboard |
| Q3 | "Hôm nay" tính theo **UTC** (giống `TaskService.Today`) |
| Q4 | **Mọi thành viên project** được xem (giống Board) |

## 2. Thiết kế

### 2.1 Rule dùng chung — `src/CreatorFlow.Api/Services/Overdue/OverdueRules.cs` (mới)

Static, thuần, không I/O (dễ unit test, một nguồn sự thật cho rule):

```csharp
public static class OverdueRules
{
    // Đã hoàn thành = đã xuất bản hoặc đã lưu trữ (CAL-05/CAL-15).
    public static bool IsFinished(string status) =>
        status is "Published" or "Archived";

    // Quá hạn = có deadline, deadline theo NGÀY đã qua, và chưa hoàn thành.
    public static bool IsOverdue(DateOnly? deadline, string status, DateOnly today) =>
        deadline is { } value && !IsFinished(status) && value < today;

    public static int DaysLate(DateOnly deadline, DateOnly today) =>
        Math.Max(0, today.DayNumber - deadline.DayNumber);
}
```

`status` nhận chuỗi PascalCase như các model hiện có (`BoardCard.Status`, `ContentDetailService`), không thêm enum chung mới.

### 2.2 Contracts — `src/CreatorFlow.Contracts/Overdue/OverdueContracts.cs` (mới)

```csharp
public sealed record OverdueAssigneeResponse(long UserId, string DisplayName);

public sealed record OverdueContentResponse(
    long ContentId, long ProjectId, string ContentCode, string ContentTitle,
    string Status, DateOnly Deadline, int DaysLate,
    IReadOnlyList<OverdueAssigneeResponse> Assignees,
    IReadOnlyList<string> Platforms);

public sealed record OverdueResponse(
    long ProjectId, DateOnly AsOfDate, int Total, IReadOnlyList<OverdueContentResponse> Items);
```

- `AsOfDate` = ngày UTC đã dùng để tính, để nơi tiêu thụ biết mốc so sánh.
- `Total` = `Items.Count` (Dashboard khỏi phải đếm lại).
- Có `assignees` để sau này Notification biết gửi cho ai (chỉ dữ liệu, chưa gửi).

### 2.3 Service — `src/CreatorFlow.Api/Services/Overdue/OverdueService.cs` (mới)

Chữ ký bám `TaskService`/`ReviewQueueService`:

```csharp
public sealed class OverdueService(
    IBoardCardRepository cards,
    ITaskRepository tasks,
    CurrentAuthenticatedUser current,
    TimeProvider timeProvider)
{
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public Task<AuthOperationResult<OverdueResponse>> GetOverdueAsync(long projectId, CancellationToken token = default);
}
```

Luồng `GetOverdueAsync`:
1. Chưa đăng nhập → `401 unauthorized` ("Vui lòng đăng nhập lại.").
2. `tasks.GetRoleAsync(projectId, user.UserId)` null → `403 forbidden` ("Bạn không thuộc Project này."). Có role bất kỳ (Owner/Manager/Creator) → được xem (Q4).
3. `cards.GetCardsAsync(projectId)` — tái dùng câu SELECT của Board (đã loại `ARCHIVED`, đã có `Code`, `Status` PascalCase, `Deadline` là `DateOnly`, `Platforms`).
4. `tasks.GetBoardAssigneesAsync(projectId)` (đã loại assignment `CANCELLED`), gom theo `ContentId` như `BoardService` để gắn `Assignees`.
5. Lọc `OverdueRules.IsOverdue(card.Deadline, card.Status, Today)`, sắp xếp theo `Deadline` rồi `ContentId`, tính `DaysLate`.

**Vì sao tái dùng `IBoardCardRepository` thay vì viết SQL mới:** Board và Overdue phải cùng một nguồn dữ liệu (hiện tại Board và Calendar đang lệch quy tắc); tránh hai câu SELECT gần giống nhau; dữ liệu mỗi project nhỏ nên lọc trong bộ nhớ chấp nhận được. Không giảm rủi ro truy vấn vì `Deadline`/`Status` lọc sau khi đọc, nhưng đổi lại không nhân bản SQL.

### 2.4 Endpoint — `src/CreatorFlow.Api/Endpoints/OverdueEndpoints.cs` (mới)

```csharp
var api = routes.MapGroup("/api").RequireAuthorization();
api.MapGet("/projects/{projectId:long}/overdue",
    async (long projectId, OverdueService service, CancellationToken token) =>
        AuthEndpoints.ToHttp(await service.GetOverdueAsync(projectId, token)));
```

### 2.5 Đăng ký — `src/CreatorFlow.Api/Program.cs`

- DI: `builder.Services.AddScoped<OverdueService>();` (cạnh `ReviewQueueService`).
- Route: `app.MapOverdueEndpoints();`.

### 2.6 Sửa client cho khớp Q1 — `src/CreatorFlow/Models/ContentBoardCard.cs`

```csharp
// trước: TaskRules.IsOverdue(Deadline, Status == ContentStatus.Published, DateTime.Today)
TaskRules.IsOverdue(Deadline, Status is ContentStatus.Published or ContentStatus.Archived, DateTime.Today)
```

Không đổi chỗ khác ở client: chưa nối UI vào endpoint mới (Q2).

## 3. Test

### 3.1 `tests/CreatorFlow.Api.Tests/Overdue/OverdueRulesTests.cs` (mới)

- `IsOverdue`: deadline hôm qua + chưa xong → true; deadline hôm nay → false; không deadline → false; Published → false; Archived → false.
- `DaysLate`: 1 ngày, nhiều ngày, deadline tương lai/hôm nay → 0.

### 3.2 `tests/CreatorFlow.Api.Tests/Overdue/OverdueServiceTests.cs` (mới)

Bám style `TaskServiceTests` (MSTest + `Fixture` + `FixedTimeProvider(Today)`), fake `IBoardCardRepository` (chỉ giữ danh sách `BoardCard`) và dùng `MemberRolesFake` (đã có `Roles` + `BoardAssignees`):

- chưa đăng nhập → 401 `unauthorized`;
- không là thành viên → 403 `forbidden`;
- thành viên Creator → xem được (Q4);
- content quá hạn chưa xong → có trong `Items`, `DaysLate` đúng, `ContentCode = CNT-xxx`;
- deadline hôm nay / tương lai / null → không có trong `Items`;
- Published → không có; Archived không xuất hiện (repo Board đã loại, test này kiểm tra rule);
- gắn đúng `Assignees` và `Platforms`;
- sắp xếp theo `Deadline` tăng dần;
- `AsOfDate = Today`, `Total = Items.Count`.

### 3.3 `tests/CreatorFlow.Api.Tests/Api/FeatureRoutingTests.cs`

- Thêm `(HttpMethod.Get, "/api/projects/1/overdue", null)` vào `AllRoutes` (kiểm tra 401 khi chưa đăng nhập).
- Đăng ký thêm `IBoardCardRepository` fake + `OverdueService`, gọi `app.MapOverdueEndpoints()`.

### 3.4 `tests/CreatorFlow.IntegrationTests/Shared/ContentBoardCardTests.cs` (mới)

- `ContentBoardCard.IsOverdue`: quá hạn + `Script` → true; quá hạn + `Published` → false; quá hạn + `Archived` → false; deadline hôm nay → false; không deadline → false.

## 4. Ngoài phạm vi (không làm ở ticket này)

- Sinh bản ghi `notifications` type `OVERDUE`, job chạy nền, màn hình Dashboard/Notification.
- Client gọi endpoint overdue mới (Board vẫn tính tại client như hiện tại).
- Migration DB: **không cần đổi schema**.
- Không sửa `TaskRules`/My Tasks (overdue theo assignment đã có, khác phạm vi Content).

## 5. Kiểm chứng dự kiến

| Kiểm tra | Cách | Lưu ý |
|---|---|---|
| Build | `dotnet build CreatorFlow.slnx` | Cần đóng app CreatorFlow + stop debug VS (đang giữ `bin`) |
| Unit/route API | `dotnet test tests/CreatorFlow.Api.Tests` | Kỳ vọng: tất cả pass, +k test mới |
| Client rule | `dotnet test tests/CreatorFlow.IntegrationTests` | Project này compile được (đã kiểm tra) |
| Runtime API thật | Không chạy trong bước này | Cần PostgreSQL local (chưa có mật khẩu) hoặc DEV API sau khi deploy |

Bước runtime thật (API + DB) phải xin phép riêng theo `post-change-test-workflow`.

## 6. Rủi ro / lưu ý

- **UTC (Q3):** content hạn ngày 19/10 giờ VN có thể bị coi quá hạn sớm ~7 giờ. Đã chốt chấp nhận; ghi lại để sau này có thể cấu hình.
- **Trùng lặp rule:** server có `OverdueRules`, client có `TaskRules` + `ContentBoardCard`. Đây là chủ ý (client hiển thị từ payload Board, server là nguồn chuẩn cho Dashboard/Notification); nếu sau này thêm `IsOverdue` vào `BoardCardResponse` thì nên bỏ bản client.
- **Phụ thuộc `IBoardCardRepository`:** nếu Board đổi cách loại `ARCHIVED`, overdue đổi theo; test sẽ bắt được.
- **AC2 chưa có nơi tiêu thụ:** endpoint là điểm dừng hợp lý; Dashboard/Notification cần ticket sau (ghi rõ để không bị coi là thiếu).

## 7. Thứ tự thực hiện sau khi plan được duyệt

1. `OverdueRules` + `OverdueContracts`.
2. `OverdueService` (+ DI), `OverdueEndpoints` (+ map).
3. Test: `OverdueRulesTests`, `OverdueServiceTests`, `FeatureRoutingTests`.
4. Sửa `ContentBoardCard.IsOverdue` + `ContentBoardCardTests`.
5. Build + chạy test, báo kết quả; dừng trước bước runtime thật.

## 8. Kết quả kiểm chứng (đã chạy)

Ngày chạy: 19/10/2026, branch `SCRUM-47-phat-hiện-overdue`.

| Kiểm tra | Lệnh | Kết quả |
|---|---|---|
| Build API | `dotnet build src/CreatorFlow.Api` | 0 error |
| Build + test API | `dotnet test tests/CreatorFlow.Api.Tests` | **418 passed, 0 failed, 60 skipped** (trước đó 405 passed) |
| Build + test client | `dotnet test tests/CreatorFlow.IntegrationTests` | **372 passed, 0 failed, 30 skipped** |
| Compile UI | `dotnet build src/CreatorFlow/CreatorFlow.csproj -t:Compile` | 0 error |
| Build toàn solution | `dotnet build CreatorFlow.slnx` | **Build succeeded**, 0 error, 8 warning (baseline NU1902/NU1903 của `System.Security.Cryptography.Pkcs` 10.0.0) |

Test mới: 6 case `OverdueRulesTests`, 6 case `OverdueServiceTests`, 1 case HTTP `Overdue_OverHttp_...` trong `FeatureRoutingTests`, 3 case `ContentBoardCardTests`.

Chưa chạy: runtime thật với API + PostgreSQL (cần deploy hoặc DB local); sinh Notification; UI Dashboard. Endpoint `GET /api/projects/{id}/overdue` chỉ có trên máy cho tới khi nhánh được merge + deploy.

