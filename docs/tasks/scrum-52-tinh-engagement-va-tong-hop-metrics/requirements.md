# SCRUM-52 — Yêu cầu: Tính Engagement và tổng hợp Metrics

Trạng thái: **CHỜ DUYỆT**. Branch: `SCRUM-52-tinh-engagement-va-tổng-hợp-metrics`
(tạo từ `SCRUM-58` sau khi commit `2aee656`).
Người viết: agent. Ngày soạn: 19/10/2026.
Nguồn yêu cầu: người dùng cung cấp trực tiếp:

> Tính chỉ số dẫn xuất và tổng hợp theo Content/Creator/Platform/Project.
> AC: (1) Xử lý Views=0; (2) Kết quả từ SQL/dữ liệu thật; (3) Không nhập cùng số liệu nhiều lần.

## 1. Mục tiêu (nói thường)

Hiện tại màn Metrics mới chỉ **nhập và xem số thô** (Views, Likes, Comments, Shares) cho **từng
platform của từng Content**. Task này thêm phần **"tính toán"**:

- Tính **Engagement** (chỉ số dẫn xuất) theo công thức có sẵn của dự án:
  `Engagement (%) = (Likes + Comments + Shares) / Views × 100`.
- **Tổng hợp** Views/Likes/Comments/Shares/Engagement theo 4 góc nhìn: **theo Content**, **theo
  Creator**, **theo Platform**, **theo cả Project**.
- Số liệu phải **lấy từ dữ liệu thật trong PostgreSQL** (không bịa/mock).
- **Không đếm trùng** số liệu khi tổng hợp và **không ghi lại y nguyên** một lần nhập cũ.

## 2. Hiện trạng đọc từ repository

| Khu vực | Sự thật | Hệ quả |
| :--- | :--- | :--- |
| `docs/ux-ui/CreatorFlow_UX.md:646-649` + `docs/planning-tong/…ClientServer.md:127` | Công thức chuẩn: `(Likes + Comments + Shares) / Views × 100` | Công thức đã được chốt, không tự nghĩ khác |
| `database/01_schema.sql:259-268` | Bảng `content_metrics(metric_id, content_platform_id, views, likes, comments, shares, recorded_by, recorded_at)` | Nơi chứa số liệu thô |
| `src/CreatorFlow.Api/Repositories/Metrics/MetricRepository.cs:17-20` | Thiết kế **APPEND-SNAPSHOT**: mỗi lần nhập = 1 dòng mới; "giá trị hiện tại" = dòng mới nhất theo `recorded_at DESC, metric_id DESC` | **Chú ý:** tổng hợp mà cộng hết mọi dòng sẽ **đếm trùng** → phải lấy **snapshot mới nhất mỗi platform** |
| `MetricRepository.GetLatestAsync:48-56` | Đã có SQL `DISTINCT ON (content_platform_id) ... ORDER BY ... DESC` | Tái dùng đúng mẫu SQL này để lấy "giá trị hiện tại" |
| `src/CreatorFlow.Api/Services/Metrics/MetricService.cs` | Chỉ có: đọc theo Content, đọc theo platform, ghi snapshot. **Chưa có** tính Engagement hay tổng hợp | Phần cần thêm nằm ở đây |
| `src/CreatorFlow.Contracts/Metrics/MetricResponses.cs` | Response chỉ có Views/Likes/Comments/Shares + timestamp; **không** có trường Engagement | Cần thêm kiểu response tổng hợp |
| `src/CreatorFlow/Services/Backends/IMetricBackend.cs` | Client chỉ có `GetContentMetrics` + `RecordMetric` (theo ContentPlatform) | Client chưa có API gọi tổng hợp → cần thêm |
| `src/CreatorFlow/Controls/ContentDetailPanel.cs:693-857` | Tab Metrics: mỗi platform 1 hàng, 4 ô nhập, nút Lưu riêng; không hiển thị Engagement | Có thể thêm Engagement + tổng theo Content ngay đây |
| `src/CreatorFlow/Controls/SidebarControl.cs` | Mục nav **"Reports"** của Project hiện **chưa có hành động** (`_items.Add(new NavItem { Title = "Reports", Icon = NavIcon.Chart, Y = y });`) | Ứng viên để đặt màn "Reports/Metrics tổng hợp" |
| `docs/ux-ui/CreatorFlow_UX.md:600-619` | Màn Project Reports dự kiến có: Project Summary, Content Performance, Creator Performance, Platform Performance, Overdue Report | Định hướng UI đã có, khớp 4 chiều tổng hợp |
| `MetricService.RecordAsync` + `MetricRepository.AddSnapshotAsync:68-94` | Nhập trùng giá trị vẫn tạo **thêm một dòng snapshot giống hệt** | **AC3:** cần chặn ghi lại y nguyên |
| `src/CreatorFlow.Api/Services/Metrics/MetricRules.cs` | Quyền nhập Owner/Manager; chỉ nhập sau Published; validate ≥ 0 | Giữ nguyên |
| Local mode (`Program.cs`) | Metrics **chỉ tồn tại qua API/DB**; InMemory không có metrics | Màn tổng hợp chỉ chạy được ở **server mode có DB** |

