# SCRUM-61 — Bản phát hành chuẩn bị cho develop/Render

## Candidate

- Branch `SCRUM-61-Tích-hợp-các-module-trên-develop`, baseline `646b365e2567fa5a2b802befdb53a7cfe10a9787`.
- Candidate gồm README, source/tests liên quan SCRUM-61 và tài liệu trong thư mục task này. Không đưa AGENTS.md đang sửa, `.agent/`, cấu hình local/secret hoặc các tài liệu untracked của task khác vào commit.
- Source API thay đổi duy nhất là bỏ đăng ký ITaskRepository bị trùng trong Program; không đổi API route/DTO/schema/migration. Các sửa tích hợp chủ yếu ở WinForms và API adapters.
- Build Release đã pass, 907/907 regression không DB pass, 0 fail/skipped. Giữ 5 warning được ghi trong test-results.md; chưa sửa source trong pha test/phát hành.
- PostgreSQL fixture tests chưa chạy và không được chạy trên Neon shared DEV. Nghiệm thu shared DEV dùng client mới → API Render → dữ liệu demo riêng, không reset/seed/migrate hoặc xóa dữ liệu team.

## Remote và Render đã kiểm tra trực tiếp

- GitHub develop vẫn tại `646b365`; branch SCRUM-61 chưa có trên origin tại lần đọc remote.
- Render service `creatorflow-dev-api`, ID `srv-db3q26aj9qps738l33h0`, region Singapore, Docker Free.
- Source Render là `Piu-Empire/CreatorFlow`, branch `develop`.
- Live deploy `dep-dbb0inqvcj2c73an7dag`, commit `646b365e2567fa5a2b802befdb53a7cfe10a9787`.
- Dashboard ghi các deploy gần nhất trigger **Auto-Deploy**. Vì vậy merge develop có thể kích hoạt deploy; không giả định deploy vẫn manual theo runbook cũ. Không tự đổi cấu hình auto-deploy.
- `GET https://creatorflow-dev-api.onrender.com/api/health` trả 200, `{"status":"ok"}`. Đây chỉ là liveness, chưa chứng minh persistence hoặc schema đầy đủ.
- Quota Render đã đọc: Hobby, không có thẻ thanh toán; Free Instance Hours 28.68/750, bandwidth 22 MB/5 GB, pipeline 15/500 phút; tháng hiện tại và dự kiến đều $0. Không thêm thẻ/nâng cấp.
- Neon đã được user đăng nhập thủ công. Organization hiển thị compute 3.54 CU-hrs, storage 0.07 GB, network 0 GB. Có hai project cùng tên creatorflow-dev; không suy đoán target DB từ tên. Cần đối chiếu project/endpoint và gói/quota trước ghi dữ liệu.
- Neon Billing đã xác minh Free Plan, $0/month; mức miễn phí theo project hiển thị 100 compute hours, 1 GB storage, 10 branches, autoscaling tới 2 CU. Không chọn gói mới hoặc tạo tài nguyên. Đợt kiểm thử nhỏ dự kiến một build Render khoảng 2 phút và vài chục request API; không keep-awake, không gọi AI/SMTP thật ngoài quyền được duyệt.
- Với shared DEV, target kiểm thử là API Render đã xác nhận; client không lấy connection string DB và không chọn một trong hai project Neon theo tên. DB thực tế do service Render cấu hình. Phép kiểm chứng đi qua API và tài khoản test; không chạy repository fixture trực tiếp trên Neon.
- Có thể kiểm thử client mới với API Render baseline hiện tại trước merge vì không đổi route/DTO; sau merge vẫn phải xác minh SHA mới và smoke lần cuối. Không cần deploy trước chỉ để bắt đầu kiểm thử client.

## Thứ tự phát hành/kiểm chứng

1. Chuẩn bị manifest file cụ thể, stage/review chỉ candidate SCRUM-61; bảo toàn thay đổi local ngoài phạm vi.
2. Commit/push branch SCRUM-61 và tạo PR hướng develop theo xác nhận phát hành của user; không force push hoặc commit trực tiếp develop. Attach PR vào chat.
3. Đọc kết quả CI ở head PR hiện tại; nếu fail, ghi nhận và đi theo bug-investigation-workflow, không merge hoặc sửa lẫn trong pha test.
4. Xác nhận quota/target và review diff trước merge. Merge sẽ có thể tự deploy Render; không bấm Manual Deploy trùng nếu auto-deploy đã chạy. Không bypass required checks/protection.
5. Kiểm tra Live SHA trùng commit phát hành và health/readiness; rollback tham chiếu Live baseline `646b365` nếu cần, không thay DB/schema/secret. Ghi giới hạn nếu chỉ xác minh được dashboard SHA và không có image digest.
6. Chạy WinForms build mới với Api__BaseUrl trỏ Render, user đăng nhập test thủ công; kiểm thử module trên dữ liệu demo SCRUM-61. Không thu thập token/password trong chat hoặc dùng repository trực tiếp từ client.
7. Ghi kết quả và phần chưa hoàn tất. Không coi build branch/local là build develop sau merge hoặc dùng fake tests thay persistence thật; không tự cập nhật Jira Done.

## Điều kiện dừng

Không đủ quyền repo/provider, CI fail, không chứng minh được quota miễn phí, chưa rõ DB target, thiếu tài khoản test hoặc schema thiếu: ghi bằng chứng và dừng đúng bước phụ thuộc. Không tạo tài nguyên/cloud credentials mới, chạy migration hoặc thực hiện paid workaround để vượt điều kiện này.
