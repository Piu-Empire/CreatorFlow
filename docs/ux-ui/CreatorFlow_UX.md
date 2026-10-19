# CreatorFlow — UX Design Final
> - CreatorFlow tạo cảm giác quen thuộc với người dùng Jira.
> - Project Workspace là trung tâm.
> - Board là màn hình nghiệp vụ chính.
> - Các phần quản trị Project/Team được gom vào Settings giống GitHub.
---

# 1. UX CONCEPT TỔNG THỂ

```text
PROJECT WORKSPACE
        ↓
BOARD / BACKLOG / REPORTS
        ↓
CONTENT DETAIL PANEL
        ↓
WORKFLOW / REVIEW / METRICS / AI
```

Người dùng luôn làm việc trong một Project hiện tại.

CreatorFlow sử dụng:
- Sidebar điều hướng theo Project giống Jira.
- Board là trung tâm vận hành Content.
- Content được hiển thị như một Issue/Work Item.
- Click Content mở Detail Panel thay vì chuyển sang một trang hoàn toàn khác.
- Calendar và Reports là các View khác của cùng dữ liệu Content.
- Settings gom toàn bộ phần quản trị Project, Member, Role, Invitation, Tag, Platform, Workflow...
- System Admin là workspace độc lập với Project.

---

# 2. CẤU TRÚC ĐIỀU HƯỚNG CHÍNH

```text
CreatorFlow
────────────────────────────

Project: Creator Team ▼

PROJECT
├── Summary
├── Board
├── Backlog
├── Calendar
└── Reports

WORK
├── My Work
└── Review Queue

AI
└── AI Assistant

────────────────────────────
Project settings
└── Settings
```

## Summary (Tổng quan Project)

Hiển thị nhanh:
- Tổng số Content.
- Content đang thực hiện.
- Content đang REVIEW (Kiểm duyệt).
- Content READY (Sẵn sàng xuất bản).
- Content PUBLISHED (Đã xuất bản).
- Content Overdue (Quá hạn).
- Upcoming Deadlines (Deadline sắp tới).
- Recent Activity (Hoạt động gần đây).
- Team workload (Khối lượng công việc thành viên).
- Quick actions (Thao tác nhanh).

Summary không phải nơi thao tác nghiệp vụ chi tiết.

## Board (Bảng công việc)

Đây là màn hình chính của CreatorFlow.

```text
[ IDEA ]       [ SCRIPT ]      [ PRODUCTION ]   [ EDITING ]      [ REVIEW ]       [ READY ]

┌───────────┐  ┌───────────┐   ┌───────────┐    ┌───────────┐    ┌───────────┐    ┌───────────┐
│ TikTok A  │  │ Video B   │   │ Reel C    │    │ Video D   │    │ TikTok E  │    │ Reel F    │
│ HIGH      │  │ MEDIUM    │   │ HIGH      │    │ LOW       │    │ HIGH      │    │ READY     │
│ TV2       │  │ TV3       │   │ TV3       │    │ TV3       │    │ TV2       │    │ TV4       │
│ 28/09     │  │ 29/09     │   │ 30/09     │    │ 01/10     │    │ 01/10     │    │ 02/10     │
└───────────┘  └───────────┘   └───────────┘    └───────────┘    └───────────┘    └───────────┘
```

Mỗi card hiển thị:
- Title (Tiêu đề).
- Platform (Nền tảng).
- Priority (Mức ưu tiên).
- Assignee (Người được giao).
- Deadline (Hạn hoàn thành).
- Tags.
- Review state.
- Overdue indicator.

## Backlog (Danh sách công việc chưa triển khai)

Chứa:
- Idea chưa chuyển thành Content.
- Content chưa được ưu tiên.
- Content chưa được assign.
- Content chưa đưa vào workflow chính.

```text
BACKLOG

IDEAS
────────────────────────────────────────
IDEA-01   TikTok trend tháng 10
IDEA-02   Video hướng dẫn sản phẩm
IDEA-03   Reel ngắn chủ đề A

PLANNED CONTENT
────────────────────────────────────────
CNT-12    YouTube Tutorial
CNT-13    TikTok Short
CNT-14    Facebook Campaign
```

Action:
- Convert to Content (Chuyển thành Content).
- Set Priority (Đặt mức ưu tiên).
- Set Deadline (Đặt hạn hoàn thành).
- Assign Creator (Giao cho Creator).
- Move to Board (Đưa lên Board).

