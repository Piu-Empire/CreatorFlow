# SCRUM-54 — Kế hoạch triển khai: Charts và Analytics

Trạng thái: **ĐÃ TRIỂN KHAI XONG PHẦN CODE; CỔNG 1 ĐẠT; CỔNG 2 CHỜ KIỂM THỬ THỦ CÔNG**.
Branch: `SCRUM-54-charts-va-analytics` (tách từ `SCRUM-60-ai-phan-tich-hiệu-suất` tại `8e6d07e`).
Kết quả kiểm chứng: `docs/tasks/scrum-54-charts-va-analytics/test-results.md`.
Tài liệu gốc: `docs/tasks/scrum-54-charts-va-analytics/requirements.md` — bản nháp, các đề xuất §6 được dùng làm quyết định mặc định trong plan này (cần xác nhận ở §12).

## 1. Quyết định đã chốt (theo đề xuất requirements §6)

- **Điểm vào:** thêm view thứ 3 **"Biểu đồ"** trong thanh view của màn Reports; giữ nguyên 2 view "Hiệu suất" và "Creator & Project".
- **Vẽ chart:** tự vẽ bằng GDI+ trong `OnPaint`, double-buffer; **không thêm NuGet**.
- **Mốc thời gian:** `PlannedPublishAt`, thiếu thì `Deadline`, gom theo tháng; Content không có cả hai vào nhóm riêng "Chưa có mốc".
- **Lọc Creator cho biểu đồ:** không làm ở bản này (xem §10).
- **AI phân tích:** giữ nguyên hành vi SCRUM-60; view Biểu đồ không đổi logic AI.
- **Không đổi API/DB:** dùng `IBoardBackend.GetBoardCardsAsync` + `IMetricBackend.GetMetricsSummary` sẵn có.

## 2. Nguyên tắc thực hiện

- Không migration, không endpoint mới, không ghi dữ liệu, không đổi công thức Engagement (SCRUM-52).
- Kiến trúc: UI → `IBoardBackend` / `IMetricBackend` → API; control không chứa SQL, không gọi HTTP trực tiếp.
- Toàn bộ phép gom nhóm/đếm/scaling nằm trong **hàm thuần** để unit test không cần UI/DB.
- Chỉ sửa file thuộc phạm vi §3; không dọn code lân cận, không đụng `src/CreatorFlow/CreatorFlow/**`.
- Không có dữ liệu mẫu, không nhãn cứng, không mốc thời gian bịa (chi tiết §6).

## 3. Danh sách thay đổi

### 3.1. File mới

| File | Nội dung |
| :--- | :--- |
| `src/CreatorFlow/Models/ChartData.cs` | Record dữ liệu biểu đồ: `StatusChartData`/`StatusBucket`, `PlatformChartData`/`PlatformBucket`, `TimelineChartData`/`MonthBucket`, `PerformanceChartData` (thuần dữ liệu, không `Color`, không control) |
| `src/CreatorFlow/Controls/Charts/ChartDataBuilder.cs` | Hàm thuần dựng 4 bộ dữ liệu từ `IEnumerable<ContentBoardCard>` và `MetricsSummaryResponse`; có `NiceCeiling` tính thang đo |
| `src/CreatorFlow/Controls/Charts/BarChartPanel.cs` | Control tự vẽ thanh ngang bằng GDI+: tiêu đề, mô tả ngắn, nhãn, giá trị, phần trăm, trạng thái rỗng; double-buffer, không cấp phát trong `OnPaint` |
| `tests/CreatorFlow.IntegrationTests/Shared/ChartDataBuilderTests.cs` | Unit test cho 4 builder + scaling + dữ liệu rỗng/biên |

### 3.2. File sửa

| File | Thay đổi |
| :--- | :--- |
| `src/CreatorFlow/Controls/ProjectReportsControl.cs` | Thêm nút view **"Biểu đồ"**, panel `_chartsView` chứa lưới 2×2 `BarChartPanel`; tải card + 2 summary Metrics; xử lý loading/lỗi/hủy; không đổi 2 view cũ và panel AI |
| `src/CreatorFlow/Forms/Board/BoardForm.cs` | Truyền `_boardBackend` vào `new ProjectReportsControl(...)` (tham số mới ở cuối, optional) |

