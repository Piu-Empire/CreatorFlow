# SCRUM-58 — Yêu cầu: AI hỗ trợ Outline và Script từ thông tin Content

Trạng thái: **CHỜ DUYỆT** (chưa triển khai). Branch: `SCRUM-58-ai-hỗ-trợ-outline-va-script`
(tạo từ nhánh `SCRUM-57-ai-gợi-y-idea-va-title`; **lưu ý:** công việc SCRUM-57 hiện **chưa commit** và
đang được mang sang nhánh này — xem mục 6.0).
Người viết: agent. Ngày soạn: 19/10/2026.
Nguồn yêu cầu: người dùng cung cấp trực tiếp:

> Sinh/gợi ý outline và script từ thông tin Content.
> AC: (1) Không tự ghi đè script; (2) User xác nhận trước khi lưu.

## 1. Mục tiêu

Cho người dùng **sinh/gợi ý Outline** và **Script** từ **thông tin Content** (tiêu đề, mô tả, loại,
priority, platform, script đã có…). Kết quả AI chỉ hiển thị ở **vùng xem trước**; người dùng **xem, chỉnh,
rồi xác nhận** mới được đưa vào kịch bản và lưu. AI **tuyệt đối không tự ghi đè** kịch bản hiện có.

## 2. Hiện trạng đọc từ repository (quan trọng — phần lớn khung đã có sẵn từ SCRUM-33)

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| `Controls/ScriptEditorControl.cs:89-97,253-314` | **Đã có** nút **"Gửi sang AI"** → `SendToAiAsync()` → `ScriptService.SendToAiAsync`; kết quả/lỗi hiện bằng **`MessageBox`** (cắt 3000 ký tự), **không** áp vào editor | Điểm vào đã có; nhưng UX chưa có preview/chỉnh/xác nhận → cần thay |
| `Services/ScriptService.cs:136-184` | `PrepareAiRequest(...)` + `SendToAiAsync(...)`: kiểm tra thành viên, loại request, độ dài; lỗi AI trả `Failure` thay vì ném | Nghiệp vụ gọi AI đã có; tái dùng |
| `Services/ScriptService.cs:24-27,139-140` | Chỉ cho các `AiRequestType`: `Script`, `Title`, `Outline`, `Repurpose`, `Other`; **không** cho `PerformanceAnalysis` | Loại request Outline/Script đã hỗ trợ ở Service |
| `Services/ScriptService.cs:148-149` | Ném `ContentValidationException` khi **script đang trống** ("chưa thể gửi sang AI") | **Xung đột tiềm ẩn:** "sinh outline từ thông tin Content" có thể cần script trống → xem 6.6 |
| `Services/ScriptService.cs:78-126` | `Save(...)` dùng **optimistic concurrency** theo `Version`, ném `ScriptConflictException`; nếu nội dung không đổi thì **không ghi DB** | Cơ chế "user xác nhận rồi mới lưu" đã an toàn sẵn |
| `Services/ScriptService.cs:201-209` + `ScriptAccessPolicy` | Quyền sửa script kiểm tra ở Service (Creator/Manager/Owner, khóa khi Published/Archived) | Không chỉ ẩn nút trên UI |
| `Services/AI/ScriptAiRequestBuilder.cs:12-43` | Dựng `AiScriptRequest` đầy đủ: `Title, Description, ContentType, Priority, Platforms, Script (cắt 30k), Sections, Statistics, Instruction` | Đã có "thông tin Content" để gửi AI |
| `Models/AiScriptRequest.cs:9-43` | Request **không** chứa prompt dựng sẵn; "AIService tự ghép thành prompt" | Prompt builder sẽ nằm ở cài đặt `IAiService` → cần thêm |
| `Models/AiScriptResponse.cs` | `record(bool Succeeded, string? Text, string? ErrorMessage)` + `Success/Failure` | Kiểu kết quả đã có |
| `Services/AI/IAiService.cs:9-14` | Cổng client: `IsConfigured` + `RequestScriptAssistAsync(AiScriptRequest)` | Cổng để cắm AI thật |
| `Services/AI/NotConfiguredAiService.cs` + `Program.cs` (`new NotConfiguredAiService()`) | Hiện cắm **stub luôn Failure** → nút AI chưa chạy được | **Việc chính:** cài đặt thật + cắm vào `Program.cs` |
| `AI/IAIService.cs` + `AI/AIService.cs` (SCRUM-57 thêm vào nhánh này) | Lõi client `CreatorFlow.AI.IAIService.AskAsync(prompt)` gọi LLM kiểu OpenAI (timeout + map lỗi HTTP) | Nguồn AI thật để tái dùng (như SCRUM-57) |
| `Services/Backends/IContentDetailBackend.cs:27-30` | Chỉ có `OpenScript`/`SaveScript`; **không** có method AI | AI luôn đi qua `ScriptService` client, ở **cả** local và server mode |
| `Models/Enums/AiRequestType.cs` | Enum `Outline`, `Script`, `Title`, `Repurpose`, `Idea`, `PerformanceAnalysis`, `Other` | Chọn loại request trên UI dễ |
| `docs/ux-ui/CreatorFlow_UX.md` (AI UX, ~653-669, ~809-813) | Nguyên tắc: `Generate → Preview → User Edit → User Confirm → Save`; "AI không tự thay đổi dữ liệu" | Khớp trực tiếp 2 AC của task |
| `database/01_schema.sql` bảng `ai_requests` | Có bảng + enum, **chưa** repository/API dùng | Có thể log, nhưng ngoài phạm vi nếu không chốt |

