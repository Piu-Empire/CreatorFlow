# SCRUM-60 — Kế hoạch triển khai: AI phân tích hiệu suất

Trạng thái: **CHỜ DUYỆT KẾ HOẠCH**. Chưa viết code.
Branch: `feature/SCRUM-60-ai-phan-tich-hieu-suat` (từ `cdeff78`, chưa push).
Tài liệu gốc: `docs/tasks/scrum-60-ai-phan-tich-hieu-suat/requirements.md` — **đã duyệt** (29/10/2026).

Quyết định đã chốt:
- **6.1** Điểm vào: nút **"AI phân tích"** trong màn Reports, panel kết quả (không dialog).
- **6.2** Tái dùng **lõi AI client POC** (`CreatorFlow.AI.IAIService`, key ở `.env`).
- **6.3** Phân tích theo **view đang mở** + luôn kèm dòng tổng Project.
- **6.4** **Không lưu** kết quả vào DB.
- **6.5** Output **tiếng Việt**, 2 mục cố định.
- **6.6** Prompt gửi **tên hiển thị** creator, không gửi email/user id.
- **6.7** Giới hạn **20 dòng/chiều** theo Views, ghi chú "(đã rút gọn)".
- **6.8** Kiểm chứng trên **Docker local**, dùng dữ liệu có sẵn, không commit dữ liệu mẫu.
- **6.9** Đổi bộ lọc: giữ kết quả cũ + nhãn phạm vi, phải bấm "Phân tích lại".
- **6.10** Không log `ai_requests`.

## 1. Nguyên tắc thực hiện

- Không thêm package, không migration, không endpoint API mới (dữ liệu đã có từ SCRUM-52/55).
- Giữ đúng kiến trúc: UI → `IPerformanceAiService` → `IAIService` (HTTP). Control không gọi HTTP/LLM trực tiếp.
- Không INSERT/UPDATE/DELETE. Không nút Lưu.
- Chỉ sửa file thuộc phạm vi dưới đây; không dọn code lân cận, không đụng `src/CreatorFlow/CreatorFlow/**`.

## 2. Danh sách thay đổi

### 2.1. File mới

| File | Nội dung |
| :--- | :--- |
| `src/CreatorFlow/Models/PerformanceAiRequest.cs` | `PerformanceAiRequest`, `PerformanceMetricLine`, `PerformanceCreatorLine`, enum `PerformanceScope` — dữ liệu đã chuẩn hoá để dựng prompt |
| `src/CreatorFlow/Models/PerformanceAiResponse.cs` | `PerformanceAiResponse` (Succeeded/Remarks/Suggestions/ErrorMessage/IsStructured) + `PerformanceParsedContent` + factory `Success`/`Failure` |
| `src/CreatorFlow/Services/AI/IPerformanceAiService.cs` | `bool IsConfigured { get; }`, `Task<PerformanceAiResponse> AnalyzeAsync(PerformanceAiRequest, CancellationToken)` |
| `src/CreatorFlow/Services/AI/PerformanceAiPromptBuilder.cs` | Dựng prompt thuần từ request (không gọi mạng) |
| `src/CreatorFlow/Services/AI/PerformanceAiResponseParser.cs` | Tách output thành `Remarks` + `Suggestions[]`, chịu lỗi thiếu mốc |
| `src/CreatorFlow/Services/AI/PerformanceAiService.cs` | Adapter gọi `IAIService.AskAsync`, map lỗi/timeout thành `Failure` |
| `tests/CreatorFlow.IntegrationTests/Shared/PerformanceAiPromptBuilderTests.cs` | Test prompt (có số liệu / rỗng / rút gọn / chặn bịa nền tảng) |
| `tests/CreatorFlow.IntegrationTests/Shared/PerformanceAiResponseParserTests.cs` | Test parser (đủ mốc / thiếu mốc / mốc sai thứ tự / bullet rỗng) |
| `tests/CreatorFlow.IntegrationTests/Shared/PerformanceAiServiceTests.cs` | Test service với `FakeAiClient` (chưa cấu hình / thành công / lỗi client / ném exception / rỗng) |

### 2.2. File sửa