Không sửa: `MetricService`, `StatisticsService`, contracts, `DashboardMetrics`, `UITheme`, thư viện.

## 4. Bốn biểu đồ và ý nghĩa nghiệp vụ

| # | Biểu đồ | Câu hỏi quản lý trả lời | Nguồn | Cách vẽ |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Phân bố trạng thái Content** | Nội dung đang tắc ở khâu nào? | `ContentBoardCard.Status` | Thanh ngang theo đúng thứ tự workflow `Idea → Published` (lấy từ `Enum.GetValues<ContentStatus>()`, bỏ `Archived` vì nguồn Board đã loại Archived); mỗi thanh: số lượng + % trên tổng |
| 2 | **Content theo Platform** | Nội dung tập trung ở nền tảng nào, còn bao nhiêu Content chưa gắn Platform? | `ContentBoardCard.Platforms` | Thanh ngang xếp giảm dần theo số Content; thêm dòng "Chưa gắn Platform" nếu có. Ghi chú một Content thuộc nhiều Platform nên tổng > số Content |
| 3 | **Kế hoạch publish theo tháng** | Tháng nào dồn việc, có Content nào chưa có mốc? | `PlannedPublishAt`, thiếu thì `Deadline` | Cột/thanh theo tháng tăng dần, chỉ sinh tháng có dữ liệu; dòng phụ "Chưa có mốc: N" |
| 4 | **Hiệu suất Content & Platform** | Content nào hiệu quả nhất, Platform nào kém? | `MetricsSummary` (`groupBy=content`, `groupBy=platform`) | Hai khối xếp hạng: Top Content theo Views (kèm Engagement %), Platform theo Views (kèm Engagement %); thanh scale theo Views |

Quy ước hiển thị:

- Thang đo mỗi biểu đồ tính từ giá trị lớn nhất **trong dữ liệu đang vẽ**, làm tròn lên mốc "đẹp" bằng `NiceCeiling` (1/2/5 × 10ⁿ). Không có mốc cố định.
- Top Content giới hạn hiển thị **8 dòng** (`MaxBars`, hằng số hiển thị, không phải dữ liệu); nếu cắt thì tiêu đề ghi "Top 8 / N".
- Dữ liệu rỗng → control hiện khối "Chưa có dữ liệu" thay vì trục rỗng hoặc toàn số 0.
- Màu cột trạng thái lấy từ `UITheme.GetStageColor(status)`; màu thanh khác lấy từ một dãy token `UITheme` cố định theo thứ tự dòng — chỉ là style, không phải dữ liệu.

## 5. Chi tiết kỹ thuật

### 5.1. Model thuần — `ChartData.cs`

```csharp
public sealed record StatusBucket(ContentStatus Status, int Count, double Percent);
public sealed record StatusChartData(IReadOnlyList<StatusBucket> Buckets, int Total);

public sealed record PlatformBucket(string Name, int Count);
public sealed record PlatformChartData(IReadOnlyList<PlatformBucket> Buckets, int Untagged, int Total);

public sealed record MonthBucket(DateTime Month, int Count);
public sealed record TimelineChartData(IReadOnlyList<MonthBucket> Buckets, int NoDate);

public sealed record PerformanceChartData(
    IReadOnlyList<MetricsSummaryRow> TopContents, int ContentTotal,
    IReadOnlyList<MetricsSummaryRow> Platforms);
```

- Không chứa `Color`, font hay text trình bày; nhãn do UI resolve từ enum/`UITheme` (tránh hard-code trong builder).
- `MetricsSummaryRow` dùng lại contract sẵn có, không tạo DTO song song.

### 5.2. Hàm thuần — `ChartDataBuilder`

