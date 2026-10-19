# CREATORFLOW — KẾ HOẠCH DỰ ÁN

## 1. Tổng quan
**Tên đề tài:** Hệ thống hỗ trợ Creator quản lý và lập kế hoạch nội dung đa nền tảng – CreatorFlow.

### Mục tiêu
- Hỗ trợ Creator cá nhân, nhóm Creator, nhóm quản lý nội dung và agency nhỏ quản lý toàn bộ vòng đời nội dung.
- Quản lý từ ý tưởng, kịch bản, phân công, sản xuất, kiểm duyệt, lịch đăng đến theo dõi hiệu suất sau khi đăng.
- Hỗ trợ nhiều Project/Campaign.
- Hỗ trợ TikTok, YouTube, Facebook, Instagram.
- Ứng dụng AI để hỗ trợ sáng tạo, tái sử dụng và phân tích nội dung.

### Mô hình hoạt động
- User có thể tạo nhiều Project hoặc được mời vào Project của người khác.
- Role OWNER / MANAGER / CREATOR được xác định riêng theo từng Project.
- System Admin là quyền cấp hệ thống, tách khỏi quyền Project.

## 2. Vai trò
### SYSTEM ADMIN
- Quản lý tài khoản.
- Khóa/mở khóa.
- Quản lý gói dịch vụ.
- Xử lý báo cáo/vi phạm.
- Thống kê hoạt động hệ thống.

### USER
- Register / Login / Logout.
- Quản lý Profile / Password.
- Tạo nhiều Project.
- Tham gia Project qua Invitation.

### OWNER
- Có toàn bộ quyền nghiệp vụ như Manager.
- Project CRUD.
- Invite/Remove Member.
- Change Role.
- Project Settings.

### MANAGER
- Quản lý Idea / Content / Planning.
- Assign Creator.
- Quản lý Priority / Deadline / Publish Date.
- Theo dõi Progress.
- Review / Approve / Reject / Feedback.
- Theo dõi Calendar / Analytics.

### CREATOR
- Xem công việc được giao.
- Cập nhật Progress.
- Chỉnh Script / Content theo quyền.
- Attachment.
- Submit Review.
- Nhận Feedback, Revision, Resubmit.
- Theo dõi Deadline / Calendar / Performance.

## 3. Chức năng hệ thống
### Project & Team
- Project CRUD.
- Project Membership.
- Invitations.
- Role / Permission.

### Idea & Content
- Idea CRUD.
- Search / Filter Idea.
- Idea → Content.
- Content Planning.
- Script.
- Platform / Tag / Priority / Deadline.
- Planned Publish Date.
- Attachment / Reference.

### Multi-platform
- TikTok.
- YouTube.
- Facebook.
- Instagram.
- Một Content có thể dùng nhiều Platform.

### Workflow
```text
IDEA → SCRIPT → PRODUCTION → EDITING → REVIEW → READY → PUBLISHED
```

Reject:
```text
REVIEW → EDITING → Revision → Resubmit → REVIEW
```

### Assignment
- Assign Creator.
- My Tasks.
- Progress.
- Deadline.
- Filter theo Assignee / Status / Deadline.

### Review
- Submit Review.
- Approve / Reject.
- Feedback.
- Revision / Resubmit.
- Review History.
- Status History.

### Calendar
- Day / Week / Month.
- Production schedule.
- Deadline.
- Planned Publish Date.
- Filter theo Project / Creator / Platform / Status.

### Content Library / Recycling
- Kho Content.
- Kho Published Content.
- Search / Filter / Sort.
- Repurpose Content.
- Lưu liên kết Content nguồn và Content mới.

### Metrics / Analytics
- Views.
- Likes.
- Comments.
- Shares.
- Engagement.

```text
Engagement (%) = (Likes + Comments + Shares) / Views × 100
```

### Notifications
- Assignment.
- Review Submitted.
- Reject / Feedback / Approve.
- Status change.
- Deadline near.
- Overdue.

## 4. AI
### AI Creative
- Idea.
- Title.
- Outline.
- Script.

### AI Repurpose
```text
Content gốc → Chọn Platform đích → AI đề xuất → User chỉnh sửa → Tạo Content mới
```

### AI Performance Analysis
- Nhận xét hiệu suất.
- Xác định Content nổi bật.
- Đề xuất cải thiện.

