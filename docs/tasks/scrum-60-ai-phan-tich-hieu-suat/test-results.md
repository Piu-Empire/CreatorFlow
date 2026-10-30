# SCRUM-60 — Kết quả kiểm chứng

Ngày chạy: 30/10/2026. Môi trường: máy local, Windows, .NET 10.0.
Branch: `SCRUM-60-ai-phan-tich-hiệu-suất` (gộp từ `SCRUM-23` + `feature/SCRUM-60-ai-phan-tich-hieu-suat`).

## Cổng 1 — kiểm tra tĩnh (ĐẠT)

| Kiểm tra | Lệnh | Kết quả |
| :--- | :--- | :--- |
| Build solution | `dotnet build CreatorFlow.slnx` | Build succeeded — 0 warning, 0 error |
| Test tính năng AI | `dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --filter "FullyQualifiedName~PerformanceAi"` | 19 passed, 0 failed |

Phạm vi test mới (offline, không cần DB/AI):

- `PerformanceAiPromptBuilderTests`: prompt có project/nhãn phạm vi/số liệu; có câu cấm truy cập TikTok/Facebook/YouTube và cấm bịa số; dữ liệu rỗng báo thiếu dữ liệu; cắt 20 dòng + ghi chú; bỏ ký tự xuống dòng trong tên.
- `PerformanceAiResponseParserTests`: tách đúng 2 mốc; thiếu mốc Gợi ý → unstructured; nhận `*` và `1.`; bỏ bullet rỗng.
- `PerformanceAiServiceTests`: chưa cấu hình; thành công; client trả lỗi; client ném exception (không lộ chi tiết); nội dung rỗng; hủy theo người dùng được ném lại.

Kiểm tra tĩnh theo `plan.md` §5.3:

- `ProjectReportsControl` chỉ gọi qua `IMetricBackend`/`IStatisticsBackend`/`IPerformanceAiService`; không có `HttpClient`/`AIService` trực tiếp, không có SQL.
- Không có lời gọi ghi DB; không có nút Lưu.

## Cổng 2 — kiểm tra runtime (ĐANG KIỂM THỬ)

Đã xác minh trước trên Docker local:

| # | Kiểm tra | Cách làm | Kết quả |
| :--- | :--- | :--- | :--- |
| A | Cấu hình AI đủ 3 biến | `.env` có `AI_API_KEY`, `AI_ENDPOINT`, `AI_MODEL` | Đủ (key dạng Groq, endpoint Groq, model `openai/gpt-oss-120b`) |
| B | Provider AI trả đúng 2 mốc | Gọi chat completions với prompt mẫu tiếng Việt | Trả `== NHẬN XÉT ==` + `== GỢI Ý CẢI THIỆN ==`, 3 bullet |
| C | API dữ liệu cho tài khoản demo | `GET /api/projects/1/metrics/summary?groupBy=creator` và `/statistics/creators` | 200, 3 creator + dòng Toàn Project |
| D | Đăng nhập tài khoản demo | `POST /api/auth/login` | 200, có access token |

Các bước còn lại do người dùng bấm trên app (checklist theo `plan.md` §6):

1. Reports → "Hiệu suất" (Theo Creator) → **AI phân tích**: panel mở, có giờ tải, 2 mục, số liệu khớp bảng.
2. Reports → "Creator & Project" → lọc 1 Creator → **AI phân tích**: nhãn "Creator: …", vẫn so với mốc Toàn Project.
3. Đổi Creator mà không bấm "Phân tích lại": kết quả + nhãn phạm vi cũ giữ nguyên.
4. **Sao chép** → dán Notepad khớp panel.
5. Bỏ AI key (hoặc để trống) rồi mở lại app: nút disable + thông báo chưa cấu hình, không crash.
6. Dừng container API rồi mở Reports: báo lỗi tải số liệu, bấm AI **không** gọi AI.
7. Sau khi thử: `SELECT count(*) FROM ai_requests` không tăng (tính năng không ghi DB).

Chưa kiểm chứng trong task này: DPI/độ phân giải khác, provider AI khác, API dev dùng chung.
