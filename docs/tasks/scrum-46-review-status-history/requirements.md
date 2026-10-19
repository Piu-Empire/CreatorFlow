# Requirements — Lịch sử Review và Status

Trạng thái: **Chờ duyệt** (chưa triển khai).

## 1. Yêu cầu gốc

- Story: "Lịch sử Review và Status" — In Progress, nhãn Improve Story.
- Mô tả: Hiển thị timeline các thay đổi trạng thái và review.
- Acceptance Criteria:
  1. Có old/new status, actor, time.
  2. Review history đầy đủ.

## 2. Hiện trạng trong `develop` (b227de3)

Những phần sau **đã có**, không cần làm lại từ đầu:

| Thành phần | Vị trí | Ghi chú |
| --- | --- | --- |
| Tab Activity trong Content Detail | `src/CreatorFlow/Controls/ContentDetailPanel.cs` (tab index 3) | `RefreshActivity()` → `FetchActivity()` |
| Điều khiển timeline | `src/CreatorFlow/Controls/ActivityTimelineControl.cs` + `.Designer.cs` | ListView 3 cột: Thời gian / Người thực hiện / Nội dung |
| Endpoint activity | `GET /api/contents/{contentId}/activity` | `ContentDetailEndpoints` → `ContentDetailService.GetActivityAsync` (kiểm tra thành viên Project) |
| Truy vấn gộp 2 nguồn | `ContentDetailRepository.GetActivityAsync` | `content_status_history` + `reviews`, sắp theo thời gian |
| Chế độ in-memory | `InMemoryActivityRepository` | Dựng activity từ `InMemoryDataStore` |
| Endpoint status-history (cấu trúc) | `GET /api/contents/{contentId}/status-history` | Đã có `FromStatus/ToStatus/ChangedBy/ChangedAt`, **client chưa dùng** |
| Schema dữ liệu | `database/01_schema.sql` | Đủ cột, không cần migration |

## 3. Khoảng trống so với Acceptance Criteria

### AC1 — old/new status, actor, time

- Hiện old/new chỉ là một chuỗi mô tả `"EDITING → REVIEW"` nằm trong `ActivityItem.Description`.
  Dữ liệu gốc (`content_status_history.from_status/to_status`) không được đưa ra contract/UI dưới dạng riêng.
- Thời gian đang format `dd/MM HH:mm`: thiếu năm, không phân biệt rõ thời điểm giữa các năm.
- Actor là một cột chữ; với sự kiện review, người gửi và người duyệt bị trộn vào cùng một cột.

### AC2 — Review history đầy đủ

- Mỗi lần gửi duyệt sinh **2 dòng**: `"EDITING → REVIEW"` và `"submitted Review #n"`.
- Mỗi lần duyệt/từ chối sinh thêm **2 dòng**: `"REVIEW → READY/EDITING"` và `"approved/rejected Review #n"`.
  → Timeline bị trùng lặp, khó đọc.
- Review đang `PENDING` không có trạng thái tường minh (chỉ có dòng "submitted").
- Không có cột hiển thị trạng thái review (PENDING/APPROVED/REJECTED), số review riêng, người duyệt riêng, thời điểm duyệt riêng.
- Feedback chỉ hiện khi có quyết định; ghi chú của lần chuyển trạng thái (`content_status_history.note`) cũng bị gộp vào cùng chỗ.

### Khác

- Không lọc/sắp xếp được theo loại sự kiện.
- Chưa có test cho nội dung cấu trúc của activity (chỉ có test quyền 401/403/404 và một test dữ liệu cơ bản).

## 4. Phạm vi đề xuất (v1)

1. **Contract/API có cấu trúc** (tương thích ngược):
   - Mở rộng `ActivityItemResponse` (và `ActivityRecord`) với:
     `EventType` (StatusChange / ReviewSubmitted / ReviewDecided),
     `FromStatus`, `ToStatus`, `ReviewNo`, `ReviewStatus`,
     `ActorUserId`, `DecidedAt`.
   - Giữ `Description` để các nơi khác không phải sửa theo; client dùng field cấu trúc để hiển thị.
2. **Repository**:
   - `ContentDetailRepository.GetActivityAsync`: trả đủ field cấu trúc từ 2 bảng.
   - `InMemoryActivityRepository`: dựng cùng cấu trúc để chế độ demo hiển thị giống API.
   - `ActivityRepository` (đường Npgsql phía client, hiện chỉ dùng khi chạy trực tiếp DB) cập nhật tương ứng.
3. **UI timeline** (`ActivityTimelineControl`):
   - Cột: Thời gian (`dd/MM/yyyy HH:mm`) · Sự kiện · Trạng thái (old → new) · Người thực hiện · Ghi chú/Feedback.
   - Mỗi sự kiện review hiển thị số review + trạng thái; review PENDING ghi rõ "Đang chờ duyệt".
   - **Gộp dòng trùng**: một lần submit/decision chỉ hiện 1 dòng sự kiện, nhưng vẫn giữ đủ old/new status, actor, thời gian.
   - Layout khai báo trong `ActivityTimelineControl.Designer.cs` (theo quy ước của team).
4. **Kiểm thử**:
   - Thêm test cho cấu trúc activity (status change + review submit + review decision) ở `CreatorFlow.Api.Tests`.

## 5. Ngoài phạm vi

- Không đổi schema DB, không thêm migration.
- Không đổi luồng workflow, permission hay endpoint hiện có (chỉ mở rộng response).
- Không làm notification, audit cấp Project, export CSV, màn hình lịch sử riêng ngoài Content Detail.
- Không sửa thư mục trùng `src/CreatorFlow/CreatorFlow/` (đang bị exclude khỏi build) — ghi nhận là technical debt riêng.

## 6. Câu hỏi cần chốt trước khi lập plan

Branch làm việc: `SCRUM-46-lịch-sử-review-va-status` (đang ở `b227de3`, bằng `origin/develop`).

1. **Vị trí**: giữ timeline trong tab Activity của Content Detail (đề xuất) hay tạo dialog/màn hình "Lịch sử" riêng?
2. **Gộp dòng trùng**: đồng ý gộp `EDITING → REVIEW` + `submitted Review #n` thành 1 dòng, và `REVIEW → READY` + `approved Review #n` thành 1 dòng? (đề xuất: có)
3. **Thứ tự**: mới nhất trên cùng (đề xuất) hay giữ cũ → mới như hiện tại?
4. **Ai được xem**: giữ như hiện tại — mọi thành viên Project xem được lịch sử của Content trong Project đó?

## 7. Kiểm chứng dự kiến

- `dotnet build CreatorFlow.slnx`.
- Unit test API cho activity (quyền + cấu trúc dữ liệu).
- Chạy app chế độ in-memory: chuyển trạng thái, submit/approve/reject → đối chiếu timeline.
- Chạy với API/PostgreSQL thật: kiểm tra dữ liệu 2 nguồn, thứ tự, quyền (401/403/404).

## 8. Ghi chú môi trường

- `AGENTS.md` tham chiếu `docs/CreatorFlow_UX.md`, `docs/ui/creatorflow-ui-styleguide.html`, `docs/CreatorFlow_KeHoachDuAn (1).md` nhưng các file này **không có trong repo hiện tại**; sẽ bám theo style của các control sẵn có.