## Calendar (Lịch Content)

Calendar là một View khác của dữ liệu Content.

```text
[ Month ] [ Week ] [ Day ]

Filter:
Creator ▼
Platform ▼
Status ▼
Priority ▼
```

Click Content:
```text
Quick Preview
      ↓
Open Content Detail
```

## Reports (Báo cáo / Phân tích)

```text
Reports
├── Project Summary
├── Content Performance
├── Creator Performance
├── Platform Performance
└── Overdue Report
```

## My Work (Công việc của tôi)

Mặc định:
```text
Assignee = Current User
Status != PUBLISHED
```

Ví dụ:
```text
CNT-12   TikTok Campaign   EDITING     Due 28/09
CNT-19   YouTube Video     REVIEW      Due 29/09
CNT-22   Reel              SCRIPT      Due 30/09
```

## Review Queue (Danh sách chờ kiểm duyệt)

Dành cho Owner / Manager.

```text
Status = REVIEW (Kiểm duyệt)
```

Ví dụ:
```text
CNT-21   TikTok Campaign
Submitted by: Creator A
Submitted at: 09:32
Review No: #2

[ Open Review ]
```

## AI Assistant (Trợ lý AI)

```text
AI Assistant

[ Generate Ideas ]
[ Generate Title ]
[ Generate Outline ]
[ Generate Script ]
[ Improve Script ]
[ Repurpose Content ]
[ Analyze Performance ]
```

AI vẫn xuất hiện theo Context trong Idea, Script và Published Content.

---

# 3. BOARD UX

Workflow chuẩn:

```text
IDEA (Ý tưởng)
    ↓
SCRIPT (Kịch bản)
    ↓
PRODUCTION (Sản xuất)
    ↓
EDITING (Chỉnh sửa)
    ↓
REVIEW (Kiểm duyệt)
    ↓
READY (Sẵn sàng xuất bản)
    ↓
PUBLISHED (Đã xuất bản)
```

Không cho drag-drop tự do qua mọi cột.

Ví dụ:
```text
Current Status:
SCRIPT (Kịch bản)

Allowed next status:
PRODUCTION (Sản xuất)
```

UI ưu tiên action hợp lệ:

```text
Current Status:
EDITING (Chỉnh sửa)

[ Submit Review (Gửi kiểm duyệt) ]
```

---

# 4. CONTENT CARD

```text
┌─────────────────────────────────┐
│ CNT-021                         │
│ TikTok Launch Campaign          │
│                                 │
│ TikTok • Instagram              │
│ HIGH (Ưu tiên cao)              │
│                                 │
│ Assignee: Nguyễn A              │
│ Deadline: 30/09                 │
│                                 │
│ #marketing  #short-video        │
└─────────────────────────────────┘
```

Có thể hiển thị thêm icon:
- Attachment.
- Review.
- Feedback.
- Overdue.
- AI generated.

---

# 5. CONTENT DETAIL PANEL

Click Content mở Detail Panel bên phải giống Jira Issue Detail.

```text
BOARD                                      CONTENT DETAIL
───────────────────────────────────────────────────────────────────

[ IDEA ] [ SCRIPT ] [ EDITING ]            ┌──────────────────────┐
                                           │ CNT-021              │
                                           │ TikTok Campaign      │
                                           │                      │
                                           │ Status: EDITING      │
                                           │ Priority: HIGH       │
                                           │ Assignee: TV3        │
                                           │ Deadline: 30/09      │
                                           │                      │
                                           │ Platforms            │
                                           │ TikTok, Instagram    │
                                           │                      │
                                           │ [ Submit Review ]    │
                                           └──────────────────────┘
```

Cấu trúc:
```text
Content Detail
│
├── Basic Information
├── Description
├── Script
├── Platforms
├── Tags
├── Attachments
├── Assignment
├── Workflow
├── Review
├── Activity
└── Metrics
```

---

# 6. ACTIVITY UX

```text
ACTIVITY

10:15  Nguyễn A changed Status
       SCRIPT → PRODUCTION

13:30  Nguyễn A uploaded attachment
       script-v2.docx

15:10  Nguyễn A submitted Review #1

15:42  Manager B rejected Review #1

15:42  Feedback added:
       "Phần mở đầu cần ngắn hơn."

15:43  Status changed
       REVIEW → EDITING
```

Activity dùng để truy vết:
- Status change.
- Assignment.
- Review.
- Feedback.
- Deadline change.
- Attachment.
- Assignee change.

