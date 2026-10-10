# CREATORFLOW — KẾ HOẠCH TRIỂN KHAI SYSTEM ADMIN QUẢN LÝ TÀI KHOẢN

## 1. Thông Tin Chung
* **Tên tính năng:** System Admin — Quản trị người dùng & Khóa/Mở khóa tài khoản.
* **Mức độ ưu tiên:** P0 (Tiêu chí cốt lõi trong [CreatorFlow_KeHoachDuAn_ClientServer.md](../planning-tong/CreatorFlow_KeHoachDuAn_ClientServer.md)).
* **Người phụ trách:** Phú (Leader / Integration / Auth / System Admin theo [CreatorFlow_PhanCong_Timeline.md](../project-management/CreatorFlow_PhanCong_Timeline.md)).
* **Mô hình kiến trúc:** Client–Server (WinForms Desktop Client $\rightarrow$ REST API $\rightarrow$ PostgreSQL).

---

## 2. Căn Cứ Tài Liệu Hệ Thống
1. **[CreatorFlow_UX.md](../ux-ui/CreatorFlow_UX.md):**
   * *Mục 16 (System Admin Workspace):* System Admin là workspace độc lập với Project. System Admin không can thiệp nghiệp vụ Project của khách hàng (không assign content, không review content, không quản lý workflow).
   * *Mục 20 (UX Principles):* Thống nhất trải nghiệm Jira/GitHub, không tạo giao diện CRUD rời rạc.
2. **[CreatorFlow_KeHoachDuAn_ClientServer.md](../planning-tong/CreatorFlow_KeHoachDuAn_ClientServer.md):**
   * *Mục 2 (Vai trò):* System Admin có quyền cấp hệ thống (`is_system_admin = TRUE`), tách biệt với vai trò trong Project (`OWNER`/`MANAGER`/`CREATOR`).
   * *Mục 8 & 10 (Quy tắc dữ liệu & Tiêu chí P0):* Authorization thực thi ở Backend; bảo mật mật khẩu; kiểm tra quyền trong Service; Basic System Admin là tiêu chí P0 bắt buộc.
3. **[CreatorFlow_QuyTacLamViec.md](../working-agreements/CreatorFlow_QuyTacLamViec.md):**
   * *Mục 1 (Kiến trúc):* `WinForms UI → ApiClient / Service → Backend API → Repository → PostgreSQL`. Không viết SQL trong Form/UserControl. Form chỉ xử lý UI và gọi Service.
   * *Mục 8 (Permission):* Quyền kiểm tra nghiêm ngặt tại Backend Service, không chỉ dựa vào việc ẩn/hiện nút trên UI.
   * *Mục 14 & 16 (Security & Definition of Done):* SQL parameterized, không nối chuỗi input; nghiệm thu đầy đủ test cases trước khi merge.

---

## 3. Mục Tiêu & Acceptance Criteria (AC)

### 3.1. AC1: Chỉ System Admin được phép truy cập chức năng quản trị người dùng
* **Backend API — hai lớp bảo vệ:**
  * **Authorization Policy:** đăng ký policy `SystemAdminOnly` kiểm tra `CurrentAuthenticatedUser.User.IsSystemAdmin`. Do JWT hiện tại không chứa claim `is_system_admin` (theo `JwtTokenIssuer.cs`), không sử dụng `[Authorize(Roles = "...")]`. Quyền được xác định từ thông tin user đã nạp từ DB trong `OnTokenValidated`. Áp policy ở **cấp group** — `routes.MapGroup("/api/admin").RequireAuthorization("SystemAdminOnly")` — để mọi endpoint hiện tại và tương lai trong group tự kế thừa, loại bỏ rủi ro "thêm endpoint mới mà quên gắn policy".
  * **Service-level authorization:** `AdminUserService` kiểm tra lại quyền trước khi thực thi. Nếu caller không phải admin → trả về `403` theo đúng quy ước của repo: `AuthOperationResult.Fail("forbidden", message, status: 403)` rồi map qua helper `Problem()` thành `Results.Problem` (tiền lệ: `AuthService.cs:50`, `TaskService.Forbidden`). ⚠️ **Không ném exception để báo 403** — `ApiExceptionHandler` chỉ map exception → `500`/`503`, ném exception sẽ thành `500`, sai hợp đồng với client.
* **HTTP status code:**
  * Chưa xác thực hoặc token không hợp lệ (sai chữ ký, hết hạn, `token_version` lệch, tài khoản `!= ACTIVE`): `401 Unauthorized` (do JwtBearer + `OnTokenValidated`).
  * Đã xác thực nhưng không có quyền System Admin: `403 Forbidden` (do policy hoặc service).
  * Lưu ý kiểm thử: `UseStatusCodePages` đứng trước AuthN trong pipeline nên response 403 từ policy (empty body) sẽ ở dạng text thuần, không phải JSON `{code, title}` — test TC-01 chỉ assert **status code**, không assert body.
* **WinForms:** chỉ hiển thị mục "Quản trị người dùng" khi `UserSession.CurrentUser.IsSystemAdmin == true`. Dùng `UserSession` làm nguồn session chuẩn; không dùng legacy `CurrentSession` để gate quyền. Ẩn menu chỉ phục vụ UX, không thay thế authorization ở backend.
* **Đã xác minh trong code (`Program.cs:52-65`, `JwtAuthenticationRegistration.cs:53`):** thứ tự middleware đúng (`UseExceptionHandler` → `UseAuthentication` → `UseAuthorization` → Map endpoints); `AddAuthorization()` đã được đăng ký sẵn nên chỉ cần thêm policy + group, không sửa pipeline.

