# CreatorFlow — Hướng dẫn cho coding agent

## Phạm vi và nguồn chuẩn

File này áp dụng cho toàn bộ repository.

Trước khi thay đổi nghiệp vụ hoặc kiến trúc, đọc các tài liệu liên quan:

- `README.md`
- `docs/CreatorFlow_KeHoachDuAn (1).md`
- `docs/CreatorFlow_QuyTacLamViec (1).md`
- `docs/CreatorFlow_UX.md`

Trước khi triển khai module mới hoặc thay đổi giao diện/điều hướng, đọc phần liên quan trong `docs/CreatorFlow_UX.md` để xác định điểm vào/ra, luồng màn hình, Project context và role-based UX. Dùng UX để xác định cách người dùng thao tác, và UI Style Guide để xác định cách trình bày; không dùng một tài liệu thay cho tài liệu còn lại. Những điều chỉnh luồng được người dùng chốt trực tiếp cho task hiện tại có ưu tiên; nêu rõ khác biệt với tài liệu và không tự mở rộng sang module khác.

Nếu tài liệu cũ còn nhắc SQL Server, quyết định chính thức là PostgreSQL + Npgsql. Yêu cầu trực tiếp của người dùng cho task hiện tại được ưu tiên; nếu yêu cầu đó mâu thuẫn với quy tắc dự án hoặc có nhiều cách hiểu, phải nêu rõ mâu thuẫn và hỏi trước khi làm.

## Nguyên tắc làm việc

Áp dụng `karpathy-guidelines` khi viết, sửa hoặc review code:

- Đọc phần liên quan và nêu giả định trước khi triển khai.
- Chọn giải pháp đơn giản nhất đáp ứng yêu cầu; không thêm tính năng hoặc abstraction chưa được yêu cầu.
- Chỉ sửa những dòng truy ngược trực tiếp được tới yêu cầu.
- Không refactor, format hoặc dọn code lân cận ngoài phạm vi.
- Giữ style hiện hữu; chỉ dọn import, biến hoặc hàm bị thừa do chính thay đổi hiện tại tạo ra.
- Xác định kết quả có thể kiểm chứng và báo trung thực phần đã hoặc chưa xác minh.

## Định tuyến workflow bắt buộc

### Task mới lớn, phức tạp hoặc chưa rõ chi tiết

Dùng `task-approval-workflow`:

1. Tạo `docs/tasks/<slug>/requirements.md`, rồi dừng chờ duyệt.
2. Sau khi requirements được duyệt, tạo `docs/tasks/<slug>/plan.md`, rồi dừng chờ duyệt.
3. Chỉ triển khai đúng plan đã duyệt. Nếu phát sinh thay đổi đáng kể ngoài plan, cập nhật plan và xin duyệt lại.

Không áp dụng quy trình này cho câu hỏi, typo hoặc thay đổi hiển nhiên rất nhỏ.

### Báo lỗi hoặc kết quả không đúng mong đợi

Dùng `bug-investigation-workflow`, kể cả khi lỗi xuất hiện sau một thay đổi vừa làm:

1. Điều tra bằng code, log và bước tái hiện có sẵn; không sửa trong lúc điều tra.
2. Chỉ hỏi thêm dữ liệu thực sự còn thiếu; không tự bịa log, input hoặc nguyên nhân.
3. Viết `docs/tasks/<slug>/bug-report.md` nếu lỗi thuộc task đang theo dõi, nếu không viết `docs/bugs/<slug>/report.md`.
4. Trình root cause, bằng chứng, cách sửa, phạm vi và rủi ro; dừng chờ duyệt trước khi sửa.

### Sau khi hoàn thành task hoặc áp dụng bug fix

Dùng `post-change-test-workflow` khi thay đổi cần kiểm chứng runtime:

1. Trình bày phần cần test, các bước test, kết quả mong đợi và khu vực rủi ro.
2. Dừng và chờ người dùng đồng ý trước khi chạy build, test, ứng dụng, playtest hoặc kiểm tra có tác động tới database/API.
3. Trong giai đoạn test chỉ quan sát và ghi nhận; tuyệt đối không sửa code.
4. Nếu phát hiện lỗi, hoàn thành phạm vi test có thể tiếp tục rồi quay lại `bug-investigation-workflow`.

Thay đổi tài liệu, typo hoặc giá trị không ảnh hưởng logic có thể chỉ cần kiểm tra tĩnh; nêu rõ vì sao không cần runtime test.

### Mã Roblox/Luau

Chỉ dùng `roblox-luau-style` khi viết, sửa hoặc review Roblox `Script`, `LocalScript`, `ModuleScript` hay file `.lua`/`.luau`. Không áp dụng các quy tắc dưới đây cho C#:

- Text hiển thị cho player phải bằng tiếng Anh.
- Comment code phải bằng tiếng Việt, đặt phía trên block quan trọng và giải thích ý định.
- Identifier dùng tiếng Anh rõ nghĩa; biến/hàm dùng camelCase, instance/class dùng PascalCase.
- Không để lại dead code hoặc debug `print()` do thay đổi hiện tại tạo ra.

