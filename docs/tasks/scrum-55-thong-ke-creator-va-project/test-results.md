# SCRUM-55 — Kết quả kiểm chứng (local)

Ngày: 27/10/2026. Môi trường: Docker local (`deploy/docker/compose.dev.yml`), PostgreSQL 18 + API
`http://localhost:5080`, WinForms trỏ API local bằng biến môi trường `Api__BaseUrl`
(không sửa `appsettings.Local.json`, không đụng DB dùng chung).

## 1. Kiểm tra tĩnh

- `dotnet build CreatorFlow.slnx` → **0 error**, 2 warning có sẵn từ trước (NotificationPanelControl, FakeNotificationRepository).
- `dotnet test --filter "FullyQualifiedName~Statistics"` → **8/8 pass**.

| Test | Nội dung |
| :--- | :--- |
| NotSignedIn_Returns401 | Chưa đăng nhập → 401, không gọi repository |
| OutsideProject_Returns403_AndDoesNotQuery | Người ngoài project → 403, không truy vấn |
| CreatorRole_CanReadProjectStatistics | Creator trong project được đọc (giống quyền đọc metrics) |
| MapsCountsAndEngagement | Map đúng Total/InProgress/Published/Overdue + Engagement 5,58% |
| ProjectRow_IsSumOfAllCreators_NotAffectedByCreatorFilter | Lọc 1 creator nhưng dòng "Toàn Project" vẫn là tổng cả project |
| FilterUnknownCreator_ReturnsEmptyList_NoLeak | creatorId lạ → rỗng, không lộ dữ liệu |
| ZeroViews_EngagementIsZero | Views = 0 → Engagement 0 (UI hiện "—") |
| CreatorIdZeroOrNegative_TreatedAsAll | id ≤ 0 = tất cả |

## 2. Kiểm chứng SQL thật (API local)

`GET /api/projects/1/statistics/creators` (project "Tết 2026", đăng nhập `an@demo.vn`):

| Creator | Total | In Progress | Published | Overdue | Views | Engagement |
| :--- | --: | --: | --: | --: | --: | --: |
| Toàn Project | 25 | 11 | 14 | 3 | 210.000 | 4,87% |
| Minh | 12 | 4 | 8 | 1 | 120.000 | 5,58% |
| Lan | 9 | 4 | 5 | 1 | 80.000 | 4,13% |
| Huy | 4 | 3 | 1 | 1 | 10.000 | 2,25% |

- `?creatorId=2` → chỉ còn dòng Minh, dòng "Toàn Project" **không đổi** (25/11/14/3).
- Content ARCHIVED của Minh (13 content nếu tính cả archived) **không** xuất hiện: Total = 12.
- Project thứ hai "Hè 2026": Minh = 3 content / 2 published / 30.000 views → chứng minh số không bị
  cộng dồn giữa project (AC2), và đổi project thì số đổi theo (AC1).

## 3. Cách mở app demo

Điều kiện: Docker Desktop đang chạy, container `creatorflow-dev-db-1` + `creatorflow-dev-api-1` up
(`docker compose --env-file deploy/docker/.env -f deploy/docker/compose.dev.yml ps`).

```
dotnet build CreatorFlow.slnx
$env:Api__BaseUrl = "http://localhost:5080/"
.\src\CreatorFlow\bin\Debug\net10.0-windows\CreatorFlow.exe
```

Đăng nhập: `an@demo.vn` / `Demo@123` → chọn project **Tết 2026** → sidebar **Reports**.

Trong màn Reports:
1. Bấm **Creator & Project** (cạnh nút "Hiệu suất").
2. Xem bảng: dòng **Toàn Project** in đậm + 3 dòng Minh/Lan/Huy.
3. Đổi combo **Creator** → bảng còn đúng dòng người đó; dòng Toàn Project giữ nguyên.
4. Đổi project sang **Hè 2026** → bảng còn Minh 3 / 1 / 2 / 0, 30.000 views.

Dữ liệu mẫu tái tạo được bằng `docs/tasks/scrum-55-thong-ke-creator-va-project/demo-seed.sql`
(chỉ dùng cho DB local; không chạy trên DB dùng chung).

## 4. Chưa kiểm chứng

- Chưa chạy trên DB dev/server dùng chung (không cần cho task này).
- Chưa test UI ở DPI/độ phân giải khác; chưa chụp ảnh màn hình.
- Chưa commit/push/tạo PR — chờ người dùng nghiệm thu.