### 3.2. AC2: Khóa/mở khóa có hiệu lực với đăng nhập
* **Tiền đề bắt buộc (xem Bước 0):** migration `database/06_auth_token_version.sql` (cột `token_version` + trigger `tr_users_auth_token_version`) **phải** được apply lên DB đang chạy — file này ghi rõ `Not applied by the app`. Nếu thiếu, việc khóa tài khoản vẫn đổi `account_status` (login mới vẫn bị chặn) nhưng **phiên đang chạy sẽ không bị đá văng** — AC2 coi như hỏng một nửa mà không báo lỗi. Do đó Bước 0 là điều kiện nghiệm thu.
* **Hiệu lực tức thì với phiên đang chạy:** Khi admin khóa tài khoản (`account_status = 'LOCKED'`), trigger PostgreSQL `tr_users_auth_token_version` tự động tăng `token_version`. Ngay tại request kế tiếp của user bị khóa, middleware xác thực (`JwtAuthenticationRegistration.cs`) phát hiện token version lệch và trạng thái `!= ACTIVE` $\rightarrow$ Hủy phiên ngay lập tức (`401 Unauthorized`). Client tái dùng `AuthApiFacade.HandleUnauthorized` đã có sẵn để clear session và đá về màn hình login.
* **Hiệu lực với lần đăng nhập mới:** Khi tài khoản bị khóa gọi API `/api/auth/login`, API kiểm tra `user.AccountStatus != Active` và từ chối với mã lỗi `403 Forbidden` (`account_ineligible`). Khi được mở khóa về `ACTIVE`, người dùng đăng nhập lại bình thường. Lưu ý: trạng thái `DISABLED` cũng bị chặn login theo cùng cơ chế (code hiện tại chặn mọi `!= Active`), nên màn hình admin phải hiển thị và lọc được cả `DISABLED` (xem AC3).
* **Quy tắc an toàn (Safety Rule):**
  * Không cho phép System Admin tự khóa chính tài khoản mình đang đăng nhập (`targetUserId != currentAdminId` → `400`).
  * Khóa một System Admin khác thì **cho phép** (đề phòng admin rogue) nhưng dialog xác nhận phải hiện cảnh báo đỏ, và hành động này bắt buộc ghi audit (xem AC4).
  * Không được khóa admin cuối cùng còn hoạt động: nếu target là System Admin và sau thao tác không còn admin nào ở trạng thái `ACTIVE` → từ chối `400` (`last_active_admin`). Kiểm tra trong cùng transaction với `SELECT ... FOR UPDATE` trên các dòng admin — hai admin khóa chéo lẫn nhau đồng thời sẽ phải xếp hàng, không thể cùng lọt (race condition). Validate ở WinForms không đủ, bắt buộc thực thi ở backend.
  * PATCH lặp lại phải idempotent: target đã ở đúng trạng thái yêu cầu → trả `200` với message "không thay đổi", bỏ qua `UPDATE` và không ghi audit (tránh làm nhiễu log kiểm toán).
* **Không có refresh token trong hệ thống** (đã grep toàn repo — phiên duy nhất là JWT với `ExpiresAtUtc`): `token_version` bao phủ toàn bộ phiên, không có đường gia hạn nào lọt ngoài. Nếu sau này thêm refresh token thì endpoint gia hạn phải check `account_status` — ghi nhận để khỏi quên.

### 3.3. AC3: Có tìm kiếm/lọc/phân trang an toàn
* **Tìm kiếm:** Hỗ trợ tìm kiếm từ khóa tương đối (`ILIKE %search%`) trên cả `email` và `display_name`. Từ khóa **phải escape** các ký tự đại diện `\%`, `\_`, `\\` trước khi đưa vào `LIKE` — nếu không, user nhập `%` sẽ match toàn bộ bảng (rò rỉ danh sách + phá ý nghĩa bộ lọc). Câu SQL dùng mệnh đề `ESCAPE '\'` tường minh (`... ILIKE '%' || @Search || '%' ESCAPE '\'`) để `\` không bị hiểu nhầm trên mọi cấu hình PostgreSQL. Giới hạn độ dài từ khóa 100 ký tự.
* **Lọc:** Bộ lọc theo các trạng thái: `Tất cả`, `Đang hoạt động (ACTIVE)`, `Đang bị khóa (LOCKED)`, `Vô hiệu hóa (DISABLED)`. Giá trị `status` không thuộc 4 giá trị này → trả `400 Bad Request` (không để lọt xuống SQL).
* **Phân trang:** `limit` mặc định 20, tối đa 100 (lớn hơn tự clamp về 100); `offset` mặc định 0, phải `>= 0` và tối đa 10_000 (vượt quá → `400`). Giá trị âm hoặc không phải số → `400`. Lý do: tránh `limit=1000000` (DoS nhẹ), `offset=-1` (nổ lỗi DB thành 500) và `offset` cực lớn (full scan nặng). Phân trang cursor là quá đà cho bảng user quản trị — cap số là đủ.
* **Nguyên tắc cột trả về:** query danh sách tuyệt đối không `SELECT password_hash` (dù đã BCrypt). Response chỉ chứa đúng các trường trong `AdminUserSummaryResponse`.

### 3.4. AC4 (mới): Mọi hành động khóa/mở đều để lại dấu vết kiểm toán
* Bảng `audit_logs` đã tồn tại (`actor_user_id, action, entity_type, entity_id, description`) nhưng chưa ai ghi. Mỗi lần PATCH status thành công phải insert một dòng trong **cùng transaction** với `UPDATE users`: `action = 'USER_LOCK' / 'USER_UNLOCK'`, `entity_type = 'users'`, `entity_id = targetUserId`, `actor_user_id` = id admin đang gọi lấy từ `CurrentAuthenticatedUser` (tuyệt đối không lấy từ body/query — client gửi gì cũng bỏ qua), `description` ghi email target. Nếu insert audit thất bại → rollback cả việc đổi trạng thái (không cho "làm mà không ghi").
* `description` ghi theo định dạng cấu trúc để điều tra được: `"{LOCK|UNLOCK} <email> (id=<targetId>) <OLD>→<NEW> trace=<TraceIdentifier>"` — gồm trạng thái trước/sau và mã tương quan request (trùng `traceId` trong ProblemDetails). `created_at` của bảng đã là `TIMESTAMPTZ` nên mốc UTC có sẵn, không cần thêm cột mới.
* Tính bất biến: không code nào được `UPDATE`/`DELETE` `audit_logs`. Ở môi trường deploy, thu hồi quyền sửa/xóa của DB role mà app dùng (`REVOKE UPDATE, DELETE ON audit_logs FROM <app_role>`) — ghi vào checklist deploy, không phải code.
* Hành vi bị từ chối (403 policy/service, tự khóa `400`, `last_active_admin`) không cần ghi `audit_logs` — middleware sẵn có (`Program.cs:53-59`) đã log mọi response `>= 400` kèm `TraceId` ở mức Warning, đủ để phát hiện dò quyền. Không log thêm trong service để tránh trùng lặp.

### 3.5. AC5 (mới): JWT không được chạy trên kênh trần ở production
* JWT của admin được gửi tường minh ở mỗi call — nếu kênh là `http://`, kẻ nghe lén lấy được token là qua mặt cả 2 lớp phòng thủ. Quy định: `BaseUrl` production bắt buộc `https://` (hiện tại `appsettings.Local.json` đã trỏ `https://creatorflow-dev-api.onrender.com/` — giữ nguyên); `http://` chỉ cho phép khi host là `localhost`/`127.0.0.1` (dev).
* Client tự bảo vệ mình: `ApiClientConfiguration` validate — scheme `http` + host không phải local → ném `InvalidOperationException` ngay khi khởi động, không đợi tới lúc gọi API mới lỗi.