---

# 7. REVIEW UX

Creator:

```text
EDITING (Chỉnh sửa)
        ↓
Submit Review (Gửi kiểm duyệt)
        ↓
REVIEW (Kiểm duyệt)
```

Owner / Manager:

```text
[ APPROVE (Phê duyệt) ]
[ REJECT (Từ chối) ]
```

Nếu REJECT:

```text
Feedback (Phản hồi) *

[____________________________________]

[ REJECT (Từ chối) ]
```

Loop:

```text
REVIEW (Kiểm duyệt)
        ↓
REJECTED (Đã bị từ chối)
        ↓
EDITING (Chỉnh sửa)
        ↓
Revision (Chỉnh sửa theo phản hồi)
        ↓
Resubmit (Gửi duyệt lại)
        ↓
REVIEW (Kiểm duyệt)
```

Mỗi lần Resubmit tạo Review mới.

---

# 8. REVIEW HISTORY

```text
Review #1
────────────────────────────────
Submitted by: Creator A
Reviewer: Manager B
Status: REJECTED (Đã từ chối)

Feedback:
"Phần mở đầu cần ngắn hơn."

Review #2
────────────────────────────────
Submitted by: Creator A
Reviewer: Manager B
Status: APPROVED (Đã phê duyệt)
```

---

# 9. SETTINGS UX

Các phần quản trị Project được gom vào Settings giống GitHub.

```text
Settings
│
├── General
├── Members
├── Roles & Permissions
├── Invitations
├── Workflow
├── Platforms
├── Tags
├── Notifications
├── AI Configuration
├── Project Activity
└── Danger Zone
```

## General

```text
Project Name
Description
Owner
Status
Created At

[ Save Changes ]
```

## Members

```text
Nguyễn A
OWNER (Chủ Project)

Nguyễn B
MANAGER (Quản lý Project)
[ Change Role ]
[ Remove ]

Nguyễn C
CREATOR (Người thực hiện nội dung)
[ Change Role ]
[ Remove ]

[ Invite Member ]
```

## Roles & Permissions

```text
OWNER (Chủ Project)
- Full Manager permissions
- Manage Members
- Change Roles
- Project Settings
- Archive/Delete Project

MANAGER (Quản lý Project)
- Manage Ideas / Content
- Assign Creator
- Review Content
- View Reports

CREATOR (Người thực hiện nội dung)
- View Assigned Work
- Update Progress
- Edit Allowed Content
- Submit Review
```

## Invitations

```text
Pending | Accepted | Rejected | Expired
```

## Workflow

Hiển thị workflow chuẩn, MVP chưa cần cho user tự sửa.

## Platforms

```text
[x] TikTok
[x] YouTube
[x] Facebook
[x] Instagram
```

## Tags

Quản lý Tag dùng chung trong Project.

## Notifications

Cấu hình:
- Assigned Content.
- Review Submitted.
- Content Rejected.
- Feedback Added.
- Content Approved.
- Deadline Near.
- Content Overdue.

## AI Configuration

- Default Language.
- Default Tone.
- Default Target Platform.
- Default AI Model.

## Project Activity

Activity cấp Project, khác Activity của từng Content.

## Danger Zone

```text
Archive Project
[ Archive Project ]

Delete Project
[ Delete Project ]
```

Chỉ Owner thấy.

---

# 10. SUMMARY UX

```text
Creator Team

Content
────────────────────────────
Total       32
In Progress  8
Review       3
Ready        3
Published   16
Overdue      2

My Work
────────────────────────────
CNT-21 TikTok Campaign
CNT-25 YouTube Tutorial

Review Queue
────────────────────────────
3 items waiting

Upcoming Deadlines
────────────────────────────
28/09  CNT-21
29/09  CNT-25

Recent Activity
────────────────────────────
...
```

---

# 11. REPORTS UX

```text
Reports

[ Project Summary ]
[ Content Performance ]
[ Creator Performance ]
[ Platform Performance ]
[ Overdue Report ]
```

Nội dung:
- Views.
- Likes.
- Comments.
- Shares.
- Engagement.
- Content theo Status.
- Content theo Platform.
- Top Content.
- Creator Performance.
- Project Performance.

---

# 12. METRICS UX

Chỉ Content PUBLISHED (Đã xuất bản) mới nhập Metrics.

