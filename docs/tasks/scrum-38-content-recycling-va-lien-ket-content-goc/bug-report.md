# Bug report: nullable AI client in RepurposeAiService

## Triệu chứng

`dotnet build CreatorFlow.slnx` thành công nhưng phát sinh `CS8602` tại `RepurposeAiService.cs:36`.

## Root cause

`RequestRepurposeAsync` kiểm tra property `IsConfigured` trước khi gọi `_ai.AskAsync`. Compiler không thể suy ra từ property đó rằng field `_ai` khác null. Ngoài ra, constructor cho phép truyền `isConfigured: true` cùng `_ai == null`, nên guard hiện tại cũng có thể cho phép dereference null ở runtime.

## Cách sửa

- `RequestRepurposeAsync` chụp `_ai` vào biến cục bộ, trả lỗi cấu hình nếu client null hoặc AI chưa được cấu hình, rồi chỉ gọi `AskAsync` qua biến cục bộ non-null.
- `IsConfigured` cũng phản ánh đúng trường hợp client null dù constructor nhận `isConfigured: true`.
- Thêm test hồi quy cho constructor nhận client null cùng `isConfigured: true`.

## Kết quả kiểm chứng

- `dotnet build CreatorFlow.slnx`: thành công, 0 lỗi, còn 5 warning không thuộc lỗi nullable vừa sửa (3 `CS8602` trong `ContentDetailPanel.cs`, 1 `CS0067` trong `NotificationPanelControl.cs`, 1 `CS0067` trong `SidebarControl.cs`).
- `ContentDetailServiceTests`: 81/81 pass.
- `RepurposeAi`: 17/17 pass.
- Không chạy test PostgreSQL theo yêu cầu.

## Trạng thái

Đã sửa và kiểm chứng theo phạm vi được duyệt. Không chỉnh các warning ngoài phạm vi hoặc diff có sẵn của `SidebarControl.cs`.

## UI follow-up: dialog Repurpose bị hẹp và các ô kết quả chưa bo góc

### Triệu chứng và root cause

- Dialog đặt `ClientSize` rộng 620px nhưng nội dung dùng kích thước cố định trong `Panel.AutoScroll`; khi scrollbar xuất hiện, nội dung sát mép bị cuộn ngang và các chip/copy button nhìn chật.
- Constructor lọc bỏ mọi platform đã gắn trên Content nguồn; vì CNT-048 đang dùng YouTube nên tile YouTube không được tạo.
- Các trường Tiêu đề, Nội dung và Kịch bản dùng `TextBox.BorderStyle.FixedSingle`, nên viền của chúng là hình chữ nhật dù card ngoài đã có góc bo.

### Phạm vi sửa

- Nới chiều rộng dialog và chừa khoảng an toàn cho scrollbar.
- Hiện tất cả platform trong danh sách; platform có trên Content nguồn vẫn nhìn thấy nhưng đánh dấu là nguồn và không cho chọn làm đích.
- Bọc các trường kết quả trong viền bo góc; căn lại nút copy và chip để tránh chồng lấn.
- Chỉ sửa `RepurposeDialog.cs`; giữ nguyên API, AI, lưu Content và các giao diện khác.

### Trạng thái

Đã nới form, chừa lề an toàn cho thanh cuộn, hiển thị platform nguồn ở trạng thái khóa, và bo góc các khung nhập kết quả. Build chuẩn bị lỗi copy `CreatorFlow.exe` vì file đang được một tiến trình khác sử dụng; build với `-p:UseAppHost=false` thành công, 0 lỗi (có cảnh báo retry khi copy file đang mở). Chưa xác nhận giao diện trực tiếp vì tiến trình đang giữ file chạy và môi trường không cho điều khiển cửa sổ đó. Không chạy AI hoặc lưu Content.

## UI follow-up: thanh cuộn kép, strip cấu hình và button bị chồng chữ

### Root cause