---

## 4. Kiến Trúc Luồng Hoạt Động (Data Flow)

```text
[System Admin WinForms]
        │
        │ 1. Mở "Quản trị người dùng" (Nhập từ khóa / Chọn lọc trạng thái)
        ▼
[AdminUserManagementControl]
        │
        │ 2. Gọi GetUsersAsync(search, status) kèm JWT
        ▼
[ApiClient.Admin] ──(HTTP GET /api/admin/users)──► [CreatorFlow.Api]
                                                            │
                                                            │ 3. Policy SystemAdminOnly (403 nếu không phải admin)
                                                            ▼
                                                     [AdminUserService]
                                                            │
                                                            │ 4. Validate search/status/limit/offset (400 nếu sai)
                                                            │    + escape ký tự LIKE + Query Parameterized SQL
                                                            ▼
                                                   [PostgreSQL: users]
                                                            │
                                                            │ 5. Trả về Data List
                                                            ▼
[DataGridView hiển thị danh sách người dùng + Badge trạng thái + Nút Khóa/Mở khóa]
        │
        │ 6. Admin click "Khóa tài khoản" -> Xác nhận Dialog
        ▼
[ApiClient.Admin] ──(HTTP PATCH /api/admin/users/{id}/status)──► [AdminUserService]
                                                                        │
                                                                        │ 7. Policy check (403) + Validate: target tồn tại (404),
                                                                        │    target != current (400 tự khóa), status thuộc {ACTIVE, LOCKED} (400)
                                                                        │ 8. TRANSACTION: UPDATE account_status + INSERT audit_logs (USER_LOCK)
                                                                        ▼
                                                             [PostgreSQL Trigger]
                                                          (token_version tự nhảy +1)
                                                                        │
                                                                        │ 9. Return 200 OK
                                                                        ▼
[Hiển thị Toast thành công + Refresh DataGridView + User bị khóa văng phiên tức thì]
```

---

## 5. Danh Sách Tệp Triển Khai & Phân Công Trách Nhiệm (SRP)

| Tệp | Trách nhiệm theo Single Responsibility Principle (SRP) | Trạng thái |
| :--- | :--- | :--- |
| `database/06_auth_token_version.sql` | Tiền đề AC2: cột `token_version` + trigger `tr_users_auth_token_version`. File hiện ghi `Not applied by the app` nên phải apply tay lên DB đang chạy, rồi chạy `verify_auth_schema.sql` để xác nhận. Không sửa nội dung file. | Áp dụng (chạy tay) |
| `src/CreatorFlow.Contracts/Admin/AdminUserContracts.cs` | Định nghĩa DTO Request/Response dùng chung cho API và Client. | Tạo mới |
| `src/CreatorFlow.Api/Repositories/Admin/IAdminUserRepository.cs` | Khai báo interface truy vấn dữ liệu User phục vụ quản trị. | Tạo mới |
| `src/CreatorFlow.Api/Repositories/Admin/AdminUserRepository.cs` | Cài đặt truy vấn SQL PostgreSQL parameterized (chống SQL Injection) + `INSERT audit_logs` trong cùng transaction với `UPDATE users`. | Tạo mới |
| `src/CreatorFlow.Api/Services/Admin/AdminUserService.cs` | Xử lý business logic, kiểm tra quyền admin (lớp phòng thủ thứ 2 sau policy) và quy tắc an toàn. | Tạo mới |
| `src/CreatorFlow.Api/Endpoints/AdminEndpoints.cs` | Khai báo các Minimal API endpoints `/api/admin/users`, tất cả gắn `.RequireAuthorization("SystemAdminOnly")`. | Tạo mới |
| `src/CreatorFlow.Api/Program.cs` | Đăng ký DI (`IAdminUserRepository`, `AdminUserService`), đăng ký policy `SystemAdminOnly` (đọc `CurrentAuthenticatedUser.User.IsSystemAdmin`), và `MapAdminEndpoints()`. | Chỉnh sửa |
| `src/CreatorFlow/ApiClients/ApiClient.Admin.cs` | Partial class cho ApiClient thực hiện HTTP request phía WinForms (truyền JWT tường minh theo pattern hiện có; `401` → dùng `AuthApiFacade.HandleUnauthorized` để clear session, `403` → hiện message, giữ session). | Tạo mới |
| `src/CreatorFlow/Controls/AdminUserManagementControl.cs` | UserControl hiển thị giao diện danh sách, tìm kiếm, lọc và nút thao tác. Vẽ bằng code (không Designer), theo tiền lệ `ModulePageHeaderControl`/`SidebarControl`. | Tạo mới |
| `src/CreatorFlow/Controls/AdminUserManagementControl.Designer.cs` | ~~Mã bố cục layout giao diện chuẩn Visual Studio Designer (theo tiền lệ `ProfileControl` / `ContentDetailPanel`).~~ Bỏ — control vẽ hoàn toàn bằng code, không cần file Designer. | Không làm |
| `src/CreatorFlow/Controls/SidebarControl.cs` | Bổ sung nút điều hướng Quản trị hệ thống (chỉ hiển thị cho System Admin) + event `AdminWorkspaceRequested` theo đúng pattern các event `BoardRequested`/`ProfileRequested` hiện có. | Chỉnh sửa |
| `src/CreatorFlow/Forms/Board/BoardForm.cs` | Điều phối chuyển đổi giữa màn hình Board và màn hình Quản trị hệ thống. | Chỉnh sửa |
| `src/CreatorFlow/Services/UserSession.cs` | **Không thêm gì mới** — `IsSystemAdmin` đã tồn tại (`Models/CurrentUser.cs`, copy trong `UserSession`). UI đọc `UserSession.CurrentUser.IsSystemAdmin`. Lưu ý: legacy `CurrentSession` static không có flag này, không được dùng để gate quyền. | Đọc sẵn có |
| `src/CreatorFlow/Services/AdminUserResults.cs` + `AuthApiFacade.cs` | Result records + 2 facade methods (`GetAdminUsersAsync`, `UpdateUserStatusAsync`) theo đúng pattern `GetCurrentProfileAsync` (401 clear phiên, 403 giữ phiên). | Tạo mới + chỉnh sửa |
| `src/CreatorFlow/Theme/UIIcons.cs` | Thêm icon `Shield` cho mục menu ADMIN. | Chỉnh sửa |
| `tests/CreatorFlow.Api.Tests/Services/AdminUserServiceTests.cs` (+ fake) | 13 unit test cho service: 403, tự khóa, parse status, 404, last-admin, idempotent, validate search/phân trang, clamp limit, 503. | Tạo mới |