## 3. Phạm vi (đề xuất)

1. **Cài đặt AI thật cho cổng script:** thêm một cài đặt `Services/AI/IAiService` (ví dụ
   `ScriptAiService`) dùng lõi `CreatorFlow.AI.IAIService.AskAsync` (đã có từ SCRUM-57) để:
   - Dựng **prompt** từ `AiScriptRequest` theo `RequestType` (Outline/Script/…) + `Instruction`.
   - Gọi LLM, trả `AiScriptResponse` (Success = text; lỗi/timeout → Failure, không ném ra UI).
   - Cắm vào `Program.cs` **thay** `NotConfiguredAiService`.
2. **UX trong `ScriptEditorControl`:** thay `MessageBox` bằng **preview**:
   - Chọn loại yêu cầu (**Outline** / **Script**) + ô `Instruction` (yêu cầu thêm, đã có trong request).
   - Bấm gọi AI → **loading** (đã có `_aiBusy`) → thành công hiện kết quả ở **vùng preview** (đọc được, chỉnh được); lỗi/chưa cấu hình → **error state** + **Thử lại** (không dùng MessageBox chặn).
   - **AC1 — Không tự ghi đè:** kết quả AI **không** bao giờ tự chèn vào `_txtScript`. Chỉ khi người dùng bấm
     **"Chèn vào kịch bản"** (thêm vào cuối / tại vị trí con trỏ) hoặc **"Thay thế toàn bộ"** (kèm hộp xác nhận) mới áp vào editor — và áp xong vẫn là "chưa lưu".
   - **AC2 — Xác nhận trước khi lưu:** AI không tự lưu; sau khi áp, người dùng vẫn bấm **"Lưu kịch bản"** (có thể kèm hộp xác nhận "Lưu nội dung do AI gợi ý?"). Dùng đúng cơ chế `ScriptService.Save` + `ScriptConflictException` sẵn có.
3. **Nguồn dữ liệu gửi AI:** tái dùng `ScriptService.PrepareAiRequest`/`ScriptAiRequestBuilder` (thông tin Content + script đã lưu + platform + sections + statistics).
4. **UI theo style guide** `docs/ux-ui/creatorflow-ui-styleguide.html`; tái dùng control/theme chung (RoundedButton/Panel, màu cảnh báo cho error).
5. **Tách phần thuần để test:** prompt builder (Outline/Script) và logic "áp kết quả vào text" (insert/replace) là hàm thuần → unit test offline.

## 4. Ngoài phạm vi

- Repurpose, Performance Analysis, Generate Title, Improve Script (chỉ Outline + Script theo yêu cầu).
- Đưa AI ra Backend (`CreatorFlow.Api`) — vẫn là **POC client** như SCRUM-57; ghi nhận nợ kiến trúc.
- Log vào bảng `ai_requests` (trừ khi bạn chốt ngược lại).
- Thêm migration; đổi workflow trạng thái; đổi `ScriptAccessPolicy`.
- Stream/token-by-token; lịch sử nhiều phiên bản gợi ý.

## 5. Luồng nghiệp vụ đề xuất

```text
Mở Content Detail → tab Script (ScriptEditorControl)
   ↓  (script có thể trống hoặc đã có — tùy chốt 6.6)
Chọn loại: [Outline] / [Script]  + nhập yêu cầu thêm (tùy chọn)
   ↓  [Gửi sang AI]
Loading… ──(lỗi/chưa cấu hình/timeout)──► Error state + [Thử lại]
   ↓ (thành công)
Vùng PREVIEW: hiện text AI (đọc + sửa được)   ← KHÔNG đụng _txtScript
   ↓
[Chèn vào kịch bản] (cuối/con trỏ)  hoặc  [Thay thế toàn bộ] (có hộp xác nhận)
   ↓
_txtScript thay đổi (dirty = true)            ← vẫn CHƯA lưu
   ↓
[Lưu kịch bản] (xác nhận) → ScriptService.Save(...) → ScriptConflictException nếu bị sửa song song
```

## 6. Giả định và quyết định cần bạn chốt