- `BuildStatus(cards)`: đếm theo status, giữ đủ các status workflow theo thứ tự enum (kể cả 0) để thấy pipeline; `Percent = Count / Total * 100` (Total = số card).
- `BuildPlatforms(cards)`: gom tên Platform (trim, so khớp không phân biệt hoa thường, hiển thị tên gốc xuất hiện đầu tiên), sắp giảm dần theo số rồi theo tên; `Untagged` = số card không có Platform.
- `BuildTimeline(cards, today?)`: mốc = `PlannedPublishAt?.Date ?? Deadline?.Date`; gom theo `(Year, Month)`, sắp tăng dần; `NoDate` = số card không có cả hai. Không nhận `today` vì không cần mốc "hiện tại" (tránh tham số thừa).
- `BuildPerformance(contentSummary, platformSummary)`: sắp `TopContents` giảm dần theo Views (hoà thì theo tên), cắt `MaxBars`; `Platforms` sắp giảm dần theo Views; giữ `ContentTotal` để UI ghi "Top 8 / N".
- `NiceCeiling(long maxValue)`: trả mốc tròn ≥ max (1/2/5 × 10ⁿ); dùng chung cho mọi biểu đồ; trả 1 khi max = 0.
- Tất cả hàm là `static`, không đọc DB/đồng hồ hệ thống; ném `ArgumentNullException` khi đầu vào null.

### 5.3. Control vẽ — `BarChartPanel`

- `UserControl` double-buffered, `Dock=Fill`, nền `UITheme.White`, viền `UITheme.Neutral200` (vẽ trong `OnPaint`).
- API: `SetData(string title, string caption, IReadOnlyList<ChartBar> bars)` với `record ChartBar(string Label, long Value, string ValueText, string? Hint, Color Color)`.
- Vẽ: tiêu đề (`FontBodyBold`), caption (`FontBody`, `Neutral600`), mỗi dòng: nhãn trái (cắt bằng `TextRenderer` nếu dài), thanh tỉ lệ `Value / NiceCeiling(max)`, số ở cuối thanh, hint mờ bên phải.
- Trạng thái rỗng: `SetEmpty(string message)` → khung giữa "Chưa có dữ liệu" / thông điệp cụ thể.
- Không tương tác hover/tooltip ở bản này (giảm rủi ro vẽ); dữ liệu chi tiết vẫn xem được ở 2 view bảng.
- Không cấp phát `Font`/`Brush` trong `OnPaint`; dùng font từ `UITheme`, brush tạo một lần theo màu dòng (hoặc `using` ngắn gọn, không trong vòng lặp lớn).

### 5.4. Tích hợp `ProjectReportsControl`

- Ctor thêm tham số cuối `IBoardBackend? boardBackend = null` (không phá caller cũ).
- Thanh view thêm `RoundedButton "Biểu đồ"`; `ShowView` chuyển sang 3 trạng thái (`metrics` / `stats` / `charts`), style Primary cho nút đang chọn.
- `_chartsView`: `Panel` chứa `TableLayoutPanel` 2 cột × 2 hàng (mỗi ô một `BarChartPanel`), đặt trong panel `AutoScroll` để không vỡ layout khi cửa sổ nhỏ.
- Lần đầu mở view:
  1. `_boardBackend null` → hiện "Biểu đồ cần chạy ở chế độ máy chủ (đã kết nối database)." cho cả 4 ô, không gọi API.
  2. `await _boardBackend.GetBoardCardsAsync(_projectId, token)`.
  3. Hai summary Metrics gọi trong `Task.Run` (content + platform) để không chặn UI; lỗi Metrics không làm mất 3 biểu đồ card — ô Hiệu suất hiện lỗi riêng.
  4. Dựng dữ liệu bằng `ChartDataBuilder`, gán vào 4 control.