---

## 6. Kế Hoạch 7 Bước Triển Khai Chi Tiết

### Bước 0: Apply migration `token_version` (tiền đề, không được bỏ qua)
* **Vị trí:** `database/06_auth_token_version.sql` trên DB đang chạy.
* **Nội dung:**
  * Backup DB, review file, chạy migration `06` sau `01`+`04`/`05` đúng thứ tự (file tự ghi `Not applied by the app`).
  * Chạy `database/verify_auth_schema.sql` (mục 81-92) để xác nhận cột `token_version` và trigger `tr_users_auth_token_version` đã tồn tại.
  * Kiểm tra seed: `admin@creatorflow.dev` có `is_system_admin = TRUE` (`02_seed.sql:19-23`).
* **Nghiệm thu bước:** `UPDATE users SET display_name = display_name WHERE email = '...'` làm `token_version` tăng +1 (chứng tỏ trigger sống). Nếu bước này chưa xong thì AC2 (đá văng phiên) không được coi là đạt dù code đã viết.

### Bước 1: Khai báo Contracts DTOs
* **Vị trí:** `src/CreatorFlow.Contracts/Admin/AdminUserContracts.cs`
* **Nội dung:**
  * `AdminUserSummaryResponse`: `UserId`, `Email`, `DisplayName`, `AccountStatus`, `IsSystemAdmin`, `CreatedAt`, `LastLoginAt`.
  * `AdminUsersListResponse`: `IReadOnlyList<AdminUserSummaryResponse> Items`, `int TotalCount`.
  * `UpdateUserStatusRequest`: `string Status` — endpoint dùng `switch` tường minh trên `"ACTIVE"` / `"LOCKED"` (so sánh không phân biệt hoa thường, có `Trim()`), mọi giá trị khác → `400`. **Không dùng `Enum.TryParse`** vì nó chấp nhận chuỗi số (`"1"` → `Locked`) — khác hẳn pattern `TryParse + IsDefined` ở `UserEndpoints.cs:28`, ở đây phải chặt hơn.
  * `UpdateUserStatusResponse`: `long UserId`, `string Status`, `bool TargetIsSystemAdmin`, `string Message`.

### Bước 2: Xây dựng Repository & Service tại Backend API
* **Vị trí:** `src/CreatorFlow.Api/Repositories/Admin/` và `src/CreatorFlow.Api/Services/Admin/`
* **Repository:**
  * Hàm tìm kiếm: escape `\%`, `\_`, `\\` trong từ khóa trước khi ghép vào `ILIKE`; `status` chỉ nhận `ACTIVE`/`LOCKED`/`DISABLED`/`ALL`; phân trang `LIMIT` (clamp 1-100, mặc định 20) và `OFFSET` (`>= 0`). Đếm `TotalCount` bằng query `COUNT(*)` cùng điều kiện lọc.
  * Hàm đổi trạng thái — thứ tự khóa cố định để vừa chống race vừa tránh deadlock: mở transaction → `SELECT ... FOR UPDATE` **ngay trên dòng target** (mọi request đụng cùng user xếp hàng tại đây; đọc trạng thái sau khóa nên TC-24 không thể cùng thấy `ACTIVE`; không tồn tại → lỗi 404) → nếu đã đúng trạng thái yêu cầu → return "no change", bỏ qua UPDATE/audit (idempotent) → khóa các dòng admin `SELECT user_id FROM users WHERE is_system_admin = TRUE ORDER BY user_id FOR UPDATE` (`ORDER BY` để hai transaction khóa chéo không deadlock) → đếm admin còn ACTIVE ngoại trừ target (bằng 0 → rollback + lỗi `400 last_active_admin`) → `UPDATE users SET account_status = @Status::account_status, updated_at = CURRENT_TIMESTAMP WHERE user_id = @UserId` → `INSERT INTO audit_logs(actor_user_id, action, entity_type, entity_id, description)` với `actor_user_id` lấy từ `CurrentAuthenticatedUser` (tuyệt đối không từ body/query) và description theo định dạng AC4 → commit. Audit lỗi → rollback toàn bộ.
  * `@Status` chỉ nhận giá trị đã parse từ enum C# (`AccountStatus.Active/Locked`), **không** truyền string thô từ request xuống SQL.
