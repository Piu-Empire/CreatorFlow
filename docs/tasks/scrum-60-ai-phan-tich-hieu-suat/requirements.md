# SCRUM-60 — Yêu cầu: AI phân tích hiệu suất

Trạng thái: **YÊU CẦU ĐÃ DUYỆT** (29/10/2026); đang chờ duyệt `plan.md`. Chưa viết code.
Branch: `feature/SCRUM-60-ai-phan-tich-hieu-suat` (tạo từ `feature/SCRUM-55-thong-ke-creator-va-project` tại `cdeff78`, **chưa push**).
Người viết: agent. Ngày soạn: 29/10/2026.

Quyết định người dùng đã chốt:
- **6.1** Điểm vào: nút **"AI phân tích"** trong màn Reports (panel kết quả).
- **6.2** Tái dùng **lõi AI client POC** như SCRUM-57/58/59.
- **6.3** Phân tích theo **view đang mở** + dòng tổng Project.
- **6.4** **Không lưu** kết quả phân tích.
- **6.8** Kiểm chứng trên **Docker local** với dữ liệu có sẵn.
- Các mục còn lại (6.5–6.7, 6.9–6.10) giữ theo đề xuất.

Nguồn yêu cầu: người dùng cung cấp trực tiếp từ Jira (ảnh chụp SCRUM-60):

> **AI phân tích hiệu suất**
> Đưa Metrics đã lưu vào AI để AI nhận xét và gợi ý cải thiện.
>
> Acceptance Criteria:
> - Dùng Metrics từ hệ thống
> - Không giả vờ AI tự truy cập social platform
> - Hiển thị lỗi API rõ ràng

## 1. Mục tiêu

Từ **Metrics đã lưu trong hệ thống** (không phải dữ liệu AI tự đi lấy), gọi AI để sinh **nhận xét hiệu suất**
và **gợi ý cải thiện** cho một Project (và/hoặc một Creator trong Project), hiển thị ngay trong màn
**Reports** để người dùng đọc và hành động.

Đúng luồng AI của `docs/ux-ui/CreatorFlow_UX.md` §13:

```text
User Input → AI Generate → Preview → User Edit → User Confirm → Save
```

Với SCRUM-60, bước **Save là KHÔNG có**: kết quả phân tích chỉ để đọc/sao chép, không ghi DB, không sửa
Metrics, không sửa Content. Đây là điểm khác biệt có chủ ý so với Repurpose (SCRUM-59) và phải được nêu rõ
trong `plan.md`.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả cho SCRUM-60 |
| :--- | :--- | :--- |
| `src/CreatorFlow.Api/Endpoints/StatisticsEndpoints.cs` + `Services/Statistics/StatisticsService.cs` (SCRUM-55) | `GET /api/projects/{id}/statistics/creators` trả Total/InProgress/Published/Overdue + Views/Likes/Comments/Shares/Engagement theo từng Creator và dòng tổng Project | Đã có nguồn số liệu chính, **không cần endpoint mới** |
| `src/CreatorFlow.Contracts/Statistics/CreatorStatisticsContracts.cs` | `CreatorStatisticsResponse(long ProjectId, CreatorStatisticsRow Project, IReadOnlyList<CreatorStatisticsRow> Creators)` | Hình dạng dữ liệu đã ổn định để dựng prompt |
| `src/CreatorFlow.Contracts/Metrics/MetricsSummaryContracts.cs` + `MetricService.GetSummaryAsync` (SCRUM-52) | `GET .../metrics/summary?groupBy=content\|creator\|platform\|project` → `MetricsSummaryRow(Key, Name, Views, Likes, Comments, Shares, EngagementPercent)` | Có thêm chiều Content/Platform (top content, platform nào kém) |
| `src/CreatorFlow/Controls/ProjectReportsControl.cs` | Đã là màn Reports: 2 view "Hiệu suất" (metrics theo chiều) và "Creator & Project" (SCRUM-55), có toolbar + grid + status label | **Điểm vào tự nhiên**: thêm nút + panel AI ngay tại đây, không đổi nav |
| `src/CreatorFlow/Program.cs:90-94` | `AIService` tạo từ `.env` qua `AIFactory.Create()`, dùng chung cho `RepurposeAiService`, `ScriptAiService` (POC client) | Tái dùng lõi AI client, không tạo client thứ hai |
| `src/CreatorFlow/Services/AI/RepurposeAiService.cs` + `RepurposeAiPromptBuilder.cs` | Mẫu adapter + prompt builder thuần, map lỗi mạng/timeout thành `Failure` thay vì ném ra UI | Dùng lại đúng khuôn này cho performance |
| `src/CreatorFlow/Services/AI/NotConfiguredAiService.cs` | Đã có câu thông báo chuẩn khi chưa cấu hình AI | Tái dùng message |
| `docs/ux-ui/CreatorFlow_UX.md` §11, §13 | Reports có Project Summary / Content Performance / Creator Performance / Platform Performance; AI chỉ gợi ý, không tự đổi dữ liệu | Vị trí + nguyên tắc khớp tài liệu |
| `database/**` | Không có bảng lưu kết quả phân tích AI | Nếu sau này cần lịch sử phân tích → task riêng, không làm ở đây |

