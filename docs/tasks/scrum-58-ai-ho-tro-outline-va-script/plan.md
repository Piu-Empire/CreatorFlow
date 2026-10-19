# SCRUM-58 — Kế hoạch: AI hỗ trợ Outline và Script từ thông tin Content

Trạng thái: **ĐÃ TRIỂN KHAI** (người dùng chốt "làm"). Branch: `SCRUM-58-ai-hỗ-trợ-outline-va-script`
(tạo từ nhánh `SCRUM-57-ai-gợi-y-idea-va-title` sau khi đã commit `0ba84c3`).
Người viết: agent. Ngày: 19/10/2026.

Quyết định đã chốt (theo đề xuất trong `requirements.md`):
- **6.0** Commit SCRUM-57 riêng (đã xong, `0ba84c3`), SCRUM-58 tách sạch.
- **6.1** Tái dùng lõi client `CreatorFlow.AI.IAIService` (POC, key ở `.env`).
- **6.2** Mở rộng `ScriptEditorControl` (không tạo màn mới).
- **6.3** Loại yêu cầu trên UI: **Outline + Script** (thêm ô "yêu cầu thêm" phục vụ cả "rút gọn/cải thiện").
- **6.4** Vùng preview: panel ngay trong tab Script.
- **6.5** Cách áp: "Chèn vào cuối" và "Thay thế toàn bộ" (Thay thế có hộp xác nhận).
- **6.6** Cho phép **script trống** để sinh từ thông tin Content.
- **6.7** Xác nhận **cả khi áp** (chỉ thay thế) **và khi lưu**.
- **6.8** Không log `ai_requests`.
- **6.9** Test ở **local mode (InMemory)**.

## 1. Phạm vi thay đổi

### Tạo mới
| File | Việc |
| :--- | :--- |
| `src/CreatorFlow/Services/AI/ScriptAiPromptBuilder.cs` | Hàm thuần dựng prompt từ `AiScriptRequest` theo `RequestType` (Outline/Script) — testable offline. |
| `src/CreatorFlow/Services/AI/ScriptAiService.cs` | Cài đặt `Services.AI.IAiService` thật, bọc `CreatorFlow.AI.IAIService.AskAsync`; map `AiResult` → `AiScriptResponse`. |
| `src/CreatorFlow/Services/AI/ScriptAiApply.cs` | Hàm thuần áp kết quả AI vào kịch bản: `Append(current, addition)` / `Normalize(addition)` — testable. |
| `tests/.../Shared/ScriptAiPromptBuilderTests.cs` | Test prompt Outline/Script + context. |
| `tests/.../Shared/ScriptAiServiceTests.cs` | Test cài đặt AI với `IAIService` giả (thành công/lỗi/chưa cấu hình/rỗng). |
| `tests/.../Shared/ScriptAiApplyTests.cs` | Test chèn/thay thế (không tự ghi đè ngoài ý muốn). |

### Sửa
| File | Việc |
| :--- | :--- |
| `src/CreatorFlow/Services/ScriptService.cs` | Bỏ chặn "kịch bản trống" để sinh từ thông tin Content (giữ nguyên các kiểm tra quyền/độ dài/loại). |
| `src/CreatorFlow/Controls/ScriptEditorControl.cs` | Thay MessageBox bằng panel preview: chọn Outline/Script + yêu cầu thêm + loading/error + chèn/thay thế (xác nhận). |
| `src/CreatorFlow/Program.cs` | Cắm `ScriptAiService` thay `NotConfiguredAiService` (tạo `aiService` trước `scriptService`). |
| `tests/.../Shared/ScriptServiceTests.cs` | Cập nhật test "script trống" theo luật mới (được phép sinh). |

## 2. Luồng UI mới trong tab Script

```text
[Nút "Trợ lý AI"] → mở panel AI
   ├─ Chọn loại: Dàn ý (Outline) / Kịch bản (Script)
   ├─ Ô "yêu cầu thêm" (tùy chọn)
   └─ [Gợi ý]  →  "Đang tạo…"  ──lỗi──► dòng lỗi (đỏ) + bấm lại
                        │
                   thành công
                        ▼
   Vùng preview (đọc + SỬA được)   ← KHÔNG đụng ô kịch bản (AC1)
        ├─ [Chèn vào cuối]   → nối vào cuối ô kịch bản (dirty=true, CHƯA lưu)
        ├─ [Thay thế toàn bộ] → hỏi xác nhận → thay ô kịch bản (dirty=true, CHƯA lưu)
        └─ [Đóng]
                        ▼
   [Lưu kịch bản] (như cũ) → ScriptService.Save (optimistic concurrency)
```

## 3. Ghi chú kiến trúc / nợ kỹ thuật
- Hai interface AI cùng tồn tại: `CreatorFlow.AI.IAIService` (lõi client chung) và
  `CreatorFlow.Services.AI.IAiService` (cổng script). `ScriptAiService` là **adapter** nối hai cái.
- Vẫn là **POC client** (key ở `.env`) như SCRUM-57; cần chuyển sang Backend sau.
- AI luôn chạy qua `ScriptService` phía client ở cả local và server mode (đúng thiết kế SCRUM-33).

## 4. Kiểm chứng
- Build `dotnet build CreatorFlow.slnx` 0 error.
- Unit test: `ScriptAiPromptBuilderTests`, `ScriptAiServiceTests`, `ScriptAiApplyTests`, `ScriptServiceTests` (đã cập nhật).
- Runtime (local mode): mở Content → tab Script → Trợ lý AI → sinh Outline/Script → chèn/thay thế → Lưu; thử input lỗi + thiếu key → error state.