- Hủy: `_chartCts` hủy trong `Dispose` và khi control rời trang (control bị dispose theo page hiện có); bắt `OperationCanceledException` im lặng.
- Lỗi card: hiện status đỏ trong `_chartsView` ("Không tải được dữ liệu biểu đồ: …"), không crash.
- Đánh dấu `_chartsLoaded` để không tải lại mỗi lần đổi view; mở lại màn Reports sẽ tạo control mới (tải mới).
- Dòng mô tả đầu màn giữ nguyên; caption từng biểu đồ nêu rõ nguồn dữ liệu (Board/Metrics đã lưu).

### 5.5. Wiring `BoardForm`

- `new ProjectReportsControl(_metricBackend, CurrentSession.CurrentProjectId, _statisticsBackend, _performanceAi, _boardBackend)`.
- Không đổi điều kiện mở Reports (`_metricBackend` vẫn là điều kiện bắt buộc).

### 5.6. "Không hard-code" — quy tắc kiểm chứng được

| Quy tắc | Cách đảm bảo |
| :--- | :--- |
| Không có số liệu mẫu | Builder chỉ nhận card/summary từ backend; test đổi input → output đổi |
| Không có danh sách nhãn cứng | Trạng thái lấy `Enum.GetValues<ContentStatus>()` + `UITheme.GetStageDisplayName`; Platform lấy từ `card.Platforms`; tháng sinh từ dữ liệu |
| Không có mốc trục cố định | `NiceCeiling` tính từ max thực tế mỗi lần vẽ |
| Không có top-N bịa | `MaxBars` chỉ là giới hạn hiển thị; UI ghi rõ "Top 8 / N" |
| Không có tháng trống bịa | Chỉ sinh tháng có ít nhất 1 Content; tháng không có dữ liệu không xuất hiện |

## 6. Thứ tự thực hiện

1. `ChartData.cs` + `ChartDataBuilder` + unit test (offline).
2. `BarChartPanel` (vẽ + trạng thái rỗng).
3. Tích hợp `ProjectReportsControl` (nút view thứ 3, panel 2×2, loading/lỗi/hủy).
4. Wiring `BoardForm`.
5. **Cổng 1** — build + unit test (theo `post-change-test-workflow`, xin phép trước khi chạy).
6. **Cổng 2** — kiểm chứng runtime theo §8 (xin phép trước khi chạy app).
7. Cập nhật `test-results.md` với kết quả thật.

## 7. Kiểm thử

### 7.1. Unit test — `ChartDataBuilderTests`

| Test | Nội dung |
| :--- | :--- |
| `Status_CountsAndPercents_MatchInput` | 3 card ở 3 trạng thái → đúng số + %; tổng % xấp xỉ 100 |
| `Status_KeepsWorkflowOrder_AndZeroRows` | Thứ tự đúng enum; trạng thái không có vẫn xuất hiện với Count 0 |
| `Status_Empty_ReturnsZeroTotal` | Rỗng → Total 0, mọi bucket 0, không chia cho 0 |
| `Platforms_GroupsCaseInsensitive_AndCountsUntagged` | "TikTok"/"tiktok" gộp; card không Platform vào `Untagged` |
| `Platforms_SortsByCountThenName` | Thứ tự giảm dần, hoà theo tên |
| `Timeline_UsesPlannedThenDeadline_AndSortsMonths` | Mốc ưu tiên Planned; tháng tăng dần; không sinh tháng trống |
| `Timeline_CountsNoDate` | Card không có cả hai mốc vào `NoDate` |
| `Performance_SortsByViews_AndCapsTopEight` | 10 dòng → 8 dòng, `ContentTotal=10`; hoà theo tên |
| `Performance_Empty_ReturnsEmptyLists` | Không có Metrics → list rỗng, không lỗi |
| `NiceCeiling_RoundsTo125` | 0→1, 87→100, 1200→2000, 4.5k→5000 |
| `Builder_NullInput_Throws` | Tham số null → `ArgumentNullException` |

### 7.2. Build

- `dotnet build CreatorFlow.slnx` → 0 error.
- `dotnet test --filter "FullyQualifiedName~ChartDataBuilder"` → pass toàn bộ test mới.

### 7.3. Kiểm tra tĩnh

