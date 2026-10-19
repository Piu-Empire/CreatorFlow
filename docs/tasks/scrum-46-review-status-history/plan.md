# Plan — SCRUM-46 Lịch sử Review và Status

Trạng thái: **Đã duyệt requirements (mặc định) — đang triển khai**.
Branch: `SCRUM-46-lịch-sử-review-va-status` (base `b227de3`).

## Quyết định đã chốt (mặc định)

1. Giữ timeline trong tab **Activity** của Content Detail.
2. Gộp dòng trùng: status row của luồng Review + review row → 1 sự kiện.
3. Thứ tự: cũ → mới (giữ như hiện tại, có scroll tới dòng cuối).
4. Quyền xem: giữ nguyên — thành viên Project xem được Content trong Project.

## Cách làm

### 1. Dữ liệu có cấu trúc (Contracts)

`src/CreatorFlow.Contracts/ContentDetail/ContentDetailContracts.cs`
- Thêm `ActivityKinds` (StatusChange / ReviewSubmitted / ReviewDecided).
- Mở rộng `ActivityItemResponse` với `Kind`, `FromStatus`, `ToStatus`, `ReviewNo`, `ReviewStatus` (giữ `Description` để tương thích).

`src/CreatorFlow.Contracts/ContentDetail/ActivityTimeline.cs` (mới)
- Hàm thuần `Compose(entries)`: gộp status row của luồng Review với review row
  (cùng actor, cùng hướng, lệch ≤ 5 giây), lấy ghi chú submit từ status row,
  bỏ dòng trùng, sắp tăng dần. API và client dùng chung để không lệch logic.

### 2. API

- `Models/ContentDetail/ContentDetailModels.cs`: `ActivityRecord` thêm field cấu trúc.
- `Repositories/ContentDetail/ContentDetailRepository.cs`: trả status PascalCase,
  review kèm `ReviewNo`/`ReviewStatus`, rồi `ActivityTimeline.Compose`.
- `Services/ContentDetail/ContentDetailService.cs`: map field mới ra response.

### 3. Client

- `Models/ActivityItem.cs`: thêm field cấu trúc.
- `Services/Backends/ApiContentDetailBackend.cs`: map field mới.
- `Repositories/InMemory/InMemoryActivityRepository.cs` và
  `Repositories/Implementations/ActivityRepository.cs`: dựng `ActivityItemResponse`
  từ nguồn tương ứng rồi dùng chung `ActivityTimeline.Compose` (đồng bộ hành vi).

### 4. UI

- `Controls/ActivityTimelineControl.Designer.cs`: 5 cột
  **Thời gian · Sự kiện · Trạng thái · Người thực hiện · Ghi chú**
  (`dd/MM/yyyy HH:mm`).
- `Controls/ActivityTimelineControl.cs`: nhãn sự kiện (Tạo nội dung / Đổi trạng thái /
  Gửi duyệt #n / Phê duyệt #n / Từ chối #n), cột trạng thái `Old → New`,
  feedback/ghi chú ở cột cuối. Item cũ không có field cấu trúc → fallback `Description`.

### 5. Test

- `tests/CreatorFlow.Api.Tests/ContentDetail/ActivityTimelineTests.cs` (mới):
  gộp submit, gộp decision, giữ status thường, sắp xếp.
- Cập nhật test activity trong `ContentDetailServiceTests` để kiểm tra field cấu trúc.

## Không đổi

- Schema DB, endpoint, workflow, permission.
- `Description` giữ nguyên format cũ nên `GetLatestRejection` trong ContentDetailPanel
  (dựa vào `"rejected Review"`) không bị ảnh hưởng.

## Kiểm chứng

1. `dotnet build CreatorFlow.slnx` — chờ duyệt trước khi chạy.
2. `dotnet test tests/CreatorFlow.Api.Tests` (unit, không cần DB).
3. Chạy app in-memory: tạo content → chuyển trạng thái → submit → approve/reject,
   kiểm tra timeline (cần duyệt riêng).

## Kết quả kiểm chứng (19/10/2026)

- `dotnet build CreatorFlow.slnx`: **Build succeeded, 0 error**; còn 8 warning NU1902/NU1903
  có sẵn từ package `System.Security.Cryptography.Pkcs` (không thuộc thay đổi này).
- `dotnet test tests/CreatorFlow.Api.Tests`: **379 passed, 0 failed, 55 skipped**
  (55 test bị skip là integration test cần PostgreSQL — chưa chạy trong lần này).
- Smoke test headless (harness tạm ngoài repo, dùng `WorkflowService` + repository in-memory +
  `ActivityTimeline` + `ActivityTimelineControl` thật):
  - Content #1 (đổi trạng thái thường): Idea → Script → Production → Editing, 3 dòng, đủ actor/ghi chú.
  - Content #4 (gửi duyệt → duyệt): đúng 2 dòng `Gửi duyệt #1` + `Phê duyệt #1`, không còn dòng trùng.
  - Content #9 (gửi duyệt → từ chối): đúng 2 dòng `Gửi duyệt #1` + `Từ chối #1` kèm feedback.
  - Lưu ý: harness dùng giờ UTC của in-memory; luồng API có `ToLocalTime()`.
- Chưa mở cửa sổ app với dữ liệu thật (DEV API chưa deploy build SCRUM-46) và chưa test
  API/PostgreSQL thật.

