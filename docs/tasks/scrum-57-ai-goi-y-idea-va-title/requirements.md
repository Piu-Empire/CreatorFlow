# SCRUM-57 — Yêu cầu: Nhập chủ đề / platform / mục tiêu và nhận gợi ý Idea/Title

Trạng thái: **CHỜ DUYỆT** (chưa triển khai). Branch: `SCRUM-57-ai-gợi-y-idea-va-title` (tạo từ `SCRUM-53-dashboard-project` đã merge `develop`).
Cập nhật 19/10/2026: **người dùng chốt phương án (A)** ở mục 6.1 (dùng lại lõi AIService của SCRUM-56) — xem `plan.md`.
Người viết: agent. Ngày soạn: 19/10/2026.
Nguồn yêu cầu: người dùng cung cấp trực tiếp:

> Nhập chủ đề/platform/mục tiêu và nhận gợi ý Idea/Title.
> AC: (1) Có loading/error state; (2) User được chỉnh trước khi lưu; (3) Lưu được thành Idea/Content.

## 1. Mục tiêu

Thêm màn **Trợ lý AI (AI Assistant)** cho Project hiện hành: người dùng nhập **chủ đề**, chọn **platform**,
nhập **mục tiêu**, bấm **Gợi ý** để nhận danh sách gợi ý **Idea/Title**; người dùng **xem, chỉnh sửa** rồi
**lưu** kết quả đã chọn thành **Idea** (và/hoặc **Content**). Trong lúc chờ AI phải có **trạng thái loading**;
khi AI lỗi/chưa cấu hình phải có **trạng thái error** rõ ràng, không làm treo UI.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| `Controls/SidebarControl.cs:227-228` | Nhóm **"AI"** + nav item **"AI Assistant"** (`NavIcon.Sparkle`, `Badge = () => -1`) **chưa có `Action`** | Điểm vào đã có sẵn nhưng bấm không làm gì → cần nối (giống cách SCRUM-53 nối "Summary") |
| `docs/ux-ui/CreatorFlow_UX.md:197-211` | Mục **"AI Assistant"** liệt kê `[Generate Ideas] [Generate Title] ...`; AI xuất hiện theo Context ở Idea/Script/Published | Task "Idea/Title" khớp UX; các mục Outline/Script/Repurpose/Analysis **ngoài phạm vi** |
| `docs/ux-ui/CreatorFlow_UX.md:653-669` (AI UX) | Luồng chuẩn: `User Input → AI Generate → Preview → User Edit → User Confirm → Save`; "AI không tự thay đổi dữ liệu" | AC "user chỉnh trước khi lưu" là bắt buộc theo UX |
| `docs/planning-tong/CreatorFlow_KeHoachDuAn_ClientServer.md:155-162` | Nguyên tắc: `AIService` chạy **phía Backend**; WinForms **không gọi trực tiếp** AI/LLM; API key chỉ ở Backend; có xử lý timeout/quota/error | Ràng buộc kiến trúc cho phần gọi AI |
| `Services/AI/IAiService.cs:9-14` | Interface client hiện **chỉ có** `RequestScriptAssistAsync(AiScriptRequest)`; là "cổng duy nhất để gửi dữ liệu script sang AI" | Chưa có cổng cho gợi ý Idea/Title → phải mở rộng |
| `Services/AI/NotConfiguredAiService.cs:6-13` + `Program.cs:89` | Hiện dùng `NotConfiguredAiService` (luôn trả Failure "Dịch vụ AI chưa được cấu hình.") | Chưa có AI thật trong develop |
| `Models/Enums/AiRequestType.cs` | Enum đã có `Idea`, `Title`, `Outline`, `Script`, `Repurpose`, `PerformanceAnalysis`, `Other` | Loại yêu cầu Idea/Title đã được dự liệu trước |
| `Models/AiScriptRequest.cs:9-43` | Request hiện gắn với **Content + script** (ContentId, Script, Sections...) | Không tái dùng trực tiếp cho input tự do (chủ đề/platform/mục tiêu) → cần dạng request mới |
| `Services/ScriptService.cs:162-182` | Mẫu gọi AI: kiểm `IsConfigured`, `try/catch`, trả `Failure` thay vì ném ra UI | Mẫu xử lý lỗi AI để tái dùng |
| `Controls/ScriptEditorControl.cs:253-298` | UI AI hiện tại dùng `MessageBox` để hiện kết quả/lỗi + cờ `_aiBusy` | Task này yêu cầu **loading/error state** trong màn (không chỉ MessageBox) |
| `src/CreatorFlow.Api/**` (glob) | **Không có** AI endpoint/service nào ở Backend | Backend AI chưa tồn tại |
| Nhánh `SCRUM-56-xây-dựng-ai-service` (commit `c97d165`, base `main` cũ `0d5f435`, **chưa merge**) | Tạo namespace riêng `src/CreatorFlow/AI/` (`IAIService`, `AIService`, `AIOptions`, `AIFactory`, `DotEnv`, `AiDemoForm`, `AiTestForm`) — chạy AI **ở client** | **Mâu thuẫn kiến trúc** (rule nói AI ở Backend) và mâu thuẫn code hiện tại (`Services/AI/IAiService`) → cần chốt (xem mục 6.1) |
| `Services/Backends/IIdeaBackend.cs:9-23` | Có `Create(projectId, IdeaDraft, userId)` và `ConvertToContent(ideaId, userId)` | Đã có sẵn 2 đường "lưu" cho `Idea` và `Content` |
| `Services/Backends/IIdeaBackend.cs` + `Forms/Board/BoardForm.cs:687-708` | `IIdeaBackend backend = _projects is null ? _ideaService : _ideaBackend`; mở Idea Bank bằng `ShowProjectPage(page, "Backlog")` + `SetActiveMenu` | Mẫu nối trang + chọn backend local/server để tái dùng |
| `Forms/IdeaBank/IdeaEditDialog.cs:25` | Dialog thêm/sửa Idea nhận `(List<string> projectTags, Idea? existing)` → trả `IdeaDraft` | Có sẵn form nhập Idea; có thể tái dùng/khởi tạo draft sẵn từ gợi ý AI |
| `Repositories/Interfaces/IPlatformRepository.cs:9` | Local: `GetActive()` (bảng `platforms`, `is_active`) | Nguồn platform cho local mode |
| `Services/Backends/IContentDetailBackend.cs:22` | Server: `GetAvailablePlatforms()` | Nguồn platform cho server mode (khác local) |
| `database/01_schema.sql:25-26,270-299` | Bảng **`ai_requests`** + enum `ai_request_type`/`ai_request_status` + index đã có trong schema | Có nơi để log yêu cầu AI, nhưng **chưa có repository/API** dùng nó |

