# SCRUM-80 — Thiết lập môi trường DEV/TEST chung cho CreatorFlow

Trạng thái: Đã duyệt với chỉnh sửa trực tiếp ngày 07/10/2026. Người phụ trách: Phú. Ưu tiên: High.
Nguồn yêu cầu: nội dung task được người dùng dán; ảnh Jira xác nhận ID SCRUM-80.

## Mục tiêu

Thiết lập một CreatorFlow.Api và PostgreSQL DEV/TEST chung, truy cập API qua HTTPS từ máy riêng của Phú, Phúc, Phát, Tính để integration/nghiệm thu. Mỗi thành viên tiếp tục code và test hằng ngày với API/DB local độc lập. Không triển khai Production.

Luồng LOCAL: WinForms → API local → PostgreSQL local/Docker riêng.
Luồng DEV: WinForms → HTTPS → API server DEV → PostgreSQL DEV riêng.

## Hiện trạng đã đọc từ repository

- API dùng .NET 10; đã có `src/CreatorFlow.Api/Dockerfile`, compose local/server và mẫu environment tại `deploy/docker/`; ưu tiên tái sử dụng sau khi kiểm tra phù hợp.
- `docs/team-api-docker-setup.md` mới mô tả recipe và prerequisites; chưa có bằng chứng server Internet đã deploy hoặc nghiệm thu.
- Client đã có `ApiClientConfiguration` và cấu hình `Api:BaseUrl`/`Api__BaseUrl`; không cần mặc định tạo UI chọn môi trường.
- `GET /api/health` hiện trả trạng thái API đáp ứng, không chứng minh DB/schema/SMTP sẵn sàng.
- Các file DB hiện có: `01_schema.sql`, `02_seed.sql`, `03_verify.sql`, `04_password_reset_requests.sql`, `05_auth_email_avatar.sql`, `06_auth_token_version.sql`. Hướng dẫn hiện tại xác định DB mới dùng 01 → 04 → 05 → 06; 02 là seed, 03 là verification. Phải đọc lại nội dung và trạng thái DB đích trước khi chốt thao tác migration.
- README và `src/CreatorFlow/Program.cs` cho thấy Board/Workflow còn repository/DB hoặc in-memory phía client; Auth đã dùng API. Chưa thể tuyên bố toàn bộ WinForms chỉ giữ API URL hoặc toàn bộ module dùng DB chung.
- Checkout hiện tại là `feature/SCRUM-39-auth-board-integration-fix`; có `AGENTS.md` và `docs/` chưa tracked. Không tự đổi branch, pull, commit, push hoặc sửa các tài liệu hiện có ngoài phạm vi.

## Phạm vi

1. Primary stack đã chốt: Render Free Web Service + Neon Free PostgreSQL, URL HTTPS `onrender.com`, ngân sách tuyệt đối 0 đồng. Không mua domain, không thêm payment method vào Render; hết free bandwidth/build quota chấp nhận suspend hoặc ngừng build. Không dùng Render Free Postgres, không chọn Koyeb/Oracle mới làm default. Chưa được phép provision/deploy.
2. Chuẩn hóa deployment tái sử dụng Docker hiện có: API tương ứng commit được chỉ định từ develop hoặc release đã duyệt; image/version xác định được, restart được, không build từ checkout có thay đổi không xác định rồi coi là release.
3. Chuẩn bị PostgreSQL DEV riêng, ví dụ `creatorflow_dev`, chỉ Backend truy cập; không public DB để WinForms kết nối. Tạo/update schema bằng scripts versioned phù hợp trạng thái DB, không tự migrate khi startup.
4. Secrets chỉ phía Backend/server: DB, JWT signing, SMTP, OTP/HMAC, AI và storage nếu có. Không hardcode, commit, đưa vào Contracts/client hoặc log; dùng mẫu placeholder và cấu hình ngoài Git. Không yêu cầu gửi secret trong chat.
5. Xác nhận client chuyển LOCAL ↔ DEV bằng configuration, không sửa source; URL local HTTP được phép, URL DEV Internet phải HTTPS với certificate validation bình thường. Access token theo session Auth hiện tại.
6. Giữ health liveness hiện tại; plan đề xuất readiness DB/backend dependency riêng. SMTP/AI availability không quyết định API process alive. Log startup/lỗi request/server/DB/provider đủ để debug và không lộ secret.
7. Ghi quy trình deploy, xác định version, backup/dump và restore cơ bản; release có migration phải backup, xác nhận DB DEV, apply đúng thứ tự, verify, deploy API tương ứng, rồi smoke test.
8. Dữ liệu integration qua Register → Verify email → Login và chức năng Project/Invitation/Membership thật khi sẵn sàng. Không seed identity để bypass, hardcode UserId/ProjectId hoặc sửa schema bằng tay tùy tiện.
9. Chuẩn bị checklist và ghi evidence thực tế của nghiệm thu; ghi blocked cho module chưa sẵn sàng, đặc biệt Project context/SCRUM-25. Assignment/Review/Notification/Workflow chỉ test qua API khi phiên bản deploy đã hỗ trợ.