## 3. Phạm vi (đề xuất)

### 3.1 Backend (CreatorFlow.Api) — nơi tính toán thật
1. **Thêm SQL tổng hợp** trong `MetricRepository` (hoặc repository mới `IMetricSummaryRepository`):
   - Bước 1: lấy **snapshot mới nhất mỗi ContentPlatform** (`DISTINCT ON`).
   - Bước 2: `GROUP BY` theo chiều yêu cầu và cộng `views/likes/comments/shares`.
   - Cột **Engagement** tính **ngay trong SQL**, có `CASE WHEN SUM(views)=0 THEN 0 ELSE ... END`.
2. **Thêm service/endpoint đọc tổng hợp**: ví dụ
   `GET /projects/{projectId}/metrics/summary?groupBy=content|creator|platform|project`.
   - Kiểm tra thành viên Project (như MetricService hiện có).
3. **AC3 — chống ghi trùng:** khi `RecordAsync` mà giá trị sau khi gộp **trùng y nguyên** snapshot
   mới nhất thì **không INSERT** nữa (trả về bản mới nhất hiện có). Ở tầng SQL/repository để không race.

### 3.2 Contracts
- Thêm model response tổng hợp (ví dụ `MetricsSummaryResponse` + `MetricsSummaryRow`:
  `key/name, views, likes, comments, shares, engagementPercent`).

### 3.3 Client (WinForms)
- Thêm `IMetricBackend.GetMetricsSummary(projectId, groupBy)` + `ApiMetricBackend` gọi endpoint mới.
- **Màn tổng hợp**: nối mục nav **"Reports"** của Project sang một control mới (ví dụ
  `ProjectReportsControl`) hiển thị 4 bảng/nhóm: theo Content / Creator / Platform / Project,
  kèm cột **Engagement %**. Ô **Views=0** hiển thị `0%` hoặc `—` (xem 5.3).
- (Tùy chọn) Tab Metrics trong Content Detail: thêm dòng **Engagement của Content** (tổng theo các platform).

### 3.4 Test
- Unit test phần tính Engagement (hàm thuần) cho các ca: Views=0, số bình thường, làm tròn.
- Test service/endpoint tổng hợp với repository giả (đúng ngữ nghĩa "snapshot mới nhất").
- Test AC3: nhập trùng → không tạo dòng mới.

## 4. Ngoài phạm vi
- Biểu đồ (charts) và phân tích xu hướng theo thời gian.
- AI Performance Analysis (task AI riêng).
- Đổi công thức Engagement, đổi quyền nhập, đổi luật "chỉ sau Published".
- Xuất báo cáo PDF/Excel.
- Sửa màn Admin Reports (đây là báo cáo vi phạm, khác hẳn).

## 5. Giả định và câu hỏi cần bạn chốt (giải thích tiếng thường)

0. **"Không nhập cùng số liệu nhiều lần" nghĩa là gì?** Có 2 cách hiểu, mình đề xuất **làm cả hai**:
   - (a) **Không đếm trùng khi tổng hợp** — vì mỗi lần nhập là một dòng mới, tổng hợp phải lấy
     *giá trị mới nhất* của mỗi platform, không cộng dồn mọi lần nhập. (Bắt buộc về mặt kỹ thuật.)
   - (b) **Không ghi lại y nguyên** — nếu người dùng bấm Lưu mà số y hệt lần trước thì **bỏ qua**,
     không tạo snapshot trùng.
   Bạn muốn làm cả hai, hay chỉ (a)?