## 3. Phạm vi (đề xuất)

1. **Điểm vào/ra:** nối nav **"AI Assistant"** ở sidebar (thêm sự kiện, ví dụ `AiAssistantRequested`) → mở
   một **trang/UserControl "Trợ lý AI"** trong vùng nội dung chính của `BoardForm`, có nút đóng/quay lại như
   các module khác (Board/Calendar/Summary).
2. **Input:** `Chủ đề` (bắt buộc), `Platform` (chọn một hoặc nhiều), `Mục tiêu` (bắt buộc/không bắt buộc — cần chốt).
3. **Gọi AI + trạng thái:** bấm **Gợi ý** → hiện **loading** trong lúc chờ → thành công hiện danh sách gợi ý;
   lỗi/chưa cấu hình/timeout hiện **error state** có nút **Thử lại**, không ném exception ra UI.
4. **Preview + chỉnh sửa:** mỗi gợi ý hiển thị dạng **Title (+ mô tả/ý tưởng)**; người dùng chọn 1 gợi ý và
   **chỉnh sửa** tự do trước khi lưu (theo UX `Preview → User Edit`).
5. **Lưu:** từ gợi ý đã chọn/chỉnh, lưu được:
   - **Thành Idea:** gọi `IIdeaBackend.Create(projectId, IdeaDraft, userId)` → xuất hiện ở Backlog.
   - **Thành Content:** theo đường đã có — lưu Idea rồi `ConvertToContent`, hoặc tạo Content trực tiếp (cần chốt — mục 6.5).
6. **Kiến trúc:** UI chỉ gọi Service/Backend; **không** đặt API key trong WinForms; nếu cần gọi LLM thật thì
   đi qua Backend (`CreatorFlow.Api`) đúng rule.
7. **UI theo style guide** `docs/ux-ui/creatorflow-ui-styleguide.html`; tái dùng control/theme chung.

## 4. Ngoài phạm vi

- Các nút AI khác trong UX: Generate Outline, Generate Script, Improve Script, Repurpose, Analyze Performance.
- Sửa `ContentDetailPanel` / `ScriptEditorControl` hiện có (trừ khi cần tái dùng helper).
- Train model riêng; đổi workflow trạng thái; notification.
- Thêm migration mới (bảng `ai_requests` đã có; nếu **không** log request thì không đụng schema).
- Tích hợp/merge nhánh SCRUM-56 (chỉ **dùng lại** nếu người dùng chốt phương án — mục 6.1).

## 5. Luồng nghiệp vụ đề xuất

```text
Mở "AI Assistant" (sidebar)
   ↓
Nhập Chủ đề + chọn Platform + Mục tiêu
   ↓  [Gợi ý]
Loading… ──(lỗi)──► Error state + [Thử lại]
   ↓ (thành công)
Danh sách gợi ý Idea/Title (chọn 1)
   ↓
User chỉnh sửa (Title/Description/Tags…)
   ↓  [Lưu thành Idea]   hoặc   [Lưu thành Content]
IIdeaBackend.Create(...)   /   ConvertToContent(...)
   ↓
Thành công: toast/thông báo + (tuỳ chọn) điều hướng sang Backlog/Board
```

