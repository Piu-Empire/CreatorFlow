# SCRUM-29 — Merge develop để giải quyết conflict PR #38

Người dùng yêu cầu rõ: “merge develop vào SCRUM-29 và xử lý conflict local”.

## Nguồn merge

- Branch đích local: `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator`, HEAD trước merge `b8caba6` (đã chứa commit CI và commit revert trên PR).
- Nguồn: `origin/develop` tại `adcfbc7`, lấy bằng `git fetch origin`.
- Không rebase/force-push, không sửa trực tiếp develop. Giữ thay đổi local của AGENTS.md và các tài liệu ngoài task.

## Giải quyết conflict

1. `src/CreatorFlow/Forms/Board/BoardForm.cs`: giữ guard Owner/Manager của SCRUM-29, đồng thời dùng Review Queue API đã có trên develop. Chỉ báo chưa hỗ trợ khi phiên API thiếu backend; không fallback local trong trường hợp đó.
2. `tests/CreatorFlow.Api.Tests/Workflow/WorkflowServiceTests.cs`: giữ nguyên bộ test notification của develop; chuyển ba test authorization của SCRUM-29 sang `Workflow/ProjectWorkflowAuthorizationTests.cs`, giữ category ProjectAuthorization.
3. Tương thích với constructor WorkflowService và IWorkflowRepository mới: cập nhật fake phân quyền với notification fixture và `GetPendingReviewInfoAsync`; cập nhật đăng ký Service trong API authorization tests. Không sửa Service/interface production để ép tương thích với test cũ.

Đã kiểm tra tĩnh việc giữ guard deadline của SCRUM-29 sau auto-merge và kiểm tra quyền Owner/Manager trong ReviewQueueService của develop. Chưa build/test phiên bản sau merge; 316 test pass ghi trước đây chỉ áp dụng code trước merge.

## Phạm vi test sau merge — chờ duyệt

- Build: `dotnet build CreatorFlow.slnx -m:1 -p:UseSharedCompilation=false`.
- Backend/API: các nhóm đã duyệt trước đó, bổ sung toàn bộ WorkflowServiceTests (notification), ReviewQueueServiceTests và NotificationServiceTests vì đây là các luồng liên quan trực tiếp conflict.
- Client/UI: ProjectClient, ProjectShell, ApiTaskBackendTests và ContentLibraryTests để kiểm tra tương thích backend mới, đổi Project, quyền và xử lý từ chối.
- Ưu tiên fake repository, HTTP stub/TestServer; không chạy database tests, không migration hoặc API triển khai thật.
- Expected: compile thành công; giữ cả quyền SCRUM-29 và notification/Review Queue mới; không mất test từ một trong hai branch.

Theo post-change-test-workflow, chưa chạy build/test khi chưa có duyệt cho đợt kiểm chứng sau merge. Không push hoặc merge PR trong yêu cầu local này.