| File | Thay đổi |
| :--- | :--- |
| `src/CreatorFlow/Controls/ProjectReportsControl.cs` | Thêm tham số `IPerformanceAiService? performanceAi`; nút "AI phân tích"; panel kết quả; logic async + hủy + nhãn phạm vi + sao chép |
| `src/CreatorFlow/Forms/Board/BoardForm.cs` | Thêm tham số optional **ở cuối** ctor `IPerformanceAiService? performanceAi = null`, truyền xuống `ProjectReportsControl` |
| `src/CreatorFlow/Program.cs` | `var performanceAi = new PerformanceAiService(aiService);` và truyền vào `BoardForm` ở cả 2 chỗ khởi tạo |

Không sửa `MetricService`, `StatisticsService`, contracts, DB.

## 3. Chi tiết kỹ thuật

### 3.1. Model

```csharp
public enum PerformanceScope { Metrics, Creator }

public sealed record PerformanceMetricLine(
    string Name, long Views, long Likes, long Comments, long Shares, double EngagementPercent);

public sealed record PerformanceCreatorLine(
    string Name, int Total, int InProgress, int Published, int Overdue,
    long Views, long Likes, long Comments, long Shares, double EngagementPercent);

public sealed record PerformanceAiRequest(
    long ProjectId,
    string ProjectName,
    PerformanceScope Scope,
    string ScopeLabel,          // "Theo Creator", "Creator: Minh", "Toàn Project"
    string? GroupBy,            // chỉ khi Scope = Metrics
    DateTime LoadedAt,
    IReadOnlyList<PerformanceMetricLine> Metrics,
    IReadOnlyList<PerformanceCreatorLine> Creators,
    PerformanceCreatorLine? ProjectTotals,
    bool IsTruncated);
```

```csharp
public sealed record PerformanceAiResponse(
    bool Succeeded, string? Remarks, IReadOnlyList<string> Suggestions,
    string? ErrorMessage, bool IsStructured = true)
{
    public static PerformanceAiResponse Success(string remarks, IReadOnlyList<string> suggestions, bool isStructured = true) => ...;
    public static PerformanceAiResponse Failure(string message) => ...;
}
```

- `Suggestions` **không bao giờ null** (dùng `Array.Empty<string>()` khi thất bại) để UI khỏi phải kiểm tra null.
- Model đặt trong `CreatorFlow.Models` như `RepurposeAiRequest/Response` để không phụ thuộc contract API.

### 3.2. Prompt builder

- Hằng số mốc: `RemarksMarker = "== NHẬN XÉT =="`, `SuggestionsMarker = "== GỢI Ý CẢI THIỆN =="`, `MaxRows = 20`.
- Cấu trúc prompt:
  1. Vai trò: "Bạn là chuyên gia phân tích hiệu suất nội dung cho CreatorFlow."
  2. Dữ liệu: project, phạm vi, thời điểm tải, groupBy.
  3. Ràng buộc (map AC2):
     - "Các số dưới đây do hệ thống CreatorFlow tổng hợp từ Metrics đã lưu. Bạn **không** có quyền truy cập
       TikTok/Facebook/YouTube và **không** được bịa số liệu."
     - "Không nhắc tới dữ liệu realtime, không thêm nền tảng không có trong dữ liệu."
     - "Nếu thiếu dữ liệu hoặc Views = 0, nói rõ là thiếu dữ liệu để kết luận."
  4. Bảng số liệu dạng text (`- Minh | Total 12 | Published 8 | Overdue 1 | Views 120000 | Engagement 5.58%`).
     Sắp xếp giảm dần theo Views, cắt còn `MaxRows`, nếu cắt thì thêm "… (đã rút gọn, còn N dòng)".
  5. Định dạng trả về đúng 2 mốc + 3–5 gợi ý dạng `-`, tiếng Việt, không markdown fence, không giải thích ngoài.
- Làm sạch đầu vào: bỏ `\r\n`, cắt tên > 100 ký tự (tránh creator name phá cấu trúc prompt).
- Hàm thuần, không `DateTime.Now` bên trong — thời điểm lấy từ `request.LoadedAt` để test tất định.

### 3.3. Parser

