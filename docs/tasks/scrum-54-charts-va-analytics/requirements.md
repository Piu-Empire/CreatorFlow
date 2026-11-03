# SCRUM-54 — Yêu cầu: Charts và Analytics

Trạng thái: **BẢN NHÁP — ĐÃ CÓ PLAN, CHỜ DUYỆT CHUNG**. Chưa viết code.
Người viết: agent. Ngày soạn: 30/10/2026.
Branch: `SCRUM-54-charts-va-analytics` (tách từ `SCRUM-60-ai-phan-tich-hiệu-suất`).
Kế hoạch: `docs/tasks/scrum-54-charts-va-analytics/plan.md` — các đề xuất §6 được dùng làm mặc định, cần xác nhận ở §12 của plan.

Nguồn yêu cầu: người dùng cung cấp trực tiếp từ Jira (ảnh chụp SCRUM-54, thuộc epic SCRUM-11):

> **Charts và Analytics**
> Biểu đồ trạng thái, platform, thời gian và hiệu suất.
>
> Acceptance Criteria:
> - Biểu đồ có ý nghĩa nghiệp vụ
> - Không hard-code
> - Có ít nhất các góc nhìn Content và Platform

> **Giả định đang dùng:** thẻ cần làm là **SCRUM-54**. SCRUM-69 (System Admin gói dịch vụ/báo cáo) đã `Done`,
> SCRUM-60 đã có code chờ commit. Nếu bạn muốn nói thẻ khác, báo mình để đổi phạm vi trước khi duyệt.

## 1. Mục tiêu

Bổ sung **góc nhìn trực quan** cho dữ liệu Project đã có: người dùng nhìn được phân bố trạng thái, hiệu quả
theo Platform, tiến độ theo thời gian và hiệu suất Content — thay vì chỉ đọc bảng số như hiện nay.

Mọi con số trên biểu đồ phải **tính từ dữ liệu thật của Project** (Metrics đã lưu + Content đã có), không có
giá trị mẫu, không có nhãn cứng, không có mốc thời gian bịa.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả cho SCRUM-54 |
| :--- | :--- | :--- |
| `src/CreatorFlow/Controls/ProjectReportsControl.cs` | Màn Reports hiện có 2 view: **"Hiệu suất"** (bảng metrics theo chiều Content/Creator/Platform/Project) và **"Creator & Project"** (SCRUM-55), có thanh view + nút "AI phân tích" (SCRUM-60) | Điểm vào tự nhiên: thêm view thứ 3 **"Biểu đồ"**, không đổi nav sidebar |
| `IBoardBackend.GetBoardCardsAsync` → `List<ContentBoardCard>` | Card có `Status`, `Priority`, `Platforms`, `Deadline`, `PlannedPublishAt`, `Assignees`; nguồn giống Board/Calendar | Đủ dữ liệu cho biểu đồ **trạng thái**, **platform (số Content)** và **thời gian** — không cần endpoint mới |
| `IMetricBackend.GetMetricsSummary(projectId, groupBy)` (SCRUM-52) | `MetricsSummaryRow(Key, Name, Views, Likes, Comments, Shares, EngagementPercent)` với `groupBy = content\|creator\|platform\|project`; chỉ cộng trên snapshot mới nhất của mỗi ContentPlatform | Đủ dữ liệu cho biểu đồ **hiệu suất** và **Views theo Platform** — không tính lại công thức ở client |
| `src/CreatorFlow/Controls/DashboardMetrics.cs` | Đã có hàm thuần đếm Total/InProgress/WaitingReview/Ready/Published/Overdue từ card, dùng chung quy tắc Overdue với Board | Tái dùng nguyên tắc đếm, không tạo định nghĩa thứ hai |
| `src/CreatorFlow/Theme/UITheme.cs`, `UIIcons.Chart` | Có sẵn màu/typography chuẩn + icon Chart; **không có thư viện vẽ biểu đồ** | Phải tự vẽ bằng GDI+ (`OnPaint`), không thêm NuGet |
| `docs/ux-ui/CreatorFlow_UX.md` §11 | Reports có Project Summary / Content Performance / Creator Performance / Platform Performance | Vị trí và tên góc nhìn khớp tài liệu UX |
| `database/**` | Không có bảng/cột nào phục vụ riêng biểu đồ | **Không migration, không ghi dữ liệu** |

Kết luận: **không cần thay đổi API/DB**. Toàn bộ nằm ở tầng UI client + hàm tổng hợp thuần (dễ unit test).

## 3. Phạm vi đề xuất

### 3.1. Điểm vào

Thêm nút **"Biểu đồ"** vào thanh view của `ProjectReportsControl` (cạnh "Hiệu suất" / "Creator & Project").
Bấm → thay vùng bảng bằng **lưới 2×2 biểu đồ** cuộn dọc. Không thêm màn mới, không đổi sidebar/topbar.

Trạng thái "AI phân tích" (SCRUM-60) vẫn hoạt động như cũ ở view "Biểu đồ": phân tích theo `MetricsSummary`
chiều đang chọn.

### 3.2. Bốn biểu đồ (đúng 4 ý trong mô tả Jira)