## 3. Phạm vi đề xuất

### 3.1. Điểm vào

Thêm nút **"AI phân tích"** trong `ProjectReportsControl`, đặt trên thanh view (cạnh "Hiệu suất" /
"Creator & Project"). Bấm → mở **panel kết quả** ngay dưới bảng (không phải dialog modal), để người dùng vẫn
nhìn thấy số liệu trong lúc đọc nhận xét.

Không thêm menu mới, không đổi điều hướng, không đổi layout sidebar/topbar.

### 3.2. Phạm vi phân tích (theo view đang mở)

- Đang ở view **"Hiệu suất"**: gửi Metrics summary của chiều đang chọn + tổng project.
- Đang ở view **"Creator & Project"**: gửi `CreatorStatisticsResponse` (dòng Project + các Creator), tôn trọng
  bộ lọc Creator hiện tại (nếu chọn 1 Creator thì phân tích đúng người đó, nhưng **vẫn gửi dòng tổng Project**
  để AI có mốc so sánh).
- Bộ lọc đổi → panel kết quả cũ **giữ nguyên** nhưng gắn nhãn "Kết quả cho: <mô tả phạm vi lúc gọi>" để không
  gây hiểu lầm kết quả đang ứng với bộ lọc mới. Người dùng bấm "Phân tích lại" để cập nhật.

### 3.3. Panel kết quả

1. **Header**: "AI phân tích hiệu suất" + dòng phụ nêu **nguồn dữ liệu và thời điểm**:
   `Dựa trên Metrics đã lưu trong hệ thống (tải lúc HH:mm dd/MM/yyyy). AI không truy cập nền tảng mạng xã hội.`
   → đây là cách hiện thực hóa AC "Không giả vờ AI tự truy cập social platform".
2. **Nội dung AI** ở `RichTextBox`/`TextBox` readonly, chia 2 mục cố định:
   `== NHẬN XÉT ==` và `== GỢI Ý CẢI THIỆN ==` (mỗi gợi ý 1 dòng, hành động được).
3. **Trạng thái**: chưa cấu hình AI / đang phân tích / lỗi / xong — dùng status label sẵn có của màn Reports.
4. **Nút**: "Phân tích" (hoặc "Phân tích lại"), "Sao chép" (clipboard), "Đóng". Không có nút "Lưu".
5. **Loading** không được treo UI: gọi async, disable nút trong lúc chờ, có hủy khi rời màn.

### 3.4. AI (client-side, POC)

Bộ ba thuần, test offline được, cùng khuôn SCRUM-59:

- `PerformanceAiRequest` — dữ liệu đầu vào đã chuẩn hoá (project, phạm vi, các dòng số liệu, thời điểm tải).
- `PerformanceAiPromptBuilder` — dựng prompt từ request; **hàm thuần**, không gọi mạng.
- `PerformanceAiResponseParser` — tách output thành `Remarks` / `Suggestions`; chịu lỗi: thiếu mốc → toàn bộ
  text vào `Remarks` + cờ `IsStructured = false` để UI cảnh báo "AI trả về không đúng định dạng".
- `PerformanceAiService` — adapter gọi `CreatorFlow.AI.IAIService.AskAsync`; map lỗi/timeout thành `Failure`,
  không ném chi tiết dịch vụ ra UI.

Quy ước output:

```text
== NHẬN XÉT ==
...
== GỢI Ý CẢI THIỆN ==
- ...
- ...
```

Ràng buộc prompt (phục vụ AC):

- Nêu rõ: "Dưới đây là Metrics do hệ thống CreatorFlow tổng hợp sẵn; bạn **không** có quyền truy cập
  TikTok/Facebook/YouTube và **không** được bịa số liệu mới. Nếu thiếu dữ liệu, nói rõ là thiếu."
- Cấm bịa nền tảng không có trong dữ liệu, cấm bịa số Views/Likes khi dòng đó bằng 0.
- Giới hạn độ dài gửi đi: tối đa ~20 dòng/chiều (top theo Views), có ghi chú "(đã rút gọn)" — tránh prompt phình
  to khi project nhiều creator/content.
- Ngôn ngữ kết quả: **tiếng Việt**, văn phong ngắn gọn, không markdown code fence.

### 3.5. Xử lý lỗi (AC3)