## Ngoài phạm vi

Production, Kubernetes, HA, autoscaling, multi-region, CI/CD hoàn chỉnh, payment, monitoring enterprise; chuyển toàn bộ business module sang API; thay đổi nghiệp vụ Auth/Project/Workflow; bypass Auth; thay đổi giao diện/điều hướng ngoài cấu hình API được yêu cầu. Lỗi integration phát hiện sẽ đi qua bug-investigation-workflow trước khi sửa.

## ZERO-COST GUARDRAIL

- Mọi thao tác có khả năng tạo billing phải dừng và xin review trước khi thực hiện. Duyệt kiến trúc/plan không cho phép phát sinh chi phí; ngân sách vẫn là 0 đồng cho đến khi người dùng thay đổi rõ ràng.
- Không thêm payment method vào bất kỳ provider nào: Render, Neon, Brevo, registry hoặc dịch vụ phụ trợ. Free tier bắt buộc thẻ hoặc có khả năng auto-charge thì dừng xin review. Khi vượt quota miễn phí chấp nhận suspend/ngừng build, không nâng cấp hoặc thêm thẻ để khôi phục.
- Không dùng trial có nguy cơ auto-charge/paid conversion, paid registry, persistent disk, object storage hoặc backup service trả phí. Không bật paid add-on, auto-upgrade hay tài nguyên vượt Free allowance.
- Brevo Free SMTP port 2525 là lựa chọn ưu tiên thử, chỉ khi account/sender hoạt động hoàn toàn miễn phí và TLS hợp lệ. Verification bị blocked thì ghi BLOCKED/follow-up; không mua domain/email service, không bypass Auth.
- Backup bằng pg_dump ngoài repository, dùng máy/dung lượng sẵn có; không thuê dịch vụ backup/storage. Nếu tài nguyên sẵn có không đủ, dừng xin review thay vì mua thêm.
- Trước thao tác hạ tầng, xác nhận đúng Free plan/quota và không có khả năng charge; nếu không xác minh được, dừng xin review. Không tự chuyển sang Koyeb/Oracle hoặc provider khác.

## Giả định và khác biệt cần duyệt

- ID dùng SCRUM-80 theo ảnh Jira; branch dự kiến nếu người dùng yêu cầu tạo: `feature/SCRUM-80-shared-dev-environment`.
- Kiến trúc API trong yêu cầu hiện tại được ưu tiên hơn sơ đồ trực tiếp WinForms → Service → Repository của tài liệu cũ; README đã có hướng chuyển tiếp tương ứng.
- Không yêu cầu toàn bộ WinForms hết DB dependency. Module đã migrate, đặc biệt Auth/User, không giữ DB/server secrets. Board/Workflow legacy được ghi technical debt và tuyệt đối không kết nối PostgreSQL DEV chung từ WinForms. Không mở rộng task sang di chuyển các module này.
- Provider đã chốt Render Free + Neon Free; SMTP ưu tiên Brevo Free 2525 có điều kiện. Giới hạn miễn phí phải xác nhận lại trước thao tác tài khoản.
- Plan và guardrail đã được duyệt; đã được phép tạo branch SCRUM-80 và triển khai source/docs. Build/test, container, mail, DB, provision/deploy và Git commit/push/PR vẫn cần quyền riêng.

## Câu hỏi mở

1. Ai quản lý tài khoản Render Free/Neon Free? Dùng URL onrender.com, không mua domain.
2. Commit/branch release nào được dùng làm nguồn triển khai? Có muốn tạo branch SCRUM-80 theo quy tắc dự án sau khi duyệt plan không?
3. Đã chốt nghiệm thu hai tầng như dưới đây; chỉ cần xác định module nào API-ready ở commit deploy để ghi kết quả tầng mở rộng.
4. DEV đã có DB/dữ liệu cần giữ chưa, và team có SMTP sender dùng riêng cho DEV chưa? Chỉ cần xác nhận tình trạng, không cung cấp credential ở đây.