| # | Biểu đồ | Góc nhìn | Nguồn dữ liệu | Cách thể hiện |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Phân bố trạng thái Content** | Content | `ContentBoardCard.Status` | Cột ngang: 7 trạng thái workflow (IDEA→PUBLISHED), số lượng + %; tô màu theo `UITheme` |
| 2 | **Content theo Platform** | Platform | `ContentBoardCard.Platforms` | Cột dọc hoặc thanh ngang: mỗi Platform bao nhiêu Content; kèm số Content chưa gắn Platform |
| 3 | **Tiến độ theo thời gian** | Content + thời gian | `ContentBoardCard.PlannedPublishAt` (thiếu thì `Deadline`) | Cột theo tháng: số Content dự kiến publish trong tháng; mốc = tháng có dữ liệu (không bịa tháng trống) |
| 4 | **Hiệu suất Content & Platform** | Content + Platform + hiệu suất | `MetricsSummary` (`groupBy=content` và `groupBy=platform`) | Hai bảng xếp hạng: Top Content theo Views (kèm Engagement %) và Views/Engagement theo Platform |

Nếu dữ liệu rỗng → mỗi biểu đồ hiện khối "Chưa có dữ liệu" thay vì trục rỗng hoặc số 0 gây hiểu nhầm.

### 3.3. "Không hard-code" — diễn giải cụ thể

- Nhãn trạng thái/platform lấy từ dữ liệu và `Enum` (không viết chuỗi danh sách cố định trong code UI).
- Mọi trục, mốc chia, thang đo tính từ giá trị lớn nhất/nhỏ nhất thực tế; không đặt sẵn "100k views".
- Tháng trên trục thời gian sinh từ dữ liệu của Project, không lấy danh sách tháng cố định.
- Màu lấy từ `UITheme` theo bảng ánh xạ trạng thái có sẵn (một chỗ duy nhất), không rải mã màu trong control.

### 3.4. Kỹ thuật

- `ChartPanel` (hoặc control nội bộ) tự vẽ bằng `Graphics` trong `OnPaint`, có double-buffer, không cấp phát
  trong vòng lặp vẽ.
- Dữ liệu cho biểu đồ gom trong một **hàm thuần** (ví dụ `ChartDataBuilder`) tách khỏi control → unit test được.
- `ProjectReportsControl` nhận thêm `IBoardBackend` để lấy card; không đọc DB trực tiếp, không gọi API mới.
- Quyền: chỉ đọc, giống Reports hiện tại; người ngoài Project vẫn bị API chặn (không xử lý ở UI).

## 4. Ngoài phạm vi

- Không thêm thư viện chart (LiveCharts/ScottPlot…) trừ khi bạn đồng ý — mặc định tự vẽ.
- Không xuất PNG/PDF, không chia sẻ biểu đồ, không lưu ảnh.
- Không thêm biểu đồ cho System Admin / toàn hệ thống (thuộc thẻ khác).
- Không đổi công thức Engagement (đã chốt ở SCRUM-52).
- Không sửa dữ liệu, không migration.

## 5. Góc nhìn quyền

| Role | Xem biểu đồ | Ghi chú |
| :--- | :--- | :--- |
| OWNER / MANAGER / CREATOR (thành viên Project) | Có | Đọc như Reports hiện tại |
| Người ngoài Project | Không | API đã chặn ở `StatisticsService` / `MetricService` |
| System Admin (không là thành viên) | Không | Không có nhánh bypass mới |

## 6. Câu hỏi mở (cần bạn chốt)

| # | Câu hỏi | Đề xuất của mình |
| :--- | :--- | :--- |
| 6.1 | Đặt biểu đồ ở đâu? | View thứ 3 **"Biểu đồ"** trong màn Reports (giữ nguyên 2 view cũ) |
| 6.2 | Vẽ chart thế nào? | Tự vẽ GDI+, **không thêm NuGet** |
| 6.3 | Biểu đồ "thời gian" dùng mốc nào? | `PlannedPublishAt`, thiếu thì `Deadline`, gom theo tháng |
| 6.4 | Có cần bộ lọc Creator cho biểu đồ không? | Có — dùng lại combo lọc Creator của view "Creator & Project" nếu rẻ, ngược lại bỏ qua ở bản này |
| 6.5 | Biểu đồ có ảnh hưởng nút "AI phân tích"? | Không đổi hành vi; ở view Biểu đồ, AI phân tích theo `groupBy=content` |
| 6.6 | Tách branch từ đâu? | `develop` để PR sạch; lưu ý SCRUM-55/60 chưa merge nên phải merge trước hoặc chấp nhận lịch sử chứa chúng |

## 7. Acceptance Criteria → cách kiểm chứng

| AC Jira | Kiểm chứng |
| :--- | :--- |
| Biểu đồ có ý nghĩa nghiệp vụ | Mỗi biểu đồ trả lời một câu hỏi quản lý cụ thể (Content đang tắc ở đâu, Platform nào kém, tháng nào dồn việc, Content nào hiệu quả) — ghi rõ trong `plan.md` và kiểm tra bằng mắt trên dữ liệu thật |
| Không hard-code | Unit test: đổi dữ liệu đầu vào → nhãn/giá trị/thang đo đổi theo; test rỗng; test 1 phần tử; grep không có danh sách nhãn cứng trong control |
| Có góc nhìn Content và Platform | Biểu đồ #1/#3 = Content, #2/#4 = Platform; checklist chụp màn hình cả 4 |

## 8. Kiểm thử dự kiến

- **Unit test (offline):** gom nhóm theo trạng thái, đếm theo platform, gom theo tháng (kể cả null ngày),
  top-N content, tính % và thang đo, dữ liệu rỗng.
- **Kiểm chứng tay:** Docker local + dữ liệu có sẵn; đối chiếu số trên biểu đồ với bảng "Hiệu suất" cùng màn.
- **Build:** `dotnet build CreatorFlow.slnx` 0 error.

## 9. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. File này chỉ kiểm tra tĩnh.
Trước khi build/chạy app phải trình checklist theo `post-change-test-workflow` và chờ duyệt.
