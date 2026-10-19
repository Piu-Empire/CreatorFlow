# CREATORFLOW — PHÂN CÔNG & TIMELINE

## 1. Nguyên tắc
- Chia theo module end-to-end.
- Mỗi người chịu trách nhiệm UI → Service → Repository → Data → Test cho module mình.
- Không chia kiểu một người chỉ UI, một người chỉ SQL.
- Mỗi thành viên tự test trước khi PR.
- Shared Model/Enum/Interface/Schema phải thống nhất trước khi đổi.

## 2. Phân công

### PHÚ — LEADER / INTEGRATION
**Auth/User**
- Register.
- Login / Logout.
- Profile.
- Current User.
- Validation.

**Project / Permission**
- Project CRUD.
- Current Project.
- Project Membership.
- Invitation.
- Owner/Manager/Creator Permission.

**System Admin**
- Users.
- Lock/Unlock.
- Plans.
- Reports.
- Basic system stats.

**Foundation**
- MainForm.
- Navigation.
- Database design.
- Solution structure.
- Shared conventions.

**Git/Integration**
- develop.
- feature branches.
- PR / merge.
- conflict.
- final integration.
- final build.

### PHÚC — IDEA / CONTENT
- Idea Bank.
- Idea CRUD.
- Search / Filter Idea.
- Idea → Content.
- Content Planning.
- Content Detail.
- Script.
- Platform / Tag / Priority / Deadline.
- Planned Publish Date.
- Attachment / Reference.
- Content Library.
- Search / Filter / Sort.
- Content Recycling.
- Source / Derived relation.

### PHÁT — WORKFLOW / TASK / REVIEW
- Assignment.
- My Tasks.
- Progress.
- Deadline.
- Workflow.
- State transition.
- Submit Review.
- Approve / Reject.
- Feedback.
- Revision.
- Resubmit.
- Review History.
- Status History.
- Overdue Warning.
- Workflow Notifications.

### TÍNH — CALENDAR / ANALYTICS / AI
- Content Calendar.
- Day / Week / Month.
- Production / Deadline / Publish schedule.
- Metrics.
- Dashboard.
- Charts.
- Statistics.
- Creator Performance.
- Platform Performance.
- Project Performance.
- AIService.
- AI Idea / Title / Script.
- AI Repurpose.
- AI Performance Analysis.

## 3. Công việc chung
- Thống nhất UI.
- Thống nhất Model/Enum dùng chung.
- Mỗi người tự test module.
- Demo Data.
- Screenshot chức năng.
- Báo thay đổi Schema/Interface.
- Integration bug fixing.

## 4. Timeline 5 tuần

### TUẦN 1 — FOUNDATION
**Phú**
- Solution.
- Git.
- Source structure.
- PostgreSQL foundation.
- Login/User/Project/Role cơ bản.

**Phúc**
- Thiết kế Idea/Content.
- UI khung Idea/Content.

**Phát**
- Thiết kế Workflow.
- Assignment.
- Review.

**Tính**
- Thiết kế Calendar.
- Metrics foundation.
- AIService.

**Milestone:** Project base chạy được, kết nối PostgreSQL và đăng nhập được.

### TUẦN 2 — CORE MANAGEMENT
**Phú**
- Project Member.
- Invitation.
- Permission.
- Basic System Admin.

**Phúc**
- Idea Bank.
- Content Planning.
- Multi-platform.

**Phát**
- Assignment.
- My Tasks.
- Deadline.
- Progress.

**Tính**
- Basic Calendar.
- Metrics Model/UI.

**Milestone:** Các module chính kết nối PostgreSQL.

### TUẦN 3 — WORKFLOW & REVIEW
**Phú**
- Integration.
- Permission Owner/Manager/Creator.

**Phúc**
- Content Library.
- Search/Filter.
- Recycling.

**Phát**
- Workflow.
- Submit Review.
- Approve/Reject.
- Feedback.

**Tính**
- Calendar dùng dữ liệu thật.
- Basic Dashboard.

**Milestone:** Create Content → Assign → Production → Review chạy end-to-end.

### TUẦN 4 — ANALYTICS & AI
**Phú**
- Hoàn thiện System Admin.
- Integration.

**Phúc**
- Hoàn thiện Content/Library/Recycling.

**Phát**
- History.
- Notification.
- Overdue Warning.

**Tính**
- Charts.
- AI Idea/Script.
- AI Repurpose.
- AI Analysis.

**Milestone:** Calendar + Dashboard + AI demo được.

### TUẦN 5 — QA & RELEASE
Công việc chung:
- Merge.
- Permission Testing.
- Workflow Testing.
- Validation.
- Error Handling.
- Bug Fixing.
- UI Polish.
- Demo Data.
- Report.
- Slides.
- Demo Script.
- Release Build.

## 5. Tiêu chí hoàn thành module
- Build thành công.
- Test luồng chính.
- Test input sai cơ bản.
- Test permission liên quan.
- Không tự sửa shared schema/model mà chưa báo nhóm.
- Có demo data nếu cần.
- Có screenshot phục vụ báo cáo.