1. **Màn tổng hợp đặt ở đâu?** Đề xuất: nối mục nav **"Reports"** của Project sang màn mới hiển thị
   4 chiều tổng hợp. Bạn đồng ý, hay muốn chỉ làm **backend + API** (chưa làm UI)?

2. **Engagement tính trên tổng hay trung bình?** Đề xuất **tính trên tổng nhóm**:
   `(Σ likes + Σ comments + Σ shares) / Σ views × 100` cho từng dòng tổng hợp (không lấy trung bình
   cộng của từng platform — tránh sai số). Làm tròn **2 chữ số thập phân**. Đồng ý?

3. **Views = 0 hiển thị gì?** Đề xuất trả **0** (0%) ở backend và hiển thị **"—"** ở UI để tránh
   hiểu lầm là "0% tương tác". Bạn chọn `0` hay `—`?

4. **"Creator" nhóm theo ai?** Đề xuất nhóm theo **người tạo Content** (`contents.created_by`).
   Hay bạn muốn theo **người được giao** (`content_assignments.assignee_id`)? (Nếu theo assignee
   thì 1 Content nhiều người → phải quyết cách cộng.)

5. **Khoảng thời gian:** Đề xuất lấy **giá trị mới nhất hiện tại** (không lọc theo ngày). Có cần
   lọc theo khoảng thời gian/`published_at` không?

6. **AC2 "kết quả từ SQL/dữ liệu thật":** mình dự định **tính bằng SQL trong PostgreSQL** và kiểm
   chứng bằng cách chạy **một truy vấn đối chiếu trên DB thật** rồi so với API/UI.
   - Bạn có **DB để test** sẵn không (DB dev/docker như SCRUM-80), hay lại **chưa verify được** như
     SCRUM-53? Nếu chưa có DB, mình sẽ làm xong code + test bằng repository giả và **ghi rõ AC2 chưa
     verify runtime**.

7. **Môi trường:** màn tổng hợp chỉ có số liệu khi chạy **server mode + DB** (local InMemory không có
   metrics). Xác nhận điều này để mình đặt kỳ vọng test đúng chỗ.

## 6. Tiêu chí hoàn thành (map với AC)

- [ ] **AC1 — Xử lý Views=0:** không chia cho 0; có test cho ca này; UI hiển thị rõ ràng.
- [ ] **AC2 — Kết quả từ SQL/dữ liệu thật:** Engagement + tổng hợp tính bằng SQL trên `content_metrics`;
      có đối chiếu với truy vấn thật (hoặc ghi rõ phần chưa verify nếu thiếu DB).
- [ ] **AC3 — Không nhập cùng số liệu nhiều lần:** tổng hợp dùng snapshot mới nhất mỗi platform (không
      đếm trùng) + chặn ghi lại y nguyên (nếu bạn chọn cả hai).
- [ ] **Tổng hợp đủ 4 chiều:** Content / Creator / Platform / Project.
- [ ] **Kiến trúc:** tính ở Backend/SQL; WinForms chỉ gọi API; không đặt công thức ở UI; không lộ secrets.
- [ ] **Kiến trúc/quyền:** giữ nguyên quyền Owner/Manager và luật "chỉ sau Published".
- [ ] **Test:** unit test công thức (Views=0, làm tròn) + service tổng hợp (fake repo) + AC3.
- [ ] **Build** `dotnet build CreatorFlow.slnx` 0 error; không thêm migration (bảng đã đủ cột).

## 7. Cổng kiểm chứng

Theo `AGENTS.md`: **requirements → duyệt → plan → duyệt → triển khai**. Trước build/test/runtime phải
trình checklist theo `post-change-test-workflow` và chờ duyệt; test chỉ quan sát, không sửa code.
Số liệu sai/lệch → quay lại `bug-investigation-workflow`.

File này chỉ cần kiểm tra tĩnh (chưa code), không cần runtime test.