- Dialog bật `AutoScroll` cho phần nội dung trong khi các control được đặt bằng tọa độ cố định; khi tổng kích thước vượt chiều cao phần thân, WinForms tạo thanh cuộn dọc và ngang ngoài.
- Form còn hiển thị dải trạng thái kết nối/thiết lập AI ở đầu dù đây không phải thao tác cần thiết trong luồng này.
- Một số chip/copy button dùng kích thước quá thấp hoặc quá hẹp so với style button custom; chữ bị cắt/chồng lên vùng lân cận.
- Platform tile tự vẽ riêng nên không dùng chung hiệu ứng hover/pressed của `ProjectActionButton`.

### Cách sửa

- Tắt cuộn ở panel ngoài và tăng dialog lên 1000 × 980; tăng chiều cao card kết quả để các trường có vùng hiển thị thoáng hơn.
- Bỏ hoàn toàn strip kết nối/Thiết lập ở đầu form; thông báo thiếu cấu hình AI vẫn hiện tại vị trí trạng thái cạnh luồng tạo đề xuất.
- Dùng `ProjectActionButton` cho platform, chip gợi ý, sao chép và các nút footer; dành kích thước cố định đủ rộng/cao cho nhãn.
- Giữ thanh cuộn dọc riêng bên trong các ô văn bản nhiều dòng để có thể xem nội dung dài.
- Chuyển chip gợi ý ra ngoài khung nhập, thu nhỏ chúng; đặt hàng nút tạo đề xuất riêng giữa chip và phần kết quả.
- Đồng bộ chiều cao ba nút cuối trang với nút thao tác trong Content detail theo kích thước hiển thị thực tế (44px ở dialog để bù khác biệt scale DPI); thay nhãn nút sao chép bằng icon nhỏ, căn ở hàng tiêu đề và tách khỏi khung nhập.
- Tăng viền các panel, card, ô nhập và divider lên `Neutral300` để đồng nhất độ tương phản; vẽ icon sao chép bằng control custom thay vì sự kiện Paint vốn không được control nền phát ra.
- Với phản hồi về cạnh dưới khó thấy, tăng viền các khung nội dung lên `Neutral400` và vẽ inset 1px với nét 1.5px để không bị clip ở sát mép control.
- Do nét viền vẫn bị native `TextBox` làm chìm, chuyển ba ô nhập kết quả sang viền nền xám đặc 2px bao quanh mặt nhập trắng inset; cạnh dưới và bốn góc không còn phụ thuộc nét vẽ phía sau TextBox.
- Theo phản hồi giao diện, bỏ lớp nền xám đặc ở ba ô kết quả và dùng cùng cấu trúc hiển thị như ô nhập yêu cầu: `BorderPanel` trắng bo góc, TextBox không viền inset bên trong; vẫn cho phép sửa nội dung AI.
- Đồng bộ nút “Tạo đề xuất” với nút “Tạo Content mới” bằng cùng `ProjectActionButton`, style Primary, icon dấu cộng và kích thước 190 × 44px.

### Kiểm chứng lần chỉnh này

- `CoreCompile` của `CreatorFlow.csproj` thành công, không có lỗi biên dịch.
- Build toàn solution không hoàn tất bước copy output vì `CreatorFlow.dll`/`CreatorFlow.Api.exe` đang bị tiến trình khác giữ. Không dừng tiến trình đó.
- Sau chỉnh layout, target `Compile` của `CreatorFlow.csproj` chạy thành công; còn 5 warning ở `ContentDetailPanel.cs`, `SidebarControl.cs` và `NotificationPanelControl.cs`, ngoài phạm vi form này.
- Chưa chạy app để xem trực tiếp; không chạy test, không gọi AI và không lưu Content.

### Phạm vi

Chỉ thay đổi `RepurposeDialog.cs` và ghi nhận bug report này. Không sửa giao diện trang khác, API, database, hoặc luồng lưu Content.
