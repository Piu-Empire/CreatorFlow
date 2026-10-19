# CREATORFLOW — QUY TẮC LÀM VIỆC

## 1. Kiến trúc
```text
WinForms UI → Service → Repository Interface → Repository Implementation → Npgsql → PostgreSQL
```

AI:
```text
WinForms UI → AIService → REST API Client → AI/LLM API
```

### Bắt buộc
- Không SQL trong Form/UserControl.
- Form chỉ xử lý UI và gọi Service.
- Service xử lý business logic, validation, permission, workflow.
- Repository truy cập PostgreSQL.
- SQL parameterized.
- Không nối user input trực tiếp vào SQL.
- Password chỉ lưu hash.
- Multi-step operation quan trọng dùng transaction.
- Permission phải kiểm tra ở Service.
- Form không gọi AI API trực tiếp.

## 2. Database
- PostgreSQL + Npgsql.
- Không cần PostGIS.
- Dùng PK/FK/UNIQUE/CHECK/NOT NULL phù hợp.
- Index cho các cột query thường xuyên.
- Không cascade delete User làm mất lịch sử.
- Role Project lưu ở ProjectMembers.
- System Admin là quyền riêng cấp hệ thống.
- Contents.Status lưu trạng thái hiện tại.
- ContentStatusHistory lưu lịch sử.
- Review không overwrite.
- ContentPlatforms cho quan hệ Content ↔ Platform.
- Metrics gắn ContentPlatform.
- ContentRelations cho Source/Derived.

## 3. Git
```text
main
  ↑
develop
  ↑
feature/*
```

- Không code trực tiếp main.
- Không code trực tiếp develop.
- Feature branch:
```text
feature/SCRUM-XX-short-name
```

## 4. Quy trình Jira → Git → PR
```text
Jira: To Do → In Progress
→ git switch develop
→ git pull origin develop
→ git switch -c feature/SCRUM-XX-short-name
→ Code / Test
→ Commit / Push
→ Pull Request feature/* → develop
→ Review / Merge
→ Jira: Done
```

## 5. Commit / PR
- Commit mô tả đúng thay đổi.
- Pull develop mới trước khi bắt đầu.
- Build + self-test trước PR.
- PR target = develop.
- Conflict do module nào gây ra thì owner module phối hợp Leader xử lý.
- Không merge code làm hỏng develop.

## 6. Shared Code
Shared:
- Model.
- Enum.
- Interface.
- Schema.
- Shared Helper.
- Shared UI Component.

Không tự ý đổi nếu ảnh hưởng module khác.

## 7. UI
- Dùng chung color, font, button, textbox, combobox, DataGridView, sidebar, dialog, spacing.
- Không tạo style riêng biệt cho từng module.
- Mỗi người làm UI module mình theo UI Guideline chung.

## 8. Permission
```text
System Level
└── System Admin

Project Level
├── OWNER
├── MANAGER
└── CREATOR
```

Permission phải kiểm tra tại Service, không chỉ ẩn button.

## 9. Workflow
```text
IDEA → SCRIPT → PRODUCTION → EDITING → REVIEW → READY → PUBLISHED
```

Reject:
```text
REVIEW → EDITING → Revision → Resubmit → REVIEW
```

Không cho chuyển trạng thái tùy ý.

## 10. Review
- Creator Submit Review.
- Owner/Manager Review.
- Approve → READY.
- Reject → EDITING.
- Reject phải có Feedback.
- Resubmit tạo Review mới.
- Không overwrite Review cũ.

## 11. AI
- AI không tự lưu thay User.
- User phải review/edit trước khi lưu.
- Form không gọi AI API trực tiếp.
- Không commit API key.
- Config nhạy cảm nằm ngoài Git.
- Có xử lý timeout/quota/error.

## 12. Test
Mỗi thành viên tự test:
- Happy path.
- Invalid input.
- Permission.
- Database persistence.
- Basic error handling.
- Build.

Tuần 5 test:
- Permission toàn hệ thống.
- Workflow end-to-end.
- Validation.
- Integration.
- UI consistency.
- Release build.

## 13. Demo Data
- Demo bằng một bộ dữ liệu xuyên suốt.
- Mỗi thành viên chuẩn bị dữ liệu và screenshot module mình.
- Dữ liệu demo nên có Project, Member, Idea, Content, Assignment, Review, Reject/Feedback/Resubmit, Published, Metrics, Analytics, AI.

## 14. Security
- Không commit password.
- Không commit API key.
- Password chỉ lưu hash.
- SQL parameterized.
- Permission kiểm tra tại Service.

## 15. Leader / Integration
Leader chịu trách nhiệm:
- Architecture.
- Shared conventions.
- Git workflow.
- Merge.
- Conflict resolution.
- Integration.
- Final build.

Leader không có nghĩa là code thay module của thành viên khác.

## 16. Definition of Done
Task chỉ Done khi:
- Code hoàn thành.
- Build thành công.
- Test chức năng chính.
- Không vi phạm architecture.
- Persistence đúng nếu có DB.
- Permission đúng nếu liên quan quyền.
- Không commit secret.
- PR đã merge vào develop.
- Jira đã cập nhật trạng thái.

## 17. Nguyên tắc cuối
> Mỗi người sở hữu module của mình từ UI đến Data/Test, nhưng CreatorFlow phải hoạt động như một hệ thống thống nhất, không phải bốn project ghép lại.
