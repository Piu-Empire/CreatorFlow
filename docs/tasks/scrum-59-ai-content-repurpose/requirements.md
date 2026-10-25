# SCRUM-59 — Yêu cầu: AI Content Repurpose

Trạng thái: **YÊU CẦU ĐÃ DUYỆT** (25/10/2026); đang chờ duyệt `plan.md`.
Branch: `SCRUM-59-ai-content-repurpose` (tạo từ `SCRUM-52...` tại `d0b6773`, sau đó merge `origin/develop`).
Người viết: agent. Ngày soạn: 25/10/2026.

Quyết định người dùng đã chốt:
- **6.1** Nút trong **Content Detail → Overview**.
- **6.2** Làm **cả local và server mode**.
- **6.3** Dùng **AI client POC** như SCRUM-57/58.
- **6.4** Mỗi lần **một platform đích**.
- **6.5** Content mới ở **IDEA**.
- Các mục 6.6–6.10 giữ theo đề xuất trong bảng bên dưới.

Nguồn yêu cầu: người dùng cung cấp trực tiếp từ Jira:

> Từ Content gốc + platform đích, AI đề xuất phiên bản mới.
> AC: (1) Tạo được Content mới sau xác nhận. (2) Giữ liên kết Content gốc.

## 1. Mục tiêu

Cho người dùng chọn một **Content gốc** và một **platform đích**, gọi AI để **đề xuất phiên bản mới**
(tiêu đề / mô tả / kịch bản phù hợp platform đích). Người dùng **xem, sửa, xác nhận** rồi mới tạo
**Content mới**. Content mới phải được lưu **liên kết nguồn → phái sinh** để truy vết.

Luồng chuẩn theo `docs/ux-ui/CreatorFlow_UX.md` (§13):

```text
Original Content → Select Target Platform → Generate → Preview / Edit
→ Create New Content → Save SOURCE → DERIVED relation
```

