# SCRUM-80 — Legacy client database technical debt

Source đã đọc: `src/CreatorFlow/Program.cs`, `UseDatabase=false`, nhánh Npgsql repositories và in-memory vẫn có; README xác nhận Board/Workflow chưa migrate toàn bộ sang Backend API. Auth/User hiện qua AuthApiFacade/API. SCRUM-80 không chuyển các module này.

- Không cấp Neon/shared DEV credentials cho WinForms. Không set ConnectionStrings:CreatorFlow phía client trỏ DB DEV chung; không bật UseDatabase cho shared server.
- Legacy chỉ local riêng/in-memory theo cách hiện có. In-memory không phải persistence chung; không tạo UserId/ProjectId giả để nghiệm thu.
- Project/Invitation/Board/Assignment/Review/Workflow/Notification integration BLOCKED đến khi release API-ready. Project context phụ thuộc SCRUM-25; owner/ticket chi tiết cần team xác nhận, không tự gán Jira ID.
- Module API-ready, đặc biệt Auth/User, không giữ DB/JWT signing/SMTP/HMAC/AI server secrets. Client chỉ URL/timeout và runtime token.

Không sửa legacy repositories/Forms/model/permission/workflow trong SCRUM-80. BLOCKED tầng mở rộng không làm task treo nếu tầng bắt buộc hạ tầng/Auth có evidence thật.