| Tình huống | Hành vi |
| :--- | :--- |
| Chưa cấu hình AI (`.env` thiếu) | Nút disable + status `NotConfiguredAiService.Message` |
| API AI timeout | Status đỏ: "AI phản hồi quá thời gian chờ. Vui lòng thử lại." + nút "Thử lại" |
| API AI lỗi mạng/5xx | "Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau." |
| AI trả rỗng | "AI không trả về nội dung." |
| API CreatorFlow lỗi khi tải Metrics (401/403/timeout/mạng) | Status đỏ, hiện đúng thông điệp đã có của `IMetricBackend`/`IStatisticsBackend`; **không gọi AI** với dữ liệu rỗng |
| Không có dữ liệu Metrics (mọi số = 0) | Vẫn cho phân tích, nhưng prompt ghi rõ "chưa có dữ liệu" để AI nói thiếu dữ liệu thay vì bịa |

Không hiển thị URL endpoint, key, hay stack trace.

### 3.6. Quyền

- Không thêm luật quyền mới: ai đọc được Reports (thành viên Project active) thì được bấm phân tích.
- Quyền vẫn do Backend quyết định khi trả Metrics/Statistics; UI chỉ hiển thị.
- Người ngoài Project: API trả 403 → panel báo lỗi, không gọi AI.

## 4. Ngoài phạm vi

- Chuyển AI ra Backend (giữ POC client như SCRUM-57/58/59) — ghi nợ kỹ thuật.
- Lưu lịch sử phân tích vào DB, bảng `ai_requests`, hay xuất PDF/Excel.
- Streaming token, chat nhiều lượt với AI về số liệu.
- AI tự sửa Metrics/Content/Deadline, tự tạo task.
- So sánh nhiều Project cùng lúc, phân tích theo khoảng thời gian (dữ liệu hiện chỉ có snapshot mới nhất).
- Endpoint mới hoặc migration mới.
- Đụng `src/CreatorFlow/CreatorFlow/**` (bản sao cũ, bị `<Compile Remove>`).

## 5. Luồng nghiệp vụ đề xuất

```text
Reports (ProjectReportsControl)
   ↓ chọn view + bộ lọc
[Tải Metrics/Statistics qua API] ──lỗi──► status đỏ, KHÔNG gọi AI
   ↓ có dữ liệu
Bấm [AI phân tích]
   ↓ (chưa cấu hình AI → status + disable)
Loading (async, có hủy)
   ↓
PerformanceAiService → prompt → AI API
   ├─ timeout/mạng ─► status lỗi + [Thử lại]
   ├─ rỗng ────────► status lỗi
   └─ thành công ──► parse 2 mục → panel readonly
                      ↓
                   [Sao chép] / [Đóng]   (không ghi DB)
```

## 6. Quyết định cần chốt

| # | Vấn đề | Đề xuất |
| :--- | :--- | :--- |
| 6.1 | Điểm vào | Nút **"AI phân tích"** trong màn Reports (panel, không dialog) |
| 6.2 | Nguồn AI | Tái dùng **lõi AI client POC** như SCRUM-57/58/59 |
| 6.3 | Phạm vi số liệu | Theo view đang mở: metrics summary **hoặc** creator statistics + dòng tổng Project |
| 6.4 | Lưu kết quả | **Không lưu** (chỉ đọc + sao chép); lịch sử phân tích để task sau |
| 6.5 | Ngôn ngữ output | **Tiếng Việt**, 2 mục cố định |
| 6.6 | Tên creator trong prompt | Gửi **tên hiển thị** (nội bộ project), không gửi email/user id |
| 6.7 | Giới hạn dữ liệu gửi | Top 20 dòng/chiều theo Views, ghi chú "(đã rút gọn)" |
| 6.8 | Môi trường kiểm chứng | **Docker local** (DB riêng), dùng dữ liệu có sẵn; không commit dữ liệu mẫu |
| 6.9 | Bộ lọc đổi sau khi có kết quả | Giữ kết quả cũ + nhãn phạm vi, yêu cầu bấm "Phân tích lại" |
| 6.10 | `ai_requests` / log | Không |

## 7. Tiêu chí hoàn thành (map AC)

- [ ] **AC1 — Dùng Metrics từ hệ thống:** prompt chỉ chứa số liệu lấy từ `IMetricBackend`/`IStatisticsBackend`
      (đã lưu trong DB); không có lời gọi ra nền tảng mạng xã hội; UI nêu rõ nguồn + thời điểm tải.
- [ ] **AC2 — Không giả vờ AI tự truy cập social platform:** prompt cấm bịa số liệu/nền tảng; UI có dòng
      disclaimer cố định; kết quả không trình bày như "số liệu realtime".