## Tiêu chí hoàn thành và evidence

### Tầng 1 — Bắt buộc để SCRUM-80 Done

- Có URL API DEV HTTPS hoạt động từ Internet; health pass và không fake healthy khi API dừng; ghi rõ health hiện chỉ là liveness.
- API kết nối đúng PostgreSQL DEV, không fallback; dữ liệu tồn tại qua restart DB/API/container.
- Migration mới và upgrade phù hợp được apply/verify theo thứ tự; dữ liệu cần giữ không mất; evidence xác nhận đúng DB đích.
- Release truy được commit/version và có hướng dẫn vận hành, deploy, migration, backup/restore.
- Audit Auth/User và các module đã migrate, tracked files và log không có server secrets; TLS certificate validation không bị bỏ qua. Legacy dependency được ghi technical debt; không cấp DB DEV credential cho client.
- Ít nhất hai máy chạy WinForms cùng API; Auth/User runtime thật, invalid login bị từ chối, protected endpoint không token trả 401, Profile và Logout/Login lại hoạt động. Khi DEV SMTP được cấu hình, Register/Verify và reset email thật là acceptance test. Nếu chưa cấu hình, ghi SMTP và flow phụ thuộc là BLOCKED/follow-up, không fake PASS hoặc bypass verification. Auth/User runtime thật vẫn là điều kiện bắt buộc; nếu không có tài khoản verified hợp lệ để kiểm thử, phần này còn BLOCKED.
- Chuyển LOCAL → DEV → LOCAL bằng config thành công; local vẫn độc lập.
- Restart, health, readiness và logging smoke test có evidence thật.

### Tầng 2 — Integration mở rộng khi module API-ready

- Khi module sẵn sàng: User A tạo Project/invite User B; B nhận/chấp nhận, hai máy thấy cùng dữ liệu và role đúng, không đọc Project ngoài quyền; Project context thật, không hardcode. Các flow đã merge được smoke test tương ứng.
- Project/Invitation/Board/Assignment/Review/Workflow chưa API-ready ghi BLOCKED kèm dependency; không làm SCRUM-80 bị treo khi tầng 1 hoàn thành. Không hardcode UserId/ProjectId để pass.
- Checklist bao phủ deployment/restart, health, secrets, Auth, multi-machine, Project context, switching, migration và SMTP; ghi kết quả pass/fail/blocked và bằng chứng, không coi blocked là pass.
- Không tuyên bố Done chỉ vì Docker build được, localhost chạy hoặc có server. PR merge/Jira Done chỉ thực hiện khi được yêu cầu riêng.

## Cổng kiểm chứng

Requirements → duyệt → plan → duyệt → triển khai. Trước build/test/runtime/container/API/DB verification, trình checklist theo post-change-test-workflow và chờ duyệt. Apply migration/ghi dữ liệu/deploy cần xác nhận môi trường và quyền thao tác cụ thể; giai đoạn test chỉ quan sát, không sửa code.

File này là tài liệu yêu cầu; chỉ kiểm tra tĩnh nội dung, không cần runtime test cho việc tạo tài liệu.

## Guardrail cuối cùng — đã duyệt

- Render build trực tiếp từ Git commit là đường triển khai chính. Không dùng registry riêng nếu Render build trực tiếp được; registry chỉ là fallback cuối cùng, cần review riêng kể cả registry miễn phí.
- Trước mỗi đợt deploy/test lớn, operator kiểm tra Render bandwidth/build quota và Neon compute/storage trên dashboard; ghi thời điểm, mức dùng/còn lại và reset nếu có vào deployment/test record, không ghi secrets.
- Nếu gần giới hạn free hoặc không đủ cho đợt dự kiến: dừng test không cần thiết, chờ quota/reset; với storage không reset thì chỉ tiếp tục khi có dung lượng miễn phí đủ và phương án đã review. Không upgrade hoặc tự xóa dữ liệu để lấy quota.
- Neon chỉ tạo tài nguyên DEV tối thiểu cần thiết. Không tạo thêm project/branch/database để thử nếu không cần; restore/upgrade drill ưu tiên PostgreSQL disposable local trên tài nguyên sẵn có, không dùng thêm cloud resources khi chưa review nhu cầu/quota.
- Không thao tác nào được phép làm ngân sách lớn hơn 0 đồng để đạt PASS. Điều kiện miễn phí không đáp ứng thì ghi BLOCKED, không mua workaround.