- Tách theo 2 mốc (so khớp không phân biệt hoa thường, cho phép khoảng trắng thừa).
- `Suggestions`: mỗi dòng bắt đầu `-`, `*`, `•` hoặc `1.` → bỏ tiền tố, trim; bỏ dòng rỗng; tối đa 8 mục.
- Thiếu mốc GỢI Ý → toàn bộ text vào `Remarks`, `Suggestions` rỗng, `IsStructured = false`.
- Thiếu cả hai mốc → `Remarks` = toàn bộ text, `IsStructured = false` (UI cảnh báo "AI trả về không đúng định dạng").
- Text rỗng/sai định dạng hoàn toàn → service trả `Failure("AI không trả về nội dung.")`.

### 3.4. Service

```csharp
public sealed class PerformanceAiService(IAIService? ai) : IPerformanceAiService
{
    public bool IsConfigured => ai is not null;
    public async Task<PerformanceAiResponse> AnalyzeAsync(PerformanceAiRequest request, CancellationToken token = default)
    { ... }
}
```

- `null` client → `Failure(NotConfiguredAiService.Message)`.
- `OperationCanceledException` khi `token.IsCancellationRequested` → ném lại (UI bỏ qua, không hiện lỗi).
- `OperationCanceledException` khác → `Failure("AI phản hồi quá thời gian chờ. Vui lòng thử lại.")`.
- Exception khác → `Failure("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.")` (không lộ chi tiết).
- `!result.Success` → dùng `result.ErrorMessage` nếu có, ngược lại "Không nhận được kết quả từ AI."
- Nội dung rỗng → `Failure("AI không trả về nội dung.")`.

### 3.5. UI — `ProjectReportsControl`

Bố cục (giữ nguyên các thành phần hiện có):

```text
[Header: Báo cáo Project]
[viewBar: Hiệu suất | Creator & Project |            AI phân tích ]
[metricsView / statsView: toolbar + grid/status]
[aiPanel (Dock=Bottom, Visible=false): tiêu đề + nguồn dữ liệu
                                       RichTextBox readonly (cao ~180)
                                       [Phân tích lại] [Sao chép] [Đóng] + status]
```

- Nút "AI phân tích": `RoundedButton` style `Secondary`, dock về phải trong `viewBar`.
  `Enabled = _performanceAi?.IsConfigured == true`; nếu chưa cấu hình → tooltip + status `NotConfiguredAiService.Message`.
- Tiêu đề panel: `"AI phân tích hiệu suất"` + dòng phụ
  `Dựa trên Metrics đã lưu trong hệ thống (tải lúc {LoadedAt:HH:mm dd/MM/yyyy}). AI không truy cập nền tảng mạng xã hội.`
- Nhãn phạm vi: `Phạm vi: {ScopeLabel}` — chốt tại thời điểm gọi, không đổi khi người dùng đổi bộ lọc sau đó.
- Nội dung: `RichTextBox` `ReadOnly`, `BorderStyle.None`, `BackColor = UITheme.White`, font `UITheme.FontBody`.
  Render: dòng `NHẬN XÉT` in đậm → đoạn văn; dòng `GỢI Ý CẢI THIỆN` in đậm → bullet.
- Nếu `!IsStructured` → thêm cảnh báo màu `UITheme.Warning` phía trên nội dung.
- Nút "Sao chép": `Clipboard.SetText` toàn bộ nội dung; lỗi clipboard → status đỏ, không crash.
- Nút "Đóng": ẩn panel, **không** xóa dữ liệu số liệu đang hiển thị.
- `_aiCts` hủy trong `Dispose` và khi bấm "Đóng" (nếu đang chạy).
- Trạng thái: `Đang phân tích...` (disable nút, `UseWaitCursor`), thành công → ẩn status; lỗi → status đỏ + nút "Thử lại".

### 3.6. Dựng request từ dữ liệu đang có

- View "Hiệu suất": tái dùng `MetricsSummaryResponse` **đã tải** cho chiều hiện tại (`_current`), map sang
  `PerformanceMetricLine`; `ScopeLabel` = nhãn chiều ("Theo Creator"...). Nếu chưa tải → gọi
  `_backend.GetMetricsSummary(_projectId, _current)` trước.