- [ ] **AC3 — Hiển thị lỗi API rõ ràng:** đủ 6 tình huống ở §3.5, thông điệp tiếng Việt, không lộ key/URL/stack.
- [ ] **Chức năng:** bấm phân tích theo view hiện tại, có loading/hủy, kết quả 2 mục, sao chép được.
- [ ] **Kiến trúc:** UI → `IPerformanceAiService` → `IAIService`; không gọi HTTP/LLM trực tiếp trong control;
      không SQL trong control; không sửa Metrics.
- [ ] **Không ghi dữ liệu:** không INSERT/UPDATE/DELETE nào; không có nút Lưu.
- [ ] **UI:** dùng `UITheme`, `RoundedButton`, status label sẵn có của Reports; không đổi nav.
- [ ] **Test:** unit test `PerformanceAiPromptBuilder` (có số liệu, rỗng, rút gọn) + `PerformanceAiResponseParser`
      (đúng mốc, thiếu mốc) + `PerformanceAiService` với fake AI client (thành công/lỗi/timeout/chưa cấu hình);
      build `dotnet build CreatorFlow.slnx` 0 error.
- [ ] **Kiểm chứng:** kịch bản ở §8 chạy được trên máy người dùng.

## 8. Kịch bản kiểm chứng

Môi trường: **Docker local** (`deploy/docker/compose.dev.yml`), API `http://localhost:5080`, WinForms trỏ API
local bằng `Api__BaseUrl` (biến môi trường, không commit). Dùng dữ liệu có sẵn trên DB local; không commit dữ liệu mẫu.

1. **Chuẩn bị:** mở app bằng tài khoản có sẵn, chọn Project đã có Metrics (không tạo/không commit dữ liệu mẫu).
2. **Reports → "Hiệu suất"** (groupBy = creator) → bấm **AI phân tích**:
   - Kỳ vọng: panel hiện 2 mục; nhận xét bám đúng số trên bảng (creator yếu/nổi bật, tỷ lệ Published, Overdue).
   - Kiểm tra **không** có số liệu nào không tồn tại trong bảng.
3. **Reports → "Creator & Project"** → lọc 1 Creator → **AI phân tích**:
   - Kỳ vọng: gợi ý nói về đúng creator đó, vẫn so với mốc "Toàn Project"; nhãn phạm vi đúng.
4. **Đổi bộ lọc sang Creator khác**: kết quả cũ giữ nguyên + nhãn phạm vi cũ; bấm "Phân tích lại" → cập nhật.
5. **Tắt AI** (bỏ key trong `.env`) → nút disable + thông báo chưa cấu hình, không crash.
6. **Giả lập lỗi API** (dừng container API rồi mở Reports) → status đỏ "Không kết nối được API"; xác nhận
   **không** gọi AI.
7. **Sao chép** → dán ra Notepad, đúng nội dung; đóng panel → không có thay đổi nào trong DB.

Phần chưa kiểm chứng dự kiến: DPI/độ phân giải khác, môi trường dev dùng chung, AI provider khác.

## 9. Subtask đề xuất cho Jira

1. **Chốt UX & phạm vi dữ liệu** — điểm vào, view nào phân tích gì, bộ lọc, không lưu DB.
2. **AI layer** — `PerformanceAiRequest/Response`, `PerformanceAiPromptBuilder`, `PerformanceAiResponseParser`,
   `PerformanceAiService` + `IPerformanceAiService`; xử lý chưa cấu hình/lỗi/timeout.
3. **UI panel trong Reports** — nút "AI phân tích", panel kết quả, loading/hủy, sao chép, nhãn nguồn dữ liệu.
4. **Wiring** — tạo `PerformanceAiService` trong `Program.cs` từ `aiService` sẵn có; truyền vào `BoardForm` →
   `ProjectReportsControl`.
5. **Test** — unit test builder/parser/service; build.
6. **Kiểm chứng** — theo §8, trình checklist trước khi chạy theo `post-change-test-workflow`.

## 10. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. File này chỉ kiểm tra tĩnh.
Trước khi build/test/chạy app phải trình checklist theo `post-change-test-workflow` và chờ duyệt; trong lúc test
chỉ quan sát, không sửa code.

Lưu ý branch: nhánh này đang được tạo từ `feature/SCRUM-55-...` (chưa merge `develop`) nên lịch sử còn commit
SCRUM-55. Nếu cần PR sạch chỉ chứa SCRUM-60 thì phải tách nhánh từ `develop` — chờ bạn quyết định.

## 11. Câu hỏi mở

Đã chốt ngày 29/10/2026: điểm vào trong Reports (6.1), phân tích theo view đang mở (6.3), không lưu kết quả
(6.4), kiểm chứng trên Docker local (6.8), tái dùng lõi AI client POC (6.2).
Còn lại chờ duyệt trong `plan.md` §10: bố cục panel, số lượng gợi ý, hành vi khi chưa có số liệu.