```text
Platform:
TikTok

Views
[________]

Likes
[________]

Comments
[________]

Shares
[________]

[ Save Snapshot ]
```

Engagement:
```text
(Likes + Comments + Shares) / Views × 100
```

---

# 13. AI UX

AI không tự thay đổi dữ liệu.

```text
User Input
    ↓
AI Generate
    ↓
Preview
    ↓
User Edit
    ↓
User Confirm
    ↓
Save
```

Repurpose:

```text
Original Content
       ↓
Select Target Platform
       ↓
Generate
       ↓
Preview / Edit
       ↓
Create New Content
       ↓
Save SOURCE → DERIVED relation
```

---

# 14. NOTIFICATION UX

Topbar:
```text
🔔 3
```

Click Notification phải mở đúng:
```text
Project
  ↓
Content
  ↓
Detail Section liên quan
```

---

# 15. ROLE-BASED UX

## OWNER (Chủ Project)

Có **toàn bộ quyền của MANAGER**, đồng thời có thêm các quyền quản trị Project:

- Idea.
- Content.
- Assign Content.
- Review.
- Calendar.
- Reports.
- Quản lý Project.
- Quản lý Members.
- Quản lý Roles.
- Project Settings.
- Archive/Delete Project.

## MANAGER (Quản lý Project)

Có thể:

- Idea.
- Content.
- Assign Content.
- Review.
- Calendar.
- Reports.

## CREATOR (Người thực hiện nội dung)

Ưu tiên:
- My Work.
- Content được giao.
- Progress.
- Script.
- Attachment.
- Submit Review.
- Feedback.
- Revision.
- Calendar.

---

# 16. SYSTEM ADMIN WORKSPACE

```text
System Admin
│
├── Dashboard
├── Users
├── Plans
├── Reports
└── Audit Logs
```

System Admin không:
- tạo Project thay khách hàng;
- Assign Content;
- Review Content;
- quản lý workflow Project.

---

# 17. EMPTY / LOADING / ERROR STATE

```text
No Content yet.

[ Create Content ]
[ Open Backlog ]
```

```text
Loading Content...
```

```text
Generating suggestions...
```

```text
Unable to load data.

[ Try Again ]
```

---

# 18. UX DEMO FLOW CUỐI KỲ

```text
Login
  ↓
Create / Select Project
  ↓
Settings > Members
  ↓
Invite Creator
  ↓
Backlog
  ↓
Create Idea
  ↓
AI Suggest Idea / Title / Script
  ↓
Convert Idea to Content
  ↓
Set Platform / Priority / Deadline
  ↓
Assign Creator
  ↓
Creator opens My Work
  ↓
SCRIPT (Kịch bản)
  ↓
PRODUCTION (Sản xuất)
  ↓
EDITING (Chỉnh sửa)
  ↓
Submit Review (Gửi kiểm duyệt)
  ↓
Review Queue
  ↓
Manager REJECT (Từ chối)
  ↓
Feedback (Phản hồi)
  ↓
Creator Revision (Chỉnh sửa theo phản hồi)
  ↓
Resubmit (Gửi duyệt lại)
  ↓
Manager APPROVE (Phê duyệt)
  ↓
READY (Sẵn sàng xuất bản)
  ↓
PUBLISHED (Đã xuất bản)
  ↓
Enter Metrics
  ↓
Reports
  ↓
AI Performance Analysis
  ↓
AI Repurpose
```

---


# 20. UX PRINCIPLES

1. Project Workspace là trung tâm.
2. Board là màn hình nghiệp vụ chính.
3. Content hoạt động giống một Issue/Work Item.
4. Content Detail mở dạng Panel để giữ Context.
5. Settings gom toàn bộ quản trị Project giống GitHub.
6. Calendar và Reports dùng cùng dữ liệu Content.
7. My Work là filter theo Current User.
8. Review Queue dành cho Owner/Manager.
9. Workflow không cho chuyển Status tùy ý.
10. UI chỉ hiện action hợp lệ.
11. Permission vẫn kiểm tra trong Service.
12. AI chỉ đề xuất, user quyết định lưu.
13. Notification mở đúng Project + Content + Context.
14. Activity phải giúp truy vết thay đổi.
15. Không biến CreatorFlow thành app CRUD nhiều menu rời rạc.

---

Mục tiêu:

> Người dùng đã quen Jira/GitHub có thể hiểu CreatorFlow nhanh mà không phải học lại một kiểu điều hướng hoàn toàn mới.