0. **Nhánh/commit:** công việc SCRUM-57 (màn Trợ lý AI, lõi `CreatorFlow.AI`, .env…) hiện **chưa commit**
   và đang nằm trên nhánh SCRUM-58 này. Bạn muốn (a) **commit SCRUM-57 riêng trước** rồi tách SCRUM-58 sạch,
   hay (b) **gộp chung** một mạch? (Đề xuất (a) để lịch sử rõ.)
1. **Nguồn AI:** tái dùng lõi **client** `CreatorFlow.AI.IAIService` (như SCRUM-57, key ở `.env`, POC) — đồng ý?
   Hay bắt buộc đẩy ra Backend (phạm vi lớn)?
2. **Điểm vào:** mở rộng **`ScriptEditorControl`** hiện có (nút "Gửi sang AI" + vùng preview) thay vì màn mới — đồng ý?
3. **Loại request trên UI:** chỉ **Outline + Script**? Có thêm `Repurpose`/`Title` không?
4. **Vùng preview:** (a) panel ngay trong tab Script, hay (b) hộp thoại riêng? Đề xuất (a).
5. **Cách áp kết quả:** cần những nút nào — "Chèn vào cuối", "Chèn tại con trỏ", "Thay thế toàn bộ"?
   Nút "Thay thế toàn bộ" có cần hộp xác nhận (đề xuất **có**)?
6. **Cho phép script trống?** Hiện `PrepareAiRequest` chặn khi script trống. SCRUM-58 "sinh từ **thông tin
   Content**" có cần cho phép **script trống** (chỉ dùng Title/Description/Platform để sinh Outline) không?
   Nếu có → phải nới guard ở Service (đề xuất: cho phép khi `RequestType = Outline`, vẫn chặn khi `Script`).
7. **"Xác nhận trước khi lưu"** hiểu là: (a) xác nhận khi **áp** kết quả vào script, (b) xác nhận khi **lưu**,
   hay (c) **cả hai**? Đề xuất (c): áp phải do người dùng bấm nút; lưu có hộp xác nhận.
8. **Có log `ai_requests`** không? Đề xuất **không** (ngoài phạm vi).
9. **Môi trường test:** **local mode (InMemory)** như SCRUM-53/57 — vì AI chạy ở client và ScriptService đọc
   repo client. Đồng ý?

## 7. Tiêu chí hoàn thành (Acceptance) và evidence

- [ ] **AC1 — Không tự ghi đè script:** kết quả AI không bao giờ tự động ghi vào `_txtScript`; chỉ vào sau khi
      người dùng bấm nút áp + xác nhận (nếu là "thay thế"). Kịch bản hiện có bị "thay thế" phải qua hộp xác nhận.
- [ ] **AC2 — User xác nhận trước khi lưu:** AI không tự lưu; sau khi áp phải bấm "Lưu kịch bản" (có xác nhận);
      dùng đúng optimistic concurrency, xử lý `ScriptConflictException`.
- [ ] **Chức năng:** sinh được **Outline** và **Script** từ thông tin Content; có chọn loại + `Instruction`;
      loading/error/timeout rõ ràng, không treo UI, không ném exception ra ngoài.
- [ ] **Kiến trúc:** UI chỉ gọi Service/cổng `IAiService`; không đặt/gọi LLM trực tiếp trong Form; không lộ key.
- [ ] **Quyền:** giữ nguyên `ScriptAccessPolicy`; chỉ hiện nút AI khi có quyền phù hợp như hiện tại.
- [ ] **UI** đúng style guide; không đổi điều hướng/nav khác.
- [ ] **Test:** unit test cho prompt builder (Outline/Script) và logic áp kết quả (insert/replace, không ghi đè
      ngoài ý muốn); cài đặt AI có test với fake `IAIService`.
- [ ] **Build** `dotnet build CreatorFlow.slnx` 0 error; không thêm migration.

## 8. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước build/test/runtime phải trình
checklist theo `post-change-test-workflow` và chờ duyệt; test chỉ quan sát, không sửa code. Kết quả AI sai/lỗi
bất thường → quay lại `bug-investigation-workflow`.

File này chỉ cần kiểm tra tĩnh (chưa code), không cần runtime test.

## 9. Câu hỏi mở cho người dùng

1. Chốt **nhánh/commit** (6.0): commit SCRUM-57 riêng trước hay gộp?
2. Chốt **nguồn AI** (6.1): tái dùng lõi client như SCRUM-57?
3. Chốt **điểm vào** (6.2): mở rộng `ScriptEditorControl`?
4. Chốt **loại request + preview + cách áp** (6.3–6.5).
5. Chốt **script trống** (6.6) và nghĩa **"xác nhận trước khi lưu"** (6.7).
6. Có **log `ai_requests`** (6.8) và **môi trường test** (6.9) không?