* **Service:**
  * Kiểm tra caller (lớp 2): `if (currentUser.User is not { IsSystemAdmin: true }) return Fail(403)`.
  * Kiểm tra an toàn: `if (targetUserId == currentUserId) return Fail(400, "Không thể tự khóa tài khoản của chính mình")`.
  * Trả kèm cờ `TargetIsSystemAdmin` trong response để client hiện cảnh báo khi khóa admin khác (quyết định cho phép/không vẫn ở server, client chỉ hiển thị).

### Bước 3: Đăng ký Endpoint API + Policy
* **Vị trí:** `src/CreatorFlow.Api/Endpoints/AdminEndpoints.cs`
* **Định nghĩa endpoints (tất cả `.RequireAuthorization("SystemAdminOnly")`):**
  * `GET /api/admin/users`: Query parameters `search` (tối đa 100 ký tự), `status` (`ALL`/`ACTIVE`/`LOCKED`/`DISABLED`), `limit`, `offset`. Giá trị sai → `400` (dùng `AuthEndpoints.ToHttp/Problem` như các endpoint hiện có).
  * `PATCH /api/admin/users/{userId:long}/status`: Request body `UpdateUserStatusRequest`. Chỉ nhận `ACTIVE`/`LOCKED` (mở/khóa); `DISABLED` không cho đổi qua màn hình này để tránh vô hiệu hóa nhầm không cứu được — nếu sau này cần, làm story riêng.
* **Cập nhật `Program.cs`:**
  * Thêm `services.AddScoped<IAdminUserRepository, AdminUserRepository>()`.
  * Thêm `services.AddScoped<AdminUserService>()`.
  * Đăng ký policy: `AddAuthorizationBuilder().AddPolicy("SystemAdminOnly", p => p.RequireAssertion(ctx => ctx ... CurrentAuthenticatedUser ... IsSystemAdmin))` (không dùng role claim vì JWT không có).
  * Gọi `app.MapAdminEndpoints()`.

### Bước 4: Viết Client API phía WinForms
* **Vị trí:** `src/CreatorFlow/ApiClients/ApiClient.Admin.cs`
* **Triển khai (theo đúng pattern `ApiClient.Auth.cs` / `ApiClient.Tasks.cs` — JWT truyền tường minh từng call):**
  * `GetAdminUsersAsync(string? search, string? status, int limit, int offset, CancellationToken token)`.
  * `UpdateUserStatusAsync(long userId, string status, CancellationToken token)`.
  * Bắt và chuyển tiếp các mã lỗi nghiệp vụ chuẩn từ API (`ApiBusinessException` đã có).
  * `401` → gọi `AuthApiFacade.HandleUnauthorized` (clear session, về login); `403` → ném lên UI hiện message, **giữ nguyên session** (đúng hành vi hiện tại của facade).

### Bước 5: Thiết kế Giao diện WinForms (`AdminUserManagementControl`)
* **Vị trí:** `src/CreatorFlow/Controls/AdminUserManagementControl.cs`
* **Bố cục giao diện theo Page Header chuẩn (style guide mục 26) + `UITheme`:**
  * **Header card:** H1 + badge pill "System Admin" + dòng mô tả.
  * **Toolbar (thứ tự cố định):** ô tìm kiếm bọc `RoundedPanel` (viền `#D4D4D4`, bo 8px) → `SegmentedControl` lọc `["Tất cả", "ACTIVE", "LOCKED", "DISABLED"]` (mục 18, thay ComboBox) → nút Làm mới (Primary).
  * **DataGridView (mục 11):** header `#FAFAFA` chữ HOA, viền ô `#E5E5E5`, dòng chẵn `#FAFAFA`, hover `#F0F3FF`; pill vẽ bằng `UITheme.DrawBadge` (mục 9) cho cột Quyền/Trạng thái/Thao tác (Ghost).
  * **States (mục 13):** empty + nút "Xóa bộ lọc" / loading / error + nút "Thử lại".
  * **Pager (mục 22):** Trước/Sau + "Đang hiện X trong tổng Y", page size 20.
  * Thành công → Toast của BoardForm (mục 16).
    * Cột dữ liệu: ID, Tên hiển thị, Email, Quyền (Badge System Admin), Trạng thái (Pill Xanh/Đỏ/Xám cho ACTIVE/LOCKED/DISABLED), Ngày đăng ký, Đăng nhập cuối.
    * Cột thao tác: pill "Khóa"/"Mở khóa"; dòng của chính mình và dòng DISABLED hiện "—" (không thao tác ở màn hình này).
  * **Hộp thoại xác nhận:** Bật `MessageBox` xác nhận hành động trước khi gửi lệnh tới API. Nếu target là System Admin khác, dialog phải có cảnh báo đỏ "Đây là tài khoản quản trị viên — hành động sẽ được ghi audit".

### Bước 6: Tích hợp vào Shell (`SidebarControl` & `BoardForm`)
* **Trong `SidebarControl.cs`:**
  * Hiển thị nhóm menu `ADMIN` với mục "Quản trị người dùng" khi `_isSystemAdmin == true`.
  * Phát sự kiện `AdminWorkspaceRequested`.
* **Trong `BoardForm.cs`:**
  * Bắt sự kiện `AdminWorkspaceRequested` $\rightarrow$ Ẩn `_pnlBoardArea`, hiển thị `AdminUserManagementControl` (tương tự như màn hình `ProfileControl`).
  * Đổi tiêu đề `TopHeader` thành "Quản trị hệ thống".
  * Khi người dùng click lại "Board" trên Sidebar $\rightarrow$ Đóng màn hình Admin và khôi phục Board làm việc.

---

## 7. Ma Trận Kịch Bản Kiểm Thử Nghiệm Thu (Test Matrix)

