# SCRUM-57 — Kế hoạch triển khai: Trợ lý AI gợi ý Idea/Title

Trạng thái: **CHỜ DUYỆT** (chưa code). Branch: `SCRUM-57-ai-gợi-y-idea-va-title`.
Người viết: agent. Ngày soạn: 19/10/2026. Cập nhật: **chốt phương án (A) — dùng lại lõi SCRUM-56**.

## 1. Quyết định đã chốt

1. **Nguồn AI = phương án (A):** dùng lại lõi đã có ở nhánh `SCRUM-56-xây-dựng-ai-service`
   (commit `c97d165`): `CreatorFlow.AI.IAIService.AskAsync(prompt)` + `AIService` (gọi endpoint kiểu OpenAI,
   timeout + map lỗi HTTP) + `AIOptions/AIFactory/DotEnv` (đọc `.env`).
2. **Chỉ lấy file lõi**, KHÔNG lấy 2 form demo (`AiDemoForm`, `AiTestForm`) và project `tools/CreatorFlow.AiDemo`
   (đều là test local, ghi chú rõ "không tham chiếu từ Program.cs").
3. **Chấp nhận API key ở client là TẠM THỜI** (trái rule "AI ở Backend") — đây là POC; sẽ chuyển sang Backend sau.
   Ghi chú rõ trong code và plan.
4. **Input:** `Chủ đề` (bắt buộc), `Platform` (chọn nhiều), `Mục tiêu` (bắt buộc). Không thêm trường khác.
5. **Kết quả:** danh sách **N=5** gợi ý (`Title` + `Description` ngắn) — do AI trả về, có **parser** chịu lỗi.
6. **Lưu thành Content:** lưu Idea rồi gọi `IIdeaBackend.ConvertToContent` (tái dùng, giữ Idea gốc).
7. **Không** log `ai_requests`; **không** thêm migration.
8. **Test chính:** unit test cho prompt builder/parser (offline); runtime local mode (cần `.env` để có gợi ý thật;
   thiếu key thì rơi vào **error state** — vẫn kiểm chứng được AC loading/error).

## 2. Kiến trúc và luồng

```text
AiAssistantControl (UI trong BoardForm)
   → IdeaAssistService.AskAsync(topic, platforms, goal)      // nghiệp vụ màn Trợ lý AI
        → IdeaAssistPromptBuilder.Build(...)                  // (thuần) dựng prompt
        → CreatorFlow.AI.IAIService.AskAsync(prompt)          // engine SCRUM-56 (gọi LLM thật)
        → IdeaAssistResponseParser.Parse(text)                // (thuần) text → List<IdeaSuggestion>
   → (user chọn + sửa) → IdeaSuggestionDraftMapper.ToDraft()  // (thuần)
   → IIdeaBackend.Create(...)  và/hoặc  ConvertToContent(...) // lưu Idea/Content (quyền kiểm tra lại ở đây)
```

- UI **không** gọi LLM trực tiếp, **không** chứa key; chỉ gọi `IdeaAssistService`.
- Key nằm ở `.env` (không commit), nạp qua `DotEnv`/`AIFactory` — **tạm thời** ở client.

## 3. Thay đổi dự kiến (thêm/sửa)

### 3.1. Đưa lõi SCRUM-56 vào (không merge cả nhánh)
`git checkout c97d165 -- src/CreatorFlow/AI/IAIService.cs src/CreatorFlow/AI/AIService.cs src/CreatorFlow/AI/AIOptions.cs src/CreatorFlow/AI/AIFactory.cs src/CreatorFlow/AI/DotEnv.cs src/CreatorFlow/AI/Models/AiResult.cs .env.example`
- KHÔNG lấy: `AiDemoForm.*`, `AiTestForm.*`, `tools/CreatorFlow.AiDemo/*`.
- `.gitignore` (sửa): thêm rule bỏ qua file bí mật `.env`, `.env.*` (giữ `!.env.example`), `secrets*.json`,
  `appsettings.Development.json`, `local.settings.json` — lấy theo SCRUM-56 và bổ sung `.env`.
- Không cần package mới (`System.Net.Http.Json` có sẵn trong framework).