### Nguyên tắc
- Không train model riêng.
- Dùng AI/LLM API.
- `AIService` chạy phía Backend.
- WinForms không gọi trực tiếp AI/LLM API; WinForms gọi `CreatorFlow.Api`.
- AI API key/secret chỉ nằm ở Backend, không đóng gói trong Client.
- User xem/chỉnh sửa trước khi lưu.
- Có xử lý timeout / quota / error.

## 5. Công nghệ
- C# / .NET.
- WinForms Desktop Client.
- ASP.NET Core Web API (`CreatorFlow.Api`).
- Shared Request/Response Contracts (`CreatorFlow.Contracts`).
- PostgreSQL.
- Npgsql ở Backend.
- HttpClient / REST giữa WinForms và Backend.
- AI/LLM API được gọi từ Backend.
- Git / GitHub.
- Jira.

> Một số đoạn cũ trong tài liệu còn ghi SQL Server. Quyết định chính thức trong phần CSDL là PostgreSQL + Npgsql.

## 6. Kiến trúc
```text
WinForms UI
    ↓
Service
    ↓
Repository Interface
    ↓
Repository Implementation
    ↓
Npgsql
    ↓
PostgreSQL
```

AI:
```text
WinForms UI
    ↓
AIService
    ↓
REST API Client
    ↓
AI/LLM API
```

## 7. Database
CreatorFlow có 22 bảng.

### Account & System
1. Users
2. Plans
3. Subscriptions
4. Reports
5. AuditLogs

### Project & Members
6. Projects
7. ProjectMembers
8. ProjectInvitations

### Idea & Content
9. Ideas
10. Contents
11. Platforms
12. ContentPlatforms
13. Tags
14. ContentTags
15. ContentAttachments
16. ContentRelations

### Workflow / Review
17. ContentAssignments
18. ContentStatusHistory
19. Reviews
20. Notifications

### Metrics & AI
21. ContentMetrics
22. AIRequests

## 8. Quy tắc dữ liệu quan trọng
- Role Project lưu ở ProjectMembers.
- System Admin là quyền hệ thống.
- Projects.OwnerId dùng lookup nhanh; Owner đồng thời phải có ProjectMembers Role = OWNER.
- Contents.Status lưu trạng thái hiện tại.
- ContentStatusHistory lưu lịch sử.
- Review không overwrite.
- Content ↔ Platform là N:N qua ContentPlatforms.
- Metrics gắn ContentPlatform.
- ContentRelations lưu quan hệ Source/Derived.
- Password chỉ lưu hash; hash/verify thực hiện phía Backend.
- SQL parameterized ở Repository phía Backend.
- Multi-step operation quan trọng dùng transaction.
- Client không giữ DB/SMTP/HMAC/AI secrets.
- Authorization thực thi ở Backend, không chỉ dựa vào UI.

## 9. Kịch bản demo
```text
Register/Login
→ Create Project
→ Invite Manager/Creator
→ Create Idea
→ AI Suggest
→ Idea → Content
→ Set Platform/Priority/Deadline
→ Assign Creator
→ My Tasks
→ SCRIPT → PRODUCTION → EDITING
→ Submit Review
→ Reject + Feedback
→ Revision + Resubmit
→ Approve
→ READY
→ PUBLISHED
→ Enter Metrics
→ Calendar
→ Analytics
→ AI Performance Analysis
→ Content Recycling / AI Repurpose
→ System Admin
```

## 10. Tiêu chí hoàn thành
### P0
- Auth/User/Profile.
- Project/Membership/Role.
- Idea/Content.
- Assignment/My Tasks.
- Workflow.
- Review/Feedback.
- PostgreSQL persistence thông qua Backend API.
- Basic System Admin.

### P1
- Calendar.
- Dashboard.
- Metrics.
- Charts/Analytics.
- Content Library.
- Search/Filter/Sort.
- Overdue.
- Recycling.
- AI Idea/Title/Script.
- Ít nhất một AI flow hoạt động thật.

## 11. Hướng phát triển
- Hoàn thiện migration các module còn lại sang kiến trúc Client–Server nếu còn debt chuyển tiếp.
- Social platform API integration.
- Automatic Metrics sync.
- Scheduled publishing.
- Cloud sync.
- Web/Mobile.
- Realtime notification.
- Advanced AI.
- Realtime collaboration.
- Export report.
- Subscription/Payment.
- SaaS cho Creator Team / Agency.