| Test ID | Tên kịch bản kiểm thử | Dữ liệu kiểm thử | Kết quả kỳ vọng | Acceptance Criteria |
| :---: | :--- | :--- | :--- | :---: |
| **TC-01** | User thường gọi API Admin | Token của `creator@creatorflow.dev` | API trả về HTTP `403 Forbidden`. | **AC1** |
| **TC-02** | User thường mở app WinForms | Đăng nhập tài khoản Creator/Manager | Sidebar không xuất hiện mục "Quản trị người dùng". | **AC1** |
| **TC-03** | System Admin đăng nhập | Đăng nhập `admin@creatorflow.dev` | Sidebar xuất hiện mục Quản trị; mở xem được danh sách User. | **AC1** |
| **TC-04** | Tìm kiếm theo từ khóa Email / Tên | Nhập từ khóa `"creator"` vào ô tìm kiếm | Bảng chỉ hiển thị người dùng có tên/email chứa chữ `"creator"`. | **AC3** |
| **TC-05** | Lọc theo trạng thái `LOCKED` | Chọn filter `"LOCKED"` | Bảng chỉ hiển thị người dùng đang có trạng thái bị khóa. | **AC3** |
| **TC-06** | Khóa tài khoản người dùng | Chọn user `creator`, bấm Khóa | Trạng thái đổi thành `LOCKED`, Toast báo thành công. | **AC2** |
| **TC-07** | Đăng nhập tài khoản bị khóa | Đăng nhập bằng `creator@creatorflow.dev` | Chặn đăng nhập: *"Tài khoản đã bị khóa hoặc vô hiệu hóa"*. | **AC2** |
| **TC-08** | Khóa tài khoản đang có phiên làm việc | `creator` đang giữ JWT hợp lệ, admin khóa | Request tiếp theo của `creator` bị trả về `401 Unauthorized` ngay. | **AC2** |
| **TC-09** | Mở khóa tài khoản | Bấm "Mở khóa" user đang bị khóa | Trạng thái chuyển về `ACTIVE`, user đăng nhập lại bình thường. | **AC2** |
| **TC-10** | Admin tự khóa tài khoản chính mình | Bấm nút khóa trên dòng của chính mình | Nút bị Disable hoặc hệ thống báo lỗi không được phép. | **AC2 / An toàn** |
| **TC-11** | PATCH user không tồn tại | `PATCH /api/admin/users/999999/status` | API trả về HTTP `404 Not Found`. | **Mới (lỗ hổng thiếu 404)** |
| **TC-12** | PATCH với status lạ | Body `{"status":"ABC"}`, `{"status":"1"}`, `{"status":"0"}`, `{"status":"Locked, Active"}`, `{"status":"DISABLED"}` | Tất cả trả `400 Bad Request`, DB không đổi. | **Mới (stringly-typed)** |
| **TC-13** | User DISABLED hiển thị được | Seed 1 user `DISABLED`, chọn filter `"DISABLED"` | Bảng hiện đúng user đó; user này login vẫn bị chặn `403`. | **AC3 (lỗ hổng quên DISABLED)** |
| **TC-14** | Tìm kiếm chứa ký tự `%` / `_` | Nhập `"100%"` vào ô tìm kiếm | Chỉ match email/tên chứa đúng chuỗi `"100%"`, không trả về toàn bộ bảng. | **AC3 (lỗ hổng LIKE escape)** |
| **TC-15** | Phân trang biên | `limit=1000000`, `limit=-5`, `offset=-1` | `limit` clamp về 100; giá trị âm/không phải số → `400`, không bao giờ 500. | **AC3 (lỗ hổng phân trang)** |
| **TC-16** | Audit sau khóa/mở | Khóa rồi mở 1 user, query `audit_logs` | Có 2 dòng `USER_LOCK`/`USER_UNLOCK` với đúng `actor_user_id` (admin) và `entity_id` (target). | **AC4 (lỗ hổng thiếu audit)** |
| **TC-17** | Khóa một System Admin khác | Admin A khóa admin B đang online | Dialog hiện cảnh báo đỏ; B văng phiên (`401` ở request kế tiếp); audit ghi nhận. | **AC2 + AC4** |
| **TC-18** | Tiền đề migration (chạy đầu tiên) | Kiểm tra DB: cột `token_version` + trigger `tr_users_auth_token_version` tồn tại | Tồn tại (chạy `verify_auth_schema.sql` pass). Nếu thiếu → dừng, apply `06` trước khi test tiếp. | **Bước 0** |
| **TC-19** | Token hỏng các loại | Không token / token hết hạn / sai chữ ký / sửa payload → gọi `GET /api/admin/users` | Tất cả trả `401`, request chưa chạm tới service. | **P0 (AUTH-01)** |
| **TC-20** | User thường gọi cả 2 verb | Token creator gọi `GET` và `PATCH /api/admin/*` | Cả hai trả `403`; DB không đổi; không sinh dòng audit mới. | **AC1 (AUTH-02)** |
| **TC-21** | Injection qua ô tìm kiếm | Nhập `' OR '1'='1`, `'; DROP TABLE users;--`, `%`, `_`, `\` | Kết quả đúng nghĩa đen, bảng users nguyên vẹn, không `500`. | **AC3 (SQL-01)** |
| **TC-22** | Audit thất bại → rollback | Giả lập `INSERT audit_logs` lỗi (test double) rồi PATCH | Trạng thái user giữ nguyên, API báo `500`, không "làm mà không ghi". | **AC4 (AUDIT-01)** |
| **TC-23** | Hai admin khóa chéo đồng thời | A và B cùng lúc PATCH khóa lẫn nhau | Đúng 1 người thành công, người còn lại nhận `400 last_active_admin`; hệ thống luôn còn ≥ 1 admin `ACTIVE`. | **AC2 (ADMIN-01)** |
| **TC-24** | PATCH lặp / đối nghịch đồng thời | PATCH `LOCKED` 2 lần; gửi `LOCK` + `UNLOCK` đồng thời | Lần 2 trả `200` "no change", không audit thừa; concurrent xong trạng thái cuối và audit nhất quán. | **AC2 (ADMIN-02)** |
| **TC-25** | DB chết giữa chừng → fail-closed | Ngắt DB rồi gọi API admin bằng token hợp lệ | Trả `503`, không bao giờ cho qua khi chưa check được quyền. | **P0 (AUTH-04)** |
| **TC-26** | Response không rò rỉ hash | `GET` list, inspect toàn bộ JSON | Không tồn tại field password/passwordHash ở bất kỳ item nào. | **AC3** |
| **TC-27** | Client từ chối kênh trần | Đặt `BaseUrl` = `http://` với host không phải localhost | Client ném lỗi ngay khi khởi động, không gửi bất kỳ request nào. | **AC5** |