### 3.2. Nghiệp vụ màn Trợ lý AI (mới, tách thuần để test)
- `src/CreatorFlow/Models/IdeaSuggestion.cs` — `Title`, `Description`.
- `src/CreatorFlow/Models/IdeaSuggestRequest.cs` — `Topic`, `Platforms` (IReadOnlyList<string>), `Goal`.
- `src/CreatorFlow/Services/AI/IdeaAssistPromptBuilder.cs` — ghép prompt yêu cầu trả về đúng N gợi ý theo
  định dạng dễ parse (ví dụ mỗi gợi ý 2 dòng `Tiêu đề` / `Mô tả`, hoặc JSON). Thuần, test được.
- `src/CreatorFlow/Services/AI/IdeaAssistResponseParser.cs` — parse text AI → `List<IdeaSuggestion>`
  (chịu được JSON, danh sách đánh số, hoặc text thô); cắt còn N; loại dòng rỗng.
- `src/CreatorFlow/Services/AI/IdeaAssistService.cs` — nhận `CreatorFlow.AI.IAIService`, dựng prompt, gọi
  `AskAsync`, parse; trả kết quả `{ Succeeded, Suggestions, ErrorMessage }` (không ném ra UI).
- `src/CreatorFlow/Services/AI/IdeaSuggestionDraftMapper.cs` — `IdeaSuggestion` + input → `IdeaDraft`
  (Title, Description, Note = chủ đề/mục tiêu/platform, Status = `Backlog`, Tags = platform). Thuần, test được.

### 3.3. UI (mới)
- `src/CreatorFlow/Controls/AiAssistant/AiAssistantControl.cs` — dựng layout bằng code (theo DashboardControl).
  - **Form nhập:** Chủ đề (TextBox), Platform (CheckedListBox nhiều chọn), Mục tiêu (TextBox), nút **Gợi ý**.
  - **Loading state:** label "Đang gợi ý…" + khoá nút trong lúc `await` (dùng `CancellationToken`).
  - **Error state:** label lỗi (từ `IdeaAssistService`/`AiResult.ErrorMessage`) + nút **Thử lại** (không MessageBox).
  - **Kết quả:** danh sách N gợi ý; chọn 1 → nạp vào **vùng chỉnh sửa** (Title/Description/Tags sửa tự do).
  - **Nút lưu:** `Lưu thành Idea`, `Lưu thành Content` (tắt theo `IIdeaBackend.CanCreate`), `Đóng` → `CloseRequested`.
  - Theo style guide `docs/ux-ui/creatorflow-ui-styleguide.html`; dùng control/theme chung.

### 3.4. Nối điều hướng (sửa)
- `src/CreatorFlow/Controls/SidebarControl.cs:228` — gắn `Action` cho item "AI Assistant" phát `AiAssistantRequested`
  (thêm event như `SummaryRequested`).
- `src/CreatorFlow/Forms/Board/BoardForm.cs`
  - nhận thêm `CreatorFlow.AI.IAIService? aiService = null` (tham số optional cuối constructor).
  - đăng ký `_sidebarControl.AiAssistantRequested += (_, _) => OpenAiAssistant();` cho **cả** nhánh auth (≈139)
    **và** khối `TẠM-LOCAL` (≈147).
  - `OpenAiAssistant()`: `if (!HasProjectContext || !CanLeaveProjectPage()) return;` → chọn backend như IdeaBank
    (`_projects is null ? _ideaService : _ideaBackend`); thiếu backend → `ShowUnavailableModule("AI Assistant")`;
    tạo `AiAssistantControl(...)` với `IdeaAssistService(aiService)` + platform provider + backend; gắn
    `CloseRequested`; `ShowProjectPage(page, "AI Assistant")`; `_sidebarControl.SetActiveMenu("AI Assistant")`.
- `src/CreatorFlow/Program.cs` — tạo `CreatorFlow.AI.AIFactory.Create()` (đọc `.env`) và truyền vào `BoardForm`
  ở **cả** đường chạy thường lẫn khối `TẠM-LOCAL`. Không đổi `ScriptService` (vẫn `NotConfiguredAiService`).