## Kiến trúc bắt buộc

Luồng ứng dụng:

```text
WinForms UI → Service → Repository Interface → Repository Implementation → Npgsql → PostgreSQL
```

Luồng AI:

```text
WinForms UI → AIService → REST API Client → AI/LLM API
```

- Form/UserControl chỉ xử lý UI và gọi Service; không chứa SQL, business rule, permission rule hoặc lời gọi AI API trực tiếp.
- Service chịu trách nhiệm business logic, validation, permission và chuyển trạng thái workflow.
- Repository chịu trách nhiệm truy cập PostgreSQL; mọi SQL nhận dữ liệu động phải parameterized.
- Dùng transaction cho operation nhiều bước cần tính toàn vẹn.
- Không đưa package mới vào dự án nếu chưa giải thích nhu cầu và tác động.

## Bất biến nghiệp vụ và dữ liệu

- System Admin là quyền cấp hệ thống; OWNER, MANAGER và CREATOR là quyền theo Project lưu tại `ProjectMembers`.
- Permission phải được kiểm tra tại Service, không chỉ bằng việc ẩn hoặc vô hiệu hóa control trên UI.
- Chỉ cho phép các bước chuyển trạng thái hợp lệ trong workflow:

```text
IDEA → SCRIPT → PRODUCTION → EDITING → REVIEW → READY → PUBLISHED
REVIEW → EDITING → Revision → Resubmit → REVIEW
```

- Reject phải có feedback; resubmit tạo Review mới; không overwrite Review hoặc lịch sử cũ.
- `Contents.Status` giữ trạng thái hiện tại; `ContentStatusHistory` giữ lịch sử.
- Quan hệ Content–Platform đi qua `ContentPlatforms`; metrics gắn với `ContentPlatform`; quan hệ nội dung nguồn/phái sinh dùng `ContentRelations`.
- Owner phải đồng thời có membership với role OWNER; không cascade-delete User làm mất lịch sử.
- Password chỉ lưu hash. Không commit password, API key, connection string hoặc config nhạy cảm.
- AI không tự lưu kết quả thay user; user phải được xem và chỉnh sửa trước khi lưu. Xử lý timeout, quota và lỗi API ở lớp phù hợp.

## Shared code và UI

- Bắt buộc đọc UI Style Guide chung tại `docs/ui/creatorflow-ui-styleguide.html` trước khi bắt đầu thiết kế giao diện.
- Model, Enum, Interface, Schema, shared helper và shared UI component có thể ảnh hưởng nhiều module; không tự ý đổi ngoài phạm vi đã duyệt.
- Trước khi thiết kế, tạo, sửa hoặc review giao diện WinForms, đọc phần liên quan trong `docs/CreatorFlow_UX.md` để giữ đúng luồng và điều hướng, đồng thời đọc `docs/ui/creatorflow-ui-styleguide.html` làm chuẩn chung cho màu sắc, typography, spacing, component và layout. Đây là các tài liệu tham chiếu của team, không phải source code chạy của ứng dụng.
- Tái sử dụng color, font, controls, sidebar, dialog và spacing chung; không tạo style riêng cho từng module nếu đã có chuẩn dùng chung.
- Khi nhận thấy vấn đề ngoài phạm vi, báo lại thay vì tiện tay sửa.

## Git và hành động bên ngoài

- Không chỉnh sửa code trực tiếp trên `main` hoặc `develop`. Feature branch dùng dạng `feature/SCRUM-XX-short-name`.
- Pull request từ `feature/*` hướng vào `develop`; không làm hỏng `develop`.
- Không tự chạy `pull`, tạo branch, commit, push, tạo/merge PR, giải quyết conflict hoặc cập nhật Jira nếu người dùng chưa yêu cầu rõ.
- Không tự áp dụng migration, ghi/xóa dữ liệu hoặc thực hiện thao tác database phá hủy. Luôn xác nhận đúng môi trường và xin phép trước.

## Verification và Definition of Done

Chọn các kiểm tra liên quan tới thay đổi, không chạy máy móc toàn bộ danh sách:

- Build solution: `dotnet build CreatorFlow.slnx`.
- Happy path và invalid input.
- Permission ở cả UI và Service khi liên quan quyền.
- Database persistence và transaction khi liên quan dữ liệu.
- Basic timeout/error handling khi liên quan API hoặc I/O.
- UI consistency khi thay đổi Form/UserControl.
- Không vi phạm kiến trúc và không lộ secrets.

Trước khi chạy các kiểm tra sau thay đổi, tuân thủ cổng duyệt của `post-change-test-workflow`. Chỉ tuyên bố phần đã thực sự kiểm chứng. PR merge và Jira Done là bước bên ngoài: nếu chưa được yêu cầu hoặc chưa thể thực hiện, ghi rõ chúng còn lại thay vì tự coi là hoàn tất.
