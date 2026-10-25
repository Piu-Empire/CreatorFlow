# SCRUM-59 — AI Content Repurpose

Trạng thái: **ĐÃ TRIỂN KHAI** (25/10/2026). Branch `SCRUM-59-ai-content-repurpose`
(merge `origin/develop` tại `646b365`, giữ SCRUM-57/58 vì chưa có trên develop).
Build 0 error; 18 test Repurpose + 78 test `ContentDetailServiceTests` pass.

## 1. Phạm vi đã chốt

- Nút **"Tái sử dụng (AI)"** trong `Content Detail → Overview`.
- Chạy được cả **local (InMemory/PostgreSQL)** và **server mode (API)**.
- AI dùng **client POC** như SCRUM-57/58; output 3 khối Tiêu đề / Mô tả / Kịch bản.
- Mỗi lần **một platform đích**; Content mới bắt đầu ở **IDEA**.
- Kế thừa `ContentType` / `Priority` / `Sprint`; `Deadline` / `PlannedPublishAt` / `Assignee` để trống.
- Chỉ Content mới hiện **"Tái sử dụng từ CNT-xxx"**; không có danh sách phái sinh ở Content gốc.
- Sau khi tạo: **reload Board + toast**, không tự mở Content mới.
- Không làm: auto-post, ảnh, nhiều platform, log `ai_requests`, migration, chuyển AI lên backend.

## 2. Đã làm

**Dữ liệu**
- `ContentDraft` / `Content`: thêm `SourceContentId` (nullable).
- `ContentDetailContracts`: `CreateContentRequest` + `ContentDetailResponse` thêm `SourceContentId = null` (cuối record).
- `ContentDetailModels` (API): `ContentDetailRecord` + `NewContent` thêm `SourceContentId`.
- Ghi `content_relations` (`REPURPOSED`) **cùng transaction** khi tạo Content:
  `InMemoryContentDetailsRepository`, `ContentDetailsRepository` (local Postgres), `ContentDetailRepository` (API).
- Đọc lại source: `GetDetail` (InMemory/Postgres) và `GetDetailAsync` (API, subquery).

**Validate**
- `ContentService.Create` và `ContentDetailService.CreateAsync`: source phải tồn tại và cùng `ProjectId`.

**AI**
- `RepurposeAiRequest`, `RepurposeAiResponse`/`RepurposeParsedContent`, `IRepurposeAiService`.
- `RepurposeAiPromptBuilder` (prompt thuần), `RepurposeAiResponseParser` (3 mốc, fallback khi thiếu mốc).
- `RepurposeAiService`: adapter gọi `CreatorFlow.AI.IAIService`; lỗi/timeout → Failure, không lộ chi tiết; giữ `OperationCanceledException`.

**UI**
- `ContentDetailPanel`: nút "Tái sử dụng (AI)" (ẩn khi Archived hoặc không có AI), label nguồn, event `ContentRepurposed`.
- `RepurposeDialog`: chọn platform đích + yêu cầu thêm → preview sửa được → xác nhận tạo Content IDEA.
- `BoardForm`: nhận `IRepurposeAiService?`, handler reload + toast.
- `Program`: tạo `RepurposeAiService(aiService)` và truyền xuống.

## 3. Kiểm chứng đã chạy

| Bước | Kết quả |
| :--- | :--- |
| `dotnet build CreatorFlow.slnx` | 0 error, 1 warning có sẵn (`NotificationPanelControl.CloseRequested`) |
| `dotnet test ...IntegrationTests --filter "FullyQualifiedName~Repurpose"` | 18/18 pass (prompt 4, parser 4, service 7, relation 3) |
| `dotnet test ...Api.Tests --filter "FullyQualifiedName~ContentDetailServiceTests"` | 78/78 pass |

## 4. Còn lại

- Runtime UI local mode và server mode: chưa chạy (chờ duyệt riêng).
- Chưa commit / push / tạo PR.