---

## 8. Tiêu Chuẩn Hoàn Thành (Definition of Done - DoD)
* [x] Mã nguồn tuân thủ SRP, parameterized SQL, không chứa hardcoded secrets.
* [x] XML doc comment (`/// <summary>`) tiếng Việt ngắn gọn theo đúng quy ước hiện có của repo (không tự đặt chuẩn chữ mới).
* [x] Toàn bộ Solution biên dịch thành công, không errors và không warnings **mới** (API: 0/0; client chỉ còn warnings cũ có sẵn).
* [x] 13 unit test mới cho service pass, toàn bộ 111 test không-DB pass, không regression.
* [x] Review code (không cần chạy): mọi endpoint trong group `/api/admin` tự kế thừa policy — đã xác minh `AdminEndpoints.cs` dùng `MapGroup(...).RequireAuthorization("SystemAdminOnly")` (AUTH-03).
* [x] Bước 0 hoàn tất trên DB local disposable (01→04→05→06 + `verify_auth_schema.sql` = `auth_schema_verified`). E2E sống: khóa → token cũ 401 → login 403 → mở → login lại OK. ⚠️ Neon DEV chung vẫn phải verify lại trước merge.
* [x] Mỗi lần khóa/mở đều sinh đúng 1 dòng `audit_logs` — đã thấy `USER_LOCK`/`USER_UNLOCK` thật trên DB local (TC-16).
* [ ] Vượt qua toàn bộ 27/27 test case trong Ma trận kiểm thử. Đã xong: unit (validate, parse, 403/404/400/503) + sống API local (TC-06,07,08,09,16). Còn lại: click UI tay (TC-02,03,04,05,10,13,17), đồng thời (TC-23,24), DB chết (TC-25), client-https (TC-27).
* [ ] Khôi phục sau test local theo mục 10 (xóa user fake + audit test + container + file tạm), ghi evidence vào PR.

**Hardening P2 — ngoài phạm vi merge, làm khi có thời gian (ghi nhận, không chặn nghiệm thu):**
* Rate limit cho group `/api/admin` (middleware `RateLimiter` của ASP.NET — hiện chưa có).
* Rate limit + chống brute-force cho `/api/auth/login` (quan trọng hơn cả rate limit group admin).
* `SslMode=Require` cho connection string — nâng lên **P1 nếu DB không nằm cùng máy với API** (đường truyền app ↔ DB hiện chưa mã hóa).
* `REVOKE UPDATE, DELETE ON audit_logs` đối với DB role của app ở môi trường deploy.
* Backlog: step-up authentication (bắt nhập lại mật khẩu) khi khóa admin khác — P0 hiện chấp nhận dialog cảnh báo đỏ + audit.

---

## 9. Giải Thích Bằng Tiếng Người — Sẽ Thêm Gì Và Thêm Như Thế Nào?

> Đoạn này viết cho người không cần đọc code vẫn hiểu: mỗi "món" mới sinh ra để làm gì, đặt ở đâu, và tại sao không thể thiếu.

**Bối cảnh một câu:** hiện tại app đã có đăng nhập và phân biệt admin/thường trong DB, nhưng chưa có màn hình nào cho admin quản lý user, chưa có API nào cho việc đó, và vài "cửa hậu" đang mở. Kế hoạch này thêm đúng 3 tầng (DB → API → màn hình) cộng các "khóa cửa" (policy, audit, HTTPS), tổng cộng 5 AC và 27 test case.

**1. Bước 0 — Bật sẵn công tắc đá văng phiên (migration `06`).**
Hãy tưởng tượng mỗi user có một "con số phiên bản tem" (`token_version`) dán trong DB, và bản photo của con số đó nằm trong JWT họ đang cầm. Mỗi lần đổi mật khẩu/khóa tài khoản, DB tự động tăng con số lên (nhờ trigger). Lần gọi API tiếp theo, server so hai con số: lệch nhau nghĩa là "tem đã cũ" → đá ra ngoài. Vấn đề: migration này chưa được app tự chạy, phải apply tay một lần. Nếu quên, khóa tài khoản xong user vẫn ngồi trong app ngon lành cho tới khi token hết hạn — nửa tính năng coi như hỏng mà không ai báo. Nên đây là việc đầu tiên, làm trước cả viết code.

**2. Policy `SystemAdminOnly` — người gác cổng ở cửa API.**
Hai endpoint mới (`GET` danh sách, `PATCH` khóa/mở) là cửa nhạy cảm nhất hệ thống: ai gọi được là xem/khóa được toàn bộ user. Policy là "người gác cổng" đứng ngay ở cửa: check cờ admin rồi mới cho vào, user thường nhận `403` ngay, chưa chạm tới logic bên trong. Vì JWT hiện không mang cờ admin nên gác cổng phải hỏi DB (qua `CurrentAuthenticatedUser` mà middleware đã nạp sẵn). Cổng này dựng ở cấp cả khu phố (`/api/admin`) chứ không phải từng nhà — endpoint mới xây sau tự nằm trong, khỏi lo quên. Trong service còn check lại lần nữa — như nhà có cả cổng ngoài lẫn khóa cửa trong, đề phòng hôm nào dev quên khóa cổng.

**3. `AdminUserRepository` — người thủ kho, chỉ làm việc với SQL.**
Mọi câu SQL của tính năng này gom vào một chỗ: tìm user (có phân trang, có lọc), đổi trạng thái, ghi audit. Viết SQL theo kiểu parameterized (tham số hóa) nghĩa là câu lệnh và dữ liệu đi hai đường riêng — hacker có nhập `' OR 1=1 --` vào ô tìm kiếm thì nó cũng chỉ là chuỗi vô hại, không bao giờ thành lệnh SQL. Thêm ba mẹo nhỏ: escape ký tự `%`/`_` kèm mệnh đề `ESCAPE` tường minh (không thì nhập `%` là moi ra cả bảng user), chặn `limit` quá lớn và `offset` quá sâu (không thì một request bắt DB cày cả bảng), và tuyệt đối không `SELECT` cột mật khẩu dù nó đã hash.