- Không có `HttpClient`/SQL/đọc DB trong `BarChartPanel` và `ChartDataBuilder`.
- Không có mảng số liệu/nhãn mẫu trong control.
- Không thêm package, không sửa migration.

## 8. Kịch bản kiểm chứng runtime (Docker local)

Chuẩn bị: Docker chạy `deploy/docker/compose.dev.yml`; client trỏ `Api__BaseUrl=http://localhost:5080/`; đăng nhập tài khoản có Project đã có Content + Metrics.

| # | Thao tác | Kết quả mong đợi |
| :--- | :--- | :--- |
| 1 | Reports → **Biểu đồ** | Lưới 2×2 hiện 4 biểu đồ; không có số liệu mẫu |
| 2 | Đối chiếu biểu đồ trạng thái với Board | Tổng theo trạng thái khớp số card từng cột Kanban |
| 3 | Đối chiếu biểu đồ Platform với chi tiết Content | Số theo Platform khớp; "Chưa gắn Platform" khớp số card không có platform |
| 4 | Đối chiếu biểu đồ tháng với cột Planned/Deadline | Các tháng và số lượng khớp dữ liệu; không có tháng rỗng bịa |
| 5 | Đối chiếu Top Content / Platform với view "Hiệu suất" | Views và Engagement khớp bảng cùng màn |
| 6 | Project không có Metrics | 3 biểu đồ card vẫn hiện; ô hiệu suất báo "Chưa có dữ liệu" |
| 7 | Dừng container API rồi mở Biểu đồ | Báo lỗi tải dữ liệu rõ ràng, không crash |
| 8 | Đổi qua lại 3 view | Không tải lại vô hạn; không mất dữ liệu view cũ; nút AI vẫn hoạt động |
| 9 | Thu nhỏ cửa sổ | Biểu đồ có thanh cuộn, không đè nút/không vỡ layout |

## 9. Rủi ro và cách xử lý

| Rủi ro | Xử lý |
| :--- | :--- |
| Vẽ GDI+ lệch DPI | Dùng font/`TextRenderer` theo `UITheme`, kiểm tra ở 100% và 125% khi test tay; không tự tính pixel cố định cho chiều cao chữ |
| Nhiều Content → biểu đồ rối | Top 8 + thanh cuộn; số liệu đầy đủ vẫn ở 2 view bảng |
| Một Content nhiều Platform gây hiểu nhầm tổng | Ghi chú ngay dưới biểu đồ Platform |
| API chậm khi mở view | Tải async + trạng thái "Đang tải…"; hủy khi rời trang |
| Metrics lỗi nhưng card OK | Tách lỗi từng ô, không làm hỏng cả view |
| Trạng thái `Archived` không có trong Board | Ghi rõ nguồn biểu đồ là Board (không gồm Archived); không tự thêm dữ liệu khác nguồn |

## 10. Ngoài phạm vi

- Không lọc theo Creator cho biểu đồ (có thể làm ở task sau khi UX chốt).
- Không xuất PNG/PDF, không lưu ảnh, không tương tác drill-down/hover.
- Không biểu đồ cho System Admin/toàn hệ thống.
- Không đổi công thức Engagement, không đổi view Hiệu suất/Creator & Project/AI.
- Không thêm endpoint, migration, package.

## 11. Cổng duyệt

Theo `AGENTS.md`: **requirements → duyệt → plan (file này) → duyệt → triển khai**. Trước khi build/test/chạy app phải trình checklist theo `post-change-test-workflow` và chờ đồng ý; trong lúc test chỉ quan sát, không sửa code.

## 12. Cần bạn xác nhận trước khi code

1. View thứ 3 **"Biểu đồ"** trong màn Reports (không thêm mục sidebar) — ổn chứ?
2. Tự vẽ GDI+, không thêm thư viện chart — ổn chứ?
3. Bản này **không lọc Creator** cho biểu đồ — chấp nhận được không?
4. Top Content hiển thị **8 dòng** (có ghi "Top 8 / N") — chấp nhận được không?