### 3.5. Test (mới)
- `IdeaAssistPromptBuilderTests` — prompt chứa chủ đề/mục tiêu/platform và yêu cầu N gợi ý.
- `IdeaAssistResponseParserTests` — nhiều định dạng đầu vào (JSON / đánh số / text thô), rỗng, rác; cắt còn N.
- `IdeaAssistServiceTests` — dùng `FakeAIService` (`CreatorFlow.AI.IAIService`): thành công → N gợi ý; AI lỗi → `Succeeded=false` + message; prompt rỗng bị chặn.
- `IdeaSuggestionDraftMapperTests` — map đúng Title/Description/Note/Tags/Status.

## 4. Phạm vi ngoài (không làm)

- Outline/Script/Repurpose/Performance Analysis; đụng `ScriptEditorControl`/`ContentDetailPanel`.
- Backend AI endpoint, log `ai_requests`, migration.
- Form demo/test `AiDemoForm`/`AiTestForm`, project `tools/CreatorFlow.AiDemo`.
- Chuyển AI ra Backend (ghi nhận là việc sau; đây là POC client).

## 5. Thứ tự thực hiện

1. Đưa lõi SCRUM-56 vào + `.env.example` + `.gitignore` → **verify:** build.
2. Model + prompt builder + parser + service + mapper → **verify:** unit test pass.
3. `AiAssistantControl` (UI + loading/error/preview/edit/save) → **verify:** build.
4. Sidebar event + `BoardForm` wiring + `Program.cs` → **verify:** build + chạy local mode.
5. Runtime test local mode (mục 6) → **verify:** luồng đầy đủ.

## 6. Checklist test runtime (local mode, chờ duyệt trước khi chạy)

Chuẩn bị: tạo `.env` (không commit) với `AI_API_KEY`/`AI_ENDPOINT`/`AI_MODEL` — hoặc test nhánh thiếu key.

1. Mở app local → bấm **AI Assistant**: trang "Trợ lý AI" mở ra, không lỗi.
2. Nhập Chủ đề + chọn ≥1 Platform + Mục tiêu → **Gợi ý**: thấy **loading** rồi hiện **5 gợi ý**.
3. Chọn 1 gợi ý → sửa Title/Description/Tags → **Lưu thành Idea**: Idea xuất hiện ở **Backlog**.
4. Từ gợi ý khác → **Lưu thành Content**: Content mới ở cột **Idea** của Board; Idea gốc chuyển Converted.
5. Bỏ trống Chủ đề → báo lỗi nhập liệu, không gọi AI.
6. **Error state:** xoá `AI_API_KEY` (hoặc dùng sai endpoint) → hiện lỗi + **Thử lại**; UI không treo.
7. Khu vực rủi ro: đóng trang AI không để lại control mồ côi; nút Lưu tắt khi không có quyền tạo Idea.

## 7. Rủi ro & giảm thiểu

| Rủi ro | Giảm thiểu |
| :--- | :--- |
| Key ở client trái rule dự án | Ghi chú rõ là POC tạm; cổng `CreatorFlow.AI.IAIService` sẵn sàng thay bằng cài đặt gọi Backend |
| `.env` lộ key | Thêm `.env` vào `.gitignore`; chỉ commit `.env.example` |
| AI trả text khó parse | Parser chịu lỗi (JSON/đánh số/text thô) + cắt còn N + test nhiều định dạng |
| Hai interface AI trùng khái niệm (`CreatorFlow.AI.IAIService` vs `Services/AI/IAiService` script) | Không đụng cái script; ghi nhận là tech-debt (có thể gộp sau) |
| Timeout/mạng chậm | `AIService` đã có timeout + map lỗi; UI khoá nút + `CancellationToken` |
| Điều hướng để lại control mồ côi | Dùng `ShowProjectPage`/`CloseProjectPage` + `CloseRequested` như Idea Bank |

## 8. Cổng kiểm chứng

Theo `AGENTS.md`: plan này **chờ duyệt** trước khi code. Trước build/test/runtime, trình checklist theo
`post-change-test-workflow` và chờ duyệt; test chỉ quan sát, không sửa code.

## 9. Câu hỏi mở

1. Duyệt plan (A) này để bắt đầu code?
2. Bạn đã có `AI_API_KEY/AI_ENDPOINT/AI_MODEL` để test gợi ý thật chưa (nếu chưa, mình test nhánh **error state**
   trước, phần "hiện 5 gợi ý" để sau khi bạn cấu hình `.env`)?