Nguyên tắc bất biến: **AI không tự tạo, không tự lưu**; chỉ tạo sau khi người dùng bấm xác nhận.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả cho SCRUM-59 |
| :--- | :--- | :--- |
| `database/01_schema.sql:195-204` | **Đã có** bảng `content_relations` (`source_content_id`, `derived_content_id`, `relation_type` default `REPURPOSED`, unique, check khác nhau) | Không cần migration mới; chỉ cần code ghi/đọc |
| Toàn bộ `src/**` | **Không có** code nào dùng `content_relations` | Phải thêm đường ghi + đọc liên kết |
| `Models/ContentDraft.cs` | Không có trường nguồn | Cần thêm `SourceContentId` (nullable) |
| `Services/ContentService.cs:199-222` + `Repositories/Implementations/ContentDetailsRepository.cs:23-40` | Local/DB: `Create(...)` insert `contents` rồi `SyncPlatforms`, **cùng transaction** của `IUnitOfWork` | Chèn `content_relations` ngay sau insert, cùng transaction |
| `Repositories/InMemory/InMemoryContentDetailsRepository.cs:8-39` | Local demo mode dùng `InMemoryDataStore` | Cần thêm store relation InMemory |
| `CreatorFlow.Api/Services/ContentDetail/ContentDetailService.cs:134-180` + `Repositories/ContentDetail/ContentDetailRepository.cs:80-133` | Server mode: `POST /projects/{id}/contents` tạo `contents` + platforms + history trong transaction | Thêm validate nguồn + insert relation trong cùng transaction |
| `Services/Backends/IContentDetailBackend.cs` + `ApiContentDetailBackend.cs` | `Create(projectId, initialStatus, ContentDraft draft)` | Chỉ cần truyền thêm `SourceContentId` trong `ContentDraft`/contract |
| `Controls/ContentDetailPanel.cs` | Drawer 6 tab (Overview / Script / Checklist / Activity / References / Metrics), giữ context Content | Điểm vào tự nhiên để "Tái sử dụng (AI)" |
| `Controls/AiAssistant/AiAssistantControl.cs` | Trang Trợ lý AI (SCRUM-57): nhập chủ đề → gợi ý Idea/Title → lưu Idea/Content | Không có Content gốc; không phù hợp làm nơi chính |
| `AI/IAIService.cs`, `Services/AI/ScriptAiService.cs` (SCRUM-57/58) | Lõi AI client `AskAsync(prompt)` + adapter; **POC gọi AI phía client**, key ở `.env` | Tái dùng lõi; cần prompt/parser riêng cho Repurpose |
| `Services/AI/ScriptAiPromptBuilder.cs` | Chỉ xử lý `Outline`/`Script` | Không dùng trực tiếp cho Repurpose |
| `Models/Enums/AiRequestType.cs` | Đã có `Repurpose` | Dùng đúng loại request khi log (nếu có) |
| `Models/AiScriptResponse.cs` | Chỉ trả **một khối text**, không có cấu trúc Title/Description/Script | Cần quy ước output + parser chịu lỗi |
| `Services/ScriptService.cs:24-27` | Danh sách loại AI cho phép đã có `Repurpose` | Không đụng nghiệp vụ script |
| `docs/planning-tong/...ClientServer.md:145-148, 155-162` | Thiết kế đích: AI chạy Backend, key không nằm Client; user xem/sửa trước khi lưu | SCRUM-59 tiếp tục nợ kỹ thuật POC client như 57/58, ghi rõ |
| `CreatorFlow.csproj` | Thư mục `CreatorFlow\CreatorFlow\` là bản sao cũ, bị `<Compile Remove>` | **Không sửa** thư mục này |

## 3. Phạm vi đề xuất

### 3.1. Điểm vào (đề xuất)

Thêm nút **"Tái sử dụng (AI)"** trong **Content Detail → tab Overview** (khu vực action), chỉ hiện khi
Content đang mở đọc được. Lý do: đã có sẵn Content gốc + Project context, đúng UX "AI xuất hiện theo Context".

Không thêm mục menu mới; không đổi điều hướng hiện có.

### 3.2. Hộp thoại Repurpose

Một dialog riêng (modal) theo style guide, gồm:

1. **Tóm tắt Content gốc** (mã + tiêu đề + platform hiện tại).
2. **Platform đích** (mặc định: platform đầu tiên khác platform gốc; nếu Content gốc chưa có platform thì
   chọn platform đầu tiên đang bật).
3. **Yêu cầu thêm** (tùy chọn — tone, độ dài, CTA...).
4. Nút **"Đề xuất bằng AI"** → loading, không treo UI.
5. **Preview 3 trường sửa được**: Tiêu đề, Mô tả, Kịch bản.
6. Nút **"Tạo Content mới"** → hộp xác nhận → tạo; **Hủy** → không ghi gì.

Lỗi AI/chưa cấu hình/timeout → error state trong dialog + nút thử lại; không dùng MessageBox chặn giữa chừng.

### 3.3. Dữ liệu Content mới

- `Status = IDEA` (đi tiếp workflow bình thường; không nhảy cóc).
- `Title` / `Description` / `Script` lấy từ preview người dùng đã sửa.
- `Platforms = [platform đích]`.
- `ContentType`, `Priority`, `Sprint` kế thừa Content gốc (người dùng có thể sửa sau ở tab Overview).
- `Deadline` / `PlannedPublishAt` / `Assignee` để trống.
- `SourceContentId` = Content gốc.

### 3.4. Liên kết nguồn → phái sinh

- Ghi 1 dòng `content_relations (source_content_id, derived_content_id, relation_type='REPURPOSED', created_by)`
  **cùng transaction** với lúc tạo Content mới.
- Validate: Content gốc tồn tại, cùng `project_id`, khác Content mới; user là thành viên Project.
- Hiển thị ở Content mới (tab Overview): dòng **"Tái sử dụng từ CNT-xxx"**.
- (Tùy chọn, nếu duyệt) hiển thị ở Content gốc danh sách "Đã tái sử dụng thành ...".

### 3.5. AI

- Thêm bộ ba thuần để test offline:
  - `RepurposeAiPromptBuilder` — dựng prompt từ Content gốc + platform đích + instruction.
  - `RepurposeAiResponseParser` — tách output thành `Title` / `Description` / `Script`.
  - `RepurposeAiService` — adapter gọi lõi `CreatorFlow.AI.IAIService`, map lỗi/timeout thành kết quả thất bại.
- Quy ước output có mốc rõ ràng:

```text
== TIÊU ĐỀ ==
...
== MÔ TẢ ==
...
== KỊCH BẢN ==
...
```

- Parser **chịu lỗi**: thiếu mốc → toàn bộ text vào Kịch bản, Tiêu đề/Mô tả fallback từ Content gốc + cảnh báo
  "AI trả về không đúng định dạng, vui lòng kiểm tra lại".
- Prompt nêu rõ platform đích, đặc trưng độ dài/format, không markdown fence, không giải thích ngoài lề.

### 3.6. Quyền

- Tạo Content: theo đúng luật hiện có (`ContentService.Create` / API `CreateAsync`), không thêm luật riêng.
- Gọi AI: chỉ khi `IsConfigured`; người dùng không phải thành viên Project → không mở được Content Detail
  (đã chặn sẵn).
- Permission kiểm tra ở Service/Backend, không chỉ ẩn nút.

## 4. Ngoài phạm vi (đề xuất)

- Chuyển AI ra Backend (vẫn POC client như SCRUM-57/58) — ghi nợ kỹ thuật.
- Log `ai_requests`.
- Repurpose hàng loạt / nhiều platform cùng lúc / nhiều Content gốc.
- Sinh ảnh, thumbnail, hashtag, lịch đăng.
- Sửa `content_relations` cho các loại quan hệ khác (`SERIES`, `REMAKE`...).
- Xóa/gỡ liên kết; cascade delete lịch sử.
- Migration mới (bảng đã có).
- Thay đổi `ScriptService`, `ContentWorkflowPolicy`.

## 5. Luồng nghiệp vụ đề xuất

```text
Mở Content Detail (Content gốc)
   ↓  [Tái sử dụng (AI)]