- View "Creator & Project": dùng `CreatorStatisticsResponse` đã tải (hoặc gọi lại `GetCreatorStatistics` với
  `SelectedCreatorId`); map dòng Project → `ProjectTotals`, các Creator → `Creators`; `ScopeLabel` =
  `"Toàn Project"` hoặc `"Creator: {tên}"`.
- Nếu lời gọi API ném lỗi → status đỏ dùng `ex.Message` (thông điệp tiếng Việt có sẵn từ `ApiClient`), **return
  sớm, không gọi AI**.
- `IsTruncated` = có dòng bị cắt theo `MaxRows`.

### 3.7. Wiring

- `Program.cs`: sau `var repurposeAi = new RepurposeAiService(aiService);` thêm
  `var performanceAi = new PerformanceAiService(aiService);`
- `BoardForm`: thêm tham số cuối `IPerformanceAiService? performanceAi = null`, giữ nguyên mọi tham số cũ;
  truyền `performanceAi` xuống `new ProjectReportsControl(_metricBackend, CurrentSession.CurrentProjectId, _statisticsBackend, _performanceAi)`.
- Local (InMemory) mode không có statistics backend → view Creator hiện thông báo sẵn có; nút AI vẫn dùng được
  cho view "Hiệu suất" nếu `IMetricBackend` khả dụng.

## 4. Thứ tự thực hiện

1. Models (`PerformanceAiRequest/Response`).
2. `PerformanceAiPromptBuilder` + tests.
3. `PerformanceAiResponseParser` + tests.
4. `IPerformanceAiService` + `PerformanceAiService` + tests.
5. UI: nút + panel + async/hủy/sao chép trong `ProjectReportsControl`.
6. Wiring `BoardForm` + `Program.cs`.
7. **Cổng 1** — build + unit test (xin phép trước khi chạy).
8. **Cổng 2** — kiểm chứng theo §6 (xin phép trước khi chạy app).
9. Cập nhật `test-results.md` (kết quả thật, không suy diễn).

## 5. Kiểm thử

### 5.1. Unit test (offline, không cần DB/AI)

| Test | Nội dung |
| :--- | :--- |
| `Build_ContainsProjectScopeAndNumbers` | Prompt có tên project, nhãn phạm vi, đúng Views/Engagement của dòng đầu |
| `Build_StatesNoSocialAccess` | Có câu cấm truy cập nền tảng mạng xã hội + cấm bịa số (AC2) |
| `Build_EmptyData_SaysNoData` | Không có dòng nào → prompt nêu "chưa có dữ liệu" |
| `Build_TruncatesTo20Rows_AndNotesIt` | 25 dòng → còn 20 + ghi chú "(đã rút gọn)" |
| `Build_StripsNewlinesFromNames` | Tên creator chứa `\r\n` không phá cấu trúc prompt |
| `Parse_BothMarkers_ReturnsRemarksAndBullets` | Tách đúng 2 mục, bỏ tiền tố bullet |
| `Parse_MissingSuggestions_IsUnstructured` | `IsStructured=false`, toàn bộ text vào Remarks |
| `Parse_BulletsWithAsteriskAndNumbers` | Nhận `*`, `1.` |
| `Analyze_NoClient_NotConfigured` | Message `NotConfiguredAiService.Message` |
| `Analyze_Success_Parses` | Prompt gửi đi có số liệu, kết quả parse đúng |
| `Analyze_ClientFailure_ReturnsFailure` | Truyền `ErrorMessage` của client |
| `Analyze_ClientThrows_GenericMessage` | Không lộ chi tiết exception |
| `Analyze_EmptyContent_Failure` | "AI không trả về nội dung." |
| `Analyze_UserCancellation_Rethrows` | `OperationCanceledException` được ném lại khi token bị hủy |

### 5.2. Build

- `dotnet build CreatorFlow.slnx` → 0 error (không tính warning có sẵn).
- `dotnet test --filter "FullyQualifiedName~PerformanceAi"` → pass toàn bộ test mới.

### 5.3. Kiểm tra tĩnh trước khi chạy

- Không có `HttpClient`/`AIService` gọi trực tiếp trong `ProjectReportsControl` (chỉ qua interface).
- Không có SQL trong control.
- Không có lời gọi ghi dữ liệu mới.

