# SCRUM-47 — Phát hiện Overdue

Trạng thái: CHỜ DUYỆT requirements (chưa viết plan, chưa code).

Branch: `SCRUM-47-phat-hiện-overdue` (tách từ `develop` `f97bb7d`).

## 1. Nội dung ticket

- **To Do / Improve Task:** Phát hiện Overdue.
- **Description:** Xác định Content quá deadline nhưng chưa hoàn thành.
- **Acceptance Criteria:**
  1. Logic overdue dùng dữ liệu thật.
  2. Cung cấp dữ liệu cho Dashboard/Notification.

## 2. Hiện trạng (bằng chứng trong code)

| Hạng mục | Hiện tại | Ghi chú |
|---|---|---|
| `ContentBoardCard.IsOverdue` | `TaskRules.IsOverdue(Deadline, Status == ContentStatus.Published, DateTime.Today)` | Chỉ tính `Published` là xong, bỏ qua `Archived` |
| `MyTaskItem.IsOverdueOn` | `TaskRules.IsOverdue(Deadline, Status is Completed or Cancelled, today)` | Overdue theo assignment, không phải Content |
| `TaskRules.IsOverdue` | `!isFinished && deadline.HasValue && deadline.Value.Date < today.Date` | Client + API có bản sao (API chỉ dùng cho Tasks) |
| API overdue cho Content | **Không có** (grep `Overdue` trong `src/CreatorFlow.Api` = 0) | Server chỉ trả `deadline` thô trong board card |
| Board card API | `BoardCardResponse.Deadline` là `DateOnly?`, đọc từ `contents.deadline TIMESTAMPTZ` qua `ToUniversalTime()` | `DateOnly.FromDateTime(utc)` → ngày theo UTC |
| `notifications` (DB) | Có bảng, `notification_type` có giá trị `OVERDUE`, `content_id`, `recipient_user_id` | Chưa có service/endpoint/UI Notification |
| Dashboard | Không có module Dashboard; sidebar có mục "Summary" nhưng **chưa wire** (không có event `SummaryRequested`) | AC2 nói "cung cấp dữ liệu cho Dashboard", chưa có nơi tiêu thụ |
| Tài liệu Content Calendar | `docs/impl/CreatorFlow_KeHoach_ContentCalendar.md`: CAL-05/CAL-15 chốt `isFinished = Published/Archived` | Mâu thuẫn với `ContentBoardCard` hiện tại (chỉ `Published`) |

**Kết luận hiện trạng:** overdue đang được tính rời rạc phía client, chưa có nguồn chuẩn phía server, và có sai lệch quy tắc giữa Board và Calendar.

## 3. Mục tiêu

1. API xác định Content quá deadline nhưng chưa hoàn thành, **dùng dữ liệu thật trong PostgreSQL** (không mock, không suy diễn từ client).
2. Trả dữ liệu có cấu trúc để Dashboard/Notification dùng lại, tránh mỗi nơi tự tính một kiểu.

## 4. Định nghĩa overdue (đề xuất)

Một Content được coi là overdue khi **đồng thời**:

- Có `deadline` khác null.
- Ngày `deadline < hôm nay` (theo ngày, không theo giờ).
- Chưa hoàn thành.

Quy tắc đã chốt (Q1): "chưa hoàn thành" = **chưa PUBLISHED và chưa ARCHIVED** — đúng theo CAL-05/CAL-15, và khớp cách Calendar hiển thị.

Content `ARCHIVED` không tính (đã đóng, không còn theo dõi). Content không có deadline không tính. Deadline đúng hôm nay **không** tính (chỉ tính khi đã qua ngày).

## 5. Phạm vi dự kiến

### Trong phạm vi

- Contracts: DTO danh sách overdue (`contentId`, `code`, `title`, `status`, `deadline`, `daysLate`, `assignees`, `platforms`) + response có `total` và số đếm theo chỉ tiêu (nếu cần cho Dashboard).
- API: `GET /api/projects/{projectId}/overdue` (metadata project, permission, đọc DB thật).
- Repository Npgsql: một câu SELECT gộp `contents` (+ assignees/platforms nếu cần), có tham số ngày hôm nay.
- Service: rule overdue dùng chung, kiểm tra permission.
- Unit test service (fake repository) cho các biên: deadline hôm nay, không deadline, Published/Archived, quá hạn chưa xong.
- Đăng ký route vào `FeatureRoutingTests` (danh sách route cần auth).
- Chỉnh `ContentBoardCard.IsOverdue` cho khớp rule đã chốt (nếu Q1 chọn Published/Archived) — nằm trong phạm vi nhỏ, có test.

### Ngoài phạm vi (trừ khi bạn yêu cầu thêm)

- Xây UI Dashboard mới hoặc màn hình Notification.
- Sinh bản ghi `notifications` (job/notification service) — chỉ làm nếu Q2 chọn "sinh notification".
- Job chạy nền theo lịch (scheduler).

## 6. Acceptance Criteria (bản nháp, sẽ chốt ở plan)

1. `GET /api/projects/{projectId}/overdue` trả đúng tập Content quá hạn theo rule đã chốt, lấy từ PostgreSQL thật.
2. Không trả Content: không deadline, deadline trong tương lai/hôm nay, đã Published/Archived (theo Q1).
3. Mỗi item có đủ dữ liệu để Dashboard/Notification dùng: `contentId`, `code`, `title`, `status`, `deadline`, `daysLate`, người được giao, nền tảng.
4. Permission: chỉ thành viên project xem được; người ngoài project bị từ chối (xem Q4).
5. Test service phủ các biên ở mục 4 + happy path.

## 7. Quyết định đã chốt

| # | Câu hỏi | Quyết định |
|---|---|---|
| Q1 | "Chưa hoàn thành" | **Chưa PUBLISHED và chưa ARCHIVED** — sửa `ContentBoardCard.IsOverdue` cho khớp |
| Q2 | Phạm vi AC2 | **Chỉ endpoint + contract**; chưa sinh Notification, chưa làm UI Dashboard |
| Q3 | Múi giờ "hôm nay" | **UTC** (khớp cách API đang đọc deadline) |
| Q4 | Quyền xem | **Mọi thành viên project** (giống Board) |

## 8. Rủi ro / lưu ý

- Lệch múi giờ (Q3) có thể làm kết quả lệch 1 ngày so với kỳ vọng người dùng.
- Nếu giữ cả logic client và server, cần chốt một nguồn chuẩn để không lệch nhau (đề xuất: server là nguồn chuẩn, client chỉ hiển thị).
- AC2 nhắc Dashboard/Notification nhưng chưa có nơi tiêu thụ trong code — nếu không chốt phạm vi sẽ khó nghiệm thu.