Dialog Repurpose: chọn platform đích + yêu cầu thêm
   ↓  [Đề xuất bằng AI]
Loading ──(lỗi/chưa cấu hình/timeout)──► Error + [Thử lại]
   ↓ thành công
Preview 3 trường (Title / Description / Script) — sửa được
   ↓  [Tạo Content mới] → hộp xác nhận
ContentService.Create / API CreateAsync
   ├─ INSERT contents + content_platforms (platform đích)
   └─ INSERT content_relations (source → derived, REPURPOSED)   [cùng transaction]
   ↓
Toast "Đã tạo Content mới từ CNT-xxx"
   ↓
Content mới (IDEA) hiển thị "Tái sử dụng từ CNT-xxx"
```

## 6. Quyết định cần chốt

| # | Vấn đề | Đề xuất |
| :--- | :--- | :--- |
| 6.1 | Điểm vào | Nút trong **Content Detail → Overview** (không thêm menu) |
| 6.2 | Phạm vi mode | Làm **cả local (InMemory/DB) và server (API)** để AC2 đúng ở cả hai |
| 6.3 | Nguồn AI | Tái dùng **lõi client POC** như SCRUM-57/58 |
| 6.4 | Platform đích | **Một platform** mỗi lần repurpose |
| 6.5 | Trạng thái Content mới | **IDEA** |
| 6.6 | Kế thừa | ContentType + Priority + Sprint từ Content gốc; Deadline/Assignee trống |
| 6.7 | Hiển thị liên kết | Content mới hiện "Tái sử dụng từ CNT-xxx"; **chưa** làm danh sách phái sinh ở Content gốc |
| 6.8 | Điều kiện Content gốc | Cho phép mọi trạng thái (kể cả chưa Published) |
| 6.9 | Log `ai_requests` | Không |
| 6.10 | Môi trường test | Local mode (InMemory) trước, sau đó server mode nếu môi trường API sẵn sàng |

## 7. Tiêu chí hoàn thành (map AC)

- [ ] **AC1 — Tạo được Content mới sau xác nhận:** AI chỉ trả preview; **không** có bản ghi Content nào được
      tạo trước khi người dùng bấm "Tạo Content mới" và xác nhận. Hủy → không có thay đổi dữ liệu.
- [ ] **AC2 — Giữ liên kết Content gốc:** sau khi tạo, có đúng 1 dòng `content_relations` REPURPOSED
      (source → derived), ghi cùng transaction; Content mới hiển thị "Tái sử dụng từ CNT-xxx".
- [ ] **Chức năng:** chọn platform đích, sinh preview 3 trường, sửa được, tạo Content mới đúng platform/status;
      xử lý chưa cấu hình/lỗi/timeout, không treo UI.
- [ ] **Kiến trúc:** UI → Service/Backend; không SQL trong Form; không gọi LLM trực tiếp trong Form; không lộ key.
- [ ] **Quyền:** validate thành viên/cùng Project ở Service/Backend; UI chỉ ẩn nút, không thay thế kiểm tra.
- [ ] **Dữ liệu:** relation + content tạo nguyên tử; Content gốc không bị sửa.
- [ ] **UI:** đúng style guide chung; không đổi nav.
- [ ] **Test:** unit test prompt builder / parser / service (fake AI) + test tạo content kèm relation;
      build `dotnet build CreatorFlow.slnx` 0 error.
- [ ] **Không** thêm migration, không đụng `src/CreatorFlow/CreatorFlow/**`.

## 8. Subtask đề xuất cho Jira

1. **Chốt UX & dữ liệu** — điểm vào, platform đích, trạng thái Content mới, trường kế thừa.
2. **Lưu liên kết nguồn → phái sinh (local)** — thêm `SourceContentId` vào `ContentDraft`; ghi
   `content_relations` trong `ContentDetailsRepository.Create` + `InMemoryContentDetailsRepository` cùng transaction.
3. **Lưu liên kết qua API (server mode)** — thêm `SourceContentId` vào `CreateContentRequest`; validate cùng
   Project + insert `content_relations` trong `ContentDetailRepository.CreateAsync`; trả thông tin nguồn trong
   `ContentDetailResponse`.
4. **AI Repurpose** — `RepurposeAiPromptBuilder` + `RepurposeAiResponseParser` + `RepurposeAiService` dùng lõi
   `CreatorFlow.AI.IAIService`; xử lý chưa cấu hình/lỗi/timeout.
5. **UI dialog Repurpose** — mở từ Content Detail Overview; chọn platform + yêu cầu thêm; loading; preview sửa
   được; xác nhận tạo; toast; error state.
6. **Hiển thị liên kết** — dòng "Tái sử dụng từ CNT-xxx" ở Content mới.
7. **Test & kiểm chứng** — unit test prompt/parser/service/relation; build; runtime local mode (và server mode
   nếu có môi trường).

## 9. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước build/test/runtime phải trình
checklist theo `post-change-test-workflow` và chờ duyệt; test chỉ quan sát, không sửa code.
File này chỉ kiểm tra tĩnh (chưa code).

## 10. Câu hỏi mở

1. Chốt điểm vào ở **Content Detail Overview** (6.1)?
2. Làm **cả local + server mode** hay chỉ local mode trước (6.2)?
3. Giữ **AI client POC** như 57/58 hay bắt buộc chuyển Backend ngay (6.3)?
4. Platform đích **một** hay **nhiều** (6.4)?
5. Content mới bắt đầu ở **IDEA** hay **SCRIPT** (6.5)?
6. Có cần hiển thị danh sách phái sinh ở Content gốc ngay trong task này (6.7)?
7. Cho repurpose **mọi trạng thái** hay chỉ Published (6.8)?