**4. `AdminUserService` — ông quản lý ra quyết định.**
Repository chỉ biết đọc/ghi, còn đúng/sai do service quyết: "có phải admin không?", "có đang tự khóa mình không?", "khóa người này thì còn admin nào không?" (cấm khóa admin cuối cùng — hai admin khóa chéo nhau thì đúng một người lọt), "user này có tồn tại không?", "status này có hợp lệ không?" (so chuỗi tường minh, nhập `"1"` cũng bị từ chối). Khi đổi trạng thái, service khóa dòng target trước rồi mới đọc — như giữ chặt tờ giấy rồi mới đọc nội dung, hai người giật cùng lúc không thể cùng thấy một chữ. "Ai làm" thì server tự lấy từ phiên đăng nhập, không tin lời client khai. Tách ra như vậy để sau này đổi luật chỉ sửa một chỗ, không đụng tới SQL hay giao diện.

**5. `audit_logs` — camera an ninh.**
Mỗi lần khóa/mở, ngoài việc đổi trạng thái còn phải ghi một dòng "ai làm, làm gì, với ai, đổi từ gì sang gì, mã vụ việc là gì" vào bảng `audit_logs` đã có sẵn, trong cùng một transaction (một giao dịch): ghi camera hỏng thì coi như việc chưa làm, rollback hết. Không có camera, mai mốt user kêu "tài khoản tôi ai khóa?" thì cả team chỉ biết nhìn nhau. Còn mấy vụ "đột nhập bất thành" (bị 403, tự khóa hụt) thì hệ thống đã tự ghi vào log ứng dụng kèm mã trace — đủ để phát hiện có người đang dò quyền, không cần camera thứ hai. Về lâu dài, két chứa băng camera (bảng audit) sẽ bị thu hồi chìa sửa/xóa của tài khoản app — chỉ được ghi thêm, không được sửa quá khứ.

**6. `ApiClient.Admin` + `AdminUserManagementControl` — tay sai và mặt tiền.**
`ApiClient.Admin` là "tay sai" của app WinForms: cầm JWT đi gọi API, dịch lỗi server thành câu tiếng Việt hiện cho admin xem. Gặp `401` (phiên chết) thì tự clear session về login; gặp `403` thì chỉ báo lỗi, không logout bậy. Tay sai này còn có một luật: production bắt buộc đi đường mã hóa (`https://`), thấy địa chỉ `http://` lạ là từ chối khởi động luôn — vì token mà chạy trên đường trần thì kẻ nghe lén nhặt được là qua mặt hết mọi lớp bảo vệ. `AdminUserManagementControl` là màn hình: ô tìm kiếm, hộp lọc, bảng user, nút Khóa/Mở khóa — thiết kế theo đúng style Dark Minimalist và pattern màn hình `ProfileControl` có sẵn để app nhìn như một, không phải hai app ghép lại. Nút tự khóa mình thì disable luôn cho khỏi bấm nhầm; khóa admin khác thì dialog đỏ cảnh báo.

**7. Cái gì cố tình KHÔNG làm (để khỏi phình scope):**
không thêm claim admin vào JWT (đụng tới login của cả hệ thống — story riêng), không cho đổi sang `DISABLED` qua màn hình này (tránh vô hiệu hóa nhầm không cứu được), không phân trang cursor hay thêm cột audit mới (cap số + nhét vào `description` là đủ cho quy mô này), không tự xóa warnings cũ của repo. Ba món để dành sau merge: rate limit (ưu tiên cổng login trước), bắt nhập lại mật khẩu khi khóa admin khác, và `SslMode=Require` nếu DB không nằm cùng máy API. Hai bug ngoài lề phát hiện khi đọc code (`TaskEndpoints` chưa được `Map` trong `Program.cs` nên routes chết; `BoardRequested` subscribe trùng 2 lần) cũng để story khác, chỉ ghi chú để Phú biết.

---

## 10. Kế Hoạch Khôi Phục Sau Test Local (Bắt Buộc, Không Được Giữ Fake)

> Tài khoản fake (`admin.local@test.dev`, `creator.local@test.dev`, mật khẩu `Admin123!`) chỉ tồn tại để test màn hình admin trên **Postgres disposable local** (`creatorflow-local-pg`). Test xong phải xóa sạch. Tuyệt đối không chạy seed fake trên Neon/DEV chung, không commit file fake vào repo (file seed nằm ngoài repo tại `%TEMP%\opencode\seed_fake_local.sql`).

**Checklist khôi phục (làm ngay sau khi test xong, trước khi xin review PR):**
* [ ] Xóa 2 user fake: `DELETE FROM users WHERE email LIKE '%@test.dev';` rồi xác nhận `SELECT COUNT(*) FROM users WHERE email LIKE '%@test.dev';` = 0.
* [ ] Xóa audit sinh ra lúc test (gắn với 2 user trên): `DELETE FROM audit_logs WHERE entity_type = 'users' AND entity_id NOT IN (SELECT user_id FROM users);`
* [ ] Dừng + xóa container: `docker stop creatorflow-local-pg && docker rm creatorflow-local-pg` (data trong container mất theo — đúng ý đồ disposable).
* [ ] Xóa file tạm ngoài repo: `%TEMP%\opencode\seed_fake_local.sql`, `%TEMP%\opencode\hashgen\`, `%TEMP%\opencode\genkeys.py`.
* [ ] Xác nhận: `docker ps -a` không còn container trên; `git status` không có file fake nào lọt vào repo.
* [ ] Người thực hiện + ngày giờ xóa: ghi 1 dòng vào PR (evidence đã dọn).

**Rào chắn để fake không bao giờ lọt lên DEV:**
* File seed fake không nằm trong `database/`, không được `git add` (đã kiểm: `git status -- database/` trống).
* Mật khẩu fake (`Admin123!`) không trùng bất kỳ mật khẩu thật nào của team.
* Nếu sau này cần test lại: dựng lại container mới từ đầu (5 phút), không "giữ lại cho tiện".
