# SCRUM-54 — Kết quả kiểm chứng

Ngày chạy: 30/10/2026. Môi trường: máy local, Windows, .NET 10.0.
Branch: `SCRUM-54-charts-va-analytics` (tách từ nhánh SCRUM-60 tại `8e6d07e`).

## Cổng 1 — kiểm tra tĩnh (ĐẠT)

| Kiểm tra | Lệnh | Kết quả |
| :--- | :--- | :--- |
| Build solution | `dotnet build CreatorFlow.slnx` | Build succeeded — 0 error; còn 2 warning có từ trước (CS0067, CS8524) |
| Test dựng dữ liệu biểu đồ | `dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj --filter "FullyQualifiedName~ChartDataBuilder"` | 16 passed, 0 failed (11 test method, 6 dòng dữ liệu cho thang đo) |

Phạm vi test mới (`ChartDataBuilderTests`):

- Trạng thái: đếm + % đúng, giữ thứ tự workflow và trạng thái 0, rỗng không chia cho 0.
- Platform: gộp tên không phân biệt hoa thường, đếm Content chưa gắn Platform, sắp giảm dần rồi theo tên.
- Thời gian: ưu tiên PlannedPublishAt rồi Deadline, tháng tăng dần, đếm Content không có mốc.
- Hiệu suất: sắp theo Views, cắt Top 8 kèm tổng, rỗng trả list rỗng.
- Thang đo: 0→1, 1→1, 87→100, 1.200→2.000, 4.500→5.000, 120.000→200.000.
- Đầu vào null ném `ArgumentNullException`.

Kiểm tra tĩnh:

- `ChartDataBuilder` thuần, không đọc DB/đồng hồ; `BarChartPanel` chỉ vẽ, không gọi backend.
- Không có mảng số liệu/nhãn mẫu trong control; nhãn trạng thái lấy từ enum + `UITheme`, Platform lấy từ card, tháng sinh từ dữ liệu.
- Không thêm package, không migration, không endpoint mới.

## Cổng 2 — kiểm tra runtime (ĐANG KIỂM THỬ)

Đã xác minh trước:

| # | Kiểm tra | Cách làm | Kết quả |
| :--- | :--- | :--- | :--- |
| A | API Board trả card cho project 1 | `GET /api/projects/1/board` bằng tài khoản demo | 200, card có `status`, `platforms`, `deadline` |
| B | API Metrics theo content/platform | `GET /api/projects/1/metrics/summary?groupBy=content\|platform` | 200, có dòng dữ liệu |
| C | Đăng nhập tài khoản demo | `POST /api/auth/login` | 200, có access token |

Các bước còn lại do người dùng bấm trên app:

1. Reports → **Biểu đồ**: lưới 2×2 hiện 4 biểu đồ, không có số liệu mẫu.
2. Đối chiếu biểu đồ trạng thái với số card từng cột Board.
3. Đối chiếu Platform + "Chưa gắn Platform" với chi tiết Content.
4. Đối chiếu các tháng với Deadline/PlannedPublishAt thực tế.
5. Đối chiếu Top Content / Platform với view "Hiệu suất" cùng màn.
6. Thu nhỏ cửa sổ: biểu đồ có thanh cuộn, không vỡ layout.
7. Dừng API rồi mở Biểu đồ: báo lỗi rõ, không crash.

Chưa kiểm chứng: DPI/độ phân giải khác, project không có Metrics, project không có Platform.