## 6. Giả định và quyết định cần bạn chốt

1. **Nguồn AI (quan trọng nhất).** Rule dự án: AI chạy ở **Backend**; nhưng Backend chưa có AI, còn nhánh
   SCRUM-56 làm AI **ở client** và chưa merge. Chọn hướng nào?
   - **(A) Tận dụng lõi AIService sẵn có (SCRUM-56) — ĐÃ CHỐT:** dùng lại `CreatorFlow.AI.IAIService.AskAsync`
     (LLM client gọi endpoint kiểu OpenAI, có timeout/map lỗi), lấy **chỉ file lõi** từ nhánh `SCRUM-56`
     (commit `c97d165`), rồi SCRUM-57 tự thêm prompt builder + parser + UI + lưu. Nhanh, tận dụng code đã có;
     **chấp nhận API key ở client là tạm** (trái rule "AI ở Backend"), sẽ đưa ra Backend sau.
   - **(B) Mở rộng seam client `Services/AI/IAiService` + generator offline:** không cần key/mạng nhưng không phải LLM thật.
   - **(C) Backend AI:** đúng rule nhất nhưng phạm vi lớn.
2. **Điểm vào & tên màn:** nav sidebar giữ "AI Assistant"; control đề xuất `AiAssistantControl`, tiêu đề
   hiển thị "Trợ lý AI" — đồng ý không?
3. **Trường input:** `Chủ đề` (bắt buộc), `Platform` (multi-select từ platform đang bật), `Mục tiêu` (bắt buộc
   hay không?). Có cần thêm `Content type` không?
4. **Dạng gợi ý:** một **danh sách N gợi ý** (mỗi gợi ý = Title + mô tả ngắn), người dùng chọn 1 để chỉnh?
   Hay gợi ý tách riêng Idea và Title? N = bao nhiêu?
5. **"Lưu thành Content":** (a) lưu Idea rồi dùng `ConvertToContent` (tái dùng, Idea gốc được giữ), hay
   (b) tạo Content trực tiếp không qua Idea? Đề xuất (a).
6. **Platform nguồn:** local `IPlatformRepository.GetActive()` vs server `IContentDetailBackend.GetAvailablePlatforms()`
   — dùng đúng theo mode như các màn khác?
7. **Có log AI request** vào bảng `ai_requests` (cần repository/endpoint mới) hay **bỏ qua** trong task này?
   Đề xuất **bỏ qua** để không tăng phạm vi.
8. **Môi trường test:** giống SCRUM-49/53 — có chạy **local mode (InMemory)** để test offline không, hay test
   server/DB thật?

## 7. Tiêu chí hoàn thành (Acceptance) và evidence

- [ ] **AC1 — Loading/Error state:** trong lúc chờ AI có chỉ báo loading; khi AI lỗi/chưa cấu hình/timeout hiện
      error state rõ ràng kèm nút **Thử lại**; UI không treo, không ném exception ra ngoài.
- [ ] **AC2 — User được chỉnh trước khi lưu:** gợi ý hiển thị ở vùng preview; người dùng sửa được Title/mô tả/
      tag trước khi lưu; AI **không** tự lưu.
- [ ] **AC3 — Lưu được thành Idea/Content:** từ gợi ý đã chọn/chỉnh, lưu thành **Idea** (Backlog) và/hoặc
      **Content** theo đường đã chốt; kiểm tra quyền tại Service/Backend (không chỉ ẩn nút).
- [ ] **Kiến trúc:** UI không gọi AI/LLM trực tiếp, không chứa API key; đi qua `IAiService`/Backend.
- [ ] **UI** đúng style guide; không tự đổi điều hướng/tên nav khác.
- [ ] **Test:** unit test cho phần dựng request/parse gợi ý và phần "chuyển gợi ý → Draft → lưu" (tách thuần
      để test không cần UI/AI thật).
- [ ] **Build** `dotnet build CreatorFlow.slnx` 0 error; không thêm migration.

## 8. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước build/test/runtime phải trình
checklist theo `post-change-test-workflow` và chờ duyệt. Test chỉ quan sát, không sửa code. Nếu AI trả kết quả
sai/lỗi bất thường → quay lại `bug-investigation-workflow`.

File này chỉ cần kiểm tra tĩnh (chưa code), không cần runtime test.

## 9. Câu hỏi mở cho người dùng

1. Chốt **phương án AI** (6.1) — (A) Backend / (B) mở rộng seam client + stub offline / (C) dùng lại SCRUM-56?
2. Xác nhận **điểm vào + tên màn** (6.2)?
3. Danh sách **trường input** và có cần `Content type` (6.3)?
4. **Dạng gợi ý** và số lượng (6.4)?
5. **"Lưu thành Content"** theo (a) hay (b) (6.5)?
6. Có **log vào `ai_requests`** không (6.7)?
7. Test bằng **local mode** hay server/DB thật (6.8)?