## 6. Kịch bản kiểm chứng (Docker local)

Chuẩn bị: Docker Desktop chạy `deploy/docker/compose.dev.yml`; `Api__BaseUrl=http://localhost:5080/`;
đăng nhập app bằng tài khoản có sẵn và mở một Project đã có Metrics. Không thêm dữ liệu mẫu vào repo.

| # | Thao tác | Kết quả mong đợi |
| :--- | :--- | :--- |
| 1 | Reports → "Hiệu suất" (Theo Creator) → **AI phân tích** | Panel mở; dòng nguồn dữ liệu có giờ tải; 2 mục Nhận xét/Gợi ý; mọi con số nêu ra đều có trong bảng |
| 2 | Kiểm tra nội dung | Không có số/nền tảng không tồn tại; có câu thể hiện chỉ dựa trên Metrics đã lưu |
| 3 | Reports → "Creator & Project" → lọc 1 Creator → **AI phân tích** | Nhãn phạm vi "Creator: …"; nhận xét về đúng người, vẫn so với mốc Toàn Project |
| 4 | Đổi sang Creator khác (không bấm phân tích lại) | Kết quả cũ giữ nguyên + nhãn phạm vi cũ; bấm "Phân tích lại" → nội dung cập nhật |
| 5 | Bấm **Sao chép** → dán Notepad | Nội dung khớp panel |
| 6 | Bỏ AI key trong `.env`, mở lại app | Nút disable, thông báo chưa cấu hình, không crash |
| 7 | Dừng container API rồi mở Reports | Status đỏ "Không kết nối được API."; bấm AI → báo lỗi tải số liệu, **không** gọi AI |
| 8 | Kiểm tra DB sau khi kiểm chứng | Không có bản ghi mới nào từ tính năng AI |

Chưa kiểm chứng trong task này: DPI/độ phân giải khác, API dev dùng chung, provider AI khác.

## 7. Rủi ro và cách xử lý

| Rủi ro | Xử lý |
| :--- | :--- |
| Prompt phình to khi project nhiều dữ liệu | Cắt 20 dòng/chiều + ghi chú; prompt chỉ chứa số đã tổng hợp, không gửi nội dung Content |
| AI bịa số liệu/nền tảng (AC2) | Ràng buộc trong prompt + UI disclaimer cố định + không hiển thị như realtime |
| Tên creator chứa ký tự xuống dòng phá prompt | Sanitize trước khi ghép |
| Người dùng đổi bộ lọc khi đang chờ | Kết quả gắn với phạm vi đã chốt; nhãn phạm vi hiển thị rõ |
| Timeout/treo UI | Async + `CancellationToken`, hủy khi đóng panel/rời control |
| Lỗi API CreatorFlow bị hiểu là lỗi AI | Tách 2 trạng thái: lỗi tải số liệu (không gọi AI) vs lỗi AI |
| Nợ kỹ thuật key AI ở client | Ghi rõ như SCRUM-57/58/59; chuyển Backend ở task riêng |

## 8. Ngoài phạm vi (giữ nguyên như requirements §4)

Không lưu lịch sử phân tích, không streaming/chat, không export, không endpoint/migration mới, không sửa
Metrics/Content, không chuyển AI sang Backend, không đụng `src/CreatorFlow/CreatorFlow/**`.

## 9. Cổng duyệt

Theo `AGENTS.md`: **requirements (đã duyệt) → plan (file này) → duyệt → triển khai**. Trước khi build/test/chạy
app/ghi dữ liệu local phải trình checklist theo `post-change-test-workflow` và chờ người dùng đồng ý; trong lúc
test chỉ quan sát, không sửa code.

## 10. Câu hỏi cần bạn xác nhận trước khi code

1. Bố cục panel ở §3.5 (panel dưới bảng, 3 nút Phân tích lại / Sao chép / Đóng) đã ổn chưa?
2. Số gợi ý tối đa 8 và 3–5 gợi ý theo prompt — chấp nhận được không?
3. Có muốn nút "AI phân tích" chỉ hiện khi có số liệu, thay vì disable khi rỗng?
